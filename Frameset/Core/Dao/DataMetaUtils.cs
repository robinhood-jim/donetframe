using Spring.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

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
                        meta.ColumnType=row["COLUMN_TYPE"].ToString();
                        meta.Nullable = !string.Equals("NO",row["IS_NULLABLE"].ToString(),StringComparison.OrdinalIgnoreCase);
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

            }
            return list;
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
