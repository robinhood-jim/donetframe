using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Serilog;
using System.Collections.Generic;


namespace Frameset.Core.Dao.Meta
{
    public class DbDialectFactory
    {
        static readonly Dictionary<Constants.DbType, AbstractSqlDialect> metaMap =[];
        static DbDialectFactory()
        {
            metaMap.Add(Constants.DbType.Mysql, new MySqlDialect());
            metaMap.Add(Constants.DbType.SqlServer, new MssqlDialect());
            metaMap.Add(Constants.DbType.Oracle, new OracleDialect());
            metaMap.Add(Constants.DbType.DB2, new DB2Dialect());
            metaMap.Add(Constants.DbType.Postgres, new PostgresDialect());
            metaMap.Add(Constants.DbType.ClickHouse, new ClickHouseDialect());

        }
        public static AbstractSqlDialect GetInstance(Constants.DbType dbType)
        {
            if (metaMap.TryGetValue(dbType, out AbstractSqlDialect? dialect))
            {
                return dialect;
            }
            Log.Error("unRegister " + dbType + "!");
            throw new ConfigMissingException("unknow dbType");
        }
    }
}
