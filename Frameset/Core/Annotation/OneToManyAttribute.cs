using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = false)]
    public class OneToManyAttribute : Attribute
    {
        public Type SubType
        {
            get; set;
        }
        public string JoinColumn
        {
            get;set;
        }
        public OneToManyAttribute(Type subType,string joinColumn)
        {
            this.SubType = subType;
            this.JoinColumn = joinColumn;
        }
    }
}
