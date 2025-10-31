using Frameset.Core.Common;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public class FileSystemFactory
    {
        public static IFileSystem GetFileSystem(DataCollectionDefine define)
        {
            IFileSystem fileSystem = null;
            switch (define.FsType)
            {
                case Constants.FileSystemType.LOCAL:
                    fileSystem= LocalFileSystem.GetInstance();
                    break;
                case Constants.FileSystemType.FTP:
                    fileSystem = new FtpFileSystem(define);
                    break;
            }
            return fileSystem;
            
        }
    }
}
