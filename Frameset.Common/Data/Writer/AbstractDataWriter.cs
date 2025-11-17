using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Spring.Globalization.Formatters;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Frameset.Common.Data.Writer
{
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
        internal DateTimeFormatter dateFormat ;
        public void Dispose()
        {
            FinishWrite();
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
            }
            if (outputStream != null)
            {
                FileSystem.FinishWrite(outputStream);
                outputStream.Close();
            }
        }
        internal AbstractDataWriter(DataCollectionDefine define, IFileSystem fileSystem)
        {
            this.MetaDefine = define;
            this.FileSystem = fileSystem;
            if (!IsReturnDictionary())
            {
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
                useDictOutput = false;
            }
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
            return retValue;
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
        public virtual void flush()
        {
            if (writer != null)
            {
                writer.Flush();
            }
            outputStream.Flush();
        }
        public bool IsReturnDictionary()
        {
            return typeof(T).Equals(typeof(Dictionary<string, object>));
        }
    }
}
