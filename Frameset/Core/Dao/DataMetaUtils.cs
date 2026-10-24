using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Frameset.Core.Dao
{
    public class DataMetaUtils
    {
        public static List<ColumnMeta> GetTableColumns(DbConnection connection, string schema, string tableName)
        {
            List<ColumnMeta> list = [];
            try
            {
                string[] resrtictions = new string[4];
                resrtictions[2] = tableName;
                resrtictions[0] = schema;

                DataTable tables = connection.GetSchema("Columns", resrtictions);
                if (tables.Rows != null)
                {
                    foreach (DataRow row in tables.Rows)
                    {
                        ColumnMeta meta = new ColumnMeta();
                        string? columnName = row["COLUMN_NAME"].ToString();
                        if (!string.IsNullOrWhiteSpace(columnName))
                        {
                            meta.ColumnName = columnName;
                        }
                        string? dataType = row["DATA_TYPE"].ToString();
                        if (!string.IsNullOrWhiteSpace(dataType))
                        {
                            meta.DataType = dataType;
                        }
                        string? columnType = row["COLUMN_TYPE"].ToString();
                        if (!string.IsNullOrWhiteSpace(columnType))
                        {
                            meta.ColumnType = columnType;
                        }
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
                            string? comment = row["COLUMN_COMMENT"].ToString();
                            if (!string.IsNullOrWhiteSpace(comment))
                            {
                                meta.Comment = comment;
                            }
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
        public static List<TableMeta> GetTables(DbConnection connection, string schema)
        {
            List<TableMeta> list = [];
            try
            {
                string[] resrtictions = new string[2];
                resrtictions[1] = schema;
                DataTable tables = connection.GetSchema("Tables", resrtictions);
                if (tables.Rows != null)
                {
                    foreach (DataRow row in tables.Rows)
                    {
                        TableMeta meta = new TableMeta();
                        string? tableName = row["TABLE_NAME"].ToString();
                        if (!string.IsNullOrWhiteSpace(tableName))
                        {
                            meta.TableName = tableName;
                        }
                        if (row["TABLE_SCHEMA"] != DBNull.Value)
                        {
                            string? selectSchema = row["TABLE_SCHEMA"].ToString();
                            if (!string.IsNullOrWhiteSpace(selectSchema))
                            {
                                meta.Schema = selectSchema;
                            }
                        }
                        if (row["TABLE_COMMENT"] != DBNull.Value)
                        {
                            string? comment = row["TABLE_COMMENT"].ToString();
                            if (!string.IsNullOrWhiteSpace(comment))
                            {
                                meta.Remark = comment;
                            }
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
                case Constants.MetaType.LONG:
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
            switch (Type.GetTypeCode(info.GetGetMethod()?.ReturnType))
            {
                case TypeCode.Int32:
                    metaType = Constants.MetaType.INTEGER;
                    break;
                case TypeCode.Int16:
                    metaType = Constants.MetaType.SHORT;
                    break;
                case TypeCode.Int64:
                    metaType = Constants.MetaType.LONG;
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
        } = null!;
        public string ColumnType
        {
            get; set;
        } = null!;
        public string DataType
        {
            get; set;
        } = null!;
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
        } = null!;
        public bool Nullable
        {
            get; set;
        }
        public string Comment
        {
            get; set;
        } = null!;
    }
    public class TableMeta
    {
        public string TableName
        {
            get; set;
        } = null!;
        public string Schema
        {
            get; set;
        } = null!;
        public string Remark
        {
            get; set;
        } = null!;
    }
}
