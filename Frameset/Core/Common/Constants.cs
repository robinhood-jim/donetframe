using System;
using System.Collections.Generic;

namespace Frameset.Core.Common
{
    public class Constants
    {
        public enum DbType : int
        {
            Unknow = -1,
            Mysql = 1,
            Oracle,
            Postgres,
            DB2,
            SqlServer,
            Sybase,
            ClickHouse
        }
        public enum MetaType
        {
            SHORT = 1,
            INTEGER,
            BIGINT,
            FLOAT,
            DOUBLE,
            DATE,
            TIMESTAMP,
            STRING,
            CLOB,
            BLOB

        }
        public enum SqlOperator
        {
            EQ,
            NE,
            GT,
            LT,
            GE,
            LE,
            BT,
            LIKE,
            LLIKE,
            RLIKE
        }
        public static List<String> DBTYPES = new List<String> { "Mysql", "Oracle", "Postgres", "db2", "SqlServer", "Sybase", "ClickHouse" };
        public static DbType dbTypeOf(String dbType)
        {
            DbType retType = DbType.Unknow;
            foreach (DbType type in Enum.GetValues(typeof(DbType)))
            {
                if (type.ToString().ToUpper().Equals(dbType.ToUpper()))
                {
                    retType = type;
                    break;
                }

            }
            return retType;
        }

    }
}
