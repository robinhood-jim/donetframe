using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Frameset.Core.Dao.Meta
{
    public class MssqlDialect : AbstractSqlDialect
    {
        internal MssqlDialect()
        {

        }

        public override string GenerateSequenceScript(string sequenceName)
        {
            throw new NotSupportedException();
        }
        public override string getAutoIncrementScript()
        {
            return " IDENTITY";
        }
        public override string AppendKeyHolder()
        {
            return ";SELECT CAST(scope_identity() AS int)";
        }
        public override int BatchInsert<V>(IJdbcDao dao, DbConnection connection, IList<V> models, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));
            using (SqlBulkCopy copy = new SqlBulkCopy((SqlConnection)connection))
            {
                copy.DestinationTableName = entityContent.TableName;
                DataTable table = new DataTable();
                foreach (FieldContent content in fields)
                {
                    if (!content.IfIncrement)
                    {
                        table.Columns.Add(content.FieldName, content.GetMethold.ReturnType);
                    }
                    else
                    {
                        DataColumn column = table.Columns.Add(content.FieldName, content.GetMethold.ReflectedType);
                        column.AutoIncrement = true;
                        column.AllowDBNull = true;
                    }
                }
                foreach (V model in models)
                {
                    DataRow row = table.NewRow();
                    foreach (FieldContent content in fields)
                    {
                        if (!content.IfIncrement)
                        {
                            row[content.FieldName] = content.GetMethold.Invoke(model, null);
                        }
                        else
                        {
                            row[content.FieldName] = DBNull.Value;
                        }
                    }
                }
                copy.WriteToServer(table);
                return models.Count;
            }
        }
        public override DbConnection GetDbConnection(string connectStr)
        {
            return new SqlConnection(connectStr);
        }
        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            return new SqlCommand(sql, (SqlConnection)connection);
        }
        public override DbParameter WrapParameter(int pos, object value)
        {
            return new SqlParameter("@" + Convert.ToString(pos), value);
        }
        public override DbParameter WrapParameter(string column, object value)
        {
            return new SqlParameter(column, value);
        }
        public override string GeneratePageSql(string baseSql, PageQuery query)
        {
            if (query != null && query.PageSize > 0)
            {
                string rawSql = baseSql.Trim();
                int pos = rawSql.ToLower().IndexOf("select");
                int pos1 = rawSql.ToLower().IndexOf("order");
                if (pos1 == -1)
                {
                    pos = rawSql.Length;
                }
                string sqlpart = rawSql.Substring(pos + 6, pos1);
                string orderFileStr = query.OrderField + " " + (query.OrderAsc ? " ASC" : "DESC");
                string orderStr = query.Order.IsNullOrEmpty() ? orderFileStr : query.Order;
                StringBuilder pagingSelect = new StringBuilder();
                pagingSelect.Append("select top " + query.PageSize + " * from (select row_number() over ( order by ").Append(orderStr + ") as rownum ").Append(sqlpart);
                pagingSelect.Append(" ) _row");
                pagingSelect.Append(" where rownum>" + (query.CurrentPage - 1) * query.PageSize);
                return pagingSelect.ToString();
            }
            else
            {
                return GetNoPageSql(baseSql, query);
            }
        }
    }
}
