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
                return ChangeFirstCharacterCase(value, true);
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
                return ChangeFirstCharacterCase(value, false);
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
                        if (i != 0)
                        {
                            buider.Append(undlineStr);
                        }
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
                builder.Append((char)(ASCII_VISIBLE_START + GetRandomChar(random)));
            }
            return builder.ToString();
        }
        private static int GetRandomChar(Random random)
        {
            return random.Next(ASCII_VISIBLE_END - ASCII_VISIBLE_START + 1);
        }


        public static bool HasLegth(string value)
        {
            return value != null && !string.IsNullOrWhiteSpace(value);
        }

        private static string ChangeFirstCharacterCase(string str, bool capitalize)
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
        public static bool IsGuidByArr(string strSrc)
        {
            if (String.IsNullOrEmpty(strSrc) || strSrc.Length != 36) { return false; }
            string[] arr = strSrc.Split('-');
            if (arr.Length != 5) { return false; }
            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = 0; j < arr[i].Length; j++)
                {
                    char a = arr[i][j];
                    if (!((a >= 48 && a <= 57) || (a >= 65 && a <= 90) || (a >= 97 && a <= 122)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static int ComputeLevenshteinDistance(string s1, string s2)
        {
            int n = s1.Length;
            int m = s2.Length;
            int[,] dp = new int[n + 1, m + 1];

            // Initialize base cases
            for (int i = 0; i <= n; i++) dp[i, 0] = i;
            for (int j = 0; j <= m; j++) dp[0, j] = j;

            // Fill the matrix
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                    );
                }
            }

            return dp[n, m];
        }
        public static int ComputeDamerauLevenshteinDistance(string a, string b)
        {
            var dxSize = a.Length + 2;
            var dySize = b.Length + 2;
            var da = new int[256];
            for (int i = 0; i < 256; i++)
                da[i] = 1;
            var d = new int[dxSize, dySize];

            var maxdist = a.Length + b.Length;
            for (var dx = 0; dx < dxSize; dx++)
            {
                d[dx, 0] = maxdist;
                if (dx < dxSize - 1) d[dx + 1, 1] = dx;
            }
            for (var dy = 0; dy < dySize; dy++)
            {
                d[0, dy] = maxdist;
                if (dy < dySize - 1) d[1, dy + 1] = dy;
            }

            for (int dx = 2; dx < dxSize; dx++)
            {
                var aPos = dx - 2;
                var db = 1;
                for (int dy = 2; dy < dySize; dy++)
                {
                    var bPos = dy - 2;
                    var k = da[b[bPos]];
                    var l = db;
                    int cost;
                    if (a[aPos] == b[bPos])
                    {
                        cost = 0;
                        db = dy;
                    }
                    else
                    {
                        cost = 1;
                    }

                    d[dx, dy] = Min(
                        d[dx - 1, dy - 1] + cost, // substitution
                        d[dx, dy - 1] + 1, // insertion
                        d[dx - 1, dy] + 1, // deletion
                        d[k - 1, l - 1] + (dx - k - 1) + 1 + (dy - l - 1)); // transposition
                }
                da[a[aPos]] = dx;
            }

            var distance = d[dxSize - 1, dySize - 1];

            return distance;
        }
        public static int Min(int i1, int i2, int i3)
        {
            return Math.Min(
                i1,
                Math.Min(i2, i3));
        }

        public static int Min(int i1, int i2, int i3, int i4)
        {
            return Math.Min(
                Min(i1, i2, i3),
                i4);
        }
        public string FillHeader(string input, int length, char fillChar)
        {
            StringBuilder builder = new();
            for (int i = 0; i < length - input.Length; i++)
            {
                builder.Append(fillChar);
            }
            builder.Append(input);
            return builder.ToString();
        }
        public string FillTail(string input, int length, char fillChar)
        {
            StringBuilder builder = new(input);
            for (int i = 0; i < length - input.Length; i++)
            {
                builder.Append(fillChar);
            }
            return builder.ToString();
        }
        public static string StringToUnicode(string source)
        {
            var bytes = Encoding.Unicode.GetBytes(source);
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < bytes.Length; i += 2)
            {
                stringBuilder.AppendFormat("\\u{0:x2}{1:x2}", bytes[i + 1], bytes[i]);
            }
            return stringBuilder.ToString();
        }
    }

}