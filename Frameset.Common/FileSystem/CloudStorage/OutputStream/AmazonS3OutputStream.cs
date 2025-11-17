using Amazon.S3;
using Amazon.S3.Model;
using Frameset.Core.FileSystem;
using Serilog;
using System.Net;

namespace Frameset.Common.FileSystem.CloudStorage.OutputStream
{
    public class AmazonS3OutputStream : UploadPartSupportStream
    {
        private AmazonS3Client client;
        public AmazonS3OutputStream(AmazonS3Client client, DataCollectionDefine define, string bucketName, string key)
        {
            this.client = client;
            this.bucketName = bucketName;
            this.key = key;
            this.define = define;
            doInit();
        }

        internal override string completeMultiUpload()
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
                return null;
            }
        }

        internal override void initiateUpload()
        {
            InitiateMultipartUploadResponse response = client.InitiateMultipartUploadAsync(bucketName, key).Result;
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                UploadId = response.UploadId;
            }
        }

        internal override void uploadAsync()
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

        internal override void uploadPart(MemoryStream stream, int partNum, long size)
        {
            UploadPartRequest request = new UploadPartRequest();
            request.BucketName = bucketName;
            request.Key = key;
            request.UploadId = UploadId;
            request.PartNumber = partNum;
            request.PartSize = size;
            request.InputStream = stream;
            UploadPartResponse response = client.UploadPartAsync(request).Result;
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
