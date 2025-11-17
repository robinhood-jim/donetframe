using System;

namespace Frameset.Core.Exceptions
{
    public class OperationNotAllowedException : Exception
    {
        public OperationNotAllowedException(string message) : base(message)
        {

        }

        public OperationNotAllowedException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
