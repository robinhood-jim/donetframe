using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

namespace Frameset.Common.Streaming.Consumer
{
    public class RabbitMqConsumer<T> : AbstractQueueConsumer<T>
    {
        private ConnectionFactory factory;
        private IConnection connection;
        private IChannel channel;
        private string? host;
        private int? port;
        private string? userName;
        private string? passwd;
        private bool bindable = false;
        private readonly string exchange = null!;
        private readonly string routingKey = null!;
        public RabbitMqConsumer(DataCollectionDefine define) : base(define)
        {
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQHOST, out host);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQPORT, out string? portStr);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQUSER, out userName);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQPASSWD, out passwd);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQEXCHANGE, out exchange);
            define.ResourceConfig.TryGetValue(ResourceConstants.RABBITMQROUTINGKEY, out routingKey);
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
            channel.BasicQosAsync(10000, 64, false).RunSynchronously();
            exchange = exchange ?? string.Empty;
            routingKey = routingKey ?? string.Empty;
        }

        public override List<T> PoolMessage(string queueName, Action<T> action)
        {
            if (!bindable)
            {
                channel.QueueBindAsync(queueName, exchange, routingKey).RunSynchronously();
                bindable = true;
            }
            List<T> retList = new();
            PoolMessage(queueName, retList).RunSynchronously();
            if (!retList.IsNullOrEmpty() && action != null)
            {
                foreach (var item in retList)
                {
                    action.Invoke(item);
                }
            }
            return retList;
        }
        private async Task PoolMessage(string queueName, List<T> values)
        {
            BasicGetResult? result;
            while ((result = await channel.BasicGetAsync(queueName, false)) != null)
            {
                T getObj = DSerailize(result.Body.ToArray());
                values.Add(getObj);
                await channel.BasicAckAsync(result.DeliveryTag, false);
            }
        }
    }
}
