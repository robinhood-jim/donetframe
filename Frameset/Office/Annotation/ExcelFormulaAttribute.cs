using System;

namespace Frameset.Office.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ExcelFormulaAttribute(string formula):Attribute
    {
        public string Formula { get; set; } = formula;
    }
}
