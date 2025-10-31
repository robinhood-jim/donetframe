using Spring.Util;
using System;
using System.Text;


namespace Frameset.Core.Utils
{
    public class StringUtils
    {
        private static readonly string undlineStr = "_";
        public static int ASCII_VISIBLE_START = 48;
        public static int ASCII_VISIBLE_END = 122;
        public static int ASCII_UPPER_START = 64;
        public static int ASCII_LOWER_START = 96;
        public static string Capitalize(string value)
        {
            if (HasLegth(value))
            {
                return changeFirstCharacterCase(value, true);
            }
            else
            {
                throw new AggregateException("value is empty!");
            }
        }

        public static string Uncapitalize(string value)
        {
            if (HasLegth(value))
            {
                return changeFirstCharacterCase(value, false);
            }
            else
            {
                throw new AggregateException("value is empty!");
            }
        }
        public static string CamelCaseUpperConvert(string column)
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
        public static string CamelCaseLowConvert(string column)
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
        public static String GenerateRandomChar(Random random, int length)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                builder.Append((char)(ASCII_VISIBLE_START + getRandomChar(random)));
            }
            return builder.ToString();
        }
        private static int getRandomChar(Random random)
        {
            return random.Next(ASCII_VISIBLE_END - ASCII_VISIBLE_START + 1);
        }


        public static bool HasLegth(string value)
        {
            return value != null && !string.IsNullOrWhiteSpace(value);
        }

        private static string changeFirstCharacterCase(string str, bool capitalize)
        {
            if (!HasLegth(str))
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