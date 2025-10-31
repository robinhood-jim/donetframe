using Frameset.Core.Exceptions;
using Frameset.Office.Core;
using Frameset.Office.Element;
using System;
using System.Globalization;
using System.Threading;

namespace Frameset.Office.Meta
{
    public class Font : IWritableElement
    {
        bool bold;
        bool italic;
        bool underlined;
        string name;
        int size;
        string rgbColor;
        bool strikethrough;
        internal static string defaultFontName;
        public static Font DEFAULT = null;
        static Font()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            defaultFontName = string.Equals(currentCulture.Name, "zh-CN", StringComparison.OrdinalIgnoreCase) ? "宋体" : "Calibri";
            DEFAULT = Build(false, false, false, defaultFontName, 12, "FF000000", false);
        }
        public Font(bool bold, bool italic, bool underlined, string name, int size, string rgbColor, bool strikethrough)
        {
            if (size < 1 || size > 409)
            {
                throw new ConfigMissingException("Font size must be between 1 and 409 points:" + size);
            }
            this.bold = bold;
            this.italic = italic;
            this.underlined = underlined;
            this.name = name;
            this.size = size;
            this.rgbColor = rgbColor;
            this.strikethrough = strikethrough;
        }
        public static Font Build(bool bold, bool italic, bool underlined, string name, int size, string rgbColor, bool strikethrough)
        {
            return new Font(bold, italic, underlined, name, size, rgbColor, strikethrough);
        }
        public void WriteOut(XmlBufferWriter w)
        {
            w.Append("<font>").Append(bold ? "<b/>" : "").Append(italic ? "<i/>" : "").Append(underlined ? "<u/>" : "").Append("<sz val=\"").Append(Convert.ToString(size)).Append("\"/>");
            w.Append(strikethrough ? "<strike/>" : "");
            if (rgbColor != null)
            {
                w.Append("<color rgb=\"").Append(rgbColor).Append("\"/>");
            }
            w.Append("<name val=\"").Append(name).Append("\"/>");
            w.Append("</font>");
        }


    }
}
