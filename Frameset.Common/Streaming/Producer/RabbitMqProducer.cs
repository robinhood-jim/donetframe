using Frameset.Common.Data;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

namespace Frameset.Common.Streaming.Producer
{
    public class RabbitMqProducer<T> : AbstractQueueProducer<T>
    {
        private ConnectionFactory factory;
        private IConnection connection;
        private IChannel channel;
        private string? host;
        private int? port;
        private string? userName;
        private string? passwd;
        //private string? exchange;
        //private string? routingKey;
        public RabbitMqProducer(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQHOST, out host);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQPORT, out string? portStr);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQUSER, out userName);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQPASSWD, out passwd);
            host = host ?? ResourceConstants.RABBITMQDEFAULTHOST;
            port = portStr.IsNullOrEmpty() ? ResourceConstants.RABBITMQDEFAULTPORT : Convert.ToInt32(portStr);

            factory = new ConnectionFactory
            {
                HostName = host,
                Port = port ?? ResourceConstants.RABBITMQDEFAULTPORT
            };
            if (!userName.IsNullOrEmpty() && !passwd.IsNullOrEmpty())
            {
                factory.UserName = userName;
                factory.Password = passwd;
            }
            connection = factory.CreateConnectionAsync().Result;
            channel = connection.CreateChannelAsync().Result;

        }

        public override bool SendMessage(string queueName, string key, T message)
        {

            return Publish(queueName, key, message).Current;
        }
        private async IAsyncEnumerator<bool> Publish(string queueName, string key, T message)
        {
            await channel.BasicPublishAsync(queueName, key, Serailize(message));
            yield return true;
        }
    }
}
