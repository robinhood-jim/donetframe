using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AutoInjectAttribute : Attribute
    {
        public String ResourceName
        {
            get; set;
        } = null!;
        public AutoInjectAttribute()
        {

        }
        public AutoInjectAttribute(string resourceName)
        {
            ResourceName = resourceName;
        }
    }
}
