using Frameset.Core.Common;
using Frameset.Web.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Web.Middleware
{
    public class ServerlessMiddleware
    {
        private readonly RequestDelegate _next;
        private string monitorPathPrefix = "/serverless";
        public ServerlessMiddleware(RequestDelegate next)
        {
            _next = next;
            string? serverlessPrefix = AppConfigurtaionServices.Configuration["serverlessPrefix"];
            if (!serverlessPrefix.IsNullOrEmpty())
            {
                monitorPathPrefix = serverlessPrefix;
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = GetRequestPath(context.Request);
            if (path.StartsWith(monitorPathPrefix))
            {
                string callFunc = path.Substring(monitorPathPrefix.Length + 1, path.Length - monitorPathPrefix.Length-1);
                object response = DynamicFunctionLoader.InvokeFunctionDynamic(context.Request, context.Response, callFunc);
                await context.Response.WriteAsJsonAsync(response);
            }
            else
            {
                await _next(context);
            }
        }
        private string GetRequestPath(HttpRequest request)
        {
            string basePath = request.PathBase;
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
    public static class ServerlessMiddlewareExtensions
    {
        public static IApplicationBuilder UseServerless(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ServerlessMiddleware>();
        }
    }
}
