using Frameset.Bigdata.NoSql;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
namespace Frameset.Bigdata.MongoDb
{
    public class MongoDbRepository<V> : NoSqlRepository<V, string> where V : BaseEntity
    {
        private readonly MongoClient client;
        private readonly IMongoDatabase database;

        public MongoDbRepository(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.MONGODBURL, out string? connectUrl);
            connectUrl = connectUrl ?? ResourceConstants.MONGODBDEFAULTURL;
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectUrl));
            client = new MongoClient(settings);
            database = client.GetDatabase(content.Schema);
        }

        public override V GetById(string pk)
        {
            var collection = database.GetCollection<BsonDocument>(content.TableName);
            if (collection != null)
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(pk));
                var result = collection.Find(filter).ToList();
                if (!result.IsNullOrEmpty())
                {
                    if (result.Count > 1)
                    {
                        throw new OperationFailedException("getById return more than one rows!");
                    }
                    else
                    {
                        BsonDocument bson = result[0];
                        V entity = Activator.CreateInstance<V>();
                        foreach (FieldContent fieldContent in fieldContents)
                        {
                            var obj = bson[fieldContent.PropertyName];
                            if (obj != null)
                            {
                                fieldContent.SetMethod.Invoke(entity, new object[] { obj });
                            }
                        }
                        return entity;
                    }
                }
                else
                {
                    throw new OperationFailedException("getById return none rows!");
                }
            }
            else
            {
                throw new OperationFailedException("collection" + content.TableName + " does not exists!");
            }
        }

        public override int RemoveEntity(IList<string> pks)
        {
            var collection = database.GetCollection<BsonDocument>(content.TableName);
            if (collection != null)
            {
                var filter = Builders<BsonDocument>.Filter.AnyIn("_id", pks.Select(x => new ObjectId(x)));
                var result = collection.DeleteMany(filter);
                return Convert.ToInt32(result.DeletedCount);
            }
            else
            {
                throw new OperationFailedException("collection" + content.TableName + " does not exists!");
            }
        }

        public override bool SaveEntity(V entity)
        {
            var collection = database.GetCollection<BsonDocument>(content.TableName);
            object pk = pkColumn.GetMethod.Invoke(entity, null);
            if (collection != null)
            {
                Dictionary<string, object> dict = [];

                foreach (FieldContent fieldContent in fieldContents)
                {
                    var obj = fieldContent.GetMethod.Invoke(entity, null);
                    if (obj != null)
                    {
                        dict.TryAdd(fieldContent.PropertyName, obj);
                    }
                }
                if (pk != null)
                {
                    dict.TryAdd("_id", new ObjectId(pk.ToString()));
                }
                var bsonDocument = new BsonDocument(dict);
                collection.InsertOne(bsonDocument);
                return true;
            }
            else
            {
                throw new OperationFailedException("collection" + content.TableName + " does not exists!");
            }
        }

        public override bool UpdateEntity(V entity)
        {
            var collection = database.GetCollection<BsonDocument>(content.TableName);
            object pk = pkColumn.GetMethod.Invoke(entity, null);
            if (collection != null)
            {
                Dictionary<string, object> dict = [];
                foreach (FieldContent fieldContent in fieldContents)
                {
                    if (!fieldContent.IfPrimary)
                    {
                        var obj = fieldContent.GetMethod.Invoke(entity, null);
                        if (obj != null || entity.GetDirties().Contains(fieldContent.PropertyName))
                        {
                            dict.TryAdd(fieldContent.PropertyName, obj);
                        }
                    }
                }
                var updateEle = new BsonDocument(dict);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(pk.ToString()));
                var updateBuilder = Builders<BsonDocument>.Update;
                var update = updateBuilder.Combine(updateEle.Select(u => u.Value != null ?
                    updateBuilder.Set(u.Name, u.Value) : updateBuilder.Unset(u.Name)
                ));
                collection.UpdateOne(filter, update);
                return true;
            }
            else
            {
                throw new OperationFailedException("collection" + content.TableName + " does not exists!");
            }
        }
        protected override void Dispose(bool disposable)
        {
            if (disposable)
            {
                client.Dispose();
            }
        }
    }
}
