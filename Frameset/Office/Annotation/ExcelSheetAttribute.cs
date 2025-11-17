using Frameset.Office.Element;
using System;

namespace Frameset.Office.Annotation
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExcelSheetAttribute(string sheetName) : Attribute
    {

        public string? Name { get; set; } = sheetName;
        public SheetVisibility? Visibility { get; set; }
        public int MaxRows { get; set; } = -1;
        public bool FillHeader { get; set; } = true;
    }
}
