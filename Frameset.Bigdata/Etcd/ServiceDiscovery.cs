using dotnet_etcd;
using Etcdserverpb;
using Google.Protobuf;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using V3Electionpb;

namespace Frameset.Bigdata.Etcd
{
    public class ServiceDiscovery
    {
        private EtcdClient etcdClient;
        internal string EndPoint
        {
            get; set;
        }
        internal ServiceDiscovery()
        {

        }
        internal void DoInit()
        {
            Trace.Assert(!EndPoint.IsNullOrEmpty(), "");
            etcdClient = new EtcdClient(EndPoint);

        }
        public bool RegisterInstance(string serverPath, string serverName, string electPath, string masterPath, bool takeLeaderShip)
        {
            var leaseResponse = etcdClient.LeaseGrant(new LeaseGrantRequest { TTL = 10 });
            etcdClient.LeaseKeepAlive(leaseResponse.ID, CancellationToken.None).ConfigureAwait(false);

            var key = ByteString.CopyFromUtf8(serverPath);
            var proposal = ByteString.CopyFromUtf8(serverName);

            var keyPutRequset = new PutRequest
            {
                Key = key,
                Lease = leaseResponse.ID,
                Value = proposal
            };
            etcdClient.Put(keyPutRequset);
            if (takeLeaderShip)
            {
                var compaingRequest = new CampaignRequest
                {
                    Name = ByteString.CopyFromUtf8(electPath),
                    Lease = leaseResponse.ID,
                    Value = proposal
                };
                var response = etcdClient.Campaign(compaingRequest);
                if (response != null)
                {
                    var putRequest = new PutRequest
                    {
                        Key = ByteString.CopyFromUtf8(masterPath),
                        Value = proposal
                    };

                    return response.Leader.Lease == leaseResponse.ID;
                }
                return false;
            }
            return true;
        }
        public void GetLeader(string masterPath)
        {

        }
        public void Watch(string groupPath, Action<WatchResponse> action)
        {
            etcdClient.Watch(groupPath, action);
        }

    }
    public class Builder
    {
        private ServiceDiscovery discovery = new ServiceDiscovery();
        private Builder()
        {

        }
        public static Builder NewBuilder()
        {
            return new Builder();
        }
        public Builder WithEndPoint(string endPoint)
        {
            discovery.EndPoint = endPoint;
            return this;
        }
        public ServiceDiscovery Build()
        {
            discovery.DoInit();
            return discovery;
        }

    }
}
