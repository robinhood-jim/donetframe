using Frameset.Common.FileSystem.CloudStorage.OutputStream;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Common.FileSystem.CloudStorage
{
    public abstract class CloudStorageFileSystem : AbstractFileSystem
    {
        internal string bucketName;
        internal string tmpFilePath;
        internal string accessKey;
        internal string secretKey;
        internal string endpoint;


        public CloudStorageFileSystem(DataCollectionDefine define) : base(define)
        {
            bucketName = getBucketName();
            define.ResourceConfig.TryGetValue(StorageConstants.CLOUDFSACCESSKEY, out accessKey);
            define.ResourceConfig.TryGetValue(StorageConstants.CLOUDFSSECRETKEY, out secretKey);
            define.ResourceConfig.TryGetValue(StorageConstants.CLOUDFSENDPOINT, out endpoint);
        }
        internal string getBucketName()
        {
            define.ResourceConfig.TryGetValue(StorageConstants.BUCKET_NAME, out string readBucket);
            return bucketName.IsNullOrEmpty() ? readBucket : bucketName;
        }
        public override Stream GetInputStream(string resourcePath)
        {
            Stream inputStream = GetObject(getBucketName(), resourcePath);
            return GetInputStreamWithCompress(resourcePath, inputStream);
        }
        public override Stream GetOutputStream(string resourcePath)
        {
            Stream outputStream = PutObject(resourcePath);
            return GetOutputStremWithCompress(resourcePath, outputStream);
        }
        public override Stream GetRawInputStream(string resourcePath)
        {
            return new BufferedStream(GetObject(getBucketName(), resourcePath));
        }
        public override Stream GetRawOutputStream(string resourcePath)
        {
            return new BufferedStream(PutObject(resourcePath));
        }
        public override bool IsDirectory(string resourcePath)
        {
            throw new MethodNotSupportedException("cloudstorage can not use this function");
        }
        internal abstract UploadPartSupportStream PutObject(string resourcePath);

        internal abstract bool PutObject(string bucketName, DataCollectionDefine define, Stream stream, long size);
        internal abstract Stream GetObject(string bucketName, string objectName);
        internal abstract bool bucketExists(string bucketName);
    }
}
