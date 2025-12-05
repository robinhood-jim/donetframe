using Frameset.Common.FileSystem.CloudStorage.OutputStream;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.FileSystem.CloudStorage
{
    public abstract class CloudStorageFileSystem : AbstractFileSystem
    {
        protected string bucketName;
        protected string tmpFilePath = null!;
        protected string? accessKey;
        protected string? secretKey;
        protected string? endpoint;


        protected CloudStorageFileSystem(DataCollectionDefine define) : base(define)
        {
            bucketName = GetBucketName();
            define.ResourceConfig.TryGetValue(StorageConstants.CLOUDFSACCESSKEY, out accessKey);
            define.ResourceConfig.TryGetValue(StorageConstants.CLOUDFSSECRETKEY, out secretKey);
            define.ResourceConfig.TryGetValue(StorageConstants.CLOUDFSENDPOINT, out endpoint);

            Trace.Assert(!bucketName.IsNullOrEmpty(), "missing parameter bucketName");
            Trace.Assert(!accessKey.IsNullOrEmpty(), "missing parameter accessKey");
            Trace.Assert(!secretKey.IsNullOrEmpty(), "missing parameter secretKey");
            Trace.Assert(!endpoint.IsNullOrEmpty(), "missing parameter endpoint");
        }
        protected string GetBucketName()
        {
            if (define.ResourceConfig.TryGetValue(StorageConstants.BUCKET_NAME, out string? readBucket))
            {
                return bucketName.IsNullOrEmpty() ? readBucket : bucketName;
            }
            else
            {
                return bucketName;
            }
        }
        public override Stream GetInputStream(string resourcePath)
        {
            Stream inputStream = GetObject(GetBucketName(), resourcePath);
            return GetInputStreamWithCompress(resourcePath, inputStream);
        }
        public override Stream GetOutputStream(string resourcePath)
        {
            Stream outputStream = PutObject(resourcePath);
            return GetOutputStremWithCompress(resourcePath, outputStream);
        }
        public override Stream GetRawInputStream(string resourcePath)
        {
            return new BufferedStream(GetObject(GetBucketName(), resourcePath));
        }
        public override Stream GetRawOutputStream(string resourcePath)
        {
            return new BufferedStream(PutObject(resourcePath));
        }
        public override bool IsDirectory(string resourcePath)
        {
            throw new MethodNotSupportedException("cloudstorage can not use this function");
        }
        /// <summary>
        /// Support UploadPart Put Large object
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        internal abstract UploadPartSupportStream PutObject(string resourcePath);
        /// <summary>
        /// Write Exist Stream to Cloud fs
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="define"></param>
        /// <param name="stream"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        internal abstract bool PutObject(string bucketName, DataCollectionDefine define, Stream stream, long size);
        internal abstract Stream GetObject(string bucketName, string objectName);
        internal abstract bool BucketExists(string bucketName);
        public void SetBucketName(string bucketName)
        {
            this.bucketName = bucketName;
        }
    }
}
