using System;

namespace Frameset.Core.Exceptions
{
    public class IllegalArgumentException : Exception
    {
        public IllegalArgumentException(string message) : base(message)
        {

        }
        public IllegalArgumentException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
