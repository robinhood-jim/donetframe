using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.FileSystem;
using Frameset.Core.Query;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;


namespace Frameset.Core.Dao.Meta
{
    public abstract class AbstractSqlDialect
    {
        internal static string ORDERSTR = "order by";
        internal AbstractSqlDialect()
        {

        }
        public virtual string GetDecimalScript(int scale, int precise)
        {

            return new StringBuilder("DECIMAL(").Append(scale).Append(",").Append(precise).Append(")").ToString();
        }
        public virtual string GetDecimalScript(FieldContent content)
        {

            return new StringBuilder("DECIMAL(").Append(content.Scale).Append(",").Append(content.Precise).Append(")").ToString();
        }
        public virtual string GenerateSequenceFunc(string sequenceName)
        {
            throw new NotSupportedException();
        }
        public virtual string GenerateSequenceQuery(string sequenceName)
        {
            throw new NotSupportedException();
        }
        
        public virtual string GenerateFieldDefine(FieldContent content)
        {
            StringBuilder builder = new();
            builder.Append(content.FieldName).Append(" ").Append(GetFieldDefineScript(content));
            if (content.IfIncrement)
            {
                builder.Append(AppendAutoIncrement());
            }
            return builder.ToString();
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
        public virtual string getVarcharFormat(FieldContent content)
        {
            return new StringBuilder("VARCHAR(").Append(content.Length).Append(")").ToString();
        }
        public virtual string GetCharFormat(int length)
        {
            return "CHAR(" + length + ")";
        }
        public virtual string GetTimestampFormat(FieldContent content)
        {
            return "TIMESTAMP";
        }
        public virtual long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            throw new NotSupportedException();
        }
        public virtual long BatchInsert(IJdbcDao dao, DbConnection connection, string schema, string tableName, List<DataSetColumnMeta> metas, IEnumerable<Dictionary<string, object>> models, CancellationToken token, int batchSize = 10000)
        {
            throw new NotSupportedException();
        }


        public virtual string GetIntegerFormat(FieldContent content)
        {
            return "INT";
        }

        public virtual string GetShortFormat(FieldContent content)
        {
            return "SMALLINT";
        }

        public virtual string GetLongFormat(FieldContent content)
        {
            return "BIGINT";
        }
        public virtual string GetFloatFormat(FieldContent content)
        {
            return "FLOAT";
        }
        public virtual string GetDoubleFormat(FieldContent content)
        {
            return "DOUBLE";
        }
        public virtual string GetDateFormat(FieldContent content)
        {
            return "DATE";
        }



        public virtual string GetBlobFormat(FieldContent content)
        {
            return "BLOB";
        }
        public virtual string GetNumericFormat(FieldContent content)
        {
            return "NUMERIC(" + content.Scale + "," + content.Precise + ")";
        }


        public virtual string GetClobFormat(FieldContent content)
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
        public virtual string AppendAutoIncrement()
        {
            return "IDENTITY";
        }
        public virtual string GetFieldDefineScript(FieldContent content)
        {
            StringBuilder builder = new StringBuilder(0);
            builder.Append(StringUtils.CamelCaseLowConvert(content.FieldName)).Append(" ");
            switch (content.DataType)
            {
                case Constants.MetaType.INTEGER:
                    builder.Append(GetIntegerFormat(content));
                    break;
                case Constants.MetaType.LONG:
                    builder.Append(GetLongFormat(content));
                    break;
                case Constants.MetaType.CHAR:
                    builder.Append(GetCharFormat(content.Length));
                    break;
                case Constants.MetaType.FLOAT:
                    builder.Append(GetFloatFormat(content));
                    break;
                case Constants.MetaType.DOUBLE:
                    builder.Append(GetDecimalScript(content));
                    break;
                case Constants.MetaType.NUMERIC:
                    builder.Append(GetNumericFormat(content));
                    break;
                case Constants.MetaType.DATE:
                    builder.Append(GetDateFormat(content));
                    break;
                case Constants.MetaType.TIMESTAMP:
                    builder.Append(GetTimestampFormat(content));
                    break;
                case Constants.MetaType.CLOB:
                    builder.Append(GetClobFormat(content));
                    break;
                case Constants.MetaType.BLOB:
                    builder.Append(GetBlobFormat(content));
                    break;
                case Constants.MetaType.STRING:
                    builder.Append(getVarcharFormat(content));
                    break;
            }
            if (content.IfIncrement)
            {
                builder.Append(" ").Append(AppendAutoIncrement());
            }
            if (content.Required)
            {
                builder.Append(" NOT NULL");
            }
            if (content.IfPrimary)
            {
                builder.Append(" ").Append(" PRIMARY KEY");
            }

            builder.Append(",");
            return builder.ToString();
        }
        public abstract DbConnection GetDbConnection(string connectStr);
        public abstract DbCommand GetDbCommand(DbConnection connection, string sql);

        public abstract DbCommand GetDbCommand(DbConnection connection);
        public abstract DbParameter WrapParameter(int pos, object value);
        public abstract DbParameter WrapParameter(string column, object value);
        public virtual bool SupportAutoIncrement()
        {
            return true;
        }
        public virtual bool SupportSequence()
        {
            return false;
        }


        public virtual long QueryIdentityByTable(IJdbcDao dao, DbConnection connection, string schema, string tableName)
        {
            throw new NotSupportedException();
        }
        public virtual long QuerySequenceValue(IJdbcDao dao, DbConnection connection, string sequenceName)
        {
            throw new NotSupportedException();
        }
        internal String GetNoPageSql(string sql, PageQuery pageQuery)
        {

            StringBuilder builder = new StringBuilder(sql);
            if (!pageQuery.Order.IsNullOrEmpty() || (!pageQuery.OrderField.IsNullOrEmpty()))
            {
                if (!pageQuery.Order.IsNullOrEmpty())
                {
                    builder.Append(" order by ").Append(pageQuery.Order);
                }
                else
                {
                    builder.Append(" order by ").Append(pageQuery.OrderField).Append(pageQuery.OrderAsc ? " ASC" : " DESC");
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
                pagingSelect.Append(ORDERSTR).Append(pageQuery.OrderField).Append(" ").Append(pageQuery.OrderAsc ? " ASC" : " DESC").Append(") as rownum");
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
                case Constants.DbType.ClickHouse:
                    defaultPort = 8123;
                    break;

            }
            return defaultPort;

        }
        public virtual void AppendAdditionalScript(StringBuilder buidler, Dictionary<string, object> paramMap)
        {

        }
        public virtual void ExecuteNoQuery(DbConnection connection, string sql)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        protected void ParseDataColumnMap(IJdbcDao dao, DataTable table, List<DataSetColumnMeta> metas)
        {
            foreach (DataSetColumnMeta meta in metas)
            {
                table.Columns.Add(meta.ColumnName, GetTypeByMeta(meta.ColumnType));
            }
        }
        protected Type GetTypeByMeta(Constants.MetaType columnType)
        {
            return columnType switch
            {
                Constants.MetaType.INTEGER => typeof(Int32),
                Constants.MetaType.LONG => typeof(Int64),
                Constants.MetaType.DOUBLE => typeof(double),
                Constants.MetaType.FLOAT => typeof(float),
                Constants.MetaType.SHORT => typeof(short),
                Constants.MetaType.TIMESTAMP => typeof(DateTime),
                Constants.MetaType.BOOLEAN => typeof(Boolean),
                Constants.MetaType.DATE => typeof(DateTime),
                _ => typeof(string)
            };
        }
        protected void WrapRowsModel(List<FieldContent> fields, DataRow row, object model)
        {
            foreach (FieldContent content in fields)
            {
                if (!content.IfIncrement)
                {
                    row[content.FieldName] = content.GetMethod.Invoke(model, null);
                }
                else
                {
                    row[content.FieldName] = DBNull.Value;
                }
            }

        }
    }
}
