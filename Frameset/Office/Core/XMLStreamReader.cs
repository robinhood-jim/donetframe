using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Frameset.Office.Core
{
    public class XMLStreamReader:IDisposable
    {
        private readonly Stream stream;
        private readonly XmlReader reader;
        public XMLStreamReader(Stream stream)
        {
            this.stream = stream;
            reader = XmlReader.Create(stream);
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
            return reader.NodeType==XmlNodeType.EndElement && reader.LocalName.Equals(eleName);
        }
        public bool Goto(string eleName)
        {
            return GoTo(() => IsStartElement(eleName));
        }
        public bool HasNext()
        {
            return reader.Read();
        }
        public string GetAttribute(string name)
        {
            return reader.GetAttribute(name);
        }
        public string GetAttribute(string nameSpace,string name)
        {
            return reader.GetAttribute(name,nameSpace);
        }
        public string GetValue(string eleName)
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                while (reader.Read())
                {
                    if(reader.NodeType==XmlNodeType.CDATA || reader.NodeType==XmlNodeType.Text || reader.NodeType == XmlNodeType.Whitespace)
                    {
                        builder.Append(reader.Value.ToString());
                    }
                    if(reader.NodeType==XmlNodeType.EndElement && reader.LocalName.Equals(eleName))
                    {
                        break;
                    }
                }
                return builder.ToString();
            }catch(Exception ex)
            {
                throw new XmlException("read xml error!");
            }
        }
        public void Foreach(string startElement,string endElement,Action<XmlReader> action)
        {
            
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
             
        }
    }
}
