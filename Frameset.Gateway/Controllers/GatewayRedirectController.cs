using System.Collections.Concurrent;
using Consul;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.Gateway.Controllers;

public class GatewayRedirectController : ControllerBase
{
    private readonly IConsulClient consulClient;
    private static readonly ConcurrentDictionary<string, int> _serviceIndexCache = new();
    public GatewayRedirectController(IConsulClient consulClient)
    {
        this.consulClient = consulClient;
    }
    [HttpGet("services/{serviceName}/{*catchAll}")]
    public async Task<IActionResult> RedirectToService(string serviceName, string? catchAll)
    {
        // 1. 從 Consul 核心註冊中心獲取該服務所有「健康的」實例節點
        var healthResult = await consulClient.Health.Service(serviceName, tag: null, passingOnly: true);
        var healthyInstances = healthResult.Response;

        // 如果找不到任何健康節點，回傳 503 服務不可用
        if (healthyInstances == null || !healthyInstances.Any())
        {
            return StatusCode(503, $"Service '{serviceName}' has no healthy running instances inside Consul.");
        }

        // 
        var targetInstance = healthyInstances.Count() == 1 
            ? healthyInstances.First() 
            : SelectNextInstanceRoundRobin(serviceName, healthyInstances);

        var service = targetInstance.Service;
        
        // 處理節點地址（若 Address 為空則取 Node 的 Address）
        string targetAddress = string.IsNullOrEmpty(service.Address) ? targetInstance.Node.Address : service.Address;
        int targetPort = service.Port;

        // 3. 組合出真實的後端微服務 URL
        // 擷取原始請求中的 QueryString (例如 ?id=123&type=json)
        string queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        
        // 這裡將 catchAll (即 api/v1/**) 拼接到真實節點後面
        string realTargetUrl = $"http://{targetAddress}:{targetPort}/{catchAll}{queryString}";

        // 4. 【核心改變】：不回傳 OK，直接執行 HTTP 302 導向真實 URL
        Console.WriteLine($"[Gateway Redirect]: Balancing '{serviceName}' -> Redirecting client to: {realTargetUrl}");
        return Redirect(realTargetUrl);
    }

    /// <summary>
    /// 線程安全的 Round-Robin 節點挑選演算法
    /// </summary>
    private ServiceEntry SelectNextInstanceRoundRobin(string serviceName, System.Collections.Generic.IList<ServiceEntry> instances)
    {
        int totalInstances = instances.Count;
        
        // 原子操作：遞增索引值並防止溢位
        int nextIndex = _serviceIndexCache.AddOrUpdate(
            serviceName, 
            0, 
            (key, currentVal) => (currentVal + 1) % totalInstances
        );

        // 如果在極端高並發下索引超出範圍，安全修正回 0
        if (nextIndex >= totalInstances)
        {
            nextIndex = 0;
            _serviceIndexCache[serviceName] = 0;
        }
        return instances[nextIndex];
    }
}