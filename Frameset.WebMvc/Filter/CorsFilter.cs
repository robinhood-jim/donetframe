using Microsoft.AspNetCore.Mvc.Filters;

namespace Frameset.WebMvc.Filter
{
    public class CorsFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Request.Headers.TryAdd("Access-Control-Allow-Headers", "*");
            context.HttpContext.Request.Headers.TryAdd("Access-Control-Allow-Methods", "*");
            context.HttpContext.Request.Headers.TryAdd("Access-Control-Max-Age", new Microsoft.Extensions.Primitives.StringValues("24*60*60"));
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {

        }
    }
}
