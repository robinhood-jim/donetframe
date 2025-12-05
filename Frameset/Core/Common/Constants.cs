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
            LIKE,
            LLIKE,
            RLIKE
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
                "[]" => SqlOperator.BT,
                "<>" => SqlOperator.NE,
                _ => SqlOperator.EQ
            };
        }
        public static readonly string VALID = "1";
        public static readonly string INVALID = "0";
        public static readonly string TRUEVALUE = "true";
        public static readonly string FALSEVALUE = "false";
    }
}
