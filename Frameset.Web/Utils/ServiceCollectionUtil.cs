using Frameset.Web.Annotaion;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;

namespace Frameset.Web.Utils
{
    public static class ServiceCollectionUtil
    {
        public static void AddBussiness(this IServiceCollection services)
        {
            //AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            List<Type> types = ScanType();
            types.ForEach(impl =>
            {
                //获取该类所继承的所有接口
                Type[] interfaces = impl.GetInterfaces();
                //获取该类注入的生命周期
                var lifetime = impl.GetCustomAttribute<ServiceAttribute>().LifeTime;
                interfaces.ToList().ForEach(i =>
                {
                    switch (lifetime)
                    {
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(i, impl);
                            break;
                        case ServiceLifetime.Scoped:
                            services.AddScoped(i, impl);
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(i, impl);
                            break;

                    }
                });

            });
        }
        private static List<Type> ScanType()
        {
            List<Type> retList = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    retList.AddRange(assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttributes(typeof(ServiceAttribute), false).Length > 0)
                                .ToList());
                }
                catch (Exception ex)
                {
                    Log.Error("{Message}", ex.Message);
                }
            }
            return retList;

        }
    }
}
