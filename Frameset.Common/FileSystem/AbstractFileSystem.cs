using Frameset.Common.Compress;
using Frameset.Common.Data;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

namespace Frameset.Common.FileSystem
{
    /// <summary>
    /// United Data File access FileSystem Abstract class 
    /// </summary>
    public abstract class AbstractFileSystem : IFileSystem, IDisposable
    {
        internal DataCollectionDefine define;
        internal Constants.FileSystemType identifier;
        internal bool busyTag = false;
        internal long count = 1;
        internal Encoding encoding = Encoding.UTF8;
        internal AbstractFileSystem(DataCollectionDefine define)
        {
            this.define = define;

        }

        internal Stream GetInputStreamWithCompress(string path, Stream inputStream)
        {
            return StreamDecoder.GetInputByCompressType(path, inputStream);
        }
        internal Stream GetOutputStremWithCompress(string path, Stream inputStrem)
        {
            return StreamEncoder.GetOutputByCompressType(path, inputStrem);
        }
        internal StreamReader GetReader(string path, Stream inputStream)
        {
            return new StreamReader(GetInputStreamWithCompress(path, inputStream), encoding);
        }

        public abstract void Dispose();
        public abstract bool Exist(string resourcePath);
        //public abstract void FinishWrite(Stream outStream);

        public Constants.FileSystemType GetIndentifier()
        {
            return identifier;
        }

        public abstract Stream? GetInputStream(string resourcePath);
        public abstract Stream? GetOutputStream(string resourcePath);
        public abstract Stream? GetRawInputStream(string resourcePath);
        public abstract Stream? GetRawOutputStream(string resourcePath);
        public virtual Tuple<Stream, StreamReader>? GetReader(string resourcePath)
        {
            Trace.Assert(!resourcePath.IsNullOrEmpty(), "path must not be null");
            Stream inputStream = GetInputStream(resourcePath);
            if (inputStream != null)
            {
                return Tuple.Create(inputStream, new StreamReader(inputStream, encoding));
            }
            else
            {
                throw new OperationFailedException("GetInputStream failed");
            }
        }
        public abstract long GetStreamSize(string resourcePath);
        public virtual Tuple<Stream, StreamWriter>? GetWriter(string resourcePath)
        {
            Trace.Assert(!resourcePath.IsNullOrEmpty(), "path must not be null");
            Stream outStream = GetOutputStream(resourcePath);
            if (outStream != null)
            {
                return Tuple.Create(outStream, new StreamWriter(outStream, encoding));
            }
            else
            {
                throw new OperationFailedException("GetOutputStream failed");
            }
        }
        public virtual void Init(DataCollectionDefine define)
        {
            this.define = define;
            string encodingStr;
            define.ResourceConfig.TryGetValue(ResourceConstants.STRINGENCODING, out encodingStr);
            if (!encodingStr.IsNullOrEmpty())
            {
                encoding = Encoding.GetEncoding(encodingStr);
            }
        }

        public abstract bool IsDirectory(string resourcePath);
        public virtual void FinishOperator()
        {

        }
        public virtual void FinishWrite(Stream outputStream, string path)
        {

        }
        public virtual void FinishWrite(Stream outputStream)
        {

        }
        internal void BeginOperator()
        {

            if (Interlocked.Read(ref count) == 0)
            {
                throw new ResourceInUsingException("exist another unfinished operator,wait!");
            }
            Interlocked.Decrement(ref count);

        }
    }
}
