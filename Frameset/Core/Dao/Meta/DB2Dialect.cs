using FastMember;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using IBM.Data.Db2;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;

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
        public override long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));
            long count = 0;
            using (DB2BulkCopy copy = new DB2BulkCopy((DB2Connection)connection))
            {
                copy.DestinationTableName = entityContent.TableName;
                List<string> columnNames = new();
                foreach (FieldContent content in fields)
                {
                    if (!content.IfIncrement)
                    {
                        columnNames.Add(content.PropertyName);
                    }
                }
                var reader = ObjectReader.Create(models, columnNames.ToArray());
                copy.WriteToServer(reader);
                return count;
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
