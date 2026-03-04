using Frameset.Common.Streaming.Producer;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Org.Apache.Rocketmq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameset.Bigdata.RocketMq
{
    public class RocketMqProducer<V> : AbstractQueueProducer<V>
    {
        private readonly string SecretKey;
        private readonly string AccessKey;
        private readonly string EndPoint;
        private Producer producer;
        private string Topics;
        private string MessageGroup;
        public RocketMqProducer(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQSECRETKEY, out SecretKey);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQACCESSKEY, out AccessKey);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQENDPOINT, out EndPoint);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMTOPICS, out Topics);
            define.ResourceConfig.TryGetValue(ResourceConstants.ROCKETMQMESSAGEGROUP, out MessageGroup);
            Trace.Assert(!string.IsNullOrWhiteSpace(SecretKey) && !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(EndPoint), "");
            var credentialProvider = new StaticSessionCredentialsProvider(AccessKey, SecretKey);
            var clientConfig = new ClientConfig.Builder().SetEndpoints(EndPoint)
                .SetCredentialsProvider(credentialProvider).Build();

            DoInit(clientConfig);
        }
        public async void DoInit(ClientConfig clientConfig)
        {
            producer = await new Producer.Builder().SetTopics(Topics).SetClientConfig(clientConfig).Build();
        }
        public override bool SendMessage(string queueName, string key, V message)
        {
            var sendmsg = new Message.Builder().SetTopic(queueName)
                .SetKeys(key).SetBody(Serailize(message)).SetMessageGroup(MessageGroup).Build();
            return DoSend(sendmsg).Result;
        }
        private async Task<bool> DoSend(Message message)
        {
            var sendReceipt = await producer.Send(message);
            return string.IsNullOrWhiteSpace(sendReceipt.MessageId);
        }
    }
}
