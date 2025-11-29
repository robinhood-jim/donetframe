using System;

namespace Frameset.Office.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ExcelColumnAttribute : Attribute
    {
        public string ColumnName
        {
            get; set;
        }
        public string Format
        {
            get; set;
        }
        public int FormatId
        {
            get; set;
        } = 0;
        public double Width
        {
            get; set;
        } = 12.0;
        public int Order
        {
            get; set;
        } = -1;
        public string Formula
        {
            get; set;
        }
    }
}
