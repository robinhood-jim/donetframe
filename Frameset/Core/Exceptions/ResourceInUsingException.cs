using System;

namespace Frameset.Core.Exceptions
{
    public class ResourceInUsingException : Exception
    {
        public ResourceInUsingException(string message) : base(message)
        {

        }
        public ResourceInUsingException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
