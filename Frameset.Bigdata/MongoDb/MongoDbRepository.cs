using Frameset.Bigdata.NoSql;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
namespace Frameset.Bigdata.MongoDb
{
    public class MongoDbRepository<V, P> : NoSqlRepository<V, P> where V : BaseEntity
    {
        private MongoClient client;
        private IMongoDatabase database;
        public MongoDbRepository(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.MONGODBURL, out string? connectUrl);
            connectUrl = connectUrl ?? ResourceConstants.MONGODBDEFAULTURL;
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectUrl));
            client = new MongoClient(settings);
            database = client.GetDatabase(content.Schema);
        }

        public override V GetById(P pk)
        {
            var collection = database.GetCollection<BsonDocument>(content.TableName);
            if (collection != null)
            {
                var filter = Builders<BsonDocument>.Filter.Eq(pkColumn.FieldName, pk);
                var result = collection.Find(filter).ToList();
                if (!result.IsNullOrEmpty())
                {
                    if (result.Count > 1)
                    {
                        throw new OperationFailedException("getById return more than one rows!");
                    }
                    else
                    {
                        BsonDocument bson = result[0];
                        V entity = Activator.CreateInstance<V>();
                        foreach (FieldContent fieldContent in fieldContents)
                        {
                            var obj = bson[fieldContent.PropertyName];
                            if (obj != null)
                            {
                                fieldContent.SetMethod.Invoke(entity, new object[] { obj });
                            }
                        }
                        return entity;
                    }
                }
                else
                {
                    throw new OperationFailedException("getById return none rows!");
                }
            }
            else
            {
                throw new OperationFailedException("collection" + content.TableName + " does not exists!");
            }
        }

        public override int RemoveEntity(IList<P> pks)
        {
            throw new NotImplementedException();
        }



        public override bool SaveEntity(V entity)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateEntity(V entity)
        {
            throw new NotImplementedException();
        }
    }
}
