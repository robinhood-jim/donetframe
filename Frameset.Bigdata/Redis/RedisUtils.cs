using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace Frameset.Bigdata.Redis
{
    public class RedisUtils
    {
        private IDatabase database;
        private ConnectionMultiplexer redis;
        private int dbId = 0;
        private string redisUrl;
        public RedisUtils(DataCollectionDefine define)
        {
            if (!define.ResourceConfig.TryGetValue(ResourceConstants.REDISURL, out redisUrl))
            {
                redisUrl = "localhost:6379";
            }
            if (define.ResourceConfig.TryGetValue(ResourceConstants.REDISDBID, out string dbIdStr))
            {
                dbId = Convert.ToInt32(dbIdStr);
            }
            redis = ConnectionMultiplexer.Connect(redisUrl);
            database = redis.GetDatabase(dbId);
        }
        public void SetString(string key, string value, int expireSeconds = 0)
        {
            if (expireSeconds == 0)
            {
                database.StringSet(key, value);
            }
            else
            {
                database.StringSet(key, value, TimeSpan.FromSeconds(expireSeconds));
            }
        }
        public void SetObject(string key, object target, int expireSeconds = 0)
        {
            Trace.Assert(target != null, "cached Value is null!");
            string content = JsonSerializer.Serialize(target);
            if (expireSeconds == 0)
            {
                database.StringSet(key, content);
            }
            else
            {
                database.StringSet(key, content, TimeSpan.FromSeconds(expireSeconds));
                database.StringSet(key, content, TimeSpan.FromSeconds(expireSeconds));
            }
        }
        public string GetString(string key)
        {
            RedisValue value = database.StringGet(key);
            if (value == RedisValue.Null || value == RedisValue.EmptyString)
            {
                return string.Empty;
            }
            else
            {
                return value.ToString();
            }
        }
        public V GetOject<V>(string key)
        {
            RedisValue value = database.StringGet(key);
            if (value == RedisValue.Null || value == RedisValue.EmptyString)
            {
                return default(V);
            }
            else
            {
                return JsonSerializer.Deserialize<V>(value.ToString());
            }
        }
        public void AddSet(string key, List<string> values)
        {
            database.SetAdd(key, values.Select(u => new RedisValue(u)).ToArray());
        }
        public void RemoveSet(string key, string value)
        {
            database.SetRemove(key, value);
        }
        public void Lpush(string key, string value)
        {
            database.ListLeftPush(key, [new RedisValue(value)]);
        }
        public bool KeyExists(string key)
        {
            return database.KeyExists(key);
        }
        public void AddSorted(string key, Dictionary<string, double> sortedDictionary)
        {
            List<SortedSetEntry> sortedSetEntries = [];
            foreach (var entry in sortedDictionary)
            {
                SortedSetEntry sortedSetEntry = new SortedSetEntry(entry.Key, entry.Value);
                sortedSetEntries.Add(sortedSetEntry);
            }
            database.SortedSetAdd(key, sortedSetEntries.ToArray());
        }

        public async Task<V> Rpop<V>(string key)
        {
            Type retType = typeof(V);
            RedisValue redisValue;
            while (true)
            {
                redisValue = await database.ListRightPopAsync(new RedisKey(key));
                if (redisValue.HasValue)
                {
                    break;
                }
            }
            if (redisValue != RedisValue.Null && redisValue != RedisValue.EmptyString)
            {
                if (retType.Equals(typeof(string)))
                {
                    return (V)(object)redisValue.ToString();
                }
                else
                {
                    return JsonSerializer.Deserialize<V>(redisValue.ToString());
                }
            }
            return default(V);
        }
        public void HashSet(string key, Dictionary<string, string> contentDict)
        {
            Trace.Assert(!contentDict.IsNullOrEmpty(), "");
            List<HashEntry> hashEntries = [];
            foreach (var entry in contentDict)
            {
                hashEntries.Add(new HashEntry(entry.Key, entry.Value));
            }
            database.HashSet(key, hashEntries.ToArray());
        }

    }
}
