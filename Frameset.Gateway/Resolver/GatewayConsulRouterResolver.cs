using System.Collections.Concurrent;
using Consul;
using Frameset.Gateway.Models;
using Nacos.V2;

namespace Frameset.Gateway.Resolver;

public class GatewayConsulRouterResolver
{
    private IConsulClient consulClient;
    private IConfiguration configuration;
    private INacosNamingService namingService;
    private bool useConsulDiscovery ;
    private bool useNacosDiscovery ;
    private static readonly ConcurrentDictionary<string, int> _serviceIndexCache = new();

    public GatewayConsulRouterResolver(IConsulClient consulClient,INacosNamingService namingService,IConfiguration configuration)
    {
        this.consulClient = consulClient;
        this.configuration = configuration;
        this.namingService = namingService;
        if (consulClient != null)
        {
            useConsulDiscovery = true;
        }else if (namingService != null)
        {
            useNacosDiscovery = true;
        }
    }

    public async Task<string> ResolveUrlAsync(RouteDefine routeDefine,string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !routeDefine.Uri.StartsWith("lb://"))
        {
            return url;
        }
        var uri = new Uri(routeDefine.Uri);
        string serviceName = uri.Host;
        if (useConsulDiscovery)
        {
            var healthResult = await consulClient.Health.Service(serviceName, tag: null, passingOnly: true);
            var instances = healthResult.Response;

            if (instances == null || !instances.Any())
            {
                throw new InvalidOperationException(
                    $"[Consul Discovery] cant not found any instance of : {serviceName}");
            }
            var selectedNodeIndex = instances.Length == 1 ? 0 : SelectNextNodeRoundRobin(serviceName, instances.Length);
            var selectedNode=instances[selectedNodeIndex];
            var service = selectedNode.Service;
            string targetIp = string.IsNullOrEmpty(service.Address) ? selectedNode.Node.Address : service.Address;
            int targetPort = service.Port;
            //Router to Consul Service
            return $"http://{targetIp}:{targetPort}{url}";
        }else if (useNacosDiscovery)
        {
            var instances = await namingService.SelectInstances(serviceName, true);
            if (instances == null || !instances.Any())
            {
                throw new InvalidOperationException(
                    $"[Nacos Discovery] cant not found any instance of : {serviceName}");
            }
            var selectedNodeIndex = instances.Count == 1 ? 0 : SelectNextNodeRoundRobin(serviceName, instances.Count);
            var selectNode = instances[selectedNodeIndex];
            return $"http://{selectNode.Ip}:{selectNode.Port}{url}";
        }
        else
        {
            throw new InvalidOperationException(
                "none of Discovery Type (Consul or Nacos) is registered!");
        }
    }
    private int SelectNextNodeRoundRobin(string serviceName, int totalNodes)
    {
        // 使用原子操作更新計數器，並對當前健康的總節點數取模 (%)，防止索引越界
        int nextIndex = _serviceIndexCache.AddOrUpdate(
            serviceName, 
            0, 
            (key, currentVal) => (currentVal + 1) % totalNodes
        );

        // 防禦性程式碼：如果節點數量剛剛發生了劇烈變動（例如突然減少），導致計算出的索引越界，則安全重置為 0
        if (nextIndex >= totalNodes)
        {
            nextIndex = 0;
            _serviceIndexCache[serviceName] = 0;
        }

        return nextIndex;
    }
}