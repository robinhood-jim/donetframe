using System;

namespace Frameset.Office.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ExcelColumnIndexAttribute:Attribute
    {
        public int Index { get; set; }
    }
}
