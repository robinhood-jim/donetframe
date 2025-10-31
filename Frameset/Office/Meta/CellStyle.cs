using Frameset.Office.Core;
using Frameset.Office.Element;
using System;
using System.Collections.Generic;

namespace Frameset.Office.Meta
{
    public class CellStyle : IWritableElement
    {
        int valueFormatting;
        int font;
        int fill;
        int border;
        Alignment alignment;
        public CellStyle(CellStyle original, int valueFormatting, int font, int fill, int border, Alignment alignment)
        {
            this.valueFormatting = (valueFormatting == 0 && original != null) ? original.valueFormatting : valueFormatting;
            this.font = (font == 0 && original != null) ? original.font : font;
            this.fill = (fill == 0 && original != null) ? original.fill : fill;
            this.border = (border == 0 && original != null) ? original.border : border;
            this.alignment = (alignment == null && original != null) ? original.alignment : alignment;
        }

        public override bool Equals(object obj)
        {
            return obj is CellStyle style &&
                   valueFormatting == style.valueFormatting &&
                   font == style.font &&
                   fill == style.fill &&
                   border == style.border &&
                   EqualityComparer<Alignment>.Default.Equals(alignment, style.alignment);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(valueFormatting, font, fill, border, alignment);
        }

        public void WriteOut(XmlBufferWriter w)
        {
            w.Append("<xf numFmtId=\"").Append(valueFormatting).Append("\" fontId=\"").Append(font).Append("\" fillId=\"").Append(fill).Append("\" borderId=\"").Append(border).Append("\" xfId=\"0\"");
            if (border != 0)
            {
                w.Append(" applyBorder=\"true\"");
            }

            if (alignment == null)
            {
                w.Append("/>");
                return;
            }
            if (alignment != null)
            {
                w.Append(" applyAlignment=\"true\"");
            }


            w.Append(">");
            if (alignment != null)
            {
                alignment.WriteOut(w);
            }

            w.Append("</xf>");
        }
    }
    public enum CellType
    {
        NUMBER,
        STRING,
        FORMULA,
        ERROR,
        BOOLEAN,
        EMPTY
    }
}
