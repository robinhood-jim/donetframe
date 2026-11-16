using System;

namespace Frameset.Core.Exceptions
{
    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message)
        {

        }
        public ServiceException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
