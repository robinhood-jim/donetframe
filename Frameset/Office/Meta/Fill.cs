using Frameset.Office.Core;
using Frameset.Office.Element;

namespace Frameset.Office.Meta
{
    public class Fill : IWritableElement
    {
        string patternType;
        string colorRgb;
        bool fg;
        public static Fill NONE = new Fill("none", null, true);
        public static Fill GRAY125 = new Fill("gray125", null, true);
        public static Fill DARKGRAY = new Fill("darkGray", null, true);
        public static Fill BLACK = new Fill("black", null, true);

        Fill(string patternType, string colorRgb, bool fg)
        {
            this.patternType = patternType;
            this.colorRgb = colorRgb;
            this.fg = fg;
        }
        public static Fill FromColor(string fgColorRgb)
        {
            return FromColor(fgColorRgb, true);
        }
        public static Fill FromColor(string colorRgb, bool fg)
        {
            return new Fill("solid", colorRgb, fg);
        }

        public void WriteOut(XmlBufferWriter w)
        {
            w.Append("<fill><patternFill patternType=\"").Append(patternType).Append("\"");
            if (colorRgb == null)
            {
                w.Append("/>");
            }
            else
            {
                w.Append("><").Append(fg ? "fg" : "bg").Append("Color rgb=\"").Append(colorRgb).Append("\"/></patternFill>");
            }
            w.Append("</fill>");
        }
    }
}
