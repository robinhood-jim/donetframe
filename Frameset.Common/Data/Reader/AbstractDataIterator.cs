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
        internal bool reUseCurrent = true;
        internal T current;
        public T Current => current;
        internal bool readAsDict = true;
        internal DateTimeFormatter? dateFormatter = null;
        internal DateTimeFormatter? timestampFormatter = null;
        public DataCollectionDefine MetaDefine
        {
            get; internal set;
        }

        object IEnumerator.Current => GetCurrent;
        public IFileSystem FileSystem
        {
            get; internal set;
        }
        private T GetCurrent()
        {
            return current != null ? current : System.Activator.CreateInstance<T>();
        }
        public Constants.FileFormatType Identifier
        {
            get; internal set;
        }
        internal Dictionary<string, object> CachedValue
        {
            get; set;
        } = [];
        internal Stream inputStream = null!;
        internal StreamReader reader = null!;
        internal bool useReader = false;
        internal bool useRawStream = false;
        readonly Dictionary<string, MethodParam> MethodMap = [];

        protected AbstractDataIterator(DataCollectionDefine define)
        {
            MetaDefine = define;
            string? reuseCurrentStr = null;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.REUSECURRENT, out reuseCurrentStr);
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

                MethodMap = AnnotationUtils.ReflectObject(typeof(T));
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
            MethodMap = AnnotationUtils.ReflectObject(typeof(T));
            current = System.Activator.CreateInstance<T>();
        }
        protected AbstractDataIterator(DataCollectionDefine define, IFileSystem fileSystem)
        {
            Trace.Assert(fileSystem != null);
            MetaDefine = define;
            FileSystem = fileSystem;
            string? reuseCurrentStr = null;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.REUSECURRENT, out reuseCurrentStr);
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
                MethodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            current = System.Activator.CreateInstance<T>();
        }

        public virtual void Initalize(string? filePath = null)
        {
            Trace.Assert(FileSystem != null);
            string processPath = filePath ?? MetaDefine.Path;
            Trace.Assert(!processPath.IsNullOrEmpty(), "path must not be null");
            string? dateFormatStr = null;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.INPUTDATEFORMATTER, out dateFormatStr);
            if (dateFormatStr.IsNullOrEmpty())
            {
                dateFormatStr = ResourceConstants.DEFAULTDATEFORMAT;
            }
            string? timestampFormatStr = null;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.INPUTTIMESTAMPFORMATTER, out timestampFormatStr);
            if (timestampFormatStr.IsNullOrEmpty())
            {
                timestampFormatStr = ResourceConstants.DEFAULTTIMESTAMPFORMAT;
            }
            dateFormatter = new DateTimeFormatter(dateFormatStr);
            timestampFormatter = new DateTimeFormatter(timestampFormatStr);
            if (useReader)
            {

                Tuple<Stream, StreamReader>? tuple = FileSystem.GetReader(processPath);
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
            if (current == null)
            {
                current = System.Activator.CreateInstance<T>();
            }
            if (readAsDict)
            {
                dynamic? tmp = Convert.ChangeType(current, typeof(Dictionary<string, object>));
                if (reUseCurrent)
                {
                    tmp?.Clear();
                }
                foreach (var item in CachedValue)
                {
                    tmp?.TryAdd(item.Key, item.Value);
                }
            }
            else
            {
                foreach (var item in CachedValue)
                {
                    MethodParam? param = null;
                    if (MethodMap.TryGetValue(item.Key, out param))
                    {
                        param?.SetMethod.Invoke(current, [ConvertUtil.ParseByType(param?.GetMethod.ReturnType, item.Value)]);
                    }
                    else
                    {
                        throw new OperationFailedException("param " + item.Key + " not exists!");
                    }

                }
            }
        }
        public abstract IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null);


        public void Reset()
        {
            throw new OperationNotAllowedException("reset not supported!");
        }
    }

}
