using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using Spring.Globalization.Formatters;
using System.Collections;
using System.Diagnostics;


namespace Frameset.Common.Data.Reader
{
    /// <summary>
    /// United Data File reader IEnumerator
    /// </summary>
    /// <typeparam name="T">return Row type</typeparam>
    public abstract class AbstractDataIterator<T> : IEnumerator<T>
    {
        internal bool reUseCurrent = false;
        internal T current;
        public T Current => current;
        internal bool readAsDict = true;
        internal DateTimeFormatter dateFormatter;
        internal DateTimeFormatter timestampFormatter;
        public DataCollectionDefine MetaDefine
        {
            get; internal set;
        }

        object IEnumerator.Current => Current;
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
        } = [];
        internal Stream inputStream;
        internal StreamReader reader;
        internal bool useReader = false;
        internal bool useRawStream = false;
        Dictionary<string, MethodParam> methodMap;

        protected AbstractDataIterator(DataCollectionDefine define)
        {
            MetaDefine = define;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.REUSECURRENT, out string reuseCurrentStr);
            if (!reuseCurrentStr.IsNullOrEmpty())
            {
                reUseCurrent = string.Equals(bool.TrueString, reuseCurrentStr, StringComparison.OrdinalIgnoreCase);
            }
            FileSystem = FileSystemFactory.GetFileSystem(MetaDefine);
            if (!IsReturnDictionary())
            {
                readAsDict = false;
                if (!define.ColumnList.IsNullOrEmpty())
                {
                    define.ColumnList.Clear();
                }
                define.ParseType(typeof(T));

                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            current = System.Activator.CreateInstance<T>();
        }
        protected AbstractDataIterator(IFileSystem fileSystem, string processPath)
        {
            Trace.Assert(fileSystem != null);
            DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
            Trace.Assert(!typeof(T).Equals(typeof(Dictionary<string, object>)));
            builder.ParseType(typeof(T)).Path(processPath);
            MetaDefine = builder.Build();
            FileSystem = fileSystem;
            readAsDict = false;
            methodMap = AnnotationUtils.ReflectObject(typeof(T));
            current = System.Activator.CreateInstance<T>();
        }
        protected AbstractDataIterator(DataCollectionDefine define, IFileSystem fileSystem)
        {
            Trace.Assert(fileSystem != null);
            MetaDefine = define;
            FileSystem = fileSystem;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.REUSECURRENT, out string reuseCurrentStr);
            if (!reuseCurrentStr.IsNullOrEmpty())
            {
                reUseCurrent = string.Equals(bool.TrueString, reuseCurrentStr, StringComparison.OrdinalIgnoreCase);
            }
            if (!IsReturnDictionary())
            {
                readAsDict = false;
                if (!define.ColumnList.IsNullOrEmpty())
                {
                    define.ColumnList.Clear();
                }
                define.ParseType(typeof(T));
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            current = System.Activator.CreateInstance<T>();
        }

        public virtual void Initalize(string filePath = null)
        {
            Trace.Assert(FileSystem != null);
            string processPath = filePath.IsNullOrEmpty() ? MetaDefine.Path : filePath;
            Trace.Assert(!processPath.IsNullOrEmpty(), "path must not be null");
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.INPUTDATEFORMATTER, out string dateFormatStr);
            if (dateFormatStr.IsNullOrEmpty())
            {
                dateFormatStr = ResourceConstants.DEFAULTDATEFORMAT;
            }
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.INPUTTIMESTAMPFORMATTER, out string timestampFormatStr);
            if (timestampFormatStr.IsNullOrEmpty())
            {
                timestampFormatStr = ResourceConstants.DEFAULTTIMESTAMPFORMAT;
            }
            dateFormatter = new DateTimeFormatter(dateFormatStr);
            timestampFormatter = new DateTimeFormatter(timestampFormatStr);
            if (useReader)
            {

                Tuple<Stream, StreamReader> tuple = FileSystem.GetReader(processPath);
                if (tuple != null)
                {
                    inputStream = tuple.Item1;
                    reader = tuple.Item2;
                }
            }
            else
            {
                if (useRawStream)
                {
                    inputStream = FileSystem.GetRawInputStream(processPath);
                }
                else
                {
                    inputStream = FileSystem.GetInputStream(processPath);
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposable)
        {
            if (!disposable)
            {
                return;
            }
            reader?.Dispose();
            inputStream?.Close();

        }

        public virtual bool MoveNext()
        {
            if (current == null || !reUseCurrent)
            {
                current = System.Activator.CreateInstance<T>();
            }

            return false;
        }
        public bool IsReturnDictionary()
        {
            return typeof(T).Equals(typeof(Dictionary<string, object>));
        }
        internal void ConstructReturn()
        {
            if (readAsDict)
            {
                dynamic tmp = Convert.ChangeType(current, typeof(Dictionary<string, object>));
                if (reUseCurrent)
                {
                    tmp.Clear();
                }
                foreach (var item in cachedValue)
                {
                    tmp.TryAdd(item.Key, item.Value);
                }
            }
            else
            {
                foreach (var item in cachedValue)
                {
                    if (methodMap.TryGetValue(item.Key, out MethodParam param))
                    {
                        param.SetMethod.Invoke(current, [ConvertUtil.ParseByType(param.GetMethod.ReturnType, item.Value)]);
                    }
                }
            }
        }
        public abstract IAsyncEnumerable<T> ReadAsync(string path = null, string filterSql = null);


        public void Reset()
        {
            throw new OperationNotAllowedException("reset not supported!");
        }
    }

}
