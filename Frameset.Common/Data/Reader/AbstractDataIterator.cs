using Frameset.Common.FileSystem;
using Frameset.Core.Common;
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
        } = new Dictionary<string, object>();
        internal Stream inputStream;
        internal StreamReader reader;
        internal bool useReader = false;
        internal bool useRawStream = false;
        Dictionary<string, MethodParam> methodMap;

        public AbstractDataIterator(DataCollectionDefine define)
        {
            MetaDefine = define;
            string reuseCurrentStr;
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

                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            current = System.Activator.CreateInstance<T>();
        }
        public AbstractDataIterator(IFileSystem fileSystem, string processPath)
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
        public AbstractDataIterator(DataCollectionDefine define, IFileSystem fileSystem)
        {
            MetaDefine = define;
            FileSystem = fileSystem;
            string reuseCurrentStr;
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
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            current = System.Activator.CreateInstance<T>();
        }
        internal void InitDefaultFs()
        {
            if (FileSystem == null)
            {
                FileSystem = FileSystemFactory.GetFileSystem(MetaDefine);
            }
        }
        public virtual void initalize(string filePath = null)
        {
            InitDefaultFs();
            string processPath = filePath.IsNullOrEmpty() ? MetaDefine.Path : filePath;
            Trace.Assert(!processPath.IsNullOrEmpty(), "path must not be null");
            string dateFormatStr;
            string timestampFormatStr;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.INPUTDATEFORMATTER, out dateFormatStr);
            if (dateFormatStr.IsNullOrEmpty())
            {
                dateFormatStr = ResourceConstants.DEFAULTDATEFORMAT;
            }
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.INPUTTIMESTAMPFORMATTER, out timestampFormatStr);
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
        public virtual void Dispose()
        {
            if (reader != null)
            {
                reader.Dispose();
            }
            if (inputStream != null)
            {
                inputStream.Close();
            }
        }

        public virtual bool MoveNext()
        {
            if (current == null)
            {
                current = System.Activator.CreateInstance<T>();
            }
            else if (!reUseCurrent)
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
                    MethodParam param = null;
                    if (methodMap.TryGetValue(item.Key, out param))
                    {
                        param.SetMethod.Invoke(current, new object[] { ConvertUtil.ParseByType(param.GetMethod.ReturnType, item.Value) });
                    }
                }
            }
        }
        public abstract IAsyncEnumerable<T> ReadAsync(string path = null, string filterSql = null);


        internal bool filterRecord()
        {
            return true;
        }
        internal void parseSql(string filterSql)
        {

        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

}
