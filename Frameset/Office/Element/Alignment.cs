using Frameset.Office.Core;
using System;

namespace Frameset.Office.Element
{
    public class Alignment : IWritableElement
    {
        private string horizontal;
        private string vertical;
        private bool wrapText;
        private int rotation;
        private int indent;
        public Alignment(string horizontal, string vertical, bool wrapText, int rotation, int indent)
        {
            this.horizontal = horizontal;
            this.vertical = vertical;
            this.wrapText = wrapText;
            this.rotation = rotation;
            this.indent = indent;
        }

        public override bool Equals(object obj)
        {
            return obj is Alignment alignment &&
                   horizontal == alignment.horizontal &&
                   vertical == alignment.vertical &&
                   wrapText == alignment.wrapText &&
                   rotation == alignment.rotation &&
                   indent == alignment.indent;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(horizontal, vertical, wrapText, rotation, indent);
        }

        public void WriteOut(XmlBufferWriter w)
        {
            w.Append("<alignment");
            if (horizontal != null)
            {
                w.Append(" horizontal=\"").Append(horizontal).Append("\"");
            }
            if (vertical != null)
            {
                w.Append(" vertical=\"").Append(vertical).Append("\"");
            }
            if (rotation != 0)
            {
                w.Append(" textRotation=\"").Append(rotation).Append("\"");
            }
            if (indent != 0)
            {
                w.Append(" indent=\"").Append(indent).Append("\"");
            }
            if (wrapText)
            {
                w.Append(" wrapText=\"true\"");
            }
            w.Append("/>");
        }
    }
}
