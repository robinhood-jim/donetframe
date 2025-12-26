using Frameset.Bigdata.NoSql;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using Microsoft.HBase.Client;

namespace Frameset.Bigdata.Hbase
{
    public class HbaseRepository<V, P> : NoSqlRepository<V, P> where V : BaseEntity
    {
        private HBaseClient client;
        public HbaseRepository(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.HBASEPROTOBUFURL, out string? url);
            define.ResourceConfig.TryGetValue(ResourceConstants.HBASEUSERNAME, out string? userName);
            define.ResourceConfig.TryGetValue(ResourceConstants.HBASEPASSWD, out string? passwd);
            var credentials = new ClusterCredentials(new Uri(url), userName, passwd);
            client = new(credentials);

        }

        public override V GetById(P pk)
        {
            throw new NotImplementedException();
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
