using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ManyToOneAttribute: Attribute
    {
        public Type ParentType;
        public ManyToOneAttribute(Type parentType)
        {
            this.ParentType = parentType;
        }
    }
}
