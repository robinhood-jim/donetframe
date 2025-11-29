using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using COSXML.Transfer;
using Frameset.Common.FileSystem.CloudStorage.OutputStream;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;

namespace Frameset.Common.FileSystem.CloudStorage
{
    public class CosFileSystem : CloudStorageFileSystem
    {
        private CosXmlServer server;
        private readonly string? region;
        private readonly TransferManager transferManager;
        private string tmpFile=null!;
        public CosFileSystem(DataCollectionDefine define) : base(define)
        {
            if (!define.ResourceConfig.TryGetValue(StorageConstants.CLOUDREGION, out region))
            {
                region = StorageConstants.COSDEFAULTREGION;
            }
            CosXmlConfig config = new CosXmlConfig.Builder().IsHttps(true).SetRegion(region).Build();
            QCloudCredentialProvider provider = new DefaultQCloudCredentialProvider(accessKey, secretKey, 600);
            server = new CosXmlServer(config, provider);
            TransferConfig transferConfig = new();
            transferManager = new(server, transferConfig);
        }

        public override bool Exist(string resourcePath)
        {
            DoesObjectExistRequest request = new(GetBucketName(), resourcePath);
            return server.DoesObjectExist(request);
        }

        public override long GetStreamSize(string resourcePath)
        {
            if (Exist(resourcePath))
            {
                HeadObjectResult result = server.HeadObject(new HeadObjectRequest(GetBucketName(), resourcePath));
                return result.size;
            }
            throw new OperationFailedException("key " + resourcePath + " not found in bucket");
        }

        internal override bool BucketExists(string bucketName)
        {
            return server.DoesBucketExist(new COSXML.Model.Bucket.DoesBucketExistRequest(bucketName));
        }

        internal override Stream GetObject(string bucketName, string objectName)
        {
            FileMeta meta = FileUtil.Parse(objectName);
            tmpFile = Path.GetTempPath() + Path.PathSeparator + meta.FileName + "." + meta.FileFormat;
            GetObjectResult result = server.GetObject(new GetObjectRequest(GetBucketName(), objectName, Path.GetTempPath(), meta.FileName + "." + meta.FileFormat));
            if (result.httpCode == 200)
            {
                return new FileStream(tmpFile, FileMode.Open);
            }
            throw new OperationFailedException("key " + objectName + " not found in bucket");
        }

        internal override UploadPartSupportStream PutObject(string resourcePath)
        {
            return new CosOutputStream(server, define, GetBucketName(), resourcePath);
        }

        internal override bool PutObject(string bucketName, DataCollectionDefine define, Stream stream, long size)
        {

            PutObjectRequest request = new(GetBucketName(), define.Path, stream);
            COSXMLUploadTask task = new(request);

            COSXMLUploadTask.UploadTaskResult result = transferManager.UploadAsync(task).Result;
            return result.httpCode == 200;
        }
        public override void Dispose(bool disposable)
        {
            if (tmpFile != null)
            {
                File.Delete(tmpFile);
            }
        }
    }
}
