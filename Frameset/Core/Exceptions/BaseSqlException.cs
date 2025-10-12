using System;

namespace Frameset.Core.Exceptions
{
    internal class BaseSqlException : Exception
    {
        public BaseSqlException(string message) : base(message)
        {

        }
        public BaseSqlException(string message, Exception innerClass) : base(message, innerClass)
        {

        }

    }
}
