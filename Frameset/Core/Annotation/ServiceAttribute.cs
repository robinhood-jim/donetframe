using Microsoft.Extensions.DependencyInjection;
using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ServiceAttribute : Attribute
    {
        public ServiceLifetime LifeTime
        {
            get; protected set;
        }
        public ServiceAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            LifeTime = serviceLifetime;
        }

    }
}
