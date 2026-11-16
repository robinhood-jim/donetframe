using System;

namespace Frameset.Core.Exceptions
{
    public class DuplicatedException : Exception
    {
        public DuplicatedException(string message) : base(message)
        {

        }
        public DuplicatedException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
