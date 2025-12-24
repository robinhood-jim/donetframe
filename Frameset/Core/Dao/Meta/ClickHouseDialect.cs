using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;
using ClickHouse.Client.Copy;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;

namespace Frameset.Core.Dao.Meta
{
    public class ClickHouseDialect : AbstractSqlDialect
    {
        public ClickHouseDialect()
        {
        }

        public override string GeneratePageSql(string baseSql, PageQuery query)
        {
            if (query != null && query.PageSize != 0)
            {
                Tuple<long, long> startEnd = GetStartEndRecord(query);
                StringBuilder builder = new StringBuilder(baseSql.Trim());
                long records = startEnd.Item2 - startEnd.Item1;
                builder.Append(" limit " + startEnd.Item1 + "," + startEnd.Item2);
                if (Log.IsEnabled(LogEventLevel.Debug))
                {
                    Log.Debug("page sql=" + builder);
                }

                return builder.ToString();
            }
            else
            {
                return GetNoPageSql(baseSql, query);
            }
        }
        public override long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            long flushRow = 0;
            using (var bulkCopy = new ClickHouseBulkCopy((ClickHouseConnection)connection))
            {

                var enumObjs = models.Select(x =>
                {
                    List<object> columns = new();
                    foreach (FieldContent content in fields)
                    {
                        if (!content.IfIncrement)
                        {
                            columns.Add(content.GetMethod.Invoke(x, null));
                        }
                        else
                        {
                            columns.Add(null);
                        }
                    }
                    return columns.ToArray();
                });
                bulkCopy.BatchSize = batchSize;
                bulkCopy.WriteToServerAsync(enumObjs, token).RunSynchronously();
                flushRow = bulkCopy.RowsWritten;
            }
            return flushRow;
        }

        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            ClickHouseCommand command = new ClickHouseCommand((ClickHouseConnection)connection);
            if (!sql.IsNullOrEmpty())
            {
                command.CommandText = sql;
            }
            return command;
        }


        public override DbConnection GetDbConnection(string connectStr)
        {
            return new ClickHouseConnection(connectStr);
        }

        public override DbParameter WrapParameter(int pos, object value)
        {
            ClickHouseDbParameter parameter = new ClickHouseDbParameter();
            parameter.ParameterName = "@" + Convert.ToString(pos);
            parameter.Value = value;
            return parameter;
        }

        public override DbParameter WrapParameter(string column, object value)
        {
            ClickHouseDbParameter parameter = new ClickHouseDbParameter();
            parameter.ParameterName = column;
            parameter.Value = value;
            return parameter;
        }
    }
}
