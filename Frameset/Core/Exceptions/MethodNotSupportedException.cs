using System;

namespace Frameset.Core.Exceptions
{
    public class MethodNotSupportedException : Exception
    {
        public MethodNotSupportedException(string message) : base(message)
        {

        }
        public MethodNotSupportedException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
