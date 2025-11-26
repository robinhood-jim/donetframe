using Aliyun.OSS;
using Frameset.Core.FileSystem;
using Serilog;
using System.Net;

namespace Frameset.Common.FileSystem.CloudStorage.OutputStream
{
    public class OssOutputStream : UploadPartSupportStream
    {
        private OssClient ossClient;

        public OssOutputStream(OssClient client, DataCollectionDefine define, string bucketName, string key) : base(define, bucketName, key)
        {
            this.ossClient = client;
        }

        internal override string completeMultiUpload()
        {
            CompleteMultipartUploadRequest request = new CompleteMultipartUploadRequest(bucketName, key, UploadId);
            CompleteMultipartUploadResult result = ossClient.CompleteMultipartUpload(request);
            return result.ETag;
        }

        internal override void initiateUpload()
        {
            InitiateMultipartUploadRequest request = new InitiateMultipartUploadRequest(bucketName, key);
            InitiateMultipartUploadResult result = ossClient.InitiateMultipartUpload(request);
            UploadId = result.UploadId;
        }

        internal override void uploadAsync()
        {
            PutObjectRequest request = new PutObjectRequest(bucketName, key, partMemMap[0]);
            PutObjectResult result = ossClient.PutObject(request);
            if (result.HttpStatusCode == HttpStatusCode.OK)
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
            UploadPartRequest request = new UploadPartRequest(bucketName, key, UploadId);
            request.InputStream = stream;
            UploadPartResult result = ossClient.UploadPart(request);
            if (result.HttpStatusCode == HttpStatusCode.OK)
            {
                etagMap.TryAdd(partNum, result.ETag);
            }
        }
    }
}
