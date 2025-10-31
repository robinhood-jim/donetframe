using Frameset.Office.Core;
using Frameset.Office.Meta;
using Frameset.Office.Util;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


namespace Frameset.Office.Element
{
    public class StyleHolder : IWritableElement
    {
        internal static ConcurrentDictionary<string, int> valueFormattings = new ConcurrentDictionary<string, int>();
        internal static ConcurrentDictionary<Font, int> fonts = new ConcurrentDictionary<Font, int>();
        internal static ConcurrentDictionary<Fill, int> fills = new ConcurrentDictionary<Fill, int>();
        internal static ConcurrentDictionary<Border, int> borders = new ConcurrentDictionary<Border, int>();
        internal static ConcurrentDictionary<CellStyle, int> styles = new ConcurrentDictionary<CellStyle, int>();
        public StyleHolder()
        {
            MergeCellStyle(0, "0", Font.DEFAULT, Fill.NONE, Border.NONE, null);
            cacheFill(Fill.DARKGRAY);
        }
        public void replaceDefaultFont(Font font)
        {
            int oVal;
            fonts.Remove(fonts.First(x => x.Value == 0).Key, out oVal);
            fonts.TryAdd(font, 0);
        }
        static int CacheStuff<T>(ConcurrentDictionary<T, int> cache, T key, Func<T, int> indexFunc)
        {
            int retVal;
            if (!cache.TryGetValue(key, out retVal))
            {
                retVal = indexFunc.Invoke(key);
                cache.TryAdd(key, retVal);
            }
            return retVal;

        }
        static int CacheStuff<T>(ConcurrentDictionary<T, int> cache, T key)
        {
            return CacheStuff<T>(cache, key, (key) => cache.Count);
        }
        public static int MergeCellStyle(int currentStyle, string numberFormat, Font font, Fill fill, Border border, Alignment alignment)
        {
            CellStyle origin = !styles.IsNullOrEmpty() ? styles.First(x => x.Value == currentStyle).Key : null;
            CellStyle style = new CellStyle(origin, cachedValueFormat(numberFormat), cacheFont(font), cacheBorder(border), cacheFill(fill), alignment);
            return CacheStuff(styles, style);
        }
        static int cachedValueFormat(string valueformat)
        {
            int numFmtId = 0;
            IEnumerator<KeyValuePair<string, string>> kviter = OpcPackage.IMPLICIT_NUM_FMTS.Where(x => x.Value.Equals(valueformat)).GetEnumerator();
            if (kviter.MoveNext())
            {
                numFmtId = Convert.ToInt16(kviter.Current.Key);
            }
            else
            {
                numFmtId = CacheStuff<string>(valueFormattings, valueformat, (k) => valueFormattings.Count + 165);
            }
            return numFmtId;

        }
        static int cacheFont(Font font)
        {
            return CacheStuff<Font>(fonts, font);
        }
        static int cacheBorder(Border b)
        {
            return CacheStuff<Border>(borders, b);
        }
        static int cacheFill(Fill f)
        {
            return CacheStuff<Fill>(fills, f);
        }

        public void WriteOut(XmlBufferWriter w)
        {
            w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            WriteContent(w, valueFormattings, "numFmts", (e) => w.Append("<numFmt numFmtId=\"").Append(e.Value).Append("\" formatCode=\"").Append(e.Key).Append("\"/>"));
            WriteContent(w, fonts, "fonts", (e) => e.Key.WriteOut(w));
            //writeContent(w, fills, "fills", e -> e.getKey().writeOut(w));
            WriteContent(w, borders, "borders", (e) => e.Key.WriteOut(w));
            w.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");
            WriteContent(w, styles, "cellXfs", (e) => e.Key.WriteOut(w));

            w.Append("</styleSheet>");
        }
        private static void WriteContent<T>(XmlBufferWriter w, ConcurrentDictionary<T, int> cache, string name, Action<KeyValuePair<T, int>> consumer)
        {
            w.Append("<").Append(name).Append(" count=\"").Append(cache.Count).Append("\">");
            if (cache.Count > 1)
            {
                KeyValuePair<T, int>[] paris = cache.OrderBy(kv => kv.Value).ToArray();
                foreach (KeyValuePair<T, int> pair in paris)
                {
                    consumer.Invoke(pair);
                }
            }
            else
            {
                foreach (var item in cache)
                {
                    consumer.Invoke(item);
                }
            }
            w.Append("</").Append(name).Append(">");
        }

    }
}
