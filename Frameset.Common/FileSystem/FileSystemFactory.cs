using Frameset.Common.FileSystem.CloudStorage;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using System.Diagnostics;

namespace Frameset.Common.FileSystem
{
    public static class FileSystemFactory
    {
        public static IFileSystem GetFileSystem(DataCollectionDefine define)
        {
            Trace.Assert(define != null, "DataCollectionDefine is null");
            return define.FsType switch
            {
                Constants.FileSystemType.LOCAL => LocalFileSystem.GetInstance(),
                Constants.FileSystemType.FTP => new FtpFileSystem(define),
                Constants.FileSystemType.SFTP => new SftpFileSystem(define),
                Constants.FileSystemType.HDFS => new HDFSFileSystem(define),
                Constants.FileSystemType.MINIO => new MinioFileSystem(define),
                Constants.FileSystemType.S3 => new AmazonS3FileSystem(define),
                Constants.FileSystemType.ALIYUN => new OssFileSystem(define),
                Constants.FileSystemType.TENCENTCOS => new CosFileSystem(define),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
