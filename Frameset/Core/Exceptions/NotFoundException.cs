using System;

namespace Frameset.Core.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {

        }
        public NotFoundException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
