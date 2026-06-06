using System;

namespace Frameset.Core.Exceptions
{
    public class ConfigIncorrectException : Exception
    {
        public ConfigIncorrectException(string message) : base(message)
        {

        }
        public ConfigIncorrectException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
