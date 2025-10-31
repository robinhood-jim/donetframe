using Frameset.Core.FileSystem;
using WebHDFS;

namespace Frameset.Common.FileSystem
{
    public class HDFSFileSystem : AbstractFileSystem
    {
        private WebHDFSClient client;

        public HDFSFileSystem(DataCollectionDefine define) : base(define)
        {
        }


        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override bool Exist(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override void FinishWrite(Stream outStream)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetInputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetOutputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetRawInputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetRawOutputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Tuple<Stream, StreamReader>? GetReader(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override long GetStreamSize(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Tuple<Stream, StreamWriter>? GetWriter(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override bool IsDirectory(string resourcePath)
        {
            throw new NotImplementedException();
        }
    }
}
