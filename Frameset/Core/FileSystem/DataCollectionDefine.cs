using Frameset.Core.Common;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace Frameset.Core.FileSystem
{
    public class DataCollectionDefine
    {
        public string Split
        {
            get; internal set;
        } = ",";
        public string Encode
        {
            get; internal set;
        } = "UTF-8";
        public IList<DataSetColumnMeta> ColumnList
        {
            get; internal set;
        } = new List<DataSetColumnMeta>();
        public string Path
        {
            get; internal set;
        }
        public Dictionary<string, string> ResourceConfig
        {
            get;

        } = new Dictionary<string, string>();
        public string ValueClassName
        {
            get; internal set;
        } = "ValueObject";
        public string ClassNamespace
        {
            get; internal set;
        } = "Frameset.avro.vo";
        public string PrimaryKeys
        {
            get; internal set;
        } = "";
        public Dictionary<string, int> ColumnNameMap
        {
            get; internal set;
        } = new Dictionary<string, int>();
        internal string defaultTimestampFormat = "yyyy-MM-dd HH:mm:ss";
        public string ResType
        {
            get; internal set;
        }
        public Constants.FileSystemType FsType
        {
            get; internal set;
        } = Constants.FileSystemType.LOCAL;
        public long DbSourceId
        {
            get; internal set;
        }
        public Constants.ResourceType SourceType
        {
            get; internal set;
        }
        public Constants.FileFormatType FileFormat
        {
            get; internal set;
        }
        internal DataCollectionDefine()
        {

        }
        public void AddColumnDefine(string columnName, Constants.MetaType columnType, object defaultValue)
        {
            ColumnList.Add(new DataSetColumnMeta(columnName, columnType, defaultValue));
            ColumnNameMap.TryAdd(columnName, 1);
        }
        public void AddColumnDefine(string columnName, Constants.MetaType columnType, bool flushOut)
        {
            ColumnList.Add(new DataSetColumnMeta(columnName, columnType, flushOut));
            ColumnNameMap.TryAdd(columnName, 1);
        }
        public void AddNotNullColumnDefine(string columnName, Constants.MetaType columnType, object defaultValue)
        {
            DataSetColumnMeta meta = new DataSetColumnMeta(columnName, columnType, defaultValue);
            meta.Required = true;
            ColumnList.Add(meta);
            ColumnNameMap.TryAdd(columnName, 1);
        }
        public void AddColumnDefine(string columnName, Constants.MetaType columnType, object defaultValue, bool required, string dateFormat)
        {
            ColumnList.Add(new DataSetColumnMeta(columnName, columnType, defaultValue, required, dateFormat));
            ColumnNameMap.TryAdd(columnName, 1);
        }
        public void AddColumnDefine(DataSetColumnMeta meta)
        {
            ColumnList.Add(meta);
            ColumnNameMap.TryAdd(meta.ColumnName, 1);
        }

    }
    public class DataCollectionBuilder
    {
        private DataCollectionDefine define = new DataCollectionDefine();
        internal DataCollectionBuilder()
        {

        }
        public static DataCollectionBuilder NewBuilder()
        {
            return new DataCollectionBuilder();
        }
        public DataCollectionBuilder AddColumnDefine(string columnName, Constants.MetaType columnType, object defaultValue = null)
        {
            define.AddColumnDefine(columnName, columnType, defaultValue);
            return this;
        }
        public DataCollectionBuilder AddColumnDefine(string columnName, Constants.MetaType columnType, bool flushOut)
        {
            define.AddColumnDefine(columnName, columnType, flushOut);
            return this;
        }
        public DataCollectionBuilder FsType(string fsType)
        {
            define.FsType = Constants.FsTypeOf(fsType);
            return this;
        }
        public DataCollectionBuilder ResourceType(string resourceType)
        {
            define.SourceType = Constants.ResTypeOf(resourceType);
            return this;
        }
        public DataCollectionBuilder FileFormat(string format)
        {
            define.FileFormat = Constants.FileFormatTypeOf(format);
            return this;
        }
        public DataCollectionBuilder AddConfig(string name, string value)
        {
            define.ResourceConfig.TryAdd(name, value);
            return this;
        }
        public DataCollectionBuilder WithConfig(Dictionary<string, string> config)
        {
            if (!config.IsNullOrEmpty())
            {
                foreach (var item in config)
                {
                    define.ResourceConfig.TryAdd(item.Key, item.Value);
                }
            }
            return this;
        }
        public DataCollectionBuilder Encode(string encode)
        {
            define.Encode = encode;
            return this;
        }
        public DataCollectionDefine Build()
        {
            return define;
        }

    }
    public class DataSetColumnMeta
    {
        public string ColumnName
        {
            get; internal set;
        }
        public string ColumnCode
        {
            get; internal set;
        }
        public Constants.MetaType ColumnType
        {
            get; internal set;
        }
        public object DefaultValue
        {
            get; internal set;
        }
        public string DateFormat
        {
            get; internal set;
        }
        public bool Required
        {
            get; internal set;
        } = false;
        public bool AlgrithColumn
        {
            get; internal set;
        } = false;
        public bool Primary
        {
            get; internal set;
        }
        public string AlgrithOper { get; internal set; }
        public int Preciseg { get; internal set; }
        public int Scale { get; internal set; }
        public bool Increment { get; internal set; }
        public int Length { get; internal set; }
        public IList<string> nominalValues
        {
            get; internal set;
        } = new List<string>();
        public bool FlushOut
        {
            get; internal set;
        } = true;
        public DataSetColumnMeta(string columnName, Constants.MetaType columnType, object defaultNullValue)
        {
            this.ColumnName = columnName;
            this.ColumnCode = columnName;
            this.ColumnType = columnType;
            if (defaultNullValue != null)
            {
                this.DefaultValue = defaultNullValue;
            }

        }
        public DataSetColumnMeta(string columnName, string columnCode, Constants.MetaType columnType, object defaultNullValue) : this(columnName, columnType, defaultNullValue)
        {
            this.ColumnCode = columnCode;
        }
        public DataSetColumnMeta(string columnName, Constants.MetaType columnType) : this(columnName, columnType, null)
        {

        }
        public DataSetColumnMeta(string columnName, Constants.MetaType columnType, string algrithOper, bool flushOut) : this(columnName, columnType, null)
        {
            this.AlgrithOper = algrithOper;
            this.FlushOut = flushOut;
        }
        public DataSetColumnMeta(string columnName, Constants.MetaType columnType, bool flushOut) : this(columnName, columnType, null)
        {
            this.FlushOut = flushOut;
        }
        public DataSetColumnMeta(string columnName, Constants.MetaType columnType, object defaultNullValue, bool required, string dateFormat) : this(columnName, columnType, defaultNullValue)
        {
            this.Required = required;
            this.DateFormat = dateFormat;
        }
        public DataSetColumnMeta(string columnName, Constants.MetaType columnType, object defaultNullValue, bool required) : this(columnName, columnType, defaultNullValue)
        {
            this.Required = required;
        }
    }
}
