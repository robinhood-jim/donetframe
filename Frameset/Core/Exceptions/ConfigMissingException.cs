using System;

namespace Frameset.Core.Exceptions
{
    public class ConfigMissingException : Exception
    {
        public ConfigMissingException(string message) : base(message)
        {

        }
        public ConfigMissingException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
