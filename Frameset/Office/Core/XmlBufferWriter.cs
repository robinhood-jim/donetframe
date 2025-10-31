using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Security;
using System.Text;

namespace Frameset.Office.Core
{
    public class XmlBufferWriter : IDisposable
    {
        private StringBuilder builder;

        private long totalSize;
        internal Stream outStream;

        public XmlBufferWriter(Stream outStream, int bufferSize = 1024 * 1024)
        {
            this.outStream = outStream;
            int size = bufferSize > 0 ? bufferSize : 1024 * 1024;
            builder = new StringBuilder(size);
        }
        private XmlBufferWriter Append(string text, bool escape)
        {
            if (escape)
            {
                builder.Append(SecurityElement.Escape(text));
            }
            else
            {
                builder.Append(text);
            }
            CheckRemain();
            return this;
        }
        public XmlBufferWriter Append(string text)
        {
            this.Append(text, false);
            return this;
        }
        public XmlBufferWriter AppendEscaped(string text)
        {
            this.Append(text, true);
            return this;
        }
        public XmlBufferWriter Append(int n)
        {
            builder.Append(n);
            CheckRemain();
            return this;
        }
        public XmlBufferWriter Append(long n)
        {
            builder.Append(n);
            CheckRemain();
            return this;
        }
        public XmlBufferWriter Append(double n)
        {
            builder.Append(n);
            CheckRemain();
            return this;
        }
        public XmlBufferWriter Append(float n)
        {
            builder.Append(n);
            CheckRemain();
            return this;
        }
        public XmlBufferWriter Append(decimal n)
        {
            builder.Append(n);
            CheckRemain();
            return this;
        }

        internal void CheckRemain()
        {
            if (builder.Capacity - builder.Length < 4 * 1024)
            {
                Flush();
            }
        }
        public void Flush()
        {
            totalSize += builder.Length;
            outStream.Write(Encoding.GetEncoding("utf-8").GetBytes(builder.ToString()));
            outStream.Flush();
            builder.Length = 0;
        }
        public long TotalSize()
        {
            return totalSize;
        }
        public ZipOutputStream GetZipOutputStream()
        {
            return (ZipOutputStream)outStream;
        }
        public bool ShouldClose(int maxSize, int threshold)
        {
            return maxSize > 0 && maxSize - totalSize - builder.Length <= threshold;
        }

        public void Dispose()
        {

            if (outStream != null)
            {
                Flush();
            }
            outStream.Close();
        }
    }
}
