using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Frameset.Core.Dao
{
    public static class DAOFactory
    {
        private readonly static Dictionary<string, IAdoDao> containner = [];
        private static Dictionary<string, object> keyValues = [];
        internal static char endSuffix = ';';
        /// <summary>
        /// Dao Accessor Factory init
        /// </summary>
        /// <param name="yamlPath">resource Path</param>
        /// <param name="autoConstructDbContext"> Auto Construct DbContext</param>
        public static void DoInit(string yamlPath, bool autoConstructDbContext = false)
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
                    using StreamReader reader = File.OpenText(processPath);
                    keyValues = deserializer.Deserialize<Dictionary<string, object>>(reader);
                    Dictionary<object, object> keyDict = (Dictionary<object, object>)GetKeyValues()["dataSource"];
                    foreach (var entry in keyDict)
                    {
                        string? key = entry.Key.ToString();
                        Trace.Assert(!string.IsNullOrWhiteSpace(key));
                        Dictionary<object, object>? dict1 = entry.Value as Dictionary<object, object>;
                        Dictionary<string, object> tmpDict = [];
                        Trace.Assert(dict1 != null);
                        foreach (KeyValuePair<object, object> pair in dict1)
                        {
                            string? pairKey = pair.Key.ToString();
                            Trace.Assert(!string.IsNullOrWhiteSpace(pairKey));
                            tmpDict.TryAdd(pairKey, pair.Value);
                        }
                        IAdoDao dao = ConstructWithDict(tmpDict);
                        containner.Add(key, dao);
                        if (autoConstructDbContext)
                        {
                            DbContextFactory.Register(new DbContext(key));
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("{Message}", ex.Message);
                }
            }
        }

        internal static IAdoDao RegisterJdbcDao(string key, Dictionary<string, object> dict, ISqlDialect sqlDialect, bool autoConstructDbContext = true)
        {
            IAdoDao dao = ConstructWithDict(dict, sqlDialect);
            containner.Add(key, dao);
            if (autoConstructDbContext)
            {
                DbContextFactory.Register(new DbContext(key));
            }
            return dao;
        }

        public static Dictionary<string, object> GetKeyValues()
        {
            return keyValues;
        }

        internal static IAdoDao ConstructWithDict(Dictionary<string, object> dict, ISqlDialect? sqlDialect = null)
        {
            var builder = GetConnectString(dict, out var dbType, out var schema, out var logicColumnStr, out object? validValue, out object? invalidValue);
            IAdoDao? dao;
            if (sqlDialect == null)
            {
                if (string.IsNullOrEmpty(logicColumnStr))
                {
                    dao = new AdoDao(dbType, schema, builder.ToString().Substring(0, builder.Length - 1));
                }
                else
                {
                    Trace.Assert(validValue != null && invalidValue != null);
                    dao = new AdoDao(dbType, schema, builder.ToString().Substring(0, builder.Length - 1),
                        logicColumnStr, validValue, invalidValue);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(logicColumnStr))
                {
                    dao = new AdoDao(sqlDialect, schema, builder.ToString().Substring(0, builder.Length - 1));
                }
                else
                {
                    Trace.Assert(validValue != null && invalidValue != null);
                    dao = new AdoDao(sqlDialect, schema, builder.ToString().Substring(0, builder.Length - 1),
                        logicColumnStr, validValue, invalidValue);
                }
            }

            return dao;

        }

        public static StringBuilder GetConnectString(Dictionary<string, object> dict, out string dbType, out string schema,
            out string logicColumnStr, out object? validValue, out object? invalidValue)
        {
            StringBuilder builder = new StringBuilder();
            dbType = string.Empty;
            schema = string.Empty;
            if (dict.TryGetValue(Constants.DBTYPECOLUMN, out object? dbTypeObj))
            {
                string? dbTypeStr = IsNull(dbTypeObj) ? "Mysql" : dbTypeObj.ToString();
                if (!string.IsNullOrWhiteSpace(dbTypeStr))
                {
                    dbType = dbTypeStr;
                }
            }
            else
            {
                dbType = "Mysql";
            }
            dict.TryGetValue("ConnectionString", out object? connStr);
            if (dict.TryGetValue("schema", out object? schemaStr))
            {
                string? schemaStr1 = schemaStr.ToString();
                if (!string.IsNullOrWhiteSpace(schemaStr1))
                {
                    schema = schemaStr1;
                }
            }
            logicColumnStr = string.Empty;
            validValue = null;
            invalidValue = null;
            Constants.DbType dbType1 = Constants.DbTypeOf(dbType);
            if (IsNull(connStr))
            {
                dict.TryGetValue("userName", out object? userName);
                dict.TryGetValue("password", out object? password);
                dict.TryGetValue("host", out object? hostObj);
                dict.TryGetValue("port", out object? portObj);
                dict.TryGetValue("maxSize", out object? maxSizeStr);
                dict.TryGetValue("minSize", out object? minSizeStr);
                dict.TryGetValue("logicColumn", out object? logicColumn);
                dict.TryGetValue("validValue", out validValue);
                dict.TryGetValue("invalidValue", out invalidValue);
                string? host = IsNull(hostObj) ? "localhost" : dict["host"].ToString();
                int port = IsNull(portObj) ? AbstractSqlDialect.GetDefaultPort(dbType) : Convert.ToInt32(portObj);
                int maxSize = IsNull(maxSizeStr) ? 0 : Convert.ToInt32(dict["maxSize"]);
                int minSize = IsNull(minSizeStr) ? 0 : Convert.ToInt32(dict["minSize"]);

                switch (dbType1)
                {
                    case Constants.DbType.Mysql:
                    case Constants.DbType.SqlServer:
                        builder.Append("Server=").Append(host).Append(";Port=").Append(port).Append(endSuffix);
                        builder.Append("User ID=").Append(userName).Append(endSuffix);
                        builder.Append("Password=").Append(password).Append(endSuffix);
                        break;
                    case Constants.DbType.Postgres:
                        builder.Append("Host=").Append(host).Append(";Port=").Append(port).Append(endSuffix);
                        builder.Append("Username=").Append(userName).Append(endSuffix);
                        builder.Append("Password=").Append(password).Append(endSuffix);
                        break;
                    case Constants.DbType.Oracle:
                        builder.Append("DataSource=").Append(host).Append(':').Append(port).Append('/').Append(schema).Append(endSuffix);
                        builder.Append("User ID=").Append(userName).Append(endSuffix);
                        builder.Append("Password=").Append(password).Append(endSuffix);
                        break;
                    case Constants.DbType.DB2:
                        builder.Append("Server=").Append(host).Append(endSuffix).Append(port).Append(endSuffix);
                        builder.Append("UID=").Append(userName).Append(endSuffix);
                        builder.Append("PWD=").Append(password).Append(endSuffix);
                        break;
                }

                if (schema != null)
                {
                    if (Constants.DbType.SqlServer.Equals(dbType1))
                    {
                        builder.Append("Initial Catalog=").Append(schema).Append(endSuffix);
                    }
                    else if (Constants.DbType.Mysql.Equals(dbType1) || Constants.DbType.Postgres.Equals(dbType1) || Constants.DbType.DB2.Equals(dbType1))
                    {
                        builder.Append("Database=").Append(schema).Append(endSuffix);
                    }

                }
                if (logicColumn != null)
                {
                    string? logicStr = logicColumn.ToString();
                    Trace.Assert(!string.IsNullOrWhiteSpace(logicStr));
                    logicColumnStr = logicStr;
                }
                if (maxSize > 0 || minSize > 0)
                {
                    builder.Append("Pooling=true;");
                    if (maxSize > 0)
                    {
                        if (!Constants.DbType.Postgres.Equals(dbType1))
                        {
                            builder.Append("Max Pool Size=").Append(maxSize).Append(endSuffix);
                        }
                        else
                        {
                            builder.Append("MaxPoolSize=").Append(maxSize).Append(endSuffix);
                        }
                    }
                    if (minSize > 0)
                    {
                        if (!Constants.DbType.Postgres.Equals(dbType1))
                        {
                            builder.Append("Min Pool Size=").Append(maxSize).Append(endSuffix);
                        }
                        else
                        {
                            builder.Append("MinPoolSize=").Append(maxSize).Append(endSuffix);
                        }

                    }
                }
                if (Constants.DbType.Mysql.Equals(dbType1))
                {
                    builder.Append("AllowLoadLocalInfile=true;");
                }
            }
            else
            {
                builder.Append(connStr).Append(endSuffix);
            }

            return builder;
        }

        private static bool IsNull(object? input)
        {
            return input == null || string.IsNullOrWhiteSpace(input.ToString());
        }
        public static IAdoDao GetAdoDao(string key)
        {
            containner.TryGetValue(key, out IAdoDao? dao);
            Trace.Assert(dao != null);
            return dao;
        }
        public static AdoDao Construct(string dbType, string schema, string connectionString)
        {
            AdoDao dao = new AdoDao(dbType, schema, connectionString);
            return dao;
        }

        public static IAdoDao Construct(Dictionary<string, object> dictParameter)
        {
            return ConstructWithDict(dictParameter);
        }

        public static IAdoDao ConstructWithMeta(DataCollectionDefine collectionDefine)
        {
            Dictionary<string, object> configDit = [];
            foreach (KeyValuePair<string, string> pair in collectionDefine.ResourceConfig)
            {
                configDit.TryAdd(pair.Key, pair.Value);
            }
            return ConstructWithDict(configDit);
        }

        public static void Register(string dsName, Dictionary<string, object> configMap, bool autoConstructDbContext = false)
        {
            if (containner.ContainsKey(dsName))
            {
                Log.Error("register {DsName} exists!", dsName);
                throw new ConfigMissingException("dsName already exist!");
            }
            else
            {
                IAdoDao dao = ConstructWithDict(configMap);
                if (dao == null)
                {
                    Log.Error("register  {Dsname} config error!", dsName);
                    throw new NotSupportedException("failed");
                }
                else
                {
                    containner.Add(dsName, dao);
                    if (autoConstructDbContext)
                    {
                        DbContextFactory.Register(new DbContext(dsName));
                    }
                }
            }
        }
    }
}

