using System;

namespace Frameset.Core.Exceptions
{
    public class ExcelException : Exception
    {
        public ExcelException(string message) : base(message)
        {

        }
        public ExcelException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
