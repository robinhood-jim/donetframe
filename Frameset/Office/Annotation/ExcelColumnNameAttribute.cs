using System;

namespace Frameset.Office.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ExcelColumnNameAttribute(string columnName) : Attribute
    {
        public string ColumnName { get; set; } = columnName;
    }
}
