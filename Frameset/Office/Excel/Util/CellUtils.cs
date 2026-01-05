using Frameset.Office.Core;
using NodaTime;
using System;
using System.Collections.Generic;
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
        public static string ReturnFormulaWithPos(Dictionary<string,List<CellFormula>> parseFormualMap, string formulaStr, int linePos)
        {
            StringBuilder builder = new StringBuilder();
            if (!parseFormualMap.TryGetValue(formulaStr, out List<CellFormula> formulas))
            {
                MatchCollection matcher = Regex.Matches(formulaStr, pattern);
                int currentPos = 0;
                formulas = [];
                foreach (Match match in matcher)
                {
                    GroupCollection groups = match.Groups;
                    if (groups.Count > 0)
                    {
                        Group group = groups[0];
                        builder.Append(formulaStr.Substring(currentPos, group.Index - currentPos));
                        formulas.Add(new CellFormula()
                        {
                            OtherContent = formulaStr.Substring(currentPos, group.Index - currentPos)
                        });
                        string groupStr = group.Value;
                        int pos = groupStr.IndexOf("{P");
                        string columnName = groupStr.Substring(0, pos);
                        int stepNum = linePos;
                        if (pos + 3 < groupStr.Length)
                        {
                            string addPlustag = groupStr.Substring(pos + 2, 1);
                            int offset = Convert.ToInt16(groupStr.Substring(pos + 3, groupStr.Length - pos - 4));
                            bool isPlus = "+".Equals(addPlustag);
                            stepNum = isPlus ? stepNum + offset : stepNum - offset;
                            formulas.Add(new CellFormula(columnName, isPlus ? offset : -offset));
                        }
                        else
                        {
                            formulas.Add(new CellFormula(columnName, 0));
                        }
                        currentPos = group.Index + match.Value.Length;
                        builder.Append(columnName).Append(stepNum);
                    }
                }
                if (currentPos < formulaStr.Length - 1)
                {
                    builder.Append(formulaStr.Substring(currentPos, formulaStr.Length - currentPos));
                    formulas.Add(new CellFormula()
                    {
                        OtherContent = formulaStr.Substring(currentPos, formulaStr.Length - currentPos)
                    });
                }
                parseFormualMap.TryAdd(formulaStr, formulas);
            }
            else
            {
                foreach(CellFormula formula in formulas)
                {
                    if (!string.IsNullOrWhiteSpace(formula.OtherContent))
                    {
                        builder.Append(formula.OtherContent);
                    }
                    else
                    {
                        int stepNum = linePos + formula.Offset;
                        builder.Append(formula.Column).Append(stepNum);
                    }
                }
            }
            return builder.ToString();
        }
    }
    public class CellFormula
    {
        public string Column
        {
            get; set;
        }
        public int Offset
        {
            get; set;
        }
        public string OtherContent
        {
            get;set;
        }
        public CellFormula()
        {

        }
        public CellFormula(string column,int Offset)
        {
            this.Column = column;
            this.Offset = Offset;
        }

    }
}
