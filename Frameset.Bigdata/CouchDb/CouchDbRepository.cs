using Frameset.Bigdata.NoSql;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using System.Text;
using System.Text.Json;

namespace Frameset.Bigdata.CouchDb
{
    public class CouchDbRepository<V> : NoSqlRepository<V, string> where V : BaseEntity
    {
        private readonly string dbUrl;
        private readonly string dbName = null!;
        private readonly string dbCredential = null!;
        private readonly IHttpClientFactory clientFactory;
        public CouchDbRepository(DataCollectionDefine define, IHttpClientFactory clientFactory) : base(define)
        {
            this.clientFactory = clientFactory;
            if (!define.ResourceConfig.TryGetValue(ResourceConstants.COUCHDBURL, out dbUrl))
            {
                dbUrl = "http://localhost:5984";
            }
            define.ResourceConfig.TryGetValue(ResourceConstants.COUCHDBNAME, out dbName);
            define.ResourceConfig.TryGetValue(ResourceConstants.COUCHDBCREDENTIAL, out dbCredential);
        }

        public override V GetById(string pk)
        {
            var httpClient = GetSecureHttpClient();
            var dbResult = httpClient.GetAsync(dbName + "/" + pk).Result;
            if (dbResult.IsSuccessStatusCode)
            {
                string content = dbResult.Content.ReadAsStringAsync().Result;
                Dictionary<string, object> dict = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                V retEntity = Activator.CreateInstance<V>();
                foreach (FieldContent fieldContent in fieldContents)
                {
                    if (dict.TryGetValue(fieldContent.PropertyName, out object? value))
                    {
                        if (value != null)
                        {
                            fieldContent.SetMethod.Invoke(retEntity, [value]);
                        }
                    }
                    pkColumn.SetMethod.Invoke(retEntity, [pk]);
                }
                return retEntity;
            }
            else
            {
                throw new BaseSqlException("get by id error!" + dbResult.ReasonPhrase);
            }
        }

        public override int RemoveEntity(IList<string> pks)
        {
            var httpClient = GetSecureHttpClient();
            int successCount = 0;
            foreach (string id in pks)
            {
                var deleteResult = httpClient.DeleteAsync(dbName + "/" + id).Result;
                if (deleteResult.IsSuccessStatusCode)
                {
                    successCount++;
                }
            }
            return successCount;
        }

        public override bool SaveEntity(V entity)
        {
            var httpClient = GetSecureHttpClient();
            ConvertUtil.ToDictRef(entity, out Dictionary<string, object> dict);
            if (!dict.TryGetValue("_id", out _))
            {
                object pkValue = pkColumn.GetMethod.Invoke(entity, null);
                dict.TryAdd("_id", pkValue);
            }
            var jsonConent = JsonSerializer.Serialize(dict);
            var httpContent = new StringContent(jsonConent, Encoding.UTF8, "application/json");
            var requestResult = httpClient.PostAsync(dbName, httpContent).Result;
            if (requestResult.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public override bool UpdateEntity(V entity)
        {
            var httpClient = GetSecureHttpClient();
            object pkId = pkColumn.GetMethod.Invoke(entity, null);
            V origin = GetById(pkId.ToString());
            ConvertUtil.WrapUpdate(origin, entity);
            ConvertUtil.ToDictRef(entity, out Dictionary<string, object> dict);
            if (!dict.TryGetValue("_id", out _))
            {
                object? pkValue = pkColumn.GetMethod.Invoke(entity, null);
                dict.TryAdd("_id", pkValue);
            }
            var jsonConent = JsonSerializer.Serialize(dict);
            var httpContent = new StringContent(jsonConent, Encoding.UTF8, "application/json");
            var requestResult = httpClient.PutAsync(dbName + "/" + pkId, httpContent).Result;
            if (requestResult.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        private HttpClient GetSecureHttpClient()
        {
            var httpClient = clientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.BaseAddress = new Uri(dbUrl);
            if (!string.IsNullOrWhiteSpace(dbCredential))
            {
                var credentialBytes = Encoding.ASCII.GetBytes(dbCredential);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(credentialBytes));
            }
            return httpClient;
        }
        protected override void Dispose(bool disposable)
        {
            if (disposable)
            {

            }
        }
    }
}
