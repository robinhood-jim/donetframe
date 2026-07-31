using System.Diagnostics;
using Frameset.Core.Exceptions;
using Frameset.Gateway.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nacos.V2;
using Nacos.V2.Naming.Dtos;
using Serilog;

namespace Frameset.Gateway.Worker;

public class NacosRegisterWorker
{
    private readonly INacosNamingService namingService;
    private readonly IConfiguration configuration;
    private IOptionsMonitor<GatewayConfig> optionsMonitor;
    private GatewayConfig activeConfig;
    private string gatewayId;
    private IDisposable? changeToken;

    public NacosRegisterWorker(INacosNamingService namingService, 
        IConfiguration configuration, IOptionsMonitor<GatewayConfig> optionsMonitor)
    {
        this.namingService = namingService;
        this.configuration = configuration;
        this.optionsMonitor = optionsMonitor;
    }

    public async Task StartAsync(CancellationToken token)
    {
        activeConfig = optionsMonitor.CurrentValue;
        gatewayId = activeConfig.ServiceId;
        Trace.Assert(namingService!=null,"");
        Instance gatewayInstance = new Instance()
        {
            ServiceName = activeConfig.ServiceName,
            Ip = activeConfig.ServiceIp,
            Port = activeConfig.ServicePort,
            Ephemeral = false
        };
        try
        {
            await namingService.RegisterInstance(activeConfig.ServiceName, gatewayInstance);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            throw new ConfigIncorrectException("unable to register gateway node");
        }
        List<ServicesModel> list = activeConfig.Services;
       
        if (!list.IsNullOrEmpty())
        {
            foreach(ServicesModel model in list)
            {
                string serviceName = model.Name;
                if (!model.Nodes.IsNullOrEmpty())
                {
                    foreach (ServicesModel.Node node in model.Nodes)
                    {
                        await RegisterService(node);
                    }
                }
            }
        }
        ConsulRegisterWorker.ScanRouters(activeConfig);
        changeToken = optionsMonitor.OnChange(async newConfig => {
            bool equals = activeConfig.Equals(newConfig);
            if (!equals)
            {
                Dictionary<int, List<ServicesModel.Node>> diff = ServicesModel.Diff(activeConfig.Services,
                    newConfig.Services);
                if (diff.TryGetValue(ServicesModel.ADD, out List<ServicesModel.Node>? addNodes))
                {
                    foreach (ServicesModel.Node node in addNodes)
                    {
                        await RegisterService(node);
                    }
                }
                if (diff.TryGetValue(ServicesModel.MODIFY, out List<ServicesModel.Node>? modifyNodes))
                {
                    foreach (ServicesModel.Node node in addNodes)
                    {
                        await namingService.DeregisterInstance(node.Id,node.Address,node.Port);
                        await RegisterService(node);
                    }
                }
                if (diff.TryGetValue(ServicesModel.DELETE, out List<ServicesModel.Node>? deleteNodes))
                {
                    foreach (ServicesModel.Node node in addNodes)
                    {
                        await namingService.DeregisterInstance(node.Id,node.Address,node.Port);
                    }
                }
                activeConfig = newConfig;
                ConsulRegisterWorker.ScanRouters(activeConfig);
            }
            else
            {
                Log.Information("Service config no change!");
            }
        });
    }

    public Task RegisterService(ServicesModel.Node node)
    {
        try
        {
            namingService.RegisterInstance(node.Id, node.Address,node.Port);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            throw new ConfigIncorrectException("unable to register gateway node");
        }
    }
}