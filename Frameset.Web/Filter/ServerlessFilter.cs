using Frameset.Web.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Frameset.Web.Filter
{
    public class ServerlessFilter : IAsyncActionFilter
    {
        private string monitorPathPrefix = "/serverless";
        public ServerlessFilter(string? monitorPath)
        {
            monitorPathPrefix = monitorPath ?? monitorPathPrefix;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string path = GetRequestPath(context.HttpContext.Request);
            if (path.StartsWith(monitorPathPrefix))
            {
                string callFunc = path.Substring(monitorPathPrefix.Length + 1, path.Length - monitorPathPrefix.Length);
                object response = DynamicFunctionLoader.InvokeFunctionDynamic(context.HttpContext.Request, context.HttpContext.Response, callFunc);
                context.Result = new JsonResult(response);
            }
            else
            {
                ActionExecutedContext result = await next();
                if (result.Exception != null)
                {

                }
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
}
