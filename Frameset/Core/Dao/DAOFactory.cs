using Frameset.Core.Dao.Meta;
using Frameset.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Frameset.Core.Dao
{
    public static class DAOFactory
    {
        private readonly static Dictionary<string, IJdbcDao> containner = [];
        private static Dictionary<string, object> keyValues = [];
        /// <summary>
        /// Dao Accessor Factory init
        /// </summary>
        /// <param name="yamlPath">resource Path</param>
        public static void DoInit(string yamlPath)
        {

            if (keyValues.IsNullOrEmpty())
            {
                IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
                try
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string processPath = yamlPath;
                    if (yamlPath.StartsWith("res:"))
                    {
                        processPath = baseDirectory + Path.DirectorySeparatorChar + yamlPath.Substring(4, yamlPath.Length - 4);
                    }
                    StreamReader reader = File.OpenText(processPath);
                    keyValues = deserializer.Deserialize<Dictionary<string, object>>(reader);
                    Dictionary<object, object> keyDict = (Dictionary<object, object>)GetKeyValues()["dataSource"];
                    foreach (string key in keyDict.Keys)
                    {
                        Dictionary<object, object> dict1 = keyDict[key] as Dictionary<object, object>;
                        IJdbcDao dao = ConstructWithDict(dict1);
                        containner.Add(key, dao);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("{Message}", ex.Message);
                }
            }
        }

        public static Dictionary<string, object> GetKeyValues()
        {
            return keyValues;
        }
        private static IJdbcDao ConstructWithDict(Dictionary<object, object> dict)
        {
            dict.TryGetValue("dbType", out object dbTypeObj);
            string dbType = IsNull(dbTypeObj) ? "Mysql" : dbTypeObj.ToString();
            dict.TryGetValue("ConnectionString", out object connStr);
            StringBuilder builder = new StringBuilder();
            dict.TryGetValue("schema", out object schemaStr);
            string schema = IsNull(schemaStr) ? null : schemaStr.ToString();
            if (IsNull(connStr))
            {
                dict.TryGetValue("userName", out object userName);
                dict.TryGetValue("password", out object password);
                dict.TryGetValue("host", out object hostObj);
                dict.TryGetValue("port", out object portObj);
                dict.TryGetValue("maxSize", out object maxSizeStr);
                dict.TryGetValue("minSize", out object minSizeStr);
                string host = IsNull(hostObj) ? "localhost" : dict["host"].ToString();
                int port = IsNull(portObj) ? AbstractSqlDialect.GetDefaultPort(dbType) : Int32.Parse(portObj.ToString());
                int maxSize = IsNull(maxSizeStr) ? 0 : Int32.Parse(dict["maxSize"].ToString());
                int minSize = IsNull(minSizeStr) ? 0 : Int32.Parse(dict["minSize"].ToString());
                builder.Append("Server=").Append(host).Append(";Port=").Append(port.ToString()).Append(";");
                builder.Append("User ID=").Append(userName).Append(";");
                if (password != null)
                {
                    builder.Append("Password=").Append(password).Append(";");
                }
                if (schema != null)
                {
                    builder.Append("Initial Catalog=").Append(schema).Append(";");
                }
                if (maxSize > 0 || minSize > 0)
                {
                    builder.Append("Pooling=true;");
                    if (maxSize > 0)
                    {
                        builder.Append("Max Pool Size=").Append(maxSize).Append(";");
                    }
                    if (minSize > 0)
                    {
                        builder.Append("Min Pool Size=").Append(maxSize).Append(";");
                    }
                }
                if (string.Equals(dbType, "Mysql", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Append("AllowLoadLocalInfile=true;");
                }
            }
            else
            {
                builder.Append(connStr).Append(";");
            }
            IJdbcDao dao = new JdbcDao(dbType, schema, builder.ToString().Substring(0, builder.Length - 1));
            return dao;

        }
        private static bool IsNull(object input)
        {
            return input == null || string.IsNullOrWhiteSpace(input.ToString());
        }
        public static IJdbcDao GetJdbcDao(string key)
        {
            containner.TryGetValue(key, out IJdbcDao dao);

            return dao;
        }
        public static void Register(string dsName, Dictionary<object, object> configMap)
        {
            if (containner.ContainsKey(dsName))
            {
                Log.Error("register {DsName} exists!", dsName);
                throw new ConfigMissingException("dsName already exist!");
            }
            else
            {
                IJdbcDao dao = ConstructWithDict(configMap);
                if (dao == null)
                {
                    Log.Error("register  {Dsname} config error!", dsName);
                    throw new NotSupportedException("failed");
                }
                else
                {
                    containner.Add(dsName, dao);
                }
            }
        }
    }
}

