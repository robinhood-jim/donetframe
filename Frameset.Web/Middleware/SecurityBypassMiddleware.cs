using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;

namespace Frameset.Web.Middleware;

public class SecurityBypassMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration configuration;
    private readonly string[] ignoreUrls = [];
    private JwtSecurityTokenHandler tokenHandler;
    private FileExtensionContentTypeProvider provider;

    public SecurityBypassMiddleware(RequestDelegate next,IConfiguration configuration)
    {
        _next = next;
        this.configuration = configuration;
        ignoreUrls = this.configuration.GetSection("login:ignoreUrls").Get<string[]>() ?? Array.Empty<string>();
        tokenHandler = new();
        provider = new();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string requestPath = GetRequestPath(context.Request);
        bool isStaticResource=provider.TryGetContentType(context.Request.Path.Value ?? "", out _);
        bool isPermitPaths = !isStaticResource && ignoreUrls.Any(p => requestPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        if (isStaticResource || isPermitPaths)
        {
            context.Items["IsBypassRequest"] = true;

            // 2. 动态为当前请求路由终结点挂载原生的 AllowAnonymousAttribute
            // 确保即使后方 Controller 贴了 [Authorize]，也会被框架的 UseAuthorization() 安全放行
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var updatedMetadata = new EndpointMetadataCollection(
                    endpoint.Metadata.Append(new AllowAnonymousAttribute())
                );
                context.SetEndpoint(new Endpoint(
                    endpoint.RequestDelegate,
                    updatedMetadata,
                    endpoint.DisplayName
                ));
            }
        }
        await _next(context);
    }
    private string GetRequestPath(HttpRequest request)
    {
        string path = request.Path;
        string relativePath = path;
        int pos = path.IndexOf('?');
        if (pos > 0)
        {
            relativePath = path.Substring(0, pos);
        }
        return relativePath;
    }
}