using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using System.Collections;
using System.Diagnostics;

namespace Frameset.Common.Data.Reader
{
    public abstract class AbstractIterator<T> : IEnumerator<T>
    {
        internal bool reUseCurrent = false;
        internal T current;
        public T Current => current;
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
        Dictionary<string, MethodParam> methodMap;

        public AbstractIterator(DataCollectionDefine define)
        {
            MetaDefine = define;
            if (!IsReturnDictionary())
            {
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }

        }
        public AbstractIterator(DataCollectionDefine define, IFileSystem fileSystem)
        {
            MetaDefine = define;
            FileSystem = fileSystem;
            if (!IsReturnDictionary())
            {
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
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
        public virtual void BeforeProcess()
        {
            InitDefaultFs();
            Trace.Assert(!MetaDefine.Path.IsNullOrEmpty(), "path must not be null");
            if (useReader)
            {
                Tuple<Stream, StreamReader> tuple = FileSystem.GetReader(MetaDefine.Path);
                if (tuple != null)
                {
                    inputStream = tuple.Item1;
                    reader = tuple.Item2;
                }
            }
            else
            {
                inputStream = FileSystem.GetInputStream(MetaDefine.Path);
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
            if (reUseCurrent)
            {
                current = System.Activator.CreateInstance<T>();
            }
            return true;
        }
        public bool IsReturnDictionary()
        {
            return typeof(T).Equals(typeof(Dictionary<string, object>));
        }
        internal void ConstructReturn()
        {
            if (IsReturnDictionary())
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
        public abstract IAsyncEnumerable<T> QueryAsync(IFileSystem fileSystem, string path = null);
        public abstract void Close();

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
