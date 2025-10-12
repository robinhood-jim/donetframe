using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using IBM.Data.Db2;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Frameset.Core.Dao.Meta
{
    public class DB2Dialect : AbstractSqlDialect
    {
        public override string GenerateSequenceScript(string sequenceName)
        {
            return "NEXT VALUE FOR " + sequenceName;
        }
        public override string AppendSequence(string sequenceName)
        {
            return ";SELECT PREVIOUS FOR " + sequenceName + " FROM SYSIBM.SYSDUMMY1";
        }
        internal DB2Dialect()
        {
        }
        public override int BatchInsert<V>(IJdbcDao dao, DbConnection connection, IList<V> models, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));
            using (DB2BulkCopy copy = new DB2BulkCopy((DB2Connection)connection))
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
            return new DB2Connection(connectStr);
        }
        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            return new DB2Command(sql, (DB2Connection)connection);
        }
        public override DbParameter WrapParameter(int pos, object value)
        {
            return new DB2Parameter("@" + Convert.ToString(pos), value);
        }
        public override DbParameter WrapParameter(string column, object value)
        {
            return new DB2Parameter(column, value);
        }
        public override string GeneratePageSql(string baseSql, PageQuery query)
        {
            if (query != null && query.PageSize > 0)
            {
                Tuple<long, long> startEnds = GetStartEndRecord(query);
                StringBuilder pagingSelect = GetPageSqlByRowNumber(baseSql, query);
                pagingSelect.Append("where rownum <= ").Append(startEnds.Item2).Append(" and rownum > ").Append(startEnds.Item1).Append(" with ur");

                return pagingSelect.ToString();
            }
            else
            {
                return GetNoPageSql(baseSql, query);
            }
        }
    }
}
