using Frameset.Bigdata.NoSql;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using Frameset.Core.Utils;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;
using static Gremlin.Net.Process.Traversal.P;

namespace Frameset.Bigdata.Graph
{
    public class HugeGraphRepository<V, P> : NoSqlRepository<V, P> where V : BaseEntity where P : notnull
    {
        GremlinClient client;
        string HostName = "localhost";
        int port = 8182;
        bool enableSsl = false;
        string? UserName;
        string? Passwd;
        GraphTraversalSource g;
        public HugeGraphRepository(DataCollectionDefine collectionDefine) : base(collectionDefine)
        {
            collectionDefine.ResourceConfig.TryGetValue(ResourceConstants.JANUSHOST, out string? HostNameStr);
            collectionDefine.ResourceConfig.TryGetValue(ResourceConstants.JANUSPORT, out string? portStr);
            collectionDefine.ResourceConfig.TryGetValue(ResourceConstants.JANUSENABLESSL, out string? enableSslStr);
            if (!string.IsNullOrWhiteSpace(HostNameStr))
            {
                HostName = HostNameStr;
            }

            if (!string.IsNullOrWhiteSpace(portStr))
            {
                port = Convert.ToInt32(portStr);
            }
            if (!string.IsNullOrWhiteSpace(enableSslStr) && string.Equals(Constants.VALID, enableSslStr, StringComparison.OrdinalIgnoreCase))
            {
                enableSsl = true;
            }
            collectionDefine.ResourceConfig.TryGetValue(ResourceConstants.JANUSUSER, out UserName);
            collectionDefine.ResourceConfig.TryGetValue(ResourceConstants.JANUSPASSWD, out Passwd);
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Passwd))
            {
                client = new GremlinClient(new GremlinServer(HostName, port, enableSsl));
            }
            else
            {
                client = new GremlinClient(new GremlinServer(HostName, port, enableSsl, UserName, Passwd));
            }
            g = Traversal().With(new DriverRemoteConnection(client));
        }


        public IList<V> GetByProperty(string propertyName, string queryValue, string ValueField)
        {

            var values = g.V().HasLabel(content.GetTableName()).Has(propertyName, queryValue).Values<T>(ValueField);

            return values.Dedup().Limit<V>(Scope.Local, Convert.ToInt64("1000")).ToList();
        }

        public bool ExecuteScript(string script)
        {
            try
            {
                client.SubmitAsync<dynamic>(script);
                return true;
            }
            catch (Exception ex)
            {
                LogUtils.Error(ex.Message);
                return false;
            }

        }

        public bool CreateSchema()
        {
            String pkName = fieldContents.Where(x => x.IfPrimary).First().FieldName;

            StringBuilder builder = new("schema=graph.schema();\\n");
            StringBuilder fieldNames = new();
            foreach (FieldContent meta in fieldContents)
            {
                builder.Append("schema.propertyKey(").Append("\"").Append(meta.FieldName).Append("\")");
                fieldNames.Append("\"").Append(meta.FieldName).Append("\",");
                switch (meta.DataType)
                {
                    case Constants.MetaType.CHAR:
                    case Constants.MetaType.CLOB:
                    case Constants.MetaType.STRING:
                        builder.Append(".asString()");
                        break;
                    case Constants.MetaType.DATE:
                    case Constants.MetaType.TIMESTAMP:
                        builder.Append(".asDate()");
                        break;
                    case Constants.MetaType.LONG:
                        builder.Append(".asLong()");
                        break;
                    case Constants.MetaType.FLOAT:
                        builder.Append(".asFloat()");
                        break;
                    case Constants.MetaType.DOUBLE:
                        builder.Append(".asDouble()");
                        break;
                    case Constants.MetaType.BOOLEAN:
                        builder.Append(".asBoolean()");
                        break;
                    default:
                        builder.Append(".asString()");
                        break;
                }

                builder.Append(".ifNotExists().create();\\n");
            }

            fieldNames.Remove(fieldNames.Length - 1, 1);
            builder.Append("schmea.vertexLabel(\"").Append(content.TableName).Append("\")")
                .Append(".properties(\"").Append(fieldNames).Append(")").Append(".primaryKeys(\"").Append(pkName)
                .Append("\").ifNotExist().create()");
            return ExecuteScript(builder.ToString());
        }
        public List<Edge> GetEdgeFromVertex(string verticesId, List<string> edgeLabelList)
        {
            var traveler = g.V(verticesId).BothE(edgeLabelList.ToArray()).Dedup().Limit<Edge>(Scope.Local, 1000);
            Dictionary<string, int> containDict = [];
            List<string> addEdgeids = [];
            List<Edge> returnEdges = [];
            while (traveler.HasNext())
            {
                Edge? edge = traveler.Next();
                if (edge == null || addEdgeids.Contains(edge.Id?.ToString()))
                {
                    continue;
                }
                object? source = edge.InV.Id;
                object? target = edge.OutV.Id;
                if (target == null || source == null)
                {
                    continue;
                }

                if (edgeLabelList.Contains(target?.ToString()) && edgeLabelList.Contains(source?.ToString()))
                {
                    addEdgeids.Add(edge.Id.ToString());
                    returnEdges.Add(edge);
                }
            }
            return returnEdges;
        }
        public IList<Vertex?> GetVertices(List<string> verticesIds)
        {
            var vertexs = g.V(verticesIds);
            if (vertexs.HasNext())
            {
                return vertexs.Dedup().ToList();
            }
            else
            {
                throw new MissingFieldException("none vertex found");
            }
        }
        public List<RelationContent> GetRelations(string relationId, string relationLabel, int limitCount)
        {
            //查找有label 边关系的所有vertex，出和进排除自己的id
            var realtions = g.V(relationId).In().Out().HasLabel(relationLabel).Filter(__.Not(__.HasId(relationId))).Dedup().Limit<Vertex>(limitCount);
            List<RelationContent> relationContents = [];
            while (realtions.HasNext())
            {
                Vertex? target = realtions.Next();
                if (target != null)
                {
                    RelationContent content = new RelationContent()
                    {
                        Id = target?.Id.ToString(),
                        Properties = target?.Properties
                    };
                    relationContents.Add(content);
                }
            }
            return relationContents;
        }

        public override V GetById(P pk)
        {
            string pkId = pk.ToString();

            IList<IDictionary<string, object>> list = g.V(pkId).HasLabel(content.TableName).Project<object>(pkColumn.PropertyName, GetProject()).By(T.Id).ToList();
            if (!list.IsNullOrEmpty())
            {
                if (list.Count == 1)
                {
                    IDictionary<string, object> dict = list[0];
                    V retEntity = Activator.CreateInstance<V>();
                    WrapEntity(dict, retEntity);
                    return retEntity;
                }
                else
                {
                    throw new NotFoundException("object not found");
                }
            }
            else
            {
                throw new OperationFailedException("found multiplex records");
            }
        }
        public override int RemoveEntity(IList<P> pks)
        {
            foreach (P pk in pks)
            {
                var v = g.V(pks.ToString()).HasLabel(content.TableName);
                if (v.HasNext() && v.Count().Current == 1)
                {
                    v.Drop().Next();
                }
            }
            return 1;
        }

        public override bool SaveEntity(V entity)
        {
            ConvertUtil.ToDictRef(entity, out Dictionary<string, object> dict);
            object? pkValue = pkColumn.GetMethod.Invoke(entity, null);
            var v = g.AddV(content.TableName).Property(T.Id, pkValue);
            foreach (FieldContent fieldContent in fieldContents)
            {
                if (dict.TryGetValue(fieldContent.PropertyName, out object value))
                {
                    v.Property(fieldContent.PropertyName, value);
                }
            }
            v.Next();
            return true;
        }

        public override bool UpdateEntity(V entity)
        {
            object? pkValue = pkColumn.GetMethod.Invoke(entity, null);
            var v = g.V(pkValue.ToString()).HasLabel(content.TableName);
            if (v.HasNext())
            {
                if (v.Count().Current == 1)
                {
                    ConvertUtil.ToDictRef(entity, out Dictionary<string, object> dict);
                    foreach (FieldContent fieldContent in fieldContents)
                    {
                        if (dict.TryGetValue(fieldContent.PropertyName, out object value))
                        {
                            v.Property(fieldContent.PropertyName, value);
                        }
                    }
                    v.Next();
                    return true;
                }
                else
                {
                    throw new OperationFailedException("found multiplex records");
                }
            }
            else
            {
                throw new NotFoundException("object not found");
            }
        }
        public override IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = "")
        {
            var q = g.V().HasLabel(content.TableName);
            IList<V> retList = [];
            switch (oper)
            {
                case Constants.SqlOperator.EQ:
                    q = q.Filter(__.Has(propertyName, values[0]));
                    break;
                case Constants.SqlOperator.LT:
                    q = q.Filter(__.Has(propertyName, Lt(values[0])));
                    break;
                case Constants.SqlOperator.LE:
                    q = q.Filter(__.Has(propertyName, Lte(values[0])));
                    break;
                case Constants.SqlOperator.GT:
                    q = q.Filter(__.Has(propertyName, Gt(values[0])));
                    break;
                case Constants.SqlOperator.GE:
                    q = q.Filter(__.Has(propertyName, Gte(values[0])));
                    break;
                case Constants.SqlOperator.BT:
                    q = q.Filter(__.Has(propertyName, Between(values[0], values[1])));
                    break;
                case Constants.SqlOperator.NE:
                    q = q.Filter(__.Has(propertyName, Neq(values[0])));
                    break;
                default:
                    throw new NotSupportedException();

            }
            IList<IDictionary<string, object>?> rsList = q.Project<object>(pkColumn.PropertyName, GetProject()).By(T.Id).Limit<IDictionary<string, object>>(1000).ToList();
            if (!rsList.IsNullOrEmpty())
            {
                foreach (IDictionary<string, object>? dict in rsList)
                {
                    if (!dict.IsNullOrEmpty())
                    {
                        V entity = Activator.CreateInstance<V>();
                        WrapEntity(dict, entity);
                        retList.Add(entity);
                    }
                }
                return retList;
            }
            else
            {
                throw new NotFoundException("query found nothing");
            }
        }
        public GraphTraversalSource GetGraphTraversal()
        {
            return g;
        }
        protected string[] GetProject()
        {
            List<string> projects = [];
            foreach (FieldContent fieldContent in fieldContents)
            {
                if (!fieldContent.IfPrimary)
                {
                    projects.Add(fieldContent.PropertyName);
                }
            }
            return projects.ToArray();
        }
        protected override void Dispose(bool disposable)
        {
            if (disposable)
            {
                client.Dispose();
            }
        }
        private void WrapEntity(IDictionary<string, object> dict, V retEntity)
        {
            foreach (FieldContent fieldContent in fieldContents)
            {
                if (dict.TryGetValue(fieldContent.PropertyName, out object? value))
                {
                    if (value != null)
                    {
                        fieldContent.SetMethod.Invoke(retEntity, [value]);
                    }
                }
            }
        }
    }
    public class RelationContent
    {
        public string Id
        {
            get; set;
        }
        public dynamic[] Properties
        {
            get; set;
        }

    }
}
