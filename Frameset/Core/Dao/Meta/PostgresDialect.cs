using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.FileSystem;
using Frameset.Core.Query;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;


namespace Frameset.Core.Dao.Meta
{
    public class PostgresDialect : AbstractSqlDialect
    {
        public override string GenerateSequenceFunc(string sequenceName)
        {
            return "nextval('" + sequenceName + "')";
        }
        public override string AppendSequence(string sequenceName)
        {
            return ";select nextval('" + sequenceName + "')";
        }
        public override string GenerateSequenceQuery(string sequenceName)
        {
            return "select nextval('" + sequenceName + "')";
        }
        public override string AppendAutoIncrement()
        {
            return "GENERATED ALWAYS AS IDENTITY";
        }
        public override string GetLongFormat(FieldContent content)
        {
            return "int8";
        }
        public override long QuerySequenceValue(IJdbcDao dao, DbConnection connection, string sequenceName)
        {
            string executeSql = GenerateSequenceQuery(sequenceName);
            using (NpgsqlCommand command = new NpgsqlCommand(executeSql, (NpgsqlConnection)connection))
            {
                return Convert.ToInt64(command.ExecuteScalar().ToString().Trim());
            }
        }
        public override long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            List<FieldContent> fields = (List<FieldContent>)EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));
            string copySql = GenerateCopySql(typeof(V));
            IEnumerable<FieldContent> selectFields = fields.TakeWhile(x => !string.IsNullOrWhiteSpace(x.SequenceName));
            FieldContent seqeunceField = !selectFields.IsNullOrEmpty() ? selectFields.First() : null;
            long seqCurrent = 0;
            if (seqeunceField != null)
            {
                seqCurrent = QuerySequenceValue(dao, connection, seqeunceField.SequenceName);
            }
            using var writer = ((NpgsqlConnection)connection).BeginBinaryImport(copySql);
            foreach (V model in models)
            {
                if (token.CanBeCanceled)
                {
                    if (writer != null)
                    {
                        writer.Dispose();
                    }
                    return -1;
                }
                writer.StartRow();
                foreach (FieldContent content in fields)
                {

                    if (!content.IfIncrement)
                    {
                        if (!string.IsNullOrWhiteSpace(content.SequenceName))
                        {
                            seqCurrent++;
                            writer.Write(seqCurrent, NpgsqlDbType.Bigint);
                        }
                        else
                        {
                            object value = content.GetMethod.Invoke(model, []);
                            if (value != null)
                            {
                                FillValue(writer, value, content.DataType);
                            }
                            else
                            {
                                Console.WriteLine("encount null at " + content.FieldName);
                                writer.WriteNull();
                            }
                        }
                    }

                }
            }
            long updateRows = (long)writer.Complete();
            //alter sequence after batch
            if (seqeunceField != null)
            {
                string updateSequcenSql = "ALTER SEQUENCE " + seqeunceField.SequenceName + " RESTART WITH " + seqCurrent;
                ExecuteNoQuery(connection, updateSequcenSql);
            }
            return updateRows;
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
        public override bool SupportSequence()
        {
            return true;
        }
        internal string GenerateCopySql(Type modelType)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(modelType);
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            StringBuilder builder = new();
            builder.Append("COPY ").Append(entityContent.GetTableName());
            StringBuilder fieldBuilder = new();
            foreach (FieldContent content in fields)
            {
                if (!content.IfIncrement)
                {
                    fieldBuilder.Append(content.FieldName).Append(",");
                }
            }
            fieldBuilder.Remove(fieldBuilder.Length - 1, 1);
            builder.Append("(").Append(fieldBuilder).Append(")  FROM STDIN BINARY");
            return builder.ToString();
        }
        internal string GenerateCopySql(string tableName, List<DataSetColumnMeta> metas)
        {

            StringBuilder builder = new();
            builder.Append("COPY ").Append(tableName);
            StringBuilder fieldBuilder = new();
            foreach (DataSetColumnMeta content in metas)
            {
                if (!content.Increment)
                {
                    fieldBuilder.Append(content.ColumnCode).Append(",");
                }
            }
            fieldBuilder.Remove(fieldBuilder.Length - 1, 1);
            builder.Append("(").Append(fieldBuilder).Append(")  FROM STDIN (FORMAT BINARY)");
            return builder.ToString();
        }
        internal void FillValue(NpgsqlBinaryImporter writer, object value, Constants.MetaType metaType)
        {
            switch (metaType)
            {
                case Constants.MetaType.LONG:
                    writer.Write(value, NpgsqlDbType.Bigint);
                    break;
                case Constants.MetaType.INTEGER:
                    writer.Write(value, NpgsqlDbType.Integer);
                    break;
                case Constants.MetaType.DOUBLE:
                    writer.Write(value, NpgsqlDbType.Double);
                    break;
                case Constants.MetaType.FLOAT:
                    writer.Write(value, NpgsqlDbType.Numeric);
                    break;
                case Constants.MetaType.DATE:
                    writer.Write(value, NpgsqlDbType.Date);
                    break;
                case Constants.MetaType.TIMESTAMP:
                    writer.Write(value, NpgsqlDbType.Timestamp);
                    break;
                case Constants.MetaType.STRING:
                    writer.Write(value, NpgsqlDbType.Varchar);
                    break;
                case Constants.MetaType.SHORT:
                    writer.Write(value, NpgsqlDbType.Smallint);
                    break;

            }

        }



    }
}
