using Frameset.Common.Data;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Frameset.Common.FileSystem.utils
{
    public class HdfsClient : IDisposable
    {
        private readonly HttpClient client;
        private readonly string? baseUrl;
        private readonly string? userName;
        private readonly string? useToken;
        private readonly AuthType type = AuthType.USERNAME;
        private long count = 1;
        public HdfsClient(DataCollectionDefine define, HttpMessageHandler? handler = null)
        {
            Debug.Assert(!define.ResourceConfig.IsNullOrEmpty());
            define.ResourceConfig.TryGetValue(ResourceConstants.HDFSBASEURL, out baseUrl);
            Debug.Assert(!baseUrl.IsNullOrEmpty());

            define.ResourceConfig.TryGetValue(ResourceConstants.HDFSAUTHTYPE, out string? authType);
            Trace.Assert(!authType.IsNullOrEmpty());
            type = AuthTypeOf(authType);
            switch (type)
            {
                case AuthType.USERNAME:
                    define.ResourceConfig.TryGetValue(ResourceConstants.HDFSUSERNAME, out userName);
                    Debug.Assert(!userName.IsNullOrEmpty());
                    break;
                case AuthType.TOKEN:
                    define.ResourceConfig.TryGetValue(ResourceConstants.HDFSTOKEN, out useToken);
                    Debug.Assert(!useToken.IsNullOrEmpty());
                    break;
                default:
                    break;
            }
            client = new HttpClient((handler != null) ? handler : new HttpClientHandler
            {
                AllowAutoRedirect = false,
                PreAuthenticate = false
            });

        }
        public static AuthType AuthTypeOf(string resourceType)
        {
            AuthType resType = AuthType.NONE;
            if (!resourceType.IsNullOrEmpty())
            {
                foreach (AuthType rtype in Enum.GetValues(typeof(AuthType)))
                {
                    if (rtype.ToString().ToUpper().Equals(resourceType.ToUpper()))
                    {
                        resType = rtype;
                        break;
                    }
                }
            }
            return resType;
        }
        public string WrapRequest(string resourcePath, Dictionary<string, string> paramMap)
        {
            StringBuilder builder = new StringBuilder(baseUrl).Append(resourcePath);
            switch (type)
            {
                case AuthType.USERNAME:
                    builder.Append("?user.name=").Append(userName).Append('&');
                    break;
                case AuthType.TOKEN:
                    builder.Append("?delegation=").Append(useToken).Append('&');
                    break;
                default:
                    break;
            }
            string? oper;
            paramMap.Remove("op", out oper);
            if (builder.Length > 0)
            {
                builder.Append("&op=").Append(oper);
            }
            else
            {
                builder.Append("?op=").Append(oper);
            }
            foreach (var item in paramMap)
            {
                builder.Append('&').Append(item.Key).Append('=').Append(item.Value);
            }
            return builder.ToString();
        }
        private void Operators(Dictionary<string, string> paramMap, string value)
        {
            paramMap.TryAdd("op", value);
        }
        private void SetParameter(Dictionary<string, string> paramMap, string name, object? value, object? defaultValue = null)
        {
            if (value != null && !value.ToString().IsNullOrEmpty())
            {
                paramMap.TryAdd(name, value.ToString());
            }
            else if (defaultValue != null)
            {
                paramMap.TryAdd(name, defaultValue.ToString());
            }
        }
        public async Task<bool> Exists(string path)
        {
            Dictionary<string, string> paramMap = new Dictionary<string, string>();
            Operators(paramMap, "GETFILESTATUS");
            string requestUrl = WrapRequest(path, paramMap);
            HttpResponseMessage message = await client.GetAsync(requestUrl);
            HttpResponseMessage okmessage = message.EnsureSuccessStatusCode();
            return okmessage.IsSuccessStatusCode;
        }
        public async Task<Dictionary<string, object>> FileStatus(string path)
        {
            Dictionary<string, string> paramMap = new Dictionary<string, string>();
            Operators(paramMap, "GETFILESTATUS");
            string requestUrl = WrapRequest(path, paramMap);
            HttpResponseMessage message = await client.GetAsync(requestUrl);
            message.EnsureSuccessStatusCode();
            Dictionary<string, object>? valueMap = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(await message.Content.ReadAsStreamAsync());
            if (valueMap != null && valueMap.TryGetValue("FileStatus", out object? filestatusObj))
            {
                return filestatusObj as Dictionary<string, object>;
            }
            return new();
        }
        public async Task<Dictionary<string, object>> ContentSummary(string path)
        {
            Dictionary<string, string> paramMap = new Dictionary<string, string>();
            Operators(paramMap, "GETCONTENTSUMMARY");
            string requestUrl = WrapRequest(path, paramMap);
            HttpResponseMessage message = await client.GetAsync(requestUrl);
            message.EnsureSuccessStatusCode();
            Dictionary<string, object>? valueMap = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(await message.Content.ReadAsStreamAsync());
            return valueMap["ContentSummary"] as Dictionary<string, object>;
        }
        public async Task<bool> IsDirectory(string path)
        {
            Dictionary<string, object> statusMap = await FileStatus(path);
            object? filestatusMap;
            statusMap.TryGetValue("FileStatus", out filestatusMap);
            if (filestatusMap != null)
            {
                Dictionary<string, object>? sMap = filestatusMap as Dictionary<string, object>;
                object? type = null;
                sMap?.TryGetValue("type", out type);
                if (type != null)
                {
                    return string.Equals(type.ToString(), "DIRECTORY", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        public async Task<bool> ReadStream(Stream stream, string path, long? offset = null, long? length = null, int? buffersize = null)
        {
            bool isExist = await Exists(path);
            if (isExist)
            {
                Dictionary<string, string> paramMap = new Dictionary<string, string>();
                Operators(paramMap, "OPEN");
                SetParameter(paramMap, "offset", offset);
                SetParameter(paramMap, "length", length);
                SetParameter(paramMap, "buffersize", buffersize);
                HttpResponseMessage response = await client.GetAsync(WrapRequest(path, paramMap));
                if (response.StatusCode.Equals(HttpStatusCode.RedirectKeepVerb))
                {
                    HttpResponseMessage response2 = await client.GetAsync(response.Headers.Location);
                    response2.EnsureSuccessStatusCode();
                    if (response2.IsSuccessStatusCode)
                    {
                        await response2.Content.CopyToAsync(stream);
                    }

                    return response2.IsSuccessStatusCode;
                }
                throw new InvalidOperationException(string.Concat("Should get a 307. Instead we got: ", response.StatusCode, " ", response.ReasonPhrase));
            }
            else
            {
                throw new OperationNotAllowedException("path " + path + " dose't exists!");
            }
        }
        public async Task<bool> WriteStream(Stream stream, string path, bool overwrite = false, long? blocksize = null, short? replication = null, string? permission = null, int? buffersize = null)
        {
            bool isExist = await Exists(path);
            Dictionary<string, string> paramMap = new Dictionary<string, string>();
            SetParameter(paramMap, "overwrite", overwrite, "false");
            if (!isExist || string.Equals("true", paramMap["overwrite"], StringComparison.OrdinalIgnoreCase))
            {
                Operators(paramMap, "OPEN");
                SetParameter(paramMap, "blocksize", blocksize);
                SetParameter(paramMap, "buffersize", buffersize);
                SetParameter(paramMap, "replication", replication);
                SetParameter(paramMap, "permission", permission);
                HttpResponseMessage response = await client.PutAsync(WrapRequest(path, paramMap), new ByteArrayContent(new byte[0]));
                if (response.StatusCode.Equals(HttpStatusCode.RedirectKeepVerb))
                {
                    HttpResponseMessage obj = await client.PutAsync(response.Headers.Location, new StreamContent(stream));
                    obj.EnsureSuccessStatusCode();
                    return obj.IsSuccessStatusCode;
                }
                throw new InvalidOperationException(string.Concat("Should get a 307. Instead we got: ", response.StatusCode, " ", response.ReasonPhrase));
            }
            else
            {
                throw new OperationNotAllowedException("file " + path + " already exists!");
            }
        }
        public async Task<IList<Dictionary<string, object>>> ListDirectory(string path)
        {
            Dictionary<string, string> paramMap = new Dictionary<string, string>();
            Operators(paramMap, "LISTSTATUS");
            HttpResponseMessage obj = await client.GetAsync(WrapRequest(path, paramMap));
            obj.EnsureSuccessStatusCode();
            Dictionary<string, object>? dictMap = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(await obj.Content.ReadAsStreamAsync());
            object? list;
            (dictMap["FileStatuses"] as Dictionary<string, object>).TryGetValue("FileStatus", out list);
            if (list != null)
            {
                return list as IList<Dictionary<string, object>>;
            }
            else
            {
                throw new OperationNotAllowedException("path " + path + " not exists!");
            }
        }
        public async Task<bool> MakeDirectory(string path, string? permission = null)
        {
            bool isExist = await Exists(path);
            Dictionary<string, string> paramMap = new Dictionary<string, string>();
            if (!isExist)
            {
                Operators(paramMap, "MKDIR");
                SetParameter(paramMap, "permission", permission);
                HttpResponseMessage obj = await client.PutAsync(WrapRequest(path, paramMap), new ByteArrayContent(new byte[0]));
                obj.EnsureSuccessStatusCode();
                Dictionary<string, object>? dictMap = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(await obj.Content.ReadAsStreamAsync());
                return (Boolean)dictMap?["boolean"];
            }
            else
            {
                throw new OperationNotAllowedException("directory " + path + " already exists!");
            }
        }
        public async Task<bool> Rename(string path, string target)
        {
            bool isExist = await Exists(path);
            Dictionary<string, string> paramMap = new Dictionary<string, string>();
            Operators(paramMap, "RENAME");
            SetParameter(paramMap, "destination", target);
            HttpResponseMessage obj = await client.PutAsync(WrapRequest(path, paramMap), new ByteArrayContent(new byte[0]));
            obj.EnsureSuccessStatusCode();
            Dictionary<string, object>? dictMap = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(await obj.Content.ReadAsStreamAsync());
            return (Boolean)dictMap?["boolean"];
        }

        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
            }
        }
    }
    public enum AuthType
    {
        NONE,
        USERNAME,
        KERBERORS,
        TOKEN
    }

}
