using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ManyToOneAttribute : Attribute
    {
        public Type ParentType;
        public string ColumnName;
        public ManyToOneAttribute(Type parentType)
        {
            this.ParentType = parentType;
        }
        public ManyToOneAttribute(Type parentType, string columnName)
        {
            this.ParentType = parentType;
            this.ColumnName = columnName;
        }
    }
}
