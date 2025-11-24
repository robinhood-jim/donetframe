using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Spring.Globalization.Formatters;
using System.Diagnostics;

namespace Frameset.Common.Data.Writer
{
    /// <summary>
    /// United Data File Writer
    /// </summary>
    /// <typeparam name="T"> write target Object Type</typeparam>
    public abstract class AbstractDataWriter<T> : IDisposable
    {
        public DataCollectionDefine MetaDefine
        {
            get; internal set;
        }

        public IFileSystem FileSystem
        {
            get; internal set;
        }
        public Constants.FileFormatType Identifier
        {
            get; internal set;
        }
        internal Dictionary<string, object> cachedValue
        {
            get; set;
        } = new Dictionary<string, object>();
        internal Stream outputStream;
        internal StreamWriter writer;
        internal bool useWriter = false;
        internal bool useRawOutputStream = false;
        internal bool useDictOutput = true;
        internal Dictionary<string, MethodParam> methodMap;

        internal DateTimeFormatter dateFormatter;
        internal DateTimeFormatter timestampFormatter;
        public void Dispose()
        {
            FinishWrite();
            if (writer != null)
            {
                if (outputStream.CanWrite)
                {
                    writer.Flush();
                }
                writer.Close();
            }
            if (outputStream != null)
            {
                if (FileSystem is HDFSFileSystem)
                {
                    FileSystem.FinishWrite(outputStream, MetaDefine.Path);
                }
                else
                {
                    FileSystem.FinishWrite(outputStream);
                }
                if (outputStream.CanWrite)
                {
                    outputStream.Close();
                }
            }
        }
        internal AbstractDataWriter(DataCollectionDefine define, IFileSystem fileSystem)
        {
            this.MetaDefine = define;
            this.FileSystem = fileSystem;
            if (!IsReturnDictionary())
            {
                if (!define.ColumnList.IsNullOrEmpty())
                {
                    define.ColumnList.Clear();
                }
                define.ParseType(typeof(T));
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            ConstructDateFormatter();
        }
        internal AbstractDataWriter(IFileSystem fileSystem, string processPath)
        {
            useDictOutput = false;
            Trace.Assert(fileSystem != null && !typeof(T).Equals(typeof(Dictionary<string, object>)));
            DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
            builder.ParseType(typeof(T)).Path(processPath);
            MetaDefine = builder.Build();
            methodMap = AnnotationUtils.ReflectObject(typeof(T));
            ConstructDateFormatter();
        }

        private void ConstructDateFormatter()
        {
            string dateFormatStr;
            string timestampFormatStr;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.OUTPUTDATEFORMATTER, out dateFormatStr);
            if (dateFormatStr.IsNullOrEmpty())
            {
                dateFormatStr = ResourceConstants.DEFAULTDATEFORMAT;
            }
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.OUTPUTTIMESTAMPFORMATTER, out timestampFormatStr);
            if (timestampFormatStr.IsNullOrEmpty())
            {
                timestampFormatStr = ResourceConstants.DEFAULTTIMESTAMPFORMAT;
            }
            dateFormatter = new DateTimeFormatter(dateFormatStr);
            timestampFormatter = new DateTimeFormatter(timestampFormatStr);
        }

        internal virtual void initalize()
        {
            Trace.Assert(!MetaDefine.Path.IsNullOrEmpty());
            if (outputStream == null)
            {
                if (!useWriter)
                {
                    if (useRawOutputStream)
                    {
                        outputStream = FileSystem.GetRawOutputStream(MetaDefine.Path);
                    }
                    else
                    {
                        outputStream = FileSystem.GetOutputStream(MetaDefine.Path);
                    }
                }
                else
                {
                    Tuple<Stream, StreamWriter> tuple = FileSystem.GetWriter(MetaDefine.Path);
                    outputStream = tuple.Item1;
                    writer = tuple.Item2;
                }
            }
        }
        internal object GetValue(T input, DataSetColumnMeta column)
        {
            object retValue = null;
            if (useDictOutput)
            {
                (input as Dictionary<string, object>).TryGetValue(column.ColumnCode, out retValue);
            }
            else
            {
                MethodParam param = null;
                methodMap.TryGetValue(column.ColumnCode, out param);
                if (param != null)
                {
                    retValue = param.GetMethod.Invoke(input, null);
                }
            }
            if (retValue != null)
            {
                if (column.ColumnType != Constants.MetaType.TIMESTAMP)
                {
                    retValue = ConvertUtil.ConvertStringToTargetObject(retValue, column, dateFormatter);
                }
                else
                {
                    retValue = ConvertUtil.ConvertStringToTargetObject(retValue, column, timestampFormatter);
                }
                if (column.ColumnType == Constants.MetaType.TIMESTAMP || column.ColumnType == Constants.MetaType.DATE)
                {
                    retValue = ((DateTime)retValue).ToUniversalTime();
                }
            }

            return retValue;
        }
        internal string GetOutputString(DataSetColumnMeta meta, object value)
        {
            if (value != null)
            {
                if (meta.ColumnType == Constants.MetaType.TIMESTAMP || meta.ColumnType == Constants.MetaType.DATE)
                {
                    if (value is DateTime || value is DateTimeOffset)
                    {
                        if (meta.ColumnType == Constants.MetaType.DATE)
                        {
                            return dateFormatter.Format(value);
                        }
                        else
                        {
                            return timestampFormatter.Format(value);
                        }
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
                else
                {
                    return value.ToString();
                }
            }
            else
            {
                return "";
            }
        }
        internal CompressType GetCompressType()
        {
            FileMeta meta = FileUtil.Parse(MetaDefine.Path, '/');
            if (meta != null)
            {
                return meta.CompressCodec;
            }
            else
            {
                return CompressType.NONE;
            }
        }
        public abstract void FinishWrite();
        public abstract void WriteRecord(T value);
        public virtual void Flush()
        {
            if (writer != null)
            {
                writer.Flush();
            }
            outputStream.Flush();
        }
        public bool IsReturnDictionary()
        {
            useDictOutput = typeof(T).Equals(typeof(Dictionary<string, object>));
            return useDictOutput;
        }
        internal object GetOutput(DataSetColumnMeta column, object input)
        {
            if (input != null)
            {
                if (column.ColumnType == Constants.MetaType.DATE)
                {
                    if (input is DateTime || input is DateTimeOffset)
                    {
                        return dateFormatter.Format(input);
                    }
                    return input;
                }
                else if (column.ColumnType == Constants.MetaType.TIMESTAMP)
                {
                    if (input is DateTime || input is DateTimeOffset)
                    {
                        return timestampFormatter.Format(input);
                    }
                    return input;
                }
            }
            else
            {
                return input;
            }
            return input;

        }
    }

}
