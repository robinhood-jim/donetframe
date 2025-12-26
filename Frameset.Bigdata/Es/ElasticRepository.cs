using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Frameset.Bigdata.NoSql;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Bigdata.Es
{
    public class ElasticRepository<V, P> : NoSqlRepository<V, P> where V : BaseEntity
    {
        protected ElasticsearchClient client;
        public ElasticRepository(DataCollectionDefine define) : base(define)
        {

            define.ResourceConfig.TryGetValue(ResourceConstants.ELASTICENDPOINTS, out string? endpoints);

            Trace.Assert(!endpoints.IsNullOrEmpty(), "");
            var nodes = endpoints?.Split('.').AsEnumerable<string>().Select(x => new Uri(x)).ToList();

            var pool = new StaticNodePool(nodes);
            var settings = new ElasticsearchClientSettings(pool);
            if (define.ResourceConfig.TryGetValue(ResourceConstants.ELASTICUSERNAME, out string? userName) &&
                define.ResourceConfig.TryGetValue(ResourceConstants.ELASTICPASSWD, out string? password) && !userName.IsNullOrEmpty() && !password.IsNullOrEmpty())
            {
                settings.Authentication(new BasicAuthentication(userName, password));
            }
            client = new ElasticsearchClient(settings);

        }
        public override bool SaveEntity(V entity)
        {
            object? id = pkColumn.GetMethod.Invoke(entity, []);
            IndexResponse response = client.IndexAsync(entity, idx =>
            {
                idx.Index(content.TableName);
                if (id != null)
                {
                    idx.Id(new Id(id));
                }
            }).Result;

            return response.IsSuccess();
        }

        public override bool UpdateEntity(V entity)
        {
            object? id = pkColumn.GetMethod.Invoke(entity, []);
            UpdateResponse<string> response = client.UpdateAsync(content.TableName, id, u => u.Doc(entity)).Result;
            return response.IsValidResponse;
        }

        public override int RemoveEntity(IList<P> pks)
        {
            List<FieldValue> idList = new();
            foreach (P pk in pks)
            {
                idList.Add(GetFieldValue(pk));
            }
            var ids = new string[] { "1", "2" };
            DeleteByQueryResponse response = client.DeleteByQuery<V>(content.TableName, action =>
                action.Query(rq => rq
                    .Bool(b => b
                        .Must(m => m
                            .Terms(ts => ts
                                .Field("_id").Terms(new TermsQueryField(idList))
                            )
                        )
                    )
                )
            );
            if (response.IsSuccess())
            {
                return (int)response.Total;
            }
            return -1;
        }

        private FieldValue GetFieldValue(object pk)
        {
            return Type.GetTypeCode(pk.GetType()) switch
            {
                TypeCode.Int32 => FieldValue.Long(long.Parse(pk.ToString())),
                TypeCode.Int64 => FieldValue.Long(long.Parse(pk.ToString())),
                TypeCode.Boolean => FieldValue.Boolean(string.Equals(Constants.TRUEVALUE, pk.ToString(), StringComparison.OrdinalIgnoreCase)),
                TypeCode.Double => FieldValue.Double(double.Parse(pk.ToString())),
                _ => FieldValue.String(pk.ToString())
            };
        }
        private Number GetNumber(object input)
        {
            return Type.GetTypeCode(input.GetType()) switch
            {
                TypeCode.Int32 => new Number(int.Parse(input.ToString())),
                TypeCode.Int64 => new Number(long.Parse(input.ToString())),
                TypeCode.Double => new Number(double.Parse(input.ToString())),
                TypeCode.Decimal => new Number(double.Parse(input.ToString())),
                _ => new Number(int.Parse(input.ToString()))
            };

        }
        public override V GetById(P pk)
        {
            var response = client.Get<V>(new GetRequest(content.TableName, new Id(pk)));
            if (response.Found)
            {
                return response.Source;
            }
            else
            {
                throw new OperationFailedException("getById return none rows!");
            }
        }

        public IList<Dictionary<string, object>> QueryBySql(string sql, object[] values)
        {
            throw new NotImplementedException();
        }
        public override IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            fieldMap.TryGetValue(propertyName, out FieldContent? fieldContent);
            Trace.Assert(fieldContent != null, "");
            var response = client.Search<V>(s =>
                {
                    var query = s
                    .Query(q => q
                        .Bool(b => b
                            .Must(m =>
                                WrapQuery(m, fieldContent, oper, values)
                            )
                         )
                    );
                    if (orderByStr.IsNullOrEmpty())
                    {
                        string[] sortArr = orderByStr.Split(' ');
                        query.Sort(so => so.Field(sortArr[0]).Score(sc => sc.Order(string.Equals("asc", sortArr[1], StringComparison.OrdinalIgnoreCase) ? SortOrder.Asc : SortOrder.Desc)));
                    }
                }
                );
            if (response.IsSuccess())
            {
                return response.Hits.ToList().Select(h => h.Source).ToList();
            }
            else
            {
                throw new OperationFailedException("query error!");
            }
        }
        public override PageDTO<V> QueryModelsPage(PageQuery query)
        {
            if (query != null && !query.Parameters.IsNullOrEmpty())
            {
                int currentParamCount = 0;
                QueryDescriptor<V> descriptor = new QueryDescriptor<V>();
                descriptor.Bool(b => ConstructQuery(b, query));
                SearchRequest<V> request = new SearchRequest<V>();
                request.Query = descriptor;
                request.From = ((int)query.PageCount - 1) * (int)query.PageSize;
                request.Size = (int)query.PageSize;
                if (!query.Order.IsNullOrEmpty())
                {
                    request.Sort = [new SortOptions
                    {
                        Field = new FieldSort(new Field(query.Order)),
                        Score = new ScoreSortDescriptor().Order(query.OrderAsc?SortOrder.Asc:SortOrder.Desc)
                    }];
                }
                else if (!query.OrderField.IsNullOrEmpty())
                {
                    string[] arr = query.OrderField.Split(' ');
                    request.Sort = [new SortOptions
                    {
                        Field = new FieldSort(new Field(arr[0])),
                        Score = new ScoreSortDescriptor().Order(string.Equals("asc",arr[1],StringComparison.OrdinalIgnoreCase)?SortOrder.Asc:SortOrder.Desc)
                    }];
                }

                var response = client.Search<V>(request);
                if (response.IsSuccess())
                {
                    PageDTO<V> dto = new PageDTO<V>(response.Total, query.PageSize);
                    dto.Results = response.Hits.ToList().Select(h => h.Source).ToList();
                    return dto;
                }
                else
                {
                    throw new OperationFailedException("query error!");
                }

            }
            else
            {
                throw new OperationFailedException("query paramter is empty!");
            }
        }
        private void ConstructQuery(BoolQueryDescriptor<V> descriptor, PageQuery query)
        {
            foreach (var item in query.Parameters)
            {
                if (fieldMap.TryGetValue(item.Key, out FieldContent? fieldContent) && fieldContent != null && item.Value != null)
                {
                    Constants.SqlOperator oper = Constants.SqlOperator.EQ;
                    if (item.Value.ToString().Contains('%'))
                    {
                        descriptor.Must(m => WrapQuery(m, fieldContent, oper, [item.Value]));
                    }
                }
            }
        }
        private void WrapQuery(QueryDescriptor<V> descriptor, FieldContent fieldContent, Constants.SqlOperator oper, object[] values)
        {
            switch (oper)
            {
                case Constants.SqlOperator.EQ:
                    descriptor.Term(t => t.Field(fieldContent.PropertyName).Value(GetFieldValue(values[0])));
                    break;
                case Constants.SqlOperator.NE:
                    descriptor.Bool(b => b.MustNot(m => m.Term(t => t.Field(fieldContent.PropertyName).Value(GetFieldValue(values[0])))));
                    break;
                case Constants.SqlOperator.GT:
                    descriptor.Range(r => r.Number(nr => nr.Field(fieldContent.PropertyName).Gt(GetNumber(values[0]))));
                    break;
                case Constants.SqlOperator.GE:
                    descriptor.Range(r => r.Number(nr => nr.Field(fieldContent.PropertyName).Gte(GetNumber(values[0]))));
                    break;
                case Constants.SqlOperator.LT:
                    descriptor.Range(r => r.Number(nr => nr.Field(fieldContent.PropertyName).Lt(GetNumber(values[0]))));
                    break;
                case Constants.SqlOperator.LE:
                    descriptor.Range(r => r.Number(nr => nr.Field(fieldContent.PropertyName).Lte(GetNumber(values[0]))));
                    break;
                case Constants.SqlOperator.IN:
                    List<FieldValue> idList = new();
                    foreach (object obj in values)
                    {
                        idList.Add(GetFieldValue(obj));
                    }
                    descriptor.Terms(term => term.Field(fieldContent.PropertyName).Terms(new TermsQueryField(idList)));
                    break;
                case Constants.SqlOperator.LIKE:
                case Constants.SqlOperator.LLIKE:
                case Constants.SqlOperator.RLIKE:
                    descriptor.Fuzzy(f => f.Field(fieldContent.PropertyName).Value(values[0]));
                    break;
                case Constants.SqlOperator.NOTIN:
                    List<FieldValue> idList1 = new();
                    foreach (object obj in values)
                    {
                        idList1.Add(GetFieldValue(obj));
                    }
                    descriptor.Bool(b => b.MustNot(m => m.Terms(term => term.Field(fieldContent.PropertyName).Terms(new TermsQueryField(idList1)))));
                    break;
                case Constants.SqlOperator.BT:
                    descriptor.Range(r => r.Number(nr => nr.Field(fieldContent.PropertyName).Gte(GetNumber(values[0])).Lte(GetNumber(values[1]))));
                    break;
                default:
                    descriptor.Term(t => t.Field(fieldContent.PropertyName).Value(GetFieldValue(values[0])));
                    break;
            }
        }

    }
}
