using Frameset.Core.Ioc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;


namespace Frameset.Web.Ioc
{
    public class UniversalAutoWiredControllerActivator : IControllerActivator
    {
        public object Create(ControllerContext context)
        {
            var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
            var requestServices = context.HttpContext.RequestServices;

            // 1. Instantiates the controller on the fly without registering it in IServiceProvider
            //    (also supports [ActivatorUtilitiesConstructor] and optional constructor parameters)
            var controller = ActivatorUtilities.CreateInstance(requestServices, controllerType);

            // 2. Recursively populates [AutoWired] properties and fields across the entire graph
            NestedBeanActivator.InjectGraph(controller, requestServices);

            return controller;
        }

        public void Release(ControllerContext context, object controller)
        {
            if (controller is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public ValueTask ReleaseAsync(ControllerContext context, object controller)
        {
            if (controller is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            Release(context, controller);
            return ValueTask.CompletedTask;
        }
    }
}
