using Frameset.Core.Common;
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
                        processPath = baseDirectory + Path.DirectorySeparatorChar + yamlPath.Substring(4, yamlPath.Length);
                    }
                    StreamReader reader = File.OpenText(processPath);
                    keyValues = deserializer.Deserialize<Dictionary<string, object>>(reader);
                    Dictionary<object, object> keyDict = (Dictionary<object, object>)GetKeyValues()["dataSource"];
                    int daoSize = keyDict.Keys.Count;
                    foreach (string key in keyDict.Keys)
                    {
                        Dictionary<object, object> dict1 = keyDict[key] as Dictionary<object, object>;
                        IJdbcDao dao = ConstructWithDict(dict1);
                        containner.Add(key, dao);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }

        public static Dictionary<string, object> GetKeyValues()
        {
            return keyValues;
        }
        private static IJdbcDao ConstructWithDict(Dictionary<object, object> dict)
        {
            string dbType = dict["dbType"] == null ? "Mysql" : dict["dbType"].ToString();
            string host = dict["host"] == null ? "localhost" : dict["host"].ToString();
            int port = CollectionUtil<object, object>.isNotEmpty(dict, "port") ? Int32.Parse(dict["port"].ToString()) : AbstractSqlDialect.GetDefaultPort(dbType);
            string userName = dict["userName"].ToString();
            string password = dict["password"] == null ? null : dict["password"].ToString();
            int maxSize = CollectionUtil<object, object>.isNotEmpty(dict, "maxSize") ? Int32.Parse(dict["maxSize"].ToString()) : 0;
            int minSize = CollectionUtil<object, object>.isNotEmpty(dict, "minSize") ? Int32.Parse(dict["minSize"].ToString()) : 0;
            string schema = CollectionUtil<object, object>.isNotEmpty(dict, "schema") ? dict["schema"].ToString() : null;

            StringBuilder builder = new StringBuilder();
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
            IJdbcDao dao = new JdbcDao(dbType, schema, builder.ToString().Substring(0, builder.Length - 1));
            return dao;

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

