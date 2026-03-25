using Cassandra;
using Frameset.Bigdata.Dao;
using Frameset.Bigdata.NoSql;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using Frameset.Core.Repo;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;


namespace Frameset.Bigdata.Cassandra
{
    public class CassandraRepository<V, P> : NoSqlRepository<V, P> where V : BaseEntity
    {
        private readonly Cluster cluster;
        private readonly ISession session;
        private readonly string? keySpace;
        //private readonly IMapper mapper;
        private readonly Type modelType;
        public CassandraRepository(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.CASSANDRAURL, out string? connectUrl);
            Trace.Assert(connectUrl == null || !connectUrl.IsNullOrEmpty(), "connect url must not null!");
            define.ResourceConfig.TryGetValue(ResourceConstants.CASSANDRAKEYSAPCE, out keySpace);
            Trace.Assert(!keySpace.IsNullOrEmpty(), "connect url must not null!");
            define.ResourceConfig.TryGetValue(ResourceConstants.CASSANDRAUSERNAME, out string? userName);
            define.ResourceConfig.TryGetValue(ResourceConstants.CASSANDRAPASSWD, out string? passwd);
            define.ResourceConfig.TryGetValue(ResourceConstants.CASSANDRASSLPATH, out string? sslPath);
            var builder = Cluster.Builder().AddContactPoints(connectUrl.Split(','));
            if (!userName.IsNullOrEmpty() && !passwd.IsNullOrEmpty())
            {
                builder.WithAuthProvider(new PlainTextAuthProvider(userName, passwd));
            }
            if (!sslPath.IsNullOrEmpty())
            {
                var serverCertificateValidator = new CustomTrustStoreCertificateValidator(new X509Certificate2(sslPath));
                var sslOptions = new SSLOptions(SslProtocols.Tls12,
                    false,
                    (sender, certificate, chain, errors) => serverCertificateValidator.Validate(certificate)
                );
                builder.WithSSL(sslOptions);
            }

            cluster = builder.Build();
            session = cluster.Connect(keySpace);
            content.Schema = keySpace;
            /*mapper = new Mapper(session);
            var map = new Map<V>().TableName(content.GetTableName()).PartitionKey(u => pkColumn.PropertyInfomation);
            foreach (FieldContent fieldContent in fieldContents)
            {
                map.Column(u => fieldContent.PropertyInfomation, cm => cm.WithName(fieldContent.FieldName));
            }
            MappingConfiguration.Global.Define(map);*/
            modelType = typeof(V);
        }


        public override V GetById(P pk)
        {
            var ps = session.Prepare("select * from " + content.GetTableName() + " where " + pkColumn.FieldName + "=?");
            var stmt = ps.Bind(pk);
            var rows = session.Execute(stmt);
            if (!rows.IsNullOrEmpty())
            {
                if (rows.Count() > 1)
                {
                    throw new OperationFailedException("getById return more than one rows!");
                }
                V entity = Activator.CreateInstance<V>();
                if (rows.GetEnumerator().MoveNext())
                {
                    var row = rows.GetEnumerator().Current;
                    foreach (FieldContent fieldContent in fieldContents)
                    {
                        var obj = row[fieldContent.FieldName];
                        if (obj != null)
                        {
                            fieldContent.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(fieldContent.ParamType, obj) });
                        }
                    }
                    return entity;
                }
                else
                {
                    throw new BaseSqlException("id not found");
                }
            }
            else
            {
                throw new OperationFailedException("getById return none rows!");
            }
        }

        public override int RemoveEntity(IList<P> pks)
        {
            StringBuilder cqlBuilder = new StringBuilder("delete from " + content.GetTableName() + " where " + pkColumn.FieldName + " in (");
            for (int i = 0; i < pks.Count; i++)
            {
                cqlBuilder.Append("?");
                if (i < pks.Count - 1)
                {
                    cqlBuilder.Append(",");
                }
            }
            cqlBuilder.Append(")");
            PreparedStatement statement = session.Prepare(cqlBuilder.ToString());
            BoundStatement boundStatement = statement.Bind(pks.ToArray());
            session.Execute(boundStatement);
            return pks.Count;
        }

        public override bool SaveEntity(V entity)
        {
            InsertSegment segment = SqlGenUtils.GetInsertSegment(entity);
            PreparedStatement prepared = session.Prepare(segment.InsertSql);
            BoundStatement bound = prepared.Bind(segment.ParamObjects.ToArray());
            var rowset = session.Execute(bound);
            return true;
        }

        public override bool UpdateEntity(V entity)
        {
            FieldContent content = EntityReflectUtils.GetPrimaryKey(modelType);
            object? pkId = content.GetMethod.Invoke(entity, null);
            V origin = GetById((P)pkId);
            UpdateSegment updateSegment = SqlGenUtils.GetUpdateSegment(origin, entity);
            PreparedStatement statement = session.Prepare(updateSegment.UpdateSql);
            BoundStatement boundStatement = statement.Bind(updateSegment.ParameterObjects.ToArray());
            session.Execute(boundStatement);
            return true;
        }
        public override IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = "")
        {
            fieldMap.TryGetValue(propertyName, out FieldContent? fieldContent);
            IList<V> returnList = [];
            if (fieldContent == null)
            {
                throw new OperationFailedException("property " + propertyName + " not defined!");
            }
            StringBuilder selectSqlBuilder = new StringBuilder("select * from ").Append(content.GetTableName()).Append(" where ").Append(AppendParameter(fieldContent, oper, values.Length));
            if (!string.IsNullOrWhiteSpace(orderByStr))
            {
                selectSqlBuilder.Append(" ").Append(orderByStr);
            }
            PreparedStatement prepared = session.Prepare(selectSqlBuilder.ToString());
            BoundStatement boundStatement = prepared.Bind(values);
            var rowSet = session.Execute(boundStatement);
            while (rowSet.IsNullOrEmpty())
            {
                foreach (Row row in rowSet)
                {
                    V entity = Activator.CreateInstance<V>();
                    foreach (FieldContent fieldContent1 in fieldContents)
                    {
                        var obj = row[fieldContent1.FieldName];
                        if (obj != null)
                        {
                            fieldContent.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(fieldContent1.ParamType, obj) });
                        }
                    }
                    returnList.Add(entity);
                }
            }
            return returnList;
        }
        private static string AppendParameter(FieldContent fieldContent, Constants.SqlOperator oper, int parameterlength)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(fieldContent.PropertyName).Append(ParameterHelper.GetOperator(oper));
            switch (oper)
            {
                case Constants.SqlOperator.EQ:
                case Constants.SqlOperator.NE:
                case Constants.SqlOperator.GT:
                case Constants.SqlOperator.LT:
                case Constants.SqlOperator.GE:
                case Constants.SqlOperator.LE:
                case Constants.SqlOperator.LIKE:
                case Constants.SqlOperator.RLIKE:
                case Constants.SqlOperator.LLIKE:
                    builder.Append("?");
                    break;
                case Constants.SqlOperator.BT:
                    builder.Append("(?,?)");
                    break;
                case Constants.SqlOperator.IN:
                case Constants.SqlOperator.NOTIN:
                    builder.Append("(");
                    for (int i = 0; i < parameterlength; i++)
                    {
                        builder.Append("?");
                        if (i < parameterlength - 1)
                        {
                            builder.Append(",");
                        }
                    }
                    builder.Append(")");
                    break;
            }
            return builder.ToString();
        }
        private class CustomTrustStoreCertificateValidator
        {
            private readonly X509Certificate2 rootCertificate;
            public CustomTrustStoreCertificateValidator(X509Certificate2 rootCertificate)
            {
                this.rootCertificate = rootCertificate;
            }

            public bool Validate(X509Certificate serverCertificate)
            {
                var chain = new X509Chain();
                chain.ChainPolicy = new X509ChainPolicy()
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    CustomTrustStore = { rootCertificate }
                };
                var isValidChain = chain.Build(new X509Certificate2(serverCertificate));
                if (!isValidChain)
                {
                    foreach (var chainStatus in chain.ChainStatus)
                    {
                        Console.Error.WriteLine("Chain failed validation: {0} ({1})", chainStatus.Status, chainStatus.StatusInformation);
                    }
                    return false;
                }
                var chainRootCertificate = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                var isExpectedRoot = chainRootCertificate.RawData.SequenceEqual(rootCertificate.RawData);
                if (!isExpectedRoot)
                {
                    Console.Error.WriteLine("Partial chain passed validation, but the expected root certificate was not in the chain.");
                    return false;
                }
                return true;
            }
        }
        protected override void Dispose(bool disposable)
        {
            if (disposable)
            {
                session.Dispose();
                cluster.Dispose();
            }
        }
    }
}
