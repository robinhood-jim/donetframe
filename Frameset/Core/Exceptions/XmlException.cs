using System;

namespace Frameset.Core.Exceptions
{
    public class XmlException:Exception
    {
        public XmlException(string message) : base(message)
        {

        }
        public XmlException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
