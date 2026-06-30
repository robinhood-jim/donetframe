using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using IBM.Data.Db2;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;

namespace Frameset.Core.Dao.Meta
{
    public class DB2Dialect : AbstractSqlDialect
    {
        public override string GenerateSequenceFunc(string sequenceName)
        {
            return "NEXT VALUE FOR " + sequenceName;
        }
        public override string AppendSequence(string sequenceName)
        {
            return ";SELECT NEXT VALUE FOR " + sequenceName + " FROM SYSIBM.SYSDUMMY1";
        }
        public override string GenerateSequenceQuery(string sequenceName)
        {
            return "SELECT NEXT VALUE FOR " + sequenceName + " FROM SYSIBM.SYSDUMMY1";
        }
        public override long QuerySequenceValue(IJdbcDao dao, DbConnection connection, string sequenceName)
        {
            string executeSql = GenerateSequenceQuery(sequenceName);
            using (DB2Command command = new DB2Command(executeSql, (DB2Connection)connection))
            {
                return Convert.ToInt64(command.ExecuteScalar().ToString().Trim());
            }
        }
        internal DB2Dialect()
        {
        }
        public override long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));
            long count = 0;
            using (DB2BulkCopy copy = new DB2BulkCopy((DB2Connection)connection, DB2BulkCopyOptions.Default))
            {
                copy.DestinationTableName = entityContent.TableName;
                var dataReader = new EnumerableDataReader<V>(dao, connection, entityContent, fields, models);
                copy.WriteToServer(dataReader);
                return count;
            }
        }
        public override DbConnection GetDbConnection(string connectStr)
        {
            return new DB2Connection(connectStr);
        }
        public override DbCommand GetDbCommand(DbConnection connection)
        {
            DB2Command command = new DB2Command();
            command.Connection = (DB2Connection)connection;
            return command;
        }
        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            DB2Command command = new DB2Command();
            command.Connection = (DB2Connection)connection;
            if (!sql.IsNullOrEmpty())
            {
                command.CommandText = sql;
            }
            return command;
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
