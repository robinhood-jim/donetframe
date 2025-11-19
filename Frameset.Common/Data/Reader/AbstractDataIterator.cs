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
    public abstract class AbstractDataIterator<T> : IEnumerator<T>
    {
        internal bool reUseCurrent = false;
        internal T current;
        public T Current => current;
        internal DateTimeFormatter dateFormat;
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
            MetaDefine.ResourceConfig.TryGetValue("fs.reUseCurrent", out reuseCurrentStr);
            if (!reuseCurrentStr.IsNullOrEmpty())
            {
                reUseCurrent = string.Equals(bool.TrueString, reuseCurrentStr, StringComparison.OrdinalIgnoreCase);
            }

            if (!IsReturnDictionary())
            {
                readAsDict = false;
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            current = System.Activator.CreateInstance<T>();
        }
        public AbstractDataIterator(DataCollectionDefine define, IFileSystem fileSystem)
        {
            MetaDefine = define;
            FileSystem = fileSystem;
            string reuseCurrentStr;
            MetaDefine.ResourceConfig.TryGetValue("fs.reUseCurrent", out reuseCurrentStr);
            if (!reuseCurrentStr.IsNullOrEmpty())
            {
                reUseCurrent = string.Equals(bool.TrueString, reuseCurrentStr, StringComparison.OrdinalIgnoreCase);
            }
            if (!IsReturnDictionary())
            {
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
            current = System.Activator.CreateInstance<T>();
        }
        internal void InitDefaultFs()
        {
            if (FileSystem == null)
            {
                switch (MetaDefine.FsType)
                {
                    case Constants.FileSystemType.LOCAL:
                        FileSystem = LocalFileSystem.GetInstance();
                        break;
                    case Constants.FileSystemType.FTP:
                        FileSystem = new FtpFileSystem(MetaDefine);
                        break;

                }
            }
        }
        public virtual void initalize(string filePath = null)
        {
            InitDefaultFs();
            string processPath = filePath.IsNullOrEmpty() ? MetaDefine.Path : filePath;
            Trace.Assert(!processPath.IsNullOrEmpty(), "path must not be null");
            string dateFormatStr;
            string timestampFormatStr;
            MetaDefine.ResourceConfig.TryGetValue("output.dateFormat", out dateFormatStr);
            if (dateFormatStr.IsNullOrEmpty())
            {
                dateFormatStr = "yyyy-MM-dd";
            }
            MetaDefine.ResourceConfig.TryGetValue("output.timestampFormat", out timestampFormatStr);
            if (timestampFormatStr.IsNullOrEmpty())
            {
                timestampFormatStr = "yyyy-MM-dd HH:mm:ss";
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
                        param.SetMethod.Invoke(current, new object[] { ConvertUtil.parseByType(param.SetMethod.ReturnType, item.Value) });
                    }
                }
            }
        }
        public abstract IAsyncEnumerable<T> ReadAsync(string path = null, string filterSql = null);

        //internal abstract bool GoNext();

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
