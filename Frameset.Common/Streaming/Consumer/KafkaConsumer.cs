using Confluent.Kafka;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.Streaming.Consumer
{
    public class KafkaConsumer<T> : AbstractQueueConsumer<T>
    {
        private IConsumer<string, byte[]> consumer;
        private readonly string groupId = null!;
        private readonly string? brokerUrl;
        private int maxReturnSize = 1000;
        private readonly string queueName;
        public KafkaConsumer(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.KAFKACONSUMERGROUPID, out groupId);
            Trace.Assert(groupId.IsNullOrEmpty(), "groupId missing");
            define.ResourceConfig.TryGetValue(ResourceConstants.KAFKABROKERURL, out brokerUrl);
            define.ResourceConfig.TryGetValue(ResourceConstants.KAFKAQUEUENAME, out queueName);
            Trace.Assert(!brokerUrl.IsNullOrEmpty(), " broker url must not be null");
            Trace.Assert(!string.IsNullOrWhiteSpace(queueName), "must provide queueName");
            var consumerConfig = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = brokerUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
            consumer.Subscribe(queueName);
        }

        public override List<T> PoolMessage(Action<T> action)
        {
            int groupSize = 0;

            List<T> retList = new(maxReturnSize);
            try
            {
                bool breakable = false;
                while (!breakable)
                {
                    var message = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (message != null && message.Message != null)
                    {
                        groupSize++;
                        T retObj = DSerailize(message.Message.Value);
                        action?.Invoke(retObj);
                        retList.Add(retObj);
                        if (groupSize == maxReturnSize)
                        {
                            breakable = true;
                        }
                    }
                    else
                    {
                        breakable = true;
                    }
                }
                consumer.Commit();
            }
            catch (ConsumeException ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }
            return retList;
        }

        protected sealed override void Dispose(bool disposable)
        {
            base.Dispose(disposable);
            consumer?.Dispose();

        }
    }
}
