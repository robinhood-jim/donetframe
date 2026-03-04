
using Frameset.Common.Streaming.Consumer;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Org.Apache.Rocketmq;


namespace Frameset.Bigdata.RocketMq
{
    public class RocketMqConsumer<V> : AbstractQueueConsumer<V>
    {
        private readonly string SecretKey;
        private readonly string AccessKey;
        private readonly string EndPoint;
        private SimpleConsumer Consumer;
        private string Topics;
        private readonly string ConsumerGroup;
        public RocketMqConsumer(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQSECRETKEY, out SecretKey);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQACCESSKEY, out AccessKey);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQENDPOINT, out EndPoint);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMTOPICS, out Topics);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQCONSUMERGROUPID, out ConsumerGroup);
            var credentialProvider = new StaticSessionCredentialsProvider(AccessKey, SecretKey);
            var clientConfig = new ClientConfig.Builder().SetEndpoints(EndPoint)
                .SetCredentialsProvider(credentialProvider).Build();
            DoInit(clientConfig);

        }
        private async void DoInit(ClientConfig clientConfig)
        {
            Dictionary<string, FilterExpression> Subscription = new Dictionary<string, FilterExpression> { { Topics, new FilterExpression("*") } };
            Consumer =await new SimpleConsumer.Builder().SetClientConfig(clientConfig)
                .SetConsumerGroup(ConsumerGroup)
                .SetAwaitDuration(TimeSpan.FromSeconds(15))
                .SetSubscriptionExpression(Subscription).Build();

        }

        
        public override List<V> PoolMessage(Action<V> action)
        {   
            return ReceiveAnsyc(action).Result;
        }
        private async Task<List<V>> ReceiveAnsyc(Action<V> action)
        {
            var messages = await Consumer.Receive(maxReturnSize, TimeSpan.FromSeconds(15));
            List<V> retMsgs = new(maxReturnSize);
            while (!messages.IsNullOrEmpty())
            {
                foreach(var message in messages)
                {
                    V entity = DSerailize(message.Body);
                    if (action != null)
                    {
                        action.Invoke(entity);
                    }
                    retMsgs.Add(entity);
                }
            }
            return retMsgs;
        }
        protected sealed override void Dispose(bool disposable)
        {
            base.Dispose(disposable);
            Consumer?.Dispose();

        }
    }
}
