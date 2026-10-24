using Frameset.Office.Core;
using Frameset.Office.Element;
using Frameset.Office.Meta;
using System;
using System.Diagnostics;

namespace Frameset.Office.Excel.Util
{
    public class CellProcessor : IDisposable
    {
        string type = null!;
        internal string? StyleString
        {
            get; set;
        } = null!;
        internal string FormatId
        {
            get; set;
        } = null!;
        internal string FormatString
        {
            get; set;
        } = null!;
        internal CellAddress Address
        {
            get; set;
        } = null!;
        internal object? Value
        {
            get; set;
        } = null!;
        internal string? Formula
        {
            get; set;
        } = null!;
        internal string RawValue
        {
            get; set;
        } = null!;
        internal CellType CellType
        {
            get; set;
        }
        internal string DataFormatId
        {
            get; set;
        } = null!;
        internal string DataFormatString
        {
            get; set;
        } = null!;


        public CellProcessor()
        {

        }



        public void SetValue(XMLStreamReader r, WorkBook workBook, CellAddress address)
        {
            string? seltype = r.GetAttribute("t");
            if (string.IsNullOrWhiteSpace(seltype))
            {
                seltype = r.GetAttribute("n");
            }
            Trace.Assert(!string.IsNullOrWhiteSpace(seltype));
            type = seltype;
            StyleString = r.GetAttribute("s");
            this.Address = address;

            if (StyleString != null)
            {
                int index = Convert.ToInt16(StyleString);
                if (index < workBook.Formats.Count)
                {
                    FormatId = workBook.Formats[index];
                    FormatString = workBook.FormatMap[FormatId];
                }
            }
        }
        public void Dispose()
        {

        }
        public string GetCellTypeStr()
        {
            return type;
        }
        public CellType GetCellType()
        {
            return CellType;
        }
        public object? GetValue()
        {
            return Value;
        }
        public string GetRawValue()
        {
            return RawValue;
        }
        public string? GetFormula()
        {
            return Formula;
        }

    }
}
