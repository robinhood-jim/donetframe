using System;

namespace Frameset.Core.Exceptions
{
    public class WebException : Exception
    {
        public WebException(string message) : base(message)
        {

        }
        public WebException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
