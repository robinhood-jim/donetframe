using Spring.Util;
using System;
using System.Text;


namespace Frameset.Core.Utils
{
    public class StringUtils
    {
        private static readonly string undlineStr = "_";
        public static string capitalize(string value)
        {
            if (hasLegth(value))
            {
                return changeFirstCharacterCase(value, true);
            }
            else
            {
                throw new AggregateException("value is empty!");
            }
        }

        public static string uncapitalize(string value)
        {
            if (hasLegth(value))
            {
                return changeFirstCharacterCase(value, false);
            }
            else
            {
                throw new AggregateException("value is empty!");
            }
        }
        public static string camelCaseUpperConvert(string column)
        {
            AssertUtils.IsTrue(!string.IsNullOrWhiteSpace(column));
            StringBuilder buider = new StringBuilder();
            for (int i = 0; i < column.Length; i++)
            {
                if (column[i].Equals(undlineStr))
                {
                    i++;
                    buider.Append(column[i].ToString().ToUpper());
                }
                else
                {
                    buider.Append(column[i].ToString().ToLower());
                }
            }
            return buider.ToString();
        }
        public static string camelCaseLowConvert(string column)
        {
            AssertUtils.IsTrue(!string.IsNullOrWhiteSpace(column));
            StringBuilder buider = new StringBuilder();
            if (!column.ToLower().Equals(column))
            {
                for (int i = 0; i < column.Length; i++)
                {
                    if (Char.IsUpper(column[i]))
                    {
                        buider.Append(undlineStr);
                        buider.Append(column[i].ToString().ToLower());
                    }
                    else
                    {
                        buider.Append(column[i].ToString().ToLower());
                    }
                }
            }
            else
            {
                buider.Append(column);
            }
            return buider.ToString();
        }



        public static bool hasLegth(string value)
        {
            return value != null && !string.IsNullOrWhiteSpace(value);
        }

        private static string changeFirstCharacterCase(string str, bool capitalize)
        {
            if (!hasLegth(str))
            {
                return str;
            }
            else
            {
                char[] arr = str.ToCharArray();
                char baseChar = arr[0];
                char updateChar;
                if (capitalize)
                {
                    updateChar = Char.ToUpper(baseChar);
                }
                else
                {
                    updateChar = Char.ToLower(baseChar);
                }

                if (baseChar == updateChar)
                {
                    return str;
                }
                else
                {
                    arr[0] = updateChar;
                    return new string(arr);
                }

            }
        }
    }
}