using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;


namespace Frameset.Core.Dao.Meta
{
    public class OracleDialect : AbstractSqlDialect
    {
        internal OracleDialect()
        {

        }
        public override string GenerateSequenceScript(string sequenceName)
        {
            return sequenceName + ".nexval";
        }
        public override string AppendSequence(string sequenceName)
        {
            return ";selece " + sequenceName + ".CURRVAL from dual";
        }
        public override int BatchInsert<V>(IJdbcDao dao, DbConnection connection, IList<V> models, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));

            using (OracleBulkCopy copy = new OracleBulkCopy((OracleConnection)connection))
            {
                copy.DestinationSchemaName = entityContent.Schema;
                copy.DestinationTableName = entityContent.TableName;
                DataTable table = new DataTable();
                foreach (FieldContent content in fields)
                {
                    if (!content.IfIncrement)
                    {
                        table.Columns.Add(content.FieldName, content.GetMethold.ReturnType);
                    }
                    else if (content.IfIncrement)
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
            return new OracleConnection(connectStr);
        }
        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            return new OracleCommand(sql, (OracleConnection)connection);
        }
        public override DbParameter WrapParameter(int pos, object value)
        {
            return new OracleParameter("@" + Convert.ToString(pos), value);
        }
        public override DbParameter WrapParameter(string column, object value)
        {
            return new OracleParameter(column, value);
        }
        public override string GeneratePageSql(string baseSql, PageQuery query)
        {
            if (query != null && query.PageSize > 0)
            {
                bool forUpdate = baseSql.ToLower().EndsWith("for update");
                Tuple<long, long> startEnds = GetStartEndRecord(query);
                StringBuilder pagingSelect = new StringBuilder();
                string rawsql = baseSql.Trim();
                bool hasOffset = startEnds.Item1 > 0;
                if (forUpdate)
                {
                    rawsql = rawsql.Substring(0, rawsql.Length - 11);
                }
                if (hasOffset)
                {
                    pagingSelect.Append("select * from ( select row_.*, rownum rownum_ from ( ");
                }
                else
                {
                    pagingSelect.Append("select * from ( ");
                }
                pagingSelect.Append(rawsql);
                long tonums = startEnds.Item2;
                if (hasOffset)
                {
                    pagingSelect.Append(" ) row_ ) where rownum_ <= ").Append(tonums).Append(" and rownum_ > ").Append(startEnds.Item1);
                }
                else
                {
                    pagingSelect.Append(" ) where rownum <= ").Append(query.PageSize);
                }
                if (forUpdate)
                {
                    pagingSelect.Append(" for update");
                }
                return pagingSelect.ToString();
            }
            else
            {
                return GetNoPageSql(baseSql, query);
            }
        }
    }
}
