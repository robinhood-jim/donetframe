using Frameset.Web.Annotation;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace Frameset.Web.Filter
{
    public class RepeatSubmitFilter : IActionFilter
    {
        private RepeatSubmitAttribute _attribute;
        public RepeatSubmitFilter(RepeatSubmitAttribute attribute)
        {
            _attribute = attribute;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            object? attributeObj = descriptor?.MethodInfo.GetCustomAttributes(typeof(RepeatSubmitAttribute), false).FirstOrDefault();
            Trace.Assert(attributeObj != null);

            throw new NotImplementedException();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
