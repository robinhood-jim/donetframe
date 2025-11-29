using Amazon.S3;
using Amazon.S3.Model;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Serilog;
using System.Net;

namespace Frameset.Common.FileSystem.CloudStorage.OutputStream
{
    public class AmazonS3OutputStream : UploadPartSupportStream
    {
        private AmazonS3Client client;
        public AmazonS3OutputStream(AmazonS3Client client, DataCollectionDefine define, string bucketName, string key) : base(define, bucketName, key)
        {
            this.client = client;

            doInit();
        }

        protected override string completeMultiUpload()
        {
            CompleteMultipartUploadRequest request = new CompleteMultipartUploadRequest();
            request.BucketName = bucketName;
            request.Key = key;
            List<PartETag> etags = new List<PartETag>();
            for (int i = 0; i < partNum; i++)
            {
                etags.Add(new PartETag(i + 1, etagMap[i]));
            }
            request.PartETags = etags;

            CompleteMultipartUploadResponse response = client.CompleteMultipartUploadAsync(request).Result;
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return response.ETag;
            }
            else
            {
                throw new OperationFailedException("complete multiUpload failed");
            }
        }

        protected override void initiateUpload()
        {
            InitiateMultipartUploadResponse response = client.InitiateMultipartUploadAsync(bucketName, key).Result;
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                UploadId = response.UploadId;
            }
            else
            {
                throw new OperationFailedException("initiateUpload failed!");
            }
        }

        protected override void uploadAsync()
        {
            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };
            request.InputStream = partMemMap[0];

            PutObjectResponse response = client.PutObjectAsync(request).Result;
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                Log.Information("put object " + key + " success");
            }
            else
            {
                Log.Error("put object " + key + " failed");
            }

        }

        protected override async void uploadPart(MemoryStream stream, int partNum, long size)
        {
            UploadPartRequest request = new UploadPartRequest();
            request.BucketName = bucketName;
            request.Key = key;
            request.UploadId = UploadId;
            request.PartNumber = partNum;
            request.PartSize = size;
            request.InputStream = stream;
            UploadPartResponse response = await client.UploadPartAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                etagMap.TryAdd(partNum, response.ETag);
            }
            else
            {
                errorPartMap.TryAdd(partNum, 1);
            }
            partMemMap[partNum].Close();
            partMemMap.Remove(partNum);
        }
    }
}
