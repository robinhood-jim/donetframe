using Aliyun.OSS;
using Frameset.Common.FileSystem.CloudStorage.OutputStream;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Net;

namespace Frameset.Common.FileSystem.CloudStorage
{
    public class OssFileSystem : CloudStorageFileSystem
    {
        private OssClient ossClient;
        public OssFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.S3;
            Debug.Assert(!accessKey.IsNullOrEmpty() && !secretKey.IsNullOrEmpty() && !endpoint.IsNullOrEmpty());
            ossClient = new OssClient(endpoint, accessKey, secretKey);

        }

        public override void Dispose(bool disposable)
        {
            ossClient = null;
        }

        public override bool Exist(string resourcePath)
        {
            return ossClient.DoesObjectExist(getBucketName(), resourcePath);
        }

        public override long GetStreamSize(string resourcePath)
        {
            if (Exist(resourcePath))
            {
                ObjectMetadata metadata = ossClient.GetObjectMetadata(getBucketName(), resourcePath);
                return metadata.ContentLength;
            }
            return -1;
        }

        internal override bool bucketExists(string bucketName)
        {
            return ossClient.DoesBucketExist(bucketName);
        }

        internal override Stream GetObject(string bucketName, string objectName)
        {
            OssObject obj = ossClient.GetObject(bucketName, objectName);
            if (obj.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return obj.ResponseStream;
            }
            throw new OperationFailedException("key " + objectName + " not found in bucket");
        }

        internal override UploadPartSupportStream PutObject(string resourcePath)
        {
            return new OssOutputStream(ossClient, define, getBucketName(), resourcePath);
        }

        internal override bool PutObject(string bucketName, DataCollectionDefine define, Stream stream, long size)
        {
            PutObjectRequest request = new PutObjectRequest(getBucketName(), define.Path, stream);
            ObjectMetadata metadata = new ObjectMetadata();
            metadata.ContentType = GetContentType(define);
            metadata.ContentLength = size;
            request.Metadata = metadata;
            PutObjectResult response = ossClient.PutObject(request);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }
    }
}
