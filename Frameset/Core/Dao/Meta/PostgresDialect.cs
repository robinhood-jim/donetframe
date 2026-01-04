using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;


namespace Frameset.Core.Dao.Meta
{
    public class PostgresDialect : AbstractSqlDialect
    {
        public override string GenerateSequenceScript(string sequenceName)
        {
            return "nextval('" + sequenceName + "')";
        }
        public override string AppendSequence(string sequenceName)
        {
            return ";select currval('" + sequenceName + "')";
        }
        public override long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));
            string insertSql = SqlUtils.GetInsertBactchSql(dao, typeof(V));
            using (NpgsqlBatch batch = new NpgsqlBatch((NpgsqlConnection)connection))
            {
                NpgsqlBatchCommand command = new NpgsqlBatchCommand(insertSql);
                foreach (V model in models)
                {
                    foreach (FieldContent content in fields)
                    {
                        if (!content.IfIncrement && !content.IfSequence)
                        {
                            command.Parameters.Add(new NpgsqlParameter("?" + content.FieldName, content.GetMethod.Invoke(model, null)));
                        }
                    }
                    batch.BatchCommands.Add(command);
                }
                return batch.ExecuteNonQuery();
            }
        }
        public override DbConnection GetDbConnection(string connectStr)
        {
            return new NpgsqlConnection(connectStr);
        }
        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            return new NpgsqlCommand(sql, (NpgsqlConnection)connection);
        }
        public override DbCommand GetDbCommand(DbConnection connection)
        {
            return new NpgsqlCommand()
            {
                Connection = (NpgsqlConnection)connection
            };
        }
        public override DbParameter WrapParameter(int pos, object value)
        {
            return new NpgsqlParameter("@" + Convert.ToString(pos), value);
        }
        public override DbParameter WrapParameter(string column, object value)
        {
            return new NpgsqlParameter(column, value);
        }
        public override string GeneratePageSql(string baseSql, PageQuery query)
        {
            if (query != null && query.PageSize != 0)
            {
                Tuple<long, long> startEnd = GetStartEndRecord(query);
                StringBuilder builder = new StringBuilder(baseSql.Trim());
                long records = startEnd.Item2 - startEnd.Item1;
                builder.Append(" limit " + startEnd.Item2 + " offset " + startEnd.Item1);
                return builder.ToString();
            }
            else
            {
                return GetNoPageSql(baseSql, query);
            }
        }
    }
}
