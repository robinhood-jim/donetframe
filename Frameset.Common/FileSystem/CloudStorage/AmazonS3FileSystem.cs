using Amazon.S3;
using Amazon.S3.Model;
using Frameset.Common.FileSystem.CloudStorage.OutputStream;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Net;

namespace Frameset.Common.FileSystem.CloudStorage
{
    public class AmazonS3FileSystem : CloudStorageFileSystem
    {
        internal AmazonS3Client client;
        public AmazonS3FileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.MINIO;
            define.ResourceConfig.TryGetValue("fs.accessKey", out accessKey);
            define.ResourceConfig.TryGetValue("fs.secretKey", out secretKey);
            define.ResourceConfig.TryGetValue("fs.endpoint", out endpoint);
            Debug.Assert(!accessKey.IsNullOrEmpty() && !secretKey.IsNullOrEmpty() && !endpoint.IsNullOrEmpty());
            //client = (MinioClient)new MinioClient().WithCredentials(accessKey, secretKey).WithEndpoint(endpoint).Build();
            AmazonS3Config config = new AmazonS3Config()
            {
                ServiceURL = endpoint
            };
            client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public override void Dispose()
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
                BucketName = getBucketName(),
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
            throw new NotImplementedException();
        }

        internal override bool bucketExists(string bucketName)
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
            return null;
        }

        internal override bool PutObject(string bucketName, DataCollectionDefine define, Stream stream, long size)
        {
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = define.Path,
                InputStream = stream
            };
            PutObjectResponse response = client.PutObjectAsync(request).Result;
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        internal override Stream PutObject(string resourcePath)
        {
            return new AmazonS3OutputStream(client, define, bucketName, resourcePath);
        }
    }
}
