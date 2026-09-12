using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Newtonsoft.Json;
using Qiniu.Http;
using Qiniu.Storage;
using Qiniu.Util;
using System.Reflection;

namespace Frameset.Common.FileSystem.CloudStorage.OutputStream
{
    public class QiniuOutputStream : UploadPartSupportStream
    {
        private readonly ResumableUploader resumableUploader;
        private readonly string token;
        private readonly MethodInfo? method = typeof(ResumableUploader).GetMethod("intReq", BindingFlags.NonPublic, new Type[] { typeof(string), typeof(string) });

        private ResumeInfo resumeInfo = null!;
        private List<Dictionary<string, object>> etags = [];
        private readonly HttpManager manager;
        private readonly string encodeName;
        private readonly Config config;
        public QiniuOutputStream(DataCollectionDefine define, string bucketName, string key, Config config, string token) : base(define, bucketName, key)
        {
            this.config = config;
            resumableUploader = new ResumableUploader(config);
            manager = new HttpManager();
            this.token = token;
            encodeName = Base64.GetEncodedObjectName(key);
            doInit();
        }

        protected override string completeMultiUpload()
        {
            string accessKeyFromUpToken = UpToken.GetAccessKeyFromUpToken(token);
            string bucketFromUpToken = UpToken.GetBucketFromUpToken(token);
            if (accessKeyFromUpToken == null || bucketFromUpToken == null)
            {
                throw new OperationFailedException("token invalidate!");
            }
            string text = config.UpHost(accessKeyFromUpToken, bucketFromUpToken);
            string upToken = $"UpToken {token}";
            Dictionary<string, object> reqBody = new Dictionary<string, object>();
            reqBody.Add("fname", define.MetaData.FullName);
            reqBody.Add("mimeType", define.MetaData.ContentType);
            reqBody.Add("parts", etags.ToArray());
            string url = $"{text}/buckets/{bucketFromUpToken}/objects/{encodeName}/uploads/{UploadId}";
            string data = JsonConvert.SerializeObject(reqBody);
            var httpResult = manager.PostJson(url, data, token);
            if (httpResult.Code == 200)
            {
                Dictionary<string, string>? resultMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(httpResult.Text);
                if (resultMap != null)
                {
                    return resultMap["hash"];
                }
            }
            throw new OperationFailedException("failed to complete upload");
        }

        protected override void initiateUpload()
        {
            HttpResult? result = method?.Invoke(resumableUploader, new object[] { encodeName, token }) as HttpResult;
            if (result != null && result.Code == 200)
            {
                Dictionary<string, string>? dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.Text);
                if (dict != null)
                {
                    UploadId = dict["uploadId"];
                    resumeInfo = new ResumeInfo
                    {
                        Uploaded = 0L,
                        ExpiredAt = long.Parse(dict["expireAt"]),
                        UploadId = UploadId
                    };
                }
            }
            else
            {
                throw new OperationFailedException("failed to init upload");
            }

        }

        protected override void uploadAsync()
        {

        }

        protected override void uploadPart(MemoryStream stream, int partNum, long size)
        {
            string accessKeyFromUpToken = UpToken.GetAccessKeyFromUpToken(token);
            string bucketFromUpToken = UpToken.GetBucketFromUpToken(token);
            string baseUrl = config.UpHost(accessKeyFromUpToken, bucketFromUpToken);
            string requsetUrl = $"{baseUrl}/buckets/{bucketFromUpToken}/objects/{encodeName}/uploads/{resumeInfo.UploadId}/{partNum + 1}";
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string authStr = $"UpToken {token}";
            dictionary.Add("Authorization", authStr);

            var httpResult = manager.PutDataWithHeaders(requsetUrl, stream.GetBuffer(), dictionary);
            if (httpResult.Code == 200)
            {
                Dictionary<string, string>? resultMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(httpResult.Text);
                Dictionary<string, object> etagDict = new Dictionary<string, object>();
                if (resultMap != null)
                {
                    etagDict.Add("etag", resultMap["etag"]);
                }
                etagDict.Add("partNumber", partNum + 1);
                etags.Add(etagDict);
            }
        }
    }
}
