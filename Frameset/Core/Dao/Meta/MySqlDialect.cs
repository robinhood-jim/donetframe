using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Query;
using MySqlConnector;
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
    public class MySqlDialect : AbstractSqlDialect
    {
        internal MySqlDialect()
        {

        }
        public override string AppendKeyHolder()
        {
            return ";SELECT LAST_INSERT_ID() as Id";
        }
        public override string AppendAutoIncrement()
        {
            return "AUTO INCREMENT";
        }
        public override long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            Type sourceType = typeof(V);
            bool isMap = sourceType.Equals(typeof(Dictionary<string, object>));
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));
            using MySqlTransaction transaction = ((MySqlConnection)connection).BeginTransaction();
            try
            {
                MySqlBulkCopy copy = new MySqlBulkCopy((MySqlConnection)connection, transaction);

                copy.DestinationTableName = entityContent.TableName;

                IDataReader dataReader = new EnumerableDataReader<V>(dao, connection, entityContent, fields, models);

                MySqlBulkCopyResult rs = copy.WriteToServerAsync(dataReader, token).Result;
                transaction.Commit();
                return rs.RowsInserted;

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new BaseSqlException(ex.Message);
            }
        }
        public override long BatchInsert(IJdbcDao dao, DbConnection connection, string schema, string tableName, List<DataSetColumnMeta> metas, IEnumerable<Dictionary<string, object>> models, CancellationToken token, int batchSize = 10000)
        {

            using MySqlTransaction transaction = ((MySqlConnection)connection).BeginTransaction();
            try
            {
                MySqlBulkCopy copy = new MySqlBulkCopy((MySqlConnection)connection, transaction);
                copy.DestinationTableName = tableName;

                IDataReader dataReader = new EnumerableDataReader<Dictionary<string, object>>(dao, connection, metas, schema, tableName, models);
                MySqlBulkCopyResult rs = copy.WriteToServerAsync(dataReader, token).Result;
                transaction.Commit();
                return rs.RowsInserted;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new BaseSqlException(ex.Message);
            }
        }

        internal MySqlDbType GetDbType(FieldContent content)
        {
            MySqlDbType type = MySqlDbType.VarChar;
            switch (content.DataType)
            {
                case Constants.MetaType.LONG:
                    type = MySqlDbType.Int64;
                    break;
                case Constants.MetaType.INTEGER:
                    type = MySqlDbType.Int32;
                    break;
                case Constants.MetaType.SHORT:
                    type = MySqlDbType.Int16;
                    break;
                case Constants.MetaType.FLOAT:
                    type = MySqlDbType.Float;
                    break;
                case Constants.MetaType.DOUBLE:
                    type = MySqlDbType.Decimal;
                    break;
                case Constants.MetaType.DATE:
                    type = MySqlDbType.DateTime;
                    break;
                case Constants.MetaType.TIMESTAMP:
                    type = MySqlDbType.Timestamp;
                    break;
                case Constants.MetaType.CLOB:
                    type = MySqlDbType.Text;
                    break;
                case Constants.MetaType.BLOB:
                    type = MySqlDbType.Binary;
                    break;

            }
            return type;
        }
        public override DbConnection GetDbConnection(string connectStr)
        {
            return new MySqlConnection(connectStr);
        }
        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            return new MySqlCommand(sql, (MySqlConnection)connection);
        }
        public override DbCommand GetDbCommand(DbConnection connection)
        {
            return new MySqlCommand()
            {
                Connection = (MySqlConnection)connection
            };
        }
        public override DbParameter WrapParameter(int pos, object value)
        {
            return new MySqlParameter("@" + Convert.ToString(pos), value);
        }
        public override DbParameter WrapParameter(string column, object value)
        {
            return new MySqlParameter(column, value);
        }
        public override long QueryIdentityByTable(IJdbcDao dao, DbConnection connection, string schema, string tableName)
        {
            StringBuilder builder = new StringBuilder("SELECT auto_increment FROM information_schema.TABLES WHERE TABLE_SCHEMA ='").Append(schema).Append("' and TABLE_NAME='").Append(tableName).Append("'");
            using (MySqlCommand command = new MySqlCommand(builder.ToString(), (MySqlConnection)connection))
            {
                return Convert.ToInt64(command.ExecuteScalar().ToString().Trim());
            }

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
        public Tuple<DbBatch, DbBatchCommand> WrapBatch(DbConnection connection, string batchSql)
        {
            DbBatch batch = new MySqlBatch((MySqlConnection)connection);
            DbBatchCommand command = new MySqlBatchCommand(batchSql);
            return Tuple.Create(batch, command);
        }
    }
}
