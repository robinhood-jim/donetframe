using Amazon.S3;
using Amazon.S3.Model;
using Frameset.Common.FileSystem.CloudStorage.OutputStream;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using System.Diagnostics;
using System.Net;

namespace Frameset.Common.FileSystem.CloudStorage
{
    public class AmazonS3FileSystem : CloudStorageFileSystem
    {
        internal AmazonS3Client client;
        public AmazonS3FileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.S3;
            AmazonS3Config config = new AmazonS3Config()
            {
                ServiceURL = endpoint
            };
            client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public override void Dispose(bool disposable)
        {
            if (client != null)
            {
                client.Dispose();
            }
        }

        public override bool Exist(string resourcePath)
        {
            GetObjectMetadataResponse response = client.GetObjectMetadataAsync(new GetObjectMetadataRequest()
            {
                BucketName = GetBucketName(),
                Key = resourcePath

            }).Result;
            if (response != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override long GetStreamSize(string resourcePath)
        {
            if (Exist(resourcePath))
            {
                GetObjectMetadataResponse response = client.GetObjectMetadataAsync(GetBucketName(), resourcePath).Result;
                return response.HttpStatusCode == HttpStatusCode.OK ? response.ContentLength : -1;
            }
            throw new OperationFailedException("key " + resourcePath + " not found in bucket");
        }

        internal override bool BucketExists(string bucketName)
        {
            Debug.Assert(client != null);
            GetBucketLocationResponse response = client.GetBucketLocationAsync(new GetBucketLocationRequest()
            {
                BucketName = bucketName
            }).Result;
            if (response != null)
            {
                return true;
            }
            return false;
        }

        internal override Stream GetObject(string bucketName, string objectName)
        {
            GetObjectResponse response = client.GetObjectAsync(new GetObjectRequest()
            {
                BucketName = bucketName,
                Key = objectName
            }).Result;
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.ResponseStream;
            }
            throw new OperationFailedException("GetObject failed!");
        }

        internal override bool PutObject(string bucketName, DataCollectionDefine define, Stream stream, long size)
        {
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = define.Path,
                InputStream = stream
            };
            request.ContentType = GetContentType(define);
            PutObjectResponse response = client.PutObjectAsync(request).Result;
            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        internal override UploadPartSupportStream PutObject(string resourcePath)
        {
            return new AmazonS3OutputStream(client, define, bucketName, resourcePath);
        }
    }
}
