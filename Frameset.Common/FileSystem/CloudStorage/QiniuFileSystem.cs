using Frameset.Common.FileSystem.CloudStorage.OutputStream;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Qiniu.Http;
using Qiniu.Storage;
using Qiniu.Util;

namespace Frameset.Common.FileSystem.CloudStorage
{
    public class QiniuFileSystem : CloudStorageFileSystem
    {
        private Mac client;
        private string? zoneStr;
        private BucketManager bucketManager;
        private string domain;
        private UploadManager uploadManager;
        private Config config;
        private bool useHttps = false;
        private bool useCdnDomain = false;
        public QiniuFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.QINIU;
            client = new Mac(accessKey, secretKey);
            define.ResourceConfig.TryGetValue(StorageConstants.QINIUZONECONFIG, out zoneStr);
            define.ResourceConfig.TryGetValue(StorageConstants.QINIUDOWNDOAMIN, out domain);
            if (define.ResourceConfig.TryGetValue(StorageConstants.QINNIUDOWNUSEHTTPS, out string? useHttpsStr) && string.Equals(Constants.TRUEVALUE, useHttpsStr, StringComparison.OrdinalIgnoreCase))
            {
                useHttps = true;
            }
            if (define.ResourceConfig.TryGetValue(StorageConstants.QINNIUDOWNUSECDN, out string? useCdnStr) && string.Equals(Constants.TRUEVALUE, useCdnStr, StringComparison.OrdinalIgnoreCase))
            {
                useCdnDomain = true;
            }

            config = new Config();
            config.Zone = GetZone();
            config.UseHttps = useHttps;
            config.UseCdnDomains = useCdnDomain;

            bucketManager = new BucketManager(client, config);
            uploadManager = new UploadManager(config);
        }

        public override bool Exist(string resourcePath)
        {
            StatResult result = bucketManager.Stat(GetBucketName(), resourcePath);
            return result != null && result.Result.Status > 0;
        }

        public override long GetStreamSize(string resourcePath)
        {
            StatResult result = bucketManager.Stat(GetBucketName(), resourcePath);
            if (result != null)
            {
                return result.Result.Fsize;
            }
            return -1;
        }

        internal override bool BucketExists(string bucketName)
        {
            BucketsResult result = bucketManager.Buckets(false);
            return result != null && result.Result.Contains(bucketName);
        }

        internal override Stream GetObject(string bucketName, string objectName)
        {
            string downUrl = DownloadManager.CreatePrivateUrl(client, domain, objectName);
            return new HttpClient().GetAsync(new Uri(downUrl)).Result.Content.ReadAsStream();
        }

        internal override UploadPartSupportStream PutObject(string resourcePath)
        {
            PutPolicy putPolicy = new();
            putPolicy.Scope = bucketName + ":" + resourcePath;
            string token = Auth.CreateUploadToken(client, putPolicy.ToJsonString());
            return new QiniuOutputStream(define, GetBucketName(), resourcePath, config, token);
        }


        internal override bool PutObject(string bucketName, DataCollectionDefine define, Stream stream, long size)
        {
            PutPolicy putPolicy = new();
            putPolicy.Scope = bucketName + ":" + define.Path;
            string token = Auth.CreateUploadToken(client, putPolicy.ToJsonString());
            PutExtra extra = new();
            extra.MimeType = GetContentType(define);
            HttpResult result = uploadManager.UploadStream(stream, define.Path, token, extra);
            return result != null && result.Code == 200;
        }
        private Zone GetZone()
        {
            Zone returnZone = Zone.ZONE_CN_South;
            if (!zoneStr.IsNullOrEmpty())
            {
                if (string.Equals("cneast", zoneStr, StringComparison.OrdinalIgnoreCase))
                {
                    returnZone = Zone.ZONE_CN_East;
                }
                else if (string.Equals("cneast2", zoneStr, StringComparison.OrdinalIgnoreCase))
                {
                    returnZone = Zone.ZONE_CN_East_2;
                }
                else if (string.Equals("cnsouth", zoneStr, StringComparison.OrdinalIgnoreCase))
                {
                    returnZone = Zone.ZONE_CN_South;
                }
                else if (string.Equals("cnnorth", zoneStr, StringComparison.OrdinalIgnoreCase))
                {
                    returnZone = Zone.ZONE_CN_North;
                }
                else if (string.Equals("usnorth", zoneStr, StringComparison.OrdinalIgnoreCase))
                {
                    returnZone = Zone.ZONE_US_North;
                }
                else if (string.Equals("singapore", zoneStr, StringComparison.OrdinalIgnoreCase))
                {
                    returnZone = Zone.ZONE_AS_Singapore;
                }
            }
            return returnZone;
        }
    }
}
