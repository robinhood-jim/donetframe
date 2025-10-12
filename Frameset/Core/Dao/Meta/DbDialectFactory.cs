using Frameset.Core.Common;
using Serilog;
using System.Collections.Generic;


namespace Frameset.Core.Dao.Meta
{
    public class DbDialectFactory
    {
        static Dictionary<Constants.DbType, AbstractSqlDialect> metaMap = new Dictionary<Constants.DbType, AbstractSqlDialect>();
        static DbDialectFactory()
        {
            metaMap.Add(Constants.DbType.Mysql, new MySqlDialect());
            metaMap.Add(Constants.DbType.SqlServer, new MssqlDialect());
            metaMap.Add(Constants.DbType.Oracle, new OracleDialect());
            metaMap.Add(Constants.DbType.DB2, new DB2Dialect());
            metaMap.Add(Constants.DbType.Postgres, new PostgresDialect());

        }
        public static AbstractSqlDialect GetInstance(Constants.DbType dbType)
        {
            AbstractSqlDialect dialect = null;
            metaMap.TryGetValue(dbType, out dialect);
            if (dialect == null)
            {
                Log.Error("unRegister " + dbType + "!");
            }
            return dialect;
        }
    }
}
