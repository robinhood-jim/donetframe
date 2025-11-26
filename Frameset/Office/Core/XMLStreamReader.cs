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
        public string GetAttribute(string name)
        {
            return reader.GetAttribute(name);
        }
        public string GetAttribute(string nameSpace, string name)
        {
            return reader.GetAttribute(name, nameSpace);
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
                throw new XmlException("read xml error!");
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
        public void DoUntilEnd(string eleName, Action<XMLStreamReader> action)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.LocalName.Equals(eleName))
                    {
                        reader.Read();
                        break;
                    }
                    else
                    {
                        reader.Read();
                    }
                }
                if (reader.IsStartElement())
                {
                    action.Invoke(this);
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
                else if (nodeType == XmlNodeType.EndElement)
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
