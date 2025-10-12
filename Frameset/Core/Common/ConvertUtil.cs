using Spring.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;


namespace Frameset.Core.Common
{
    public class ConvertUtil
    {
        private static string[] DEFAULTFORMATTER = { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "MM-dd-yy" };
        public static object parseByType(Type targetType, object input)
        {
            object retVal = null;
            AssertUtils.ArgumentNotNull(input, "must not be null!");

            if (targetType.Equals(input.GetType()))
            {
                retVal = input;
            }
            else
            {
                switch (Type.GetTypeCode(targetType))
                {
                    case TypeCode.Int32:
                        retVal = Int32.Parse(input.ToString());
                        break;
                    case TypeCode.Int16:
                        retVal = Int16.Parse(input.ToString());
                        break;
                    case TypeCode.Int64:
                        retVal = long.Parse(input.ToString());
                        break;
                    case TypeCode.Double:
                        retVal = double.Parse(input.ToString());
                        break;
                    case TypeCode.Decimal:
                        retVal = double.Parse(input.ToString());
                        break;
                    case TypeCode.DateTime:
                        DateTime? time = parseDateTime(input.ToString());
                        if (time.HasValue)
                        {
                            retVal = time.Value;
                        }
                        break;
                    default:
                        if (targetType.Equals(typeof(DateTimeOffset)))
                        {
                            if (input.GetType().Equals(typeof(DateTime)))
                            {
                                retVal = new DateTimeOffset((DateTime)input, TimeSpan.FromHours(8));
                            }
                            else
                            {
                                DateTime? stime = parseDateTime(input.ToString());
                                if (stime.HasValue)
                                {
                                    retVal = new DateTimeOffset(stime.Value, TimeSpan.FromHours(8));
                                }
                            }
                        }
                        else
                        {
                            retVal = input.ToString();
                        }
                        break;
                }
            }
            return retVal;

        }
        public static DateTime? parseDateTime(string dateStr)
        {
            DateTime? retTime = null;
            if (NumberUtils.IsNumber(dateStr))
            {
                retTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(dateStr)).LocalDateTime;
            }
            else
            {
                foreach (string pattern in DEFAULTFORMATTER)
                {
                    Tuple<bool, DateTime> parseTuple = guessFormat(dateStr, pattern);
                    if (parseTuple.Item1)
                    {
                        retTime = parseTuple.Item2;
                        break;
                    }
                }

            }
            return retTime;
        }
        public static Tuple<bool, DateTime> guessFormat(string dateStr, string pattern)
        {
            DateTime parseDate;
            if (DateTime.TryParseExact(dateStr, pattern, null, DateTimeStyles.None, out parseDate))
            {
                return new Tuple<bool, DateTime>(true, parseDate);
            }
            return new Tuple<bool, DateTime>(false, DateTime.MinValue);
        }
        public static void ToDict(object obj, Dictionary<string, object> dict)
        {
            PropertyInfo[] propertyInfos = obj.GetType().GetProperties();
            foreach (PropertyInfo prop in propertyInfos)
            {
                string name = prop.Name;
                MethodInfo info = prop.GetMethod;
                object value = info.Invoke(obj, null);
                dict.TryAdd(name, value);
            }

        }
    }
}
