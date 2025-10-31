using Frameset.Office.Core;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Frameset.Office.Excel.Util
{
    public class CellUtils
    {
        static string defaultFontName = "Calibri";
        static string pattern = @"\w+(\{P([\+|-]?[\d+])*\})";

        static CellUtils()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            defaultFontName = string.Equals(currentCulture.Name, "zh-CN", StringComparison.OrdinalIgnoreCase) ? "宋体" : "Calibri";
        }
        public static void SetCultureInfo(CultureInfo currentCulture)
        {
            defaultFontName = string.Equals(currentCulture.Name, "zh-CN", StringComparison.OrdinalIgnoreCase) ? "宋体" : "Calibri";
        }

        public static String ColToString(int col)
        {
            StringBuilder sb = new StringBuilder();
            while (col >= 0)
            {
                sb.Append((char)('A' + (col % 26)));
                col = (col / 26) - 1;
            }
            char[] reverschars = sb.ToString().ToCharArray();
            Array.Reverse(reverschars);
            return new string(reverschars);
        }
        public static void AppendEscaped(StringBuilder sb, String s)
        {
            sb.Append(XmlEscapeHelper.Escape(s));
        }
        public static string GetDefaultFontName()
        {
            return defaultFontName;
        }
        public static string ReturnFormulaWithPos(string formula, int linePos)
        {
            MatchCollection matcher = Regex.Matches(formula, pattern);
            StringBuilder builder = new StringBuilder();
            int currentPos = 0;
            foreach (Match match in matcher)
            {
                GroupCollection groups = match.Groups;
                if (groups.Count > 0)
                {
                    Group group = groups[0];
                    builder.Append(formula.Substring(currentPos, group.Index - currentPos));
                    string groupStr = group.Value;
                    int pos = groupStr.IndexOf("{P");
                    string columnName = groupStr.Substring(0, pos);
                    int stepNum = linePos;
                    if (pos + 3 < groupStr.Length)
                    {
                        string addPlustag = groupStr.Substring(pos + 2, 1);
                        stepNum = "+".Equals(addPlustag) ? stepNum + Convert.ToInt16(groupStr.Substring(pos + 3, groupStr.Length - pos - 4)) : stepNum - Convert.ToInt16(groupStr.Substring(pos + 3, groupStr.Length - pos - 4));

                    }
                    currentPos = group.Index + match.Value.Length;
                    builder.Append(columnName + stepNum);
                }

            }
            if (currentPos < formula.Length - 1)
            {
                builder.Append(formula.Substring(currentPos, formula.Length - currentPos - 1));
            }
            return builder.ToString();
        }
    }
}
