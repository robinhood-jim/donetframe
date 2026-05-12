using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnFormulaAttribute : Attribute
    {
        public string Value
        {
            get; set;
        }
        public ColumnFormulaAttribute(string value)
        {
            Value = value;
        }
    }
}
