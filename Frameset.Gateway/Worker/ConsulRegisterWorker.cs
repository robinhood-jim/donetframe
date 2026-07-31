using Consul;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Gateway.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Frameset.Gateway.Worker;

public class ConsulRegisterWorker : IHostedService
{
    private readonly IConsulClient consulClient;
    private readonly IConfiguration configuration;
    private readonly IOptionsMonitor<GatewayConfig> configMointor;
    private IDisposable? changeToken;
    private GatewayConfig activeConfig;
    private string gatewayId;

    public ConsulRegisterWorker(IConsulClient consulClient, IConfiguration configuration,IOptionsMonitor<GatewayConfig> configMointor)
    {
        this.consulClient = consulClient;
        this.configuration = configuration;
        this.configMointor = configMointor;
    }

    public async Task StartAsync(CancellationToken token)
    {
        activeConfig = configMointor.CurrentValue;
        gatewayId = activeConfig.ServiceId;
        
        var gatewayService = new AgentServiceRegistration()
        {
            ID = activeConfig.ServiceId,
            Name = activeConfig.ServiceName,
            Address = activeConfig.ServiceIp,
            Port = activeConfig.ServicePort,
            Tags = Array.Empty<string>(),
            Check = new AgentServiceCheck()
            {
                HTTP = $"http://{activeConfig.ServiceIp}:{activeConfig.ServicePort}/health",
                Interval = TimeSpan.FromSeconds(10)
            }
        };
        try
        {
            await consulClient.Agent.ServiceRegister(gatewayService, token);
        }
        catch (Exception ex)
        {
            throw new ConfigIncorrectException(ex.Message);
        }
        List<ServicesModel> list = activeConfig.Services;
        //get register service
        Dictionary<string, AgentService> dict = consulClient.Agent.Services().Result.Response;
        if (!list.IsNullOrEmpty())
        {
            foreach(ServicesModel model in list)
            {
                string serviceName = model.Name;
                if (!model.Nodes.IsNullOrEmpty())
                {
                    foreach (ServicesModel.Node node in model.Nodes)
                    {
                        if (dict.TryGetValue(node.Id, out AgentService? service))
                        {
                            consulClient.Agent.ServiceDeregister(node.Id, token);
                        }
                        await RegisterService(node, serviceName,token);
                    }
                }
            }
        }
        ScanRouters(activeConfig);
        
        changeToken = configMointor.OnChange(async newConfig => {
            bool equals = activeConfig.Equals(newConfig);
            if (!equals)
            {
                Dictionary<int, List<ServicesModel.Node>> diff = ServicesModel.Diff(activeConfig.Services,
                    newConfig.Services);
                if (diff.TryGetValue(ServicesModel.ADD, out List<ServicesModel.Node>? addNodes))
                {
                    foreach (ServicesModel.Node node in addNodes)
                    {
                        await RegisterService(node,node.ServiceName ,token);
                    }
                }
                if (diff.TryGetValue(ServicesModel.MODIFY, out List<ServicesModel.Node>? modifyNodes))
                {
                    foreach (ServicesModel.Node node in addNodes)
                    {
                        await consulClient.Agent.ServiceDeregister(node.Id);
                        await RegisterService(node,node.ServiceName ,token);
                    }
                }
                if (diff.TryGetValue(ServicesModel.DELETE, out List<ServicesModel.Node>? deleteNodes))
                {
                    foreach (ServicesModel.Node node in addNodes)
                    {
                        await consulClient.Agent.ServiceDeregister(node.Id);
                    }
                }
                activeConfig = newConfig;
                ScanRouters(activeConfig);
            }
            else
            {
                Log.Information("Service config no change!");
            }
        });
    }

    private async Task RegisterService(ServicesModel.Node node, string serviceName,CancellationToken token)
    {
        var registration = new AgentServiceRegistration()
        {
            ID = node.Id,
            Name = serviceName,
            Address = node.Address,
            Port = node.Port,
            Tags = node.Tags?? Array.Empty<string>(),
            Check = new AgentServiceCheck()
            {
                HTTP = $"http://{node.Address}:{node.Port}/health",
                Interval = TimeSpan.FromSeconds(10)
            }
        };
        await consulClient.Agent.ServiceRegister(registration, token);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        changeToken?.Dispose();
        consulClient.Agent.ServiceDeregister(gatewayId);
        if (!activeConfig.Services.IsNullOrEmpty())
        {
            foreach (ServicesModel model in activeConfig.Services)
            {
                foreach (ServicesModel.Node node in model.Nodes)
                {
                    consulClient.Agent.ServiceDeregister(node.Id, cancellationToken);
                }
            }
        }
        return Task.CompletedTask;
    }

    internal static void ScanRouters(GatewayConfig config)
    {
        if (!config.Routes.IsNullOrEmpty())
        {
            foreach (RouteDefine routeDefine in config.Routes)
            {
                if (!routeDefine.Predicates.IsNullOrEmpty())
                {
                    foreach (string configTxt in routeDefine.Predicates)
                    {
                        if (configTxt.StartsWith("Path="))
                        {
                            routeDefine.PathPattern = configTxt.Substring(5);
                        }
                    }
                }
                if (!routeDefine.Filters.IsNullOrEmpty())
                {
                    foreach (string configTxt in routeDefine.Predicates)
                    {
                        if (configTxt.StartsWith("StripPrefix="))
                        {
                            routeDefine.StripPrefix = Constants.VALID.Equals(configTxt.Substring(12));
                        }
                    }
                }
            }
        }
    }
    
}