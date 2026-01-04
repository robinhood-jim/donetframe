using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Query;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
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
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));

            DataTable table = new DataTable();

            MySqlTransaction transaction = ((MySqlConnection)connection).BeginTransaction();
            try
            {

                MySqlBulkCopy copy = new MySqlBulkCopy((MySqlConnection)connection, transaction);
                copy.DestinationTableName = entityContent.TableName;
                int columns = 0;
                foreach (FieldContent content in fields)
                {
                    if (!content.IfIncrement)
                    {
                        table.Columns.Add(content.FieldName, content.ParamType);
                        columns++;
                    }
                    else
                    {
                        DataColumn column = table.Columns.Add(content.FieldName, content.ParamType);
                        column.AutoIncrement = true;
                        column.AllowDBNull = true;
                        column.AutoIncrementSeed = QueryIdentityByTable(dao, connection, transaction, entityContent);
                    }
                }
                var enumRows = models.Select(model =>
                {
                    DataRow row = table.NewRow();
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
                    return row;
                });
                MySqlBulkCopyResult rs = copy.WriteToServerAsync(enumRows, columns, token).Result;
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
                case Constants.MetaType.BIGINT:
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
        public override long QueryIdentityByTable(IJdbcDao dao, DbConnection connection, DbTransaction transaction, EntityContent entityContent)
        {
            string schema = entityContent.Schema.IsNullOrEmpty() ? dao.GetCurrentSchema() : entityContent.Schema;
            StringBuilder builder = new StringBuilder("SELECT auto_increment FROM information_schema.TABLES WHERE TABLE_SCHEMA ='").Append(schema).Append("' and TABLE_NAME='").Append(entityContent.TableName).Append("'");
            using (MySqlCommand command = new MySqlCommand(builder.ToString(), (MySqlConnection)connection))
            {
                command.Transaction = (MySqlTransaction)transaction;
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
    }
}
