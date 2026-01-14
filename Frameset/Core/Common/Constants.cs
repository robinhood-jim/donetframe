using System;
using System.Collections.Generic;

namespace Frameset.Core.Common
{
    public class Constants
    {
        public enum DbType
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
            BOOLEAN,
            CHAR,
            CLOB,
            BLOB,
            FORMULA
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
            IN,
            NOTIN,
            LIKE,
            LLIKE,
            RLIKE,
            EXISTS,
            NOTEXISTS,
            NOTNULL,
            ISNULL,
            UNION,
            UNIONALL
        }
        public enum FileSystemType
        {
            LOCAL,
            FTP,
            SFTP,
            SMB,
            HDFS,
            S3,
            ALIYUN,
            TENCENTCOS,
            QINIU,
            BOS,
            OBS,
            MINIO
        }
        public enum ResourceType
        {
            NONE,
            KAFAK,
            RABITMQ,
            ZEROQ,
            MONGODB,
            CASSANDRA
        }
        public enum FileFormatType
        {
            CSV,
            XML,
            XLSX,
            ARFF,
            JSON,
            AVRO,
            ORC,
            PARQUET,
            PROTOBUF
        }
        public enum JoinType
        {
            INNER,
            LEFT,
            RIGHT,
            FULLOUTTER
        }
        public static readonly List<String> DBTYPES = new List<String> { "Mysql", "Oracle", "Postgres", "db2", "SqlServer", "Sybase", "ClickHouse" };
        public static DbType DbTypeOf(string dbType)
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
        public static FileSystemType FsTypeOf(string resourceType)
        {
            FileSystemType resType = FileSystemType.LOCAL;
            foreach (FileSystemType rtype in Enum.GetValues(typeof(FileSystemType)))
            {
                if (rtype.ToString().ToUpper().Equals(resourceType.ToUpper()))
                {
                    resType = rtype;
                    break;
                }
            }
            return resType;
        }
        public static ResourceType ResTypeOf(string resourceType)
        {
            ResourceType resType = ResourceType.NONE;
            foreach (ResourceType rtype in Enum.GetValues(typeof(ResourceType)))
            {
                if (rtype.ToString().ToUpper().Equals(resourceType.ToUpper()))
                {
                    resType = rtype;
                    break;
                }
            }
            return resType;
        }
        public static FileFormatType FileFormatTypeOf(string resourceType)
        {
            FileFormatType resType = FileFormatType.CSV;
            foreach (FileFormatType rtype in Enum.GetValues(typeof(FileFormatType)))
            {
                if (rtype.ToString().ToUpper().Equals(resourceType.ToUpper()))
                {
                    resType = rtype;
                    break;
                }
            }
            return resType;
        }
        public static SqlOperator Parse(string cmpOper)
        {
            return cmpOper.ToLower() switch
            {
                ">" => SqlOperator.GT,
                "<" => SqlOperator.LT,
                ">=" => SqlOperator.GE,
                "<=" => SqlOperator.LE,
                "in" => SqlOperator.IN,
                "between" => SqlOperator.BT,
                "<>" => SqlOperator.NE,
                "notin" => SqlOperator.NOTIN,
                "exists" => SqlOperator.EXISTS,
                "notexists" => SqlOperator.NOTEXISTS,
                _ => SqlOperator.EQ
            };
        }
        public static string OperatorValue(SqlOperator sqlOperator)
        {
            return sqlOperator switch
            {
                SqlOperator.EQ => "=",
                SqlOperator.LT => "<",
                SqlOperator.LE => "<=",
                SqlOperator.GT => ">",
                SqlOperator.GE => ">=",
                SqlOperator.BT => "BETWEEN",
                SqlOperator.NE => "<>",
                SqlOperator.IN => " IN ",
                SqlOperator.NOTIN => " NOT IN ",
                SqlOperator.LIKE => " LIKE ",
                SqlOperator.LLIKE => " LIKE ",
                SqlOperator.RLIKE => " LIKE ",
                SqlOperator.EXISTS => " EXISTS ",
                SqlOperator.NOTEXISTS => " NOT EXISTS",
                SqlOperator.NOTNULL => " NOT NULL",
                SqlOperator.ISNULL => " IS NULL ",
                SqlOperator.UNION => " UNION ",
                SqlOperator.UNIONALL => " UNION ALL ",
                _ => "="
            };

        }
        public static string GetJoinType(JoinType joinType)
        {
            return joinType switch
            {
                JoinType.INNER => " INNER ",
                JoinType.LEFT => " LEFT ",
                JoinType.RIGHT => " RIGHT ",
                JoinType.FULLOUTTER => " OUTTER "
            };
        }
        public static readonly string VALID = "1";
        public static readonly string INVALID = "0";
        public static readonly string TRUEVALUE = "true";
        public static readonly string FALSEVALUE = "false";
        public static readonly string LINK_AND = "AND";
        public static readonly string LINK_OR = "OR";
        public static readonly string SQL_AND = " AND ";
        public static readonly string SQL_OR = " OR ";
        public static readonly string SQL_AS = " AS ";
        public static readonly string SQL_EQ = "=";
        public static readonly string SQL_SELECT = "SELECT ";
        public static readonly string SQL_FROM = " FROM ";
        public static readonly string SQL_HAVING = " HAVING ";
        public static readonly string SQL_JOIN = " JOIN ";
        public static readonly string SQL_ON = " ON ";
        public static readonly string SQL_IN = " IN ";
        public static readonly string SQL_NOTIN = " NOT IN ";
        public static readonly string SQL_GROUPBY = " GROUP BY ";
        public static readonly string SQL_WHERE = " WHERE ";
        public static readonly string SQL_ORDERBY = " ORDER BY ";
        public static readonly string WHERECAUSE = "WHERE";
        public static readonly string HAVING = "HAVING";
        public static readonly string GROUPBY = "GROUP BY";
        public static readonly string ORDERBY = "ORDER BY";
        public static readonly string SELECT = "SELECT";
        public static readonly string SELECTCOLUMS = "SELECTCOLUMNS";
        public static readonly string NEWCOLUMN = "NEWCOLUMN";
        public static readonly string SUM = "SUM";
        public static readonly string AVG = "AVG";
        public static readonly string MAX = "MAX";
        public static readonly string MIN = "MIN";
        public static readonly string CONCAT = "CONCAT";
        public static readonly string CASE = "CASE";

        public static readonly List<string> IGNOREPARAMS = [WHERECAUSE, HAVING, GROUPBY, ORDERBY, SELECTCOLUMS, NEWCOLUMN];
        public static readonly List<string> SQLFUNCTIONS = [SUM, AVG, MAX, MIN, CASE, CONCAT, "UPPER", "LOWER", "LEN", "ROUND", "SUBSTR", "NOW", "SYSDATE", "FORMAT", "EXP", "TRIM", "LTRIM", "RTIME", "DATE", "MONTH", "DAY", "TO_DATE", "COALESCE", "CAST"];
        public static readonly List<string> SQLOPERATORS = ["(", ")", "*", "/", "+", "-"];
    }
}
