using ExtendedNumerics;
using Frameset.Core.Exceptions;
using System;

namespace Frameset.Office.Excel.Util
{
    public class DateUtils
    {
        private static BigDecimal BD_NANOSEC_DAY = new BigDecimal(8.64E7);
        private static BigDecimal BD_MILISEC_RND = new BigDecimal(500000.0);
        private static BigDecimal BD_SECOND_RND = new BigDecimal(5.0E8);
        private static int BAD_DATE = -1;
        public static int SECONDS_PER_MINUTE = 60;
        public static int MINUTES_PER_HOUR = 60;
        public static int HOURS_PER_DAY = 24;
        public static int SECONDS_PER_DAY = (HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE);
        public static long DAY_MILLISECONDS = SECONDS_PER_DAY * 1000L;
        public static double MILL_DAY = 8.6E7;

        public static double ConvertDateTime(DateTime dateTime)
        {
            return internalGetExcelDate(dateTime.Year, dateTime.DayOfYear, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
        }
        public static Double ConvertDateTime(DateTimeOffset dateTime)
        {
            return internalGetExcelDate(dateTime.Year, dateTime.DayOfYear, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
        }
        private static double internalGetExcelDate(int year, int dayOfYear, int hour, int minute, int second, int milliSecond)
        {
            if (year < 1900)
            {
                return BAD_DATE;
            }

            // Because of daylight time saving we cannot use
            //     date.getTime() - calStart.getTimeInMillis()
            // as the difference in milliseconds between 00:00 and 04:00
            // can be 3, 4 or 5 hours but Excel expects it to always
            // be 4 hours.
            // E.g. 2004-03-28 04:00 CEST - 2004-03-28 00:00 CET is 3 hours
            // and 2004-10-31 04:00 CET - 2004-10-31 00:00 CEST is 5 hours
            double fraction = (((hour * 60.0 + minute) * 60.0 + second) * 1000.0 + milliSecond) / DAY_MILLISECONDS;

            double value = fraction + absoluteDay(year, dayOfYear);

            if (value >= 60)
            {
                value++;
            }

            return value;
        }
        private static int absoluteDay(int year, int dayOfYear)
        {
            return dayOfYear + daysInPriorYears(year);
        }

        static int daysInPriorYears(int yr)
        {
            if (yr < 1900)
            {
                throw new IllegalArgumentException("'year' must be 1900 or greater");
            }
            int yr1 = yr - 1;
            int leapDays = yr1 / 4   // plus julian leap days in prior years
                    - yr1 / 100 // minus prior century years
                    + yr1 / 400 // plus years divisible by 400
                    - 460;      // leap days in previous 1900 years

            return 365 * (yr - 1900) + leapDays;
        }
        public static DateTime GetDateTime(double date, bool use1904windowing, bool roundSeconds)
        {
            if (!isValidExcelDate(date))
            {
                return DateTime.MinValue;
            }
            else
            {

                int wholeDays = (int)date;
                int startYear = 1900;
                int dayAdjust = -1;
                if (use1904windowing)
                {
                    startYear = 1904;
                    dayAdjust = 1;
                }
                else if (wholeDays < 61)
                {
                    dayAdjust = 0;
                }


                DateTime ldt = new DateTime(startYear, 1, 1, 0, 0, 0, 0);

                ldt = ldt.AddDays((long)(wholeDays + dayAdjust - 1));
                long nanosTime = (long)((date - wholeDays) * MILL_DAY);
                ldt = ldt.AddMilliseconds(nanosTime);
                return ldt;
            }
        }
        public static bool isValidExcelDate(double value)
        {
            return value > -4.9E-324;
        }
    }
}
