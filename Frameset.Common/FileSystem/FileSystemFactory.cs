using Frameset.Common.FileSystem.CloudStorage;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public class FileSystemFactory
    {
        public static IFileSystem GetFileSystem(DataCollectionDefine define)
        {
            IFileSystem fileSystem = LocalFileSystem.GetInstance();
            switch (define.FsType)
            {
                case Constants.FileSystemType.LOCAL:
                    fileSystem = LocalFileSystem.GetInstance();
                    break;
                case Constants.FileSystemType.FTP:
                    fileSystem = new FtpFileSystem(define);
                    break;
                case Constants.FileSystemType.HDFS:
                    fileSystem = new HDFSFileSystem(define);
                    break;
                case Constants.FileSystemType.SFTP:
                    fileSystem = new SftpFileSystem(define);
                    break;
                case Constants.FileSystemType.MINIO:
                    fileSystem = new MinioFileSystem(define);
                    break;
                case Constants.FileSystemType.S3:
                    fileSystem = new AmazonS3FileSystem(define);
                    break;
            }
            return fileSystem;

        }
    }
}
