using dotnet_etcd;
using Etcdserverpb;
using Google.Protobuf;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using V3Electionpb;

namespace Frameset.Core.Raft
{
    public class EtcdServiceClient : IDisposable
    {
        private EtcdClient etcdClient;
        public EtcdServiceClient(string hostUrl)
        {
            Trace.Assert(!hostUrl.IsNullOrEmpty(), "");
            etcdClient = new EtcdClient(hostUrl, configureChannelOptions: options => options.Credentials = Grpc.Core.ChannelCredentials.Insecure);

        }
        public async Task<bool> Campaign(string nodePath, string nodeValue)
        {
            var leaseResponse = etcdClient.LeaseGrant(new LeaseGrantRequest { TTL = 10 });
            etcdClient.LeaseKeepAlive(leaseResponse.ID, CancellationToken.None).ConfigureAwait(false);
            var campaignRequest = new CampaignRequest
            {
                Lease = leaseResponse.ID,
                Name = ByteString.CopyFromUtf8(nodePath),
                Value = ByteString.CopyFromUtf8(nodeValue),
            };
            var response = etcdClient.Campaign(campaignRequest);

            if (response.Leader == null || response.Leader.Lease == leaseResponse.ID)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> LeaderElectionAsync(long leaseId, string nodePath, byte[] values, CancellationToken cancellationToken)
        {
            try
            {
                var txn = new TxnRequest();

                // 條件：如果 LeaderPath 的 CreateRevision 等於 0 (代表此 Key 尚不存在，無人佔領)
                txn.Compare.Add(new Compare
                {
                    Key = ByteString.CopyFromUtf8(nodePath),
                    Target = Compare.Types.CompareTarget.Create,
                    Result = Compare.Types.CompareResult.Equal,
                    CreateRevision = 0
                });

                txn.Success.Add(new RequestOp
                {
                    RequestPut = new PutRequest
                    {
                        Key = ByteString.CopyFromUtf8(nodePath),
                        Value = ByteString.CopyFrom(values),
                        Lease = leaseId
                    }
                });

                TxnResponse txnResult = await etcdClient.TransactionAsync(txn, cancellationToken: cancellationToken);
                return txnResult.Succeeded;

            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public void KeepAlive(string nodePath, byte[] content, CancellationToken cancellation)
        {
            var leaseResponse = etcdClient.LeaseGrant(new LeaseGrantRequest { TTL = 10 });
            etcdClient.LeaseKeepAlive(leaseResponse.ID, cancellation).ConfigureAwait(false);

            var key = ByteString.CopyFromUtf8(nodePath);
            var proposal = ByteString.CopyFrom(content);

            var keyPutRequset = new PutRequest
            {
                Key = key,
                Lease = leaseResponse.ID,
                Value = proposal
            };
            etcdClient.Put(keyPutRequset);

        }
        public void PutValue(string nodePath, byte[] data)
        {
            var keyPutRequset = new PutRequest
            {
                Key = ByteString.CopyFromUtf8(nodePath),
                Value = ByteString.CopyFrom(data)
            };
            etcdClient.Put(keyPutRequset);
        }
        public void Watch(string groupPath, Action<WatchResponse> action)
        {
            etcdClient.Watch(groupPath, action);
        }
        public List<Tuple<string, long>> GetChildrens(string basePath)
        {

            List<Tuple<string, long>> childrens = [];
            var response = etcdClient.GetRange(basePath);
            var kvs = response.Kvs;
            if (!kvs.IsNullOrEmpty())
            {
                foreach (var kv in kvs)
                {
                    long startTs = Convert.ToInt64(kv.Value.ToStringUtf8());
                    childrens.Add(Tuple.Create(kv.Key.ToStringUtf8(), startTs));
                }
            }
            return childrens;
        }
        public EtcdClient GetClient()
        {
            return etcdClient;
        }

        public void Dispose()
        {
            if (etcdClient != null)
            {
                etcdClient.Dispose();
            }
        }
    }
}
