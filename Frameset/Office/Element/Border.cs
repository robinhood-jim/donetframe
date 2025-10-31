using Frameset.Office.Core;
using System.Collections.Generic;

namespace Frameset.Office.Element
{
    public class Border : IWritableElement
    {
        internal Dictionary<BorderSide, BorderElement> elements = new Dictionary<BorderSide, BorderElement>();
        public static Border NONE = new Border();
        public static Border BLACK = new Border(new BorderElement(null, "FFFFFFFF"));
        internal Border(BorderElement left, BorderElement right, BorderElement top, BorderElement bottom, BorderElement diagonal)
        {
            elements.TryAdd(BorderSide.TOP, top);
            elements.TryAdd(BorderSide.LEFT, left);
            elements.TryAdd(BorderSide.BOTTOM, bottom);
            elements.TryAdd(BorderSide.RIGHT, right);
            elements.TryAdd(BorderSide.DIAGONAL, diagonal);
        }
        internal Border() : this(BorderElement.NONE, BorderElement.NONE, BorderElement.NONE, BorderElement.NONE, BorderElement.NONE)
        {

        }


        internal Border(BorderElement element) : this(element, element, element, element, BorderElement.NONE)
        {

        }


        public void WriteOut(XmlBufferWriter w)
        {
            w.Append("<border");

            w.Append(">");
            elements[BorderSide.LEFT].Write("left", w);
            elements[BorderSide.RIGHT].Write("right", w);
            elements[BorderSide.TOP].Write("top", w);
            elements[BorderSide.BOTTOM].Write("bottom", w);
            elements[BorderSide.DIAGONAL].Write("diagonal", w);
            w.Append("</border>");
        }
    }
    public enum BorderSide
    {
        TOP, LEFT, BOTTOM, RIGHT, DIAGONAL
    }
    public class BorderElement
    {
        internal string style;
        internal string rgbColor;
        internal static BorderElement NONE = new BorderElement(null, null);
        internal BorderElement(string style, string rgbColor)
        {
            this.style = style;
            this.rgbColor = rgbColor;
        }
        internal void Write(string name, XmlBufferWriter w)
        {
            w.Append("<").Append(name);
            if (style == null && rgbColor == null)
            {
                w.Append("/>");
            }
            else
            {
                if (style != null)
                {
                    w.Append(" style=\"").Append(style).Append("\"");
                }
                w.Append(">");
                if (rgbColor != null)
                {
                    w.Append("<color rgb=\"").Append(rgbColor).Append("\"/>");
                }
                w.Append("</").Append(name).Append(">");
            }
        }
    }
}
