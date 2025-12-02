using COSXML;
using COSXML.Model.Object;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Serilog;

namespace Frameset.Common.FileSystem.CloudStorage.OutputStream
{
    public class CosOutputStream : UploadPartSupportStream
    {
        private CosXmlServer server;
        public CosOutputStream(CosXmlServer server, DataCollectionDefine define, string bucketName, string key) : base(define, bucketName, key)
        {
            this.server = server;
        }

        protected override string completeMultiUpload()
        {
            CompleteMultipartUploadRequest request = new(bucketName, key, UploadId);
            for (int i = 0; i < partNum; i++)
            {
                request.SetPartNumberAndETag(i + 1, etagMap[i]);
            }
            CompleteMultipartUploadResult result = server.CompleteMultiUpload(request);
            if (result?.httpCode == 200)
            {
                return result.completeResult.eTag;
            }
            throw new OperationFailedException("complete multiUpload failed");
        }

        protected override void initiateUpload()
        {
            InitMultipartUploadResult result = server.InitMultipartUpload(new(bucketName, key));
            if (result.httpCode == 200)
            {
                UploadId = result.initMultipartUpload.uploadId;
            }
            else
            {
                throw new OperationFailedException("initiateUpload failed!");
            }
        }

        protected override void uploadAsync()
        {
            PutObjectResult result = server.PutObject(new PutObjectRequest(bucketName, key, partMemMap[0]));
            if (result.httpCode == 200)
            {
                Log.Information("put object " + key + " success");
            }
            else
            {
                Log.Error("put object " + key + " failed");
            }
        }

        protected override void uploadPart(MemoryStream stream, int partNum, long size)
        {
            UploadPartResult result = server.UploadPart(new UploadPartRequest(bucketName, key, partNum, UploadId, stream.GetBuffer()));
            if (result.httpCode == 200)
            {
                etagMap.TryAdd(partNum, result.eTag);
            }
        }
    }
}
