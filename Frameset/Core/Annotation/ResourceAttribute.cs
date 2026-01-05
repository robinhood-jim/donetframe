using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ResourceAttribute : Attribute
    {
        public String ResourceName
        {
            get; set;
        }
        public ResourceAttribute()
        {

        }
        public ResourceAttribute(string resourceName)
        {
            ResourceName = resourceName;
        }
    }
}
