using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Frameset.Office.Core
{
    public class XMLStreamReader : IDisposable
    {
        private readonly Stream stream;
        private readonly XmlReader reader;
        public XMLStreamReader(Stream stream)
        {
            this.stream = stream;
            reader = XmlReader.Create(stream);
        }
        public bool GotoElement(string eleName)
        {
            return GoTo(() => IsStartElement(eleName));
        }
        public bool GoTo(Func<bool> predicate)
        {
            while (reader.Read())
            {
                if (predicate.Invoke())
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsStartElement(string eleName)
        {
            return reader.IsStartElement() && reader.LocalName.Equals(eleName);
        }
        public bool IsEndElement(string eleName)
        {
            return reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals(eleName);
        }
        public string GetLocalName()
        {
            return reader.LocalName;
        }
        public bool HasNext()
        {
            return !reader.EOF;
        }
        public void Next()
        {
            if (HasNext())
            {
                reader.Read();
            }
        }
        public string GetAttribute(string name)
        {
            return reader.GetAttribute(name);
        }
        public string GetAttributeAt(int pos)
        {
            return reader.GetAttribute(pos);
        }
        public string GetAttribute(string nameSpace, string name)
        {
            return reader.GetAttribute(name, nameSpace);
        }
        public int GetAttributeCount()
        {
            return reader.AttributeCount;
        }
        public string GetValue(string eleName)
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.CDATA || reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.Whitespace)
                    {
                        builder.Append(reader.Value.ToString());
                    }
                    if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals(eleName))
                    {
                        break;
                    }
                }
                return builder.ToString();
            }
            catch (Exception ex)
            {
                throw new XmlException("read xml error! message " + ex.Message);
            }
        }
        public string GetText()
        {
            return reader.Value;
        }
        public void Foreach(string startElement, string endElement, Action<XMLStreamReader> action)
        {
            while (GoTo(() => IsStartElement(startElement) || IsEndElement(endElement)))
            {
                if (reader.LocalName.Equals(endElement))
                {
                    break;
                }
                action.Invoke(this);
            }
        }
        public void DoUntilEnd(string eleName, Action<XmlReader> action)
        {
            bool breakable = false;
            while (reader.Read())
            {
                while (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (string.Equals(reader.LocalName, eleName, StringComparison.OrdinalIgnoreCase))
                    {
                        reader.Read();
                        breakable = true;
                        break;
                    }
                    else
                    {
                        reader.Read();
                    }
                }
                if (breakable)
                {
                    break;
                }

                if (reader.IsStartElement())
                {
                    action.Invoke(reader);
                }
            }
        }
        public string GetValueUntilEndElement(string eleName)
        {
            return GetValueUntilEndElement(eleName, "");
        }
        internal string GetValueUntilEndElement(string eleName, string skipping)
        {
            StringBuilder builder = new StringBuilder();
            int childElement = 1;
            while (reader.Read())
            {
                XmlNodeType nodeType = reader.NodeType;
                if (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.CDATA || nodeType == XmlNodeType.Whitespace)
                {
                    builder.Append(reader.Value);
                }
                else if (reader.IsStartElement())
                {
                    if (skipping.Equals(reader.LocalName))
                    {
                        GetValueUntilEndElement(reader.LocalName);
                    }
                    else
                    {
                        childElement++;
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    childElement--;
                    if (eleName.Equals(reader.LocalName) && childElement == 0)
                    {
                        break;
                    }
                }
            }
            return builder.ToString();
        }
        public void DoInAttributes(Action<XmlReader> consumer)
        {
            consumer.Invoke(reader);
        }

        public void Dispose()
        {
            if (reader != null)
            {
                reader.Close();
            }
            if (stream != null)
            {
                stream.Close();
            }
            GC.SuppressFinalize(this);
        }
    }
}
