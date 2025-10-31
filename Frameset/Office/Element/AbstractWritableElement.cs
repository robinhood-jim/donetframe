using Frameset.Office.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;

namespace Frameset.Office.Element
{
    public abstract class AbstractWritableElement : IWritableElement
    {
        public virtual void WriteOut(XmlBufferWriter writer)
        {
            throw new NotImplementedException();
        }
        public void BeginPart(XmlBufferWriter writer, string partName)
        {
            writer.GetZipOutputStream().PutNextEntry(new ZipEntry(partName));
        }
        public void ClosePart(XmlBufferWriter writer)
        {
            writer.GetZipOutputStream().CloseEntry();
        }
    }
}
