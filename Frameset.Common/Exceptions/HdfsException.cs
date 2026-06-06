namespace Frameset.Common.Exceptions
{
    public class HdfsException : Exception
    {
        public HdfsException(string message) : base(message)
        {

        }

        public HdfsException(string message, Exception innerClass) : base(message, innerClass)
        {

        }
    }
}
