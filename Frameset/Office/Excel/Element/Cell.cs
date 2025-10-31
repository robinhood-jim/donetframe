using Frameset.Office.Core;
using Frameset.Office.Element;
using Frameset.Office.Excel.Util;
using Frameset.Office.Meta;
using System;

namespace Frameset.Office.Excel.Element
{
    public class Cell
    {
        private WorkBook workBook;
        public CellType CellType
        {
            get; internal set;
        }
        private object value;
        private CellAddress address;
        private string formula;
        private string rawValue;
        private string dataFormatId;
        private string dataFormatString;
        private int style;
        public Cell(WorkBook workbook, CellType type, object value, CellAddress address, string formula, string rawValue) : this(workbook, type, value, address, formula, rawValue, null, null)
        {

        }
        public Cell(WorkBook workBook, CellType type, CellAddress address)
        {
            this.workBook = workBook;
            this.CellType = type;
            this.address = address;
        }
        public Cell(WorkBook workBook, CellProcessor processor, CellAddress address) : this(workBook, processor.GetCellType(), processor.GetValue(), address, processor.GetFormula(), processor.GetRawValue())
        {

        }

        public Cell(WorkBook workbook, CellType type, object value, CellAddress address, string formula, string rawValue,
             string dataFormatId, string dataFormatString)
        {
            this.workBook = workbook;
            this.CellType = type;
            this.value = value;
            this.address = address;
            this.formula = formula;
            this.rawValue = rawValue;
            this.dataFormatId = dataFormatId;
            this.dataFormatString = dataFormatString;
        }

        public void SetValue(Object value)
        {
            this.value = value;
        }

        public void SetStyle(int style)
        {
            this.style = style;
        }

        public void Write(XmlBufferWriter w, int r, int c)
        {
            if (value != null || style != 0)
            {
                w.Append("<c r=\"").Append(CellUtils.ColToString(c)).Append(r).Append("\"");
                if (style != 0)
                {
                    w.Append(" s=\"").Append(style).Append("\"");
                }
                if (value != null && !(value.GetType().Equals(typeof(Formula))))
                {
                    w.Append(" t=\"").Append(getCellType(value)).Append("\"");
                }
                w.Append(">");
                if (value.GetType().Equals(typeof(Formula)))
                {
                    w.Append("<f>").Append(((Formula)value).GetExpression()).Append("</f>");
                }
                else if (value.GetType().Equals(typeof(string)))
                {
                    w.Append("<is><t>").AppendEscaped((string)value);
                    w.Append("</t></is>");
                }
                else if (value != null)
                {
                    w.Append("<v>");
                    if (value.GetType().Equals(typeof(ShardingString)))
                    {
                        w.Append(((ShardingString)value).Index);
                    }
                    else if (value.GetType().Equals(typeof(int)))
                    {
                        w.Append((int)value);
                    }
                    else if (value.GetType().Equals(typeof(long)))
                    {
                        w.Append((long)value);
                    }
                    else if (value.GetType().Equals(typeof(double)))
                    {
                        w.Append((double)value);
                    }
                    else if (value.GetType().Equals(typeof(bool)))
                    {
                        w.Append((bool)value ? '1' : '0');
                    }
                    else
                    {
                        w.Append(value.ToString());
                    }
                    w.Append("</v>");
                }
                w.Append("</c>");
            }
        }
        static string getCellType(object value)
        {
            if (value.GetType().Equals(typeof(ShardingString)))
            {
                return "s";
            }
            else if (value.GetType().Equals(typeof(bool)))
            {
                return "b";
            }
            else if (value.GetType().Equals(typeof(string)))
            {
                return "inlineStr";
            }
            else
            {
                return "n";
            }
        }
        public object GetValue()
        {
            return value;
        }
        public CellAddress GetAddress()
        {
            return address;
        }
    }

}
