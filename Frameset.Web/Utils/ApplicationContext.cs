using Frameset.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Frameset.Web.Utils
{
    public class ApplicationContext
    {
        private static IServiceProvider provider = null!;
        private static readonly Dictionary<Type, object> registerMap = [];
        private ApplicationContext()
        {

        }
        public static void SetContext(IServiceProvider serviceProvider)
        {
            provider = serviceProvider;
        }
        public static object? GetBean(Type type, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            //change singleton to scope
            using (var scope = provider.CreateScope())
            {
                object? cachedObj = scope.ServiceProvider.GetRequiredService(type);
                if (cachedObj == null)
                {
                    registerMap.TryGetValue(type, out cachedObj);
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
                    registerMap.TryGetValue(type, out cachedObj);
                }
                return (T)cachedObj;
            }
        }
        public static void Register(Type type, object obj)
        {
            Trace.Assert((obj.GetType().GetInterfaces().Length > 0 && obj.GetType().GetInterfaces()[0] == type) || obj.GetType() == type || obj.GetType().IsSubclassOf(type));
            if (registerMap.ContainsKey(type))
            {
                throw new OperationFailedException("type already register!");
            }
            registerMap.TryAdd(type, obj);

        }
    }
}
