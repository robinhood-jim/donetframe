using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.FileSystem
{
    public class LocalFileSystem : AbstractFileSystem
    {
        static LocalFileSystem fileSystem = new LocalFileSystem(null);
        internal LocalFileSystem(DataCollectionDefine define) : base(define)
        {

        }
        public static LocalFileSystem GetInstance()
        {
            return fileSystem;
        }

        public override void Dispose()
        {

        }

        public override bool Exist(string resourcePath)
        {
            return Path.Exists(resourcePath);
        }



        public override void FinishWrite(Stream outStream)
        {

        }

        public override Stream? GetInputStream(string resourcePath)
        {
            if (!File.Exists(resourcePath))
            {
                return null;
            }
            return GetInputStreamWithCompress(resourcePath, new FileStream(resourcePath, FileMode.Open));
        }
        public override Stream? GetOutputStream(string resourcePath)
        {
            if (!File.Exists(resourcePath))
            {
                return null;
            }
            return GetOutputStremWithCompress(resourcePath, new FileStream(resourcePath, FileMode.CreateNew));
        }
        public override Stream? GetRawInputStream(string resourcePath)
        {
            if (!File.Exists(resourcePath))
            {
                return null;
            }
            return new FileStream(resourcePath, FileMode.Open);
        }

        public override Stream? GetRawOutputStream(string resourcePath)
        {
            if (!File.Exists(resourcePath))
            {
                return null;
            }
            return new FileStream(resourcePath, FileMode.CreateNew);
        }

        public override Tuple<Stream, StreamReader>? GetReader(string resourcePath)
        {
            Trace.Assert(!resourcePath.IsNullOrEmpty(), "path must not be null");
            if (!File.Exists(resourcePath))
            {
                return null;
            }
            Stream stream = GetInputStream(resourcePath);
            return Tuple.Create(stream, new StreamReader(GetInputStream(resourcePath)));
        }

        public override long GetStreamSize(string resourcePath)
        {
            if (File.Exists(resourcePath))
            {
                FileInfo info = new FileInfo(resourcePath);
                return info.Length;
            }
            else
            {
                return -1;
            }
        }

        public override Tuple<Stream, StreamWriter> GetWriter(string resourcePath)
        {
            Trace.Assert(!resourcePath.IsNullOrEmpty(), "path must not be null");
            Stream outStream = GetOutputStream(resourcePath);
            return Tuple.Create(outStream, new StreamWriter(outStream));
        }

        public override void Init(DataCollectionDefine define)
        {
            base.Init(define);
        }

        public override bool IsDirectory(string resourcePath)
        {
            return Directory.Exists(resourcePath);
        }
    }
}
