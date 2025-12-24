using Frameset.Core.Annotation;
using Frameset.Core.Context;
using Frameset.Web.Controller;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

namespace Frameset.Web.Utils
{
    public class ApplicationContext
    {
        private static IServiceProvider provider = null!;
        private ApplicationContext()
        {

        }
        public static void SetContext(IServiceProvider serviceProvider)
        {
            provider = serviceProvider;
            AutoWireInject(serviceProvider);
        }
        public static object? GetBean(Type type, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            //change singleton to scope
            using (var scope = provider.CreateScope())
            {
                object? cachedObj = scope.ServiceProvider.GetRequiredService(type);
                if (cachedObj == null)
                {
                    cachedObj = RegServiceContext.GetBean(type);
                }
                return cachedObj;
            }
        }
        public static T GetBean<T>()
        {
            Type type = typeof(T);
            using (var scope = provider.CreateScope())
            {
                object? cachedObj = scope.ServiceProvider.GetRequiredService(type);
                if (cachedObj == null)
                {
                    cachedObj = RegServiceContext.GetBean<T>();
                }
                return (T)cachedObj;
            }
        }
        private static void AutoWireInject(IServiceProvider serviceProvider)
        {
            IEnumerator<AbstractControllerBase> webControllers = serviceProvider.GetServices<AbstractControllerBase>().GetEnumerator();
            while (webControllers.MoveNext())
            {
                AbstractControllerBase controllerBase = webControllers.Current;
                AutoWireInject(controllerBase);
            }
        }
        private static void AutoWireInject(AbstractControllerBase originObj)
        {
            var fields = originObj.GetType().GetTypeInfo().DeclaredFields.Where(field => Attribute.IsDefined(field, typeof(ResourceAttribute))).GetEnumerator();
            while (fields.MoveNext())
            {
                FieldInfo info = fields.Current;
                if (info.GetValue(originObj) == null)
                {
                    object? constructObj = GetBean(info.FieldType);
                    if (constructObj == null)
                    {
                        constructObj = RegServiceContext.GetBean(info.FieldType);
                    }
                    info.SetValue(originObj, constructObj);
                }
            }

        }
        public static void Register(Type type, object obj)
        {
            Trace.Assert((obj.GetType().GetInterfaces().Length > 0 && obj.GetType().GetInterfaces()[0] == type) || obj.GetType() == type || obj.GetType().IsSubclassOf(type));
            RegServiceContext.Register(type, obj);

        }
    }
}
