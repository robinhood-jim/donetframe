using Frameset.Core.Common;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public class LocalFileSystem : AbstractFileSystem
    {
        static LocalFileSystem fileSystem = new LocalFileSystem(null);
        internal LocalFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.LOCAL;
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
                return GetOutputStremWithCompress(resourcePath, new FileStream(resourcePath, FileMode.CreateNew));
            }
            return GetOutputStremWithCompress(resourcePath, new FileStream(resourcePath, FileMode.Create));
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
                return new FileStream(resourcePath, FileMode.CreateNew);
            }
            return new FileStream(resourcePath, FileMode.Create);
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
