using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Serilog;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Frameset.Core.Dao
{
    public class DataMetaUtils
    {
        public static IList<ColumnMeta> GetTableColumns(DbConnection connection, string schema, string tableName)
        {
            IList<ColumnMeta> list = new List<ColumnMeta>();
            try
            {
                string[] resrtictions = new string[4];
                resrtictions[2] = tableName;
                resrtictions[0] = schema;

                DataTable tables = connection.GetSchema("Columns", resrtictions);
                if (!CollectionUtils.IsEmpty(tables.Rows))
                {
                    foreach (DataRow row in tables.Rows)
                    {
                        ColumnMeta meta = new ColumnMeta();
                        meta.ColumnName = row["COLUMN_NAME"].ToString();
                        meta.DataType = row["DATA_TYPE"].ToString();
                        meta.ColumnType = row["COLUMN_TYPE"].ToString();
                        meta.Nullable = !string.Equals("NO", row["IS_NULLABLE"].ToString(), StringComparison.OrdinalIgnoreCase);
                        if (row["EXTRA"] != DBNull.Value)
                        {
                            if (string.Equals("auto_increment", row["EXTRA"].ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                meta.IncrementTag = true;
                            }
                        }
                        if (row["COLUMN_KEY"] != DBNull.Value)
                        {
                            if (string.Equals("PRI", row["COLUMN_KEY"].ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                meta.PrimaryTag = true;
                            }
                        }
                        if (row["COLUMN_COMMENT"] != DBNull.Value)
                        {
                            meta.Comment = row["COLUMN_COMMENT"].ToString();
                        }
                        list.Add(meta);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }
            return list;
        }
        public static IList<TableMeta> GetTables(DbConnection connection, string schema)
        {
            IList<TableMeta> list = new List<TableMeta>();
            try
            {
                string[] resrtictions = new string[2];
                resrtictions[1] = schema;
                DataTable tables = connection.GetSchema("Tables", resrtictions);
                if (!CollectionUtils.IsEmpty(tables.Rows))
                {
                    foreach (DataRow row in tables.Rows)
                    {
                        TableMeta meta = new TableMeta();
                        meta.TableName = row["TABLE_NAME"].ToString();
                        if (row["TABLE_SCHEMA"] != DBNull.Value)
                        {
                            meta.Schema = row["TABLE_SCHEMA"].ToString();
                        }
                        if (row["TABLE_COMMENT"] != DBNull.Value)
                        {
                            meta.Remark = row["TABLE_COMMENT"].ToString();
                        }

                        list.Add(meta);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("{Message}", ex.Message);
            }
            return list;
        }
        public static Type GetValueType(Constants.MetaType type)
        {
            Type retType;
            switch (type)
            {
                case Constants.MetaType.SHORT:
                    retType = typeof(short);
                    break;
                case Constants.MetaType.INTEGER:
                    retType = typeof(int);
                    break;
                case Constants.MetaType.BIGINT:
                    retType = typeof(long);
                    break;
                case Constants.MetaType.DOUBLE:
                    retType = typeof(double);
                    break;
                case Constants.MetaType.FLOAT:
                    retType = typeof(float);
                    break;
                case Constants.MetaType.CLOB:
                case Constants.MetaType.STRING:
                    retType = typeof(string);
                    break;
                case Constants.MetaType.BLOB:
                    retType = typeof(byte[]);
                    break;
                case Constants.MetaType.TIMESTAMP:
                    retType = typeof(DateTime);
                    break;
                case Constants.MetaType.DATE:
                    retType = typeof(DateTime);
                    break;
                default:
                    retType = typeof(string);
                    break;
            }
            return retType;
        }
        public static Constants.MetaType GetMetaType(PropertyInfo info)
        {
            Constants.MetaType metaType = Constants.MetaType.STRING;
            switch (Type.GetTypeCode(info.GetGetMethod().ReturnType))
            {
                case TypeCode.Int32:
                    metaType = Constants.MetaType.INTEGER;
                    break;
                case TypeCode.Int16:
                    metaType = Constants.MetaType.SHORT;
                    break;
                case TypeCode.Int64:
                    metaType = Constants.MetaType.BIGINT;
                    break;
                case TypeCode.Decimal:
                    metaType = Constants.MetaType.FLOAT;
                    break;
                case TypeCode.Double:
                    metaType = Constants.MetaType.DOUBLE;
                    break;
                case TypeCode.DateTime:
                    metaType = Constants.MetaType.TIMESTAMP;
                    break;
                case TypeCode.Byte:
                    metaType = Constants.MetaType.BLOB;
                    break;
                case TypeCode.String:
                    metaType = Constants.MetaType.STRING;
                    break;
            }
            return metaType;
        }
    }

    public class ColumnMeta
    {
        public string ColumnName
        {
            get; set;
        }
        public string ColumnType
        {
            get; set;
        }
        public string DataType
        {
            get; set;
        }
        public bool PrimaryTag
        {
            get; set;
        }
        public bool IncrementTag
        {
            get; set;
        }
        public string SequnceName
        {
            get; set;
        }
        public bool Nullable
        {
            get; set;
        } = false;
        public string Comment
        {
            get; set;
        }
    }
    public class TableMeta
    {
        public string TableName
        {
            get; set;
        }
        public string Schema
        {
            get; set;
        }
        public string Remark
        {
            get; set;
        }
    }
}
