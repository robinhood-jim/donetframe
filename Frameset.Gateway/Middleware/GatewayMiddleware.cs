using Frameset.Gateway.Models;
using Frameset.Gateway.Resolver;
using Microsoft.Extensions.Options;

namespace Frameset.Gateway.Middleware;

public class GatewayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<GatewayConfig> optionsMonitor;
    private readonly HttpClient httpClient;
    private GatewayConsulRouterResolver resolver;
    
    public GatewayMiddleware(RequestDelegate next, IOptionsMonitor<GatewayConfig> optionsMonitor,
        GatewayConsulRouterResolver resolver,HttpClient httpClient)
    {
        _next = next;
        this.optionsMonitor = optionsMonitor;
        this.resolver = resolver;
        this.httpClient = httpClient;

    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value ?? "";
        if (requestPath.Equals("/health"))
        {
            await _next(context); 
            return;
        }
        var matchedRoute = optionsMonitor.CurrentValue.Routes
            .FirstOrDefault(r => requestPath.StartsWith(r.PathPattern, StringComparison.OrdinalIgnoreCase));
        if (matchedRoute == null)
        {
            await _next(context); // 未命中路由規則，交由下一個 Middleware（如本地 /health 端點）
            return;
        }
        try
        {
            // 2. 處理路徑（處理 Spring Cloud 常見的 StripPrefix 需求）
            string processedPath = requestPath;
            if (matchedRoute.StripPrefix)
            {
                processedPath = requestPath.Substring(matchedRoute.PathPattern.Length);
                if (!processedPath.StartsWith("/")) processedPath = "/" + processedPath;
            }
            string queryString = context.Request.QueryString.Value ?? "";

            string virtualUrl = $"{processedPath}{queryString}";
            string realTargetUrl = await resolver.ResolveUrlAsync(matchedRoute, virtualUrl);
            await ForwardProxyRequestAsync(context, realTargetUrl);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync($"[Gateway Error] : {ex.Message}");
        }
    }
    private async Task ForwardProxyRequestAsync(HttpContext context, string targetUrl)
    {
        var targetUri = new Uri(targetUrl);
        var proxyRequest = new HttpRequestMessage();
        proxyRequest.Method = new HttpMethod(context.Request.Method);
        if (context.Request.ContentLength > 0)
        {
            proxyRequest.Content = new StreamContent(context.Request.Body);
        }
        foreach (var header in context.Request.Headers)
        {
            if (string.Equals(header.Key, "transfer-encoding", StringComparison.OrdinalIgnoreCase) || 
                header.Key.Equals("host", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
        proxyRequest.RequestUri = targetUri;
        proxyRequest.Headers.Host = targetUri.Authority;
        using var responseMessage = await httpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)responseMessage.StatusCode;
        
        foreach (var header in responseMessage.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in responseMessage.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        context.Response.Headers.Remove("transfer-encoding");
        await responseMessage.Content.CopyToAsync(context.Response.Body);
    }
}