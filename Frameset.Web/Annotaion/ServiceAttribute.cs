using Microsoft.Extensions.DependencyInjection;

namespace Frameset.Web.Annotaion
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ServiceAttribute : Attribute
    {
        public ServiceLifetime LifeTime
        {
            get; protected set;
        }
        public ServiceAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            LifeTime = serviceLifetime;
        }

    }
}
