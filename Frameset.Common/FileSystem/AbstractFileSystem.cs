using Frameset.Common.Compress;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public abstract class AbstractFileSystem : IFileSystem, IDisposable
    {
        internal DataCollectionDefine define;
        internal string identifier;
        internal bool busyTag = false;
        internal AbstractFileSystem(DataCollectionDefine define)
        {
            this.define = define;

        }

        internal static Stream GetInputStreamWithCompress(string path, Stream inputStream)
        {
            return StreamDecoder.GetInputByCompressType(path, inputStream);
        }
        internal static Stream GetOutputStremWithCompress(string path, Stream inputStrem)
        {
            return StreamEncoder.GetOutputByCompressType(path, inputStrem);
        }

        public abstract void Dispose();
        public abstract bool Exist(string resourcePath);
        public abstract void FinishWrite(Stream outStream);

        public string GetIndentifier()
        {
            return identifier;
        }

        public abstract Stream? GetInputStream(string resourcePath);
        public abstract Stream? GetOutputStream(string resourcePath);
        public abstract Stream? GetRawInputStream(string resourcePath);
        public abstract Stream? GetRawOutputStream(string resourcePath);
        public abstract Tuple<Stream, StreamReader>? GetReader(string resourcePath);
        public abstract long GetStreamSize(string resourcePath);
        public abstract Tuple<Stream, StreamWriter>? GetWriter(string resourcePath);



        public virtual void Init(DataCollectionDefine define)
        {
            this.define = define;
        }

        public abstract bool IsDirectory(string resourcePath);
    }
}
