using Amazon.S3;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem.CloudStorage.OutputStream
{
    public class AmazonS3OuputStream : UploadPartSupportStream
    {
        private AmazonS3Client client;
        public AmazonS3OuputStream(AmazonS3Client client, DataCollectionDefine define, string bucketName, string key)
        {
            this.client = client;
            this.bucketName = bucketName;
            this.key = key;
            this.define = define;
            doInit();
        }

        internal override string completeMultiUpload()
        {
            throw new NotImplementedException();
        }

        internal override void initiateUpload()
        {
            throw new NotImplementedException();
        }

        internal override void uploadAsync()
        {
            throw new NotImplementedException();
        }

        internal override void uploadPart(MemoryStream stream, int partNum, long size)
        {
            throw new NotImplementedException();
        }
    }
}
