using System;

namespace Frameset.Core.Exceptions
{
    public class OperationFailedException : Exception
    {
        public OperationFailedException(string message) : base(message)
        {

        }

        public OperationFailedException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
