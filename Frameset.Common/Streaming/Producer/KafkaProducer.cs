using Confluent.Kafka;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.Streaming.Producer
{
    public class KafkaProducer<T> : AbstractQueueProducer<T>
    {
        private readonly IProducer<string, byte[]> producer;
        private readonly string? brokerUrl;
        public KafkaProducer(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.KAFKABROKERURL, out brokerUrl);
            Trace.Assert(!brokerUrl.IsNullOrEmpty());

            ProducerConfig config = new ProducerConfig
            {
                BootstrapServers = brokerUrl
            };
            producer = new ProducerBuilder<string, byte[]>(config).Build();
        }

        public override bool SendMessage(string queueName, string key, T message)
        {
            var result = producer.ProduceAsync(queueName, new Message<string, byte[]> { Key = key, Value = Serailize(message) }).Result;
            return result.Status == PersistenceStatus.Persisted;
        }
        protected sealed override void Dispose(bool disposable)
        {
            base.Dispose(disposable);
            producer?.Dispose();
        }
    }
}
