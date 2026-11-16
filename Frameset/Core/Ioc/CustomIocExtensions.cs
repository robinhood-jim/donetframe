using Frameset.Core.Annotation;
using Frameset.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Frameset.Core.Ioc
{
    public static class CustomIocExtensions
    {
        private static readonly HashSet<Type> IgnoredInterfaces =[typeof(IDisposable), typeof(IAsyncDisposable) ];
        public static IServiceCollection AddCustomIoc(this IServiceCollection services)
        {
            var assemblyNames = ClassUtils.GetRunningAssemblies();
            List<Type> types = typeof(ServiceAttribute).ScanTypesWithAttribute();
            var targetNameSet = new HashSet<string>(
            assemblyNames.Where(a => a.Name != null).Select(a => a.Name!),
            StringComparer.OrdinalIgnoreCase);
            // 1. Register all [Service] scanned classes with compiled constructor + nested [AutoInject]
            foreach (var implType in types)
            {
                var serviceAttr = implType.GetCustomAttribute<ServiceAttribute>();
                if (serviceAttr == null) continue;

                NestedBeanActivator.PreCompile(implType);

                var lifetime = serviceAttr.LifeTime;

                var interfaces = implType.GetInterfaces()
                .Where(i => !IgnoredInterfaces.Contains(i) && i.Namespace?.StartsWith("System") != true).ToArray();
                if (interfaces.Length > 0)
                {
                    var primaryInterface = interfaces[0];

                    // Register primary interface with NestedBeanActivator
                    services.Add(new ServiceDescriptor(
                        primaryInterface,
                        sp => NestedBeanActivator.CreateAndInjectGraph(sp, implType),
                        lifetime));

                    for (int i = 1; i < interfaces.Length; i++)
                    {
                        var extraInterface = interfaces[i];

                        if (lifetime == ServiceLifetime.Transient)
                        {
                            services.Add(new ServiceDescriptor(
                                extraInterface,
                                sp => NestedBeanActivator.CreateAndInjectGraph(sp, implType),
                                lifetime));
                        }
                        else
                        {
                            services.Add(new ServiceDescriptor(
                                extraInterface,
                                sp =>
                                {
                                    var instance = sp.GetRequiredService(primaryInterface);
                                    //NestedBeanActivator.InjectGraph(instance, sp);
                                    return instance;
                                },
                                lifetime));
                        }
                    }
                }
                else
                {

                    services.Add(new ServiceDescriptor(
                        implType,
                        sp => NestedBeanActivator.CreateAndInjectGraph(sp, implType),
                        lifetime));
                }
            }
            LogUtils.Debug($"custom Ioc successful loading {types.Count} Beans");

            return services;
        }
    }
}
