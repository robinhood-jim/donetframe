using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;


namespace Frameset.Core.Dao.Meta
{
    public abstract class AbstractSqlDialect
    {
        internal static string ORDERSTR = "order by";
        internal AbstractSqlDialect()
        {

        }

        public virtual string GetDecimalScript(int percise, int scale)
        {

            return new StringBuilder("DECIMAL(").Append(scale).Append(",").Append(percise).Append(")").ToString();
        }
        public virtual string GenerateSequenceScript(string sequenceName)
        {
            throw new NotSupportedException();
        }
        public virtual string getAutoIncrementScript()
        {
            return "AUTO INCREMENT";
        }
        public virtual string GenerateCountSql(string inputSql)
        {
            string plainSql = inputSql.Replace("\\n", "").Replace("\\r", "").Trim().ToLower();
            int orderPos = plainSql.LastIndexOf(ORDERSTR);
            int fromPos = plainSql.IndexOf(" from ");
            if (orderPos == -1)
            {
                orderPos = plainSql.Length;
            }
            StringBuilder builder = new StringBuilder("select count(1) as total ");
            builder.Append(plainSql, fromPos, orderPos - fromPos);
            return builder.ToString();
        }
        public abstract string GeneratePageSql(string baseSql, PageQuery query);
        public virtual string getVarcharFormat(int length)
        {
            return new StringBuilder("VARCHAR(").Append(length).Append(")").ToString();
        }
        public virtual string GetCharFormat()
        {
            return "CHAR(1)";
        }
        public virtual string GetTimestampFormat()
        {
            return "TIMESTAMP";
        }
        public virtual int BatchInsert<V>(IJdbcDao dao, DbConnection connection, IList<V> models, int batchSize = 10000)
        {
            throw new NotSupportedException();
        }
        public virtual string GetDateTimeFormat()
        {
            return "DATETIME";
        }

        public virtual string GetIntegerFormat()
        {
            return "INT";
        }

        public virtual string GetShortFormat()
        {
            return "SMALLINT";
        }

        public virtual string GetLongFormat()
        {
            return "BIGINT";
        }



        public virtual string GetBlobFormat()
        {
            return "BLOB";
        }
        public virtual string GetNumericFormat()
        {
            return "NUMERIC";
        }


        public virtual string GetClobFormat()
        {
            return "TEXT";
        }
        public virtual string AppendKeyHolder()
        {
            return "";
        }
        public virtual string AppendSequence(string sequenceName)
        {
            return "";
        }
        public abstract DbConnection GetDbConnection(string connectStr);
        public abstract DbCommand GetDbCommand(DbConnection connection, string sql);
        public abstract DbParameter WrapParameter(int pos, object value);
        public abstract DbParameter WrapParameter(string column, object value);
        public virtual long QueryIdentityByTable(IJdbcDao dao, DbConnection connection, DbTransaction transaction, EntityContent entityContent)
        {
            throw new NotSupportedException();
        }
        internal String GetNoPageSql(string sql, PageQuery pageQuery)
        {

            StringBuilder builder = new StringBuilder(sql);
            if (!pageQuery.Order.IsNullOrEmpty() || (!pageQuery.OrderField.IsNullOrEmpty() && !pageQuery.OrderDirection.IsNullOrEmpty()))
            {
                if (!pageQuery.Order.IsNullOrEmpty())
                {
                    builder.Append(" order by ").Append(pageQuery.Order);
                }
                else
                {
                    builder.Append(" order by ").Append(pageQuery.OrderField).Append(pageQuery.OrderDirection);
                }
            }

            return builder.ToString();
        }
        internal Tuple<long, long> GetStartEndRecord(PageQuery pageQuery)
        {
            long nBegin = (pageQuery.CurrentPage - 1) * pageQuery.PageSize;
            long tonums = nBegin + pageQuery.PageSize;
            if (pageQuery.Total != 0 && pageQuery.Total < tonums)
            {
                tonums = pageQuery.Total;
            }
            return Tuple.Create(nBegin, tonums);
        }
        internal StringBuilder GetPageSqlByRowNumber(string strSQL, PageQuery pageQuery)
        {
            StringBuilder pagingSelect = new StringBuilder();
            pagingSelect.Append("select * from ( select r.*,row_number() over(");
            if (!pageQuery.Order.IsNullOrEmpty())
            {
                pagingSelect.Append(ORDERSTR).Append(pageQuery.Order).Append(") as rownum");
            }
            else if (!pageQuery.OrderField.IsNullOrEmpty())
            {
                pagingSelect.Append(ORDERSTR).Append(pageQuery.OrderField).Append(" ").Append(pageQuery.OrderDirection).Append(") as rownum");
            }
            else
            {
                pagingSelect.Append(") as rownum");
            }
            pagingSelect.Append(" from ( ");
            pagingSelect.Append(strSQL);

            pagingSelect.Append(" )r) r ");
            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug("page sql=" + pagingSelect);
            }
            return pagingSelect;
        }
        public String GetColumnDefine(FieldContent fieldContent)
        {
            StringBuilder builder = new StringBuilder();
            switch (fieldContent.DataType)
            {
                case Constants.MetaType.SHORT:
                    builder.Append(fieldContent.FieldName).Append(" ").Append(GetShortFormat());
                    break;
                case Constants.MetaType.BIGINT:
                    builder.Append(fieldContent.FieldName).Append(" ").Append(GetLongFormat());
                    break;
                case Constants.MetaType.INTEGER:
                    builder.Append(fieldContent.FieldName).Append(" ").Append(GetIntegerFormat());
                    break;
                case Constants.MetaType.FLOAT:
                case Constants.MetaType.DOUBLE:
                    builder.Append(fieldContent.FieldName).Append(" ").Append(GetNumericFormat());
                    if (fieldContent.Scale != 0 && fieldContent.Precise != 0)
                    {
                        builder.Append("(").Append(fieldContent.Scale).Append(",").Append(fieldContent.Precise).Append(")");
                    }
                    break;
                case Constants.MetaType.DATE:
                    builder.Append(fieldContent.FieldName).Append(" ").Append(GetDateTimeFormat());
                    break;
                case Constants.MetaType.TIMESTAMP:
                    builder.Append(fieldContent.FieldName).Append(" ").Append(GetTimestampFormat());
                    break;

            }
            return builder.ToString();

        }
        public static int GetDefaultPort(string dbType)
        {
            Constants.DbType dbTypes = Constants.DbTypeOf(dbType);
            int defaultPort = 0;
            switch (dbTypes)
            {
                case Constants.DbType.Mysql:
                    defaultPort = 3306;
                    break;
                case Constants.DbType.Oracle:
                    defaultPort = 1521;
                    break;
                case Constants.DbType.DB2:
                    defaultPort = 50000;
                    break;
                case Constants.DbType.Postgres:
                    defaultPort = 5432;
                    break;
                case Constants.DbType.SqlServer:
                    defaultPort = 1433;
                    break;
                case Constants.DbType.Sybase:
                    defaultPort = 5007;
                    break;

            }
            return defaultPort;

        }
    }
}
