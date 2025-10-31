using System;
using System.Linq;
using System.Text;

namespace Frameset.Office.Core
{
    public class XmlEscapeHelper
    {
        internal XmlEscapeHelper()
        {

        }
        public static String Escape(string text)
        {
            int offset = 0;
            StringBuilder sb = new StringBuilder();
            while (offset < text.Length)
            {
                int codePoint = text[offset];
                sb.Append(escape(codePoint));
                offset += Encoding.GetEncoding("utf-8").GetCharCount(BitConverter.GetBytes(codePoint));
            }
            return sb.ToString();
        }

        /**
         * Escape char with XML escaping.
         * Invalid characters in XML 1.0 are ignored.
         *
         * @param c Character code point.
         */
        private static string escape(int c)
        {
            if (!(c == 0x9 || c == 0xa || c == 0xD
                    || (c >= 0x20 && c <= 0xd7ff)
                    || (c >= 0xe000 && c <= 0xfffd)
                    || (c >= 0x10000 && c <= 0x10ffff)))
            {
                return "";
            }
            switch (c)
            {
                case '<':
                    return "&lt;";
                case '>':
                    return "&gt;";
                case '&':
                    return "&amp;";
                case '\'':
                    return "&apos;";
                case '"':
                    return "&quot;";
                default:
                    if (c > 0x7e || c < 0x20)
                    {

                        return "&#x".Concat(Convert.ToHexString(BitConverter.GetBytes(c))).Concat(";").ToString();
                    }
                    else
                    {
                        return ((char)c).ToString();
                    }
            }
        }
    }
}
