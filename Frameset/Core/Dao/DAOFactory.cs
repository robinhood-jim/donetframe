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
    public class DAOFactory
    {
        private Dictionary<string, IJdbcDao> containner = [];
        private Dictionary<string, object> keyValues = [];
        private static DAOFactory fact = null;
        static DAOFactory()
        {
            fact = new DAOFactory();
        }
        public static DAOFactory DoInit(string yamlPath)
        {

            if (fact.getKeyValues().IsNullOrEmpty())
            {
                IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
                try
                {
                    StreamReader reader = File.OpenText(yamlPath);
                    fact.keyValues = deserializer.Deserialize<Dictionary<string, object>>(reader);
                    Dictionary<object, object> keyDict = (Dictionary<object, object>)fact.getKeyValues()["dataSource"];
                    int daoSize = keyDict.Keys.Count;
                    foreach (string key in keyDict.Keys)
                    {
                        Dictionary<object, object> dict1 = keyDict[key] as Dictionary<object, object>;
                        IJdbcDao dao = constructWithDict(dict1);
                        fact.containner.Add(key, dao);
                    }

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }
            }
            return fact;
        }
        public static DAOFactory getInstance()
        {
            return fact;
        }
        public Dictionary<string, object> getKeyValues()
        {
            return keyValues;
        }
        static IJdbcDao constructWithDict(Dictionary<object, object> dict)
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
        public IJdbcDao getJdbcDao(string key)
        {
            if (containner.ContainsKey(key))
            {
                return containner[key];
            }
            return null;
        }
        public static void Register(string dsName, Dictionary<object, object> configMap)
        {
            if (fact.containner.ContainsKey(dsName))
            {
                Log.Error("register " + dsName + " exists!");
                throw new ConfigMissingException("dsName already exist!");
            }
            else
            {
                IJdbcDao dao = constructWithDict(configMap);
                if (dao == null)
                {
                    Log.Error("register " + dsName + " config error!");
                    throw new NotSupportedException("failed");
                }
                else
                {
                    fact.containner.Add(dsName, dao);
                }
            }
        }

        static void Main(string[] args)
        {
            DAOFactory f = DAOFactory.DoInit("f:/1.yaml");
            Dictionary<string, object> kv = f.getKeyValues();
            System.Console.WriteLine(kv);



        }
    }
}

