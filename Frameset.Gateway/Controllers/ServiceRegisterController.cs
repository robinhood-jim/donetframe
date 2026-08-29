using System.Text;
using System.Text.Json;
using Consul;
using Frameset.Gateway.Models;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.Gateway.Controllers;

[ApiController]
[Route("api/registry")]
public class ServiceRegisterController : ControllerBase
{
    private readonly IConsulClient consulClient;

    public ServiceRegisterController(IConsulClient consulClient)
    {
        this.consulClient = consulClient;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterService([FromBody] MicroserviceRegModel model)
    {
        string uniqueServiceId=$"{model.ServiceName}-{model.Address}-{model.Port}";
        var registration = new AgentServiceRegistration
        {
            ID = uniqueServiceId,
            Name = model.ServiceName,
            Address = model.Address,
            Port = model.Port,
            Tags = model.Tags,
            Check = new AgentServiceCheck
            {
                TTL = TimeSpan.FromSeconds(15),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };
        await consulClient.Agent.ServiceRegister(registration);
        await ConfigureConsulGatewayRouteAsync(model.ServiceName);
        return Ok(new { Message = "Registered inside Consul mesh.", ServiceId = uniqueServiceId });
    }
    [HttpPost("deregister/{serviceId}")]
    public async Task<IActionResult> DeregisterService(string serviceId)
    {
        await consulClient.Agent.ServiceDeregister(serviceId);
        Console.WriteLine($"[Registry Center]: Node gracefully left. Node ID: '{serviceId}'");
        return Ok(new { Message = "Node disconnected successfully." });
    }
    [HttpPost("heartbeat/{serviceId}")]
    public async Task<IActionResult> Heartbeat(string serviceId)
    {
        await consulClient.Agent.PassTTL($"service:{serviceId}", "Instance healthy.");
        return Ok();
    }
    private async Task ConfigureConsulGatewayRouteAsync(string serviceName)
    {
        // 建立 Consul API Gateway 的 HttpRoute 配置結構
        var httpRouteConfig = new
        {
            Kind = "http-route",
            Name = $"{serviceName}-route",
            Rules = new[]
            {
                new
                {
                    Matches = new[] { new { Path = new { Type = "Exact", Value = $"/services/{serviceName}" } } },
                    Services = new[] { new { Name = serviceName } }
                }
            }
        };

        string jsonPayload = JsonSerializer.Serialize(httpRouteConfig);
        var kvPair = new KVPair($"config/config-entries/http-route/{serviceName}-route")
        {
            Value = Encoding.UTF8.GetBytes(jsonPayload)
        };

        // 將路由配置寫入 Consul 的核心配置庫
        await consulClient.KV.Put(kvPair);
    }
    

}