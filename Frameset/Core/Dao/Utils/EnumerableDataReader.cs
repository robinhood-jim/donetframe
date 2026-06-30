using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Frameset.Core.Dao.Utils
{
    public class EnumerableDataReader<T> : IDataReader
    {
        private List<DataSetColumnMeta> columnMetas = [];
        private DataTable dataTable;
        private IList<FieldContent> fields;
        private IEnumerator<T> records;
        private Type modelType;
        private bool usingMap = false;

        private Dictionary<string, MethodParam> methodMap = [];
        private T current;
        private bool active = true;
        protected AbstractSqlDialect sqlDialect;
        protected DbConnection connection;
        protected IJdbcDao dao;
        protected EntityContent entityContent;
        protected string[] memberNames = [];
        private string schema;
        private string tableName;
        private Dictionary<string, string> mappingField = [];

        public EnumerableDataReader(IJdbcDao dao, DbConnection connection, EntityContent entityContent, IList<FieldContent> fieldContents, IEnumerable<T> enums)
        {
            modelType = typeof(T);
            methodMap = AnnotationUtils.ReflectObject(modelType);
            fields = fieldContents;
            records = enums.GetEnumerator();
            this.dao = dao;
            this.connection = connection;
            this.entityContent = entityContent;
            sqlDialect = dao.GetDialect();
            memberNames = fields.Select(x => x.FieldName).ToArray();
            columnMetas.AddRange(fields.Select(x =>
            {
                DataSetColumnMeta meta = new(x.PropertyName, x.FieldName, x.DataType, null);
                meta.Increment = x.IfIncrement;
                meta.SequenceName = x.SequenceName;
                meta.Required = x.Required;
                meta.Scale = x.Scale;
                meta.Primary = x.IfPrimary;
                return meta;
            }));
            schema = entityContent.Schema.IsNullOrEmpty() ? dao.GetCurrentSchema() : entityContent.Schema;
            memberNames = columnMetas.Select(x => x.ColumnCode).ToArray();
            mappingField = fields.ToDictionary(x => x.FieldName, x => x.PropertyName);
            tableName = entityContent.TableName;
        }
        public EnumerableDataReader(IJdbcDao dao, DbConnection connection, List<DataSetColumnMeta> columnMetas, string schema, string tableName, IEnumerable<T> records)
        {
            modelType = typeof(T);
            usingMap = modelType.Equals(typeof(Dictionary<string, object>));
            if (!usingMap)
            {
                methodMap = AnnotationUtils.ReflectObject(modelType);
            }
            this.columnMetas = columnMetas;
            this.records = records.GetEnumerator();
            this.dao = dao;
            sqlDialect = dao.GetDialect();
            memberNames = columnMetas.Select(x => x.ColumnCode).ToArray();
            this.schema = schema;
            this.tableName = tableName;
            this.connection = connection;
            mappingField = columnMetas.ToDictionary(x => x.ColumnCode, x => x.ColumnName);
        }


        public object this[int i] => GetByPos(i);

        public object this[string name] => GetByName(name);

        public int Depth => 0;

        public bool IsClosed => current.Equals(default(T));

        public int RecordsAffected => 0;

        public int FieldCount => GetFieldCount();

        public void Close()
        {
            Shutdown();
        }
        private void Shutdown()
        {
            active = false;
            current = default;
            IDisposable disposable = records as IDisposable;
            records = null;
            disposable?.Dispose();
        }

        public void Dispose()
        {
            Shutdown();
        }

        public bool GetBoolean(int i)
        {
            return (bool)this[i];
        }

        public byte GetByte(int i)
        {
            return (byte)this[i];
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            byte[] array = (byte[])this[i];
            int num = array.Length - (int)fieldOffset;
            if (num <= 0)
            {
                return 0L;
            }

            int num2 = Math.Min(length, num);
            Buffer.BlockCopy(array, (int)fieldOffset, buffer, bufferoffset, num2);
            return num2;
        }

        public char GetChar(int i)
        {
            return (char)this[i];
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            string text = (string)this[i];
            int num = text.Length - (int)fieldoffset;
            if (num <= 0)
            {
                return 0L;
            }

            int num2 = Math.Min(length, num);
            text.CopyTo((int)fieldoffset, buffer, bufferoffset, num2);
            return num2;
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            Trace.Assert(i < columnMetas.Count, "pos overflow");
            Type targetType = GetTypeByMeta(columnMetas[i].ColumnType);
            return targetType.Name;
        }

        public DateTime GetDateTime(int i)
        {
            return (DateTime)this[i];
        }

        public decimal GetDecimal(int i)
        {
            return (decimal)this[i];
        }

        public double GetDouble(int i)
        {
            return (double)this[i];
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type GetFieldType(int pos)
        {
            Trace.Assert(pos < columnMetas.Count, "over max column size " + columnMetas.Count);
            return GetTypeByMeta(columnMetas[pos].ColumnType);
        }

        public float GetFloat(int i)
        {
            return (float)this[i];
        }

        public Guid GetGuid(int i)
        {
            return (Guid)this[i];
        }

        public short GetInt16(int i)
        {
            return (short)this[i];
        }

        public int GetInt32(int i)
        {
            return (int)this[i];
        }

        public long GetInt64(int i)
        {
            DataSetColumnMeta meta = columnMetas[i];
            if (sqlDialect.SupportAutoIncrement() && meta.Increment)
            {
                return sqlDialect.QueryIdentityByTable(dao, connection, schema, tableName);

            }
            else if (sqlDialect.SupportSequence() && !string.IsNullOrWhiteSpace(meta.SequenceName))
            {
                return sqlDialect.QuerySequenceValue(dao, connection, meta.SequenceName);
            }
            return (long)this[i];
        }

        public string GetName(int i)
        {
            return memberNames[i];
        }

        public int GetOrdinal(string name)
        {
            return Array.IndexOf(memberNames, name);
        }

        public DataTable GetSchemaTable()
        {
            if (dataTable == null)
            {
                dataTable = new DataTable();
                dataTable.Rows.Add("ColumnOrdinal", typeof(int));
                dataTable.Rows.Add("ColumnName", typeof(string));
                dataTable.Rows.Add("ColumnSize", typeof(int));
                dataTable.Rows.Add("DataType", typeof(int));
                dataTable.Rows.Add("AllowDBNull", typeof(bool));
                dataTable.Rows.Add("IsKey", typeof(bool));
                if (sqlDialect.SupportAutoIncrement())
                {
                    dataTable.Rows.Add("IsAutoIncrement", typeof(bool));
                    //dataTable.Rows.Add("AutoIncrementSeed", typeof(long));
                }

                for (int i = 0; i < columnMetas.Count; i++)
                {
                    DataSetColumnMeta content = columnMetas[i];
                    DataRow row = dataTable.Rows.Add();
                    row["ColumnOrdinal"] = i;
                    row["ColumnName"] = content.ColumnCode;
                    row["DataType"] = GetTypeByMeta(content.ColumnType);
                    row["ColumnSize"] = -1;
                    if (content.Primary)
                    {
                        row["IsKey"] = true;
                        row["AllowDBNull"] = false;
                    }
                    else
                    {
                        row["IsKey"] = false;
                        row["AllowDBNull"] = true;
                    }

                    if (sqlDialect.SupportAutoIncrement() && content.Increment)
                    {
                        row["IsAutoIncrement"] = true;

                    }
                    else
                    {
                        row["IsAutoIncrement"] = false;
                    }
                }
            }
            return dataTable;
        }

        public string GetString(int i)
        {
            return (string)this[i];
        }

        public object GetValue(int i)
        {
            return this[i];
        }

        public int GetValues(object[] values)
        {
            string[] array = memberNames;
            object target = current;

            int num = Math.Min(values.Length, array.Length);
            for (int i = 0; i < num; i++)
            {
                values[i] = this[i] ?? DBNull.Value;
            }

            return num;
        }

        public bool IsDBNull(int i)
        {
            return this[i] is null || this[i] is DBNull;
        }

        public bool NextResult()
        {
            active = false;
            return false;
        }

        public bool Read()
        {
            if (active)
            {

                if (records != null && records.MoveNext())
                {
                    current = records.Current;
                    return true;
                }
                active = false;
            }
            current = default;
            return false;
        }
        internal object GetByName(string name)
        {
            Trace.Assert(current != null, "");
            int pos = Array.IndexOf(memberNames, name);
            DataSetColumnMeta meta = columnMetas[pos];
            if (usingMap)
            {
                Dictionary<string, object> dict = current as Dictionary<string, object>;
                if (dict.TryGetValue(meta.ColumnName, out object value))
                {
                    return value;
                }
                return null;
            }
            else
            {
                if (!meta.Increment)
                {
                    if (sqlDialect.SupportSequence() && !string.IsNullOrWhiteSpace(meta.SequenceName))
                    {
                        return sqlDialect.QuerySequenceValue(dao, connection, meta.SequenceName);
                    }
                    else if (methodMap.TryGetValue(meta.ColumnName, out MethodParam param))
                    {
                        return param.GetMethod.Invoke(current, []);
                    }
                }
                return null;
            }

        }
        internal object GetByPos(int pos)
        {
            Trace.Assert(current != null, "");
            string sourceColumn = null;
            if (!fields.IsNullOrEmpty())
            {
                Trace.Assert(pos < fields.Count, "over max column size " + fields.Count);
                sourceColumn = fields[pos].FieldName;
            }
            else if (!columnMetas.IsNullOrEmpty())
            {
                Trace.Assert(pos < columnMetas.Count, "over max column size " + columnMetas.Count);
                sourceColumn = columnMetas[pos].ColumnCode;
            }
            if (sourceColumn != null)
            {
                return GetByName(sourceColumn);
            }
            return null;

        }
        protected Type GetTypeByMeta(Constants.MetaType columnType)
        {
            return columnType switch
            {
                Constants.MetaType.INTEGER => typeof(int),
                Constants.MetaType.LONG => typeof(long),
                Constants.MetaType.DOUBLE => typeof(double),
                Constants.MetaType.FLOAT => typeof(float),
                Constants.MetaType.SHORT => typeof(short),
                Constants.MetaType.TIMESTAMP => typeof(DateTime),
                Constants.MetaType.BOOLEAN => typeof(bool),
                Constants.MetaType.DATE => typeof(DateTime),
                _ => typeof(string)
            };
        }
        internal int GetFieldCount()
        {
            if (!fields.IsNullOrEmpty())
            {
                return fields.Count;
            }
            else if (!columnMetas.IsNullOrEmpty())
            {
                return columnMetas.Count;
            }
            return 0;
        }
    }
}
