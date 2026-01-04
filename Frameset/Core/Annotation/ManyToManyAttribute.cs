using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ManyToManyAttribute : Attribute
    {
        public Type MiddleType
        {
            get; set;
        }
        public ManyToManyAttribute(Type middleType)
        {
            MiddleType = middleType;
        }
    }
}
