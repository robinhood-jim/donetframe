using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Xml;

namespace Frameset.Common.Data.Reader
{
    public class XmlIterator<T> : AbstractDataIterator<T>
    {
        private XmlReader xmlReader;
        private string rootEleNodeName;
        private string childEleNodeName;
        public XmlIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.XML;
        }

        public XmlIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.XML;
        }

        public override void initalize(string filePath = null)
        {
            base.initalize(filePath);
            xmlReader = XmlReader.Create(inputStream);
            while (xmlReader.Read())
            {
                if (xmlReader.IsStartElement())
                {
                    if (rootEleNodeName.IsNullOrEmpty())
                    {
                        rootEleNodeName = xmlReader.LocalName;
                    }
                    else if (childEleNodeName.IsNullOrEmpty())
                    {
                        childEleNodeName = xmlReader.LocalName;
                        break;
                    }
                }
            }
        }

        public override IAsyncEnumerable<T> ReadAsync(string path = null, string filterSql = null)
        {
            base.initalize(path);
            return aysncQuery();

        }
        private async IAsyncEnumerable<T> aysncQuery()
        {
            while (await xmlReader.ReadAsync())
            {
                if (xmlReader.NodeType != XmlNodeType.EndElement && xmlReader.NodeType != XmlNodeType.EndEntity)
                {
                    if (xmlReader.HasAttributes && xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        while (xmlReader.MoveToNextAttribute())
                        {
                            cachedValue.TryAdd(xmlReader.Name, xmlReader.Value);
                        }
                        xmlReader.MoveToElement();
                    }
                    else if (!xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        cachedValue.TryAdd(xmlReader.LocalName, xmlReader.Value);
                    }
                }
                else if (xmlReader.NodeType == XmlNodeType.EndElement)
                {
                    if (xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        xmlReader.Read();
                        ConstructReturn();
                        yield return current;
                    }
                }
            }

        }
        public override bool MoveNext()
        {
            base.MoveNext();
            cachedValue.Clear();
            bool hasNext = false;
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType != XmlNodeType.EndElement && xmlReader.NodeType != XmlNodeType.EndEntity)
                {
                    if (xmlReader.HasAttributes && xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        while (xmlReader.MoveToNextAttribute())
                        {
                            cachedValue.TryAdd(xmlReader.Name, xmlReader.Value);
                        }
                        xmlReader.MoveToElement();
                    }
                    else if (!xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        cachedValue.TryAdd(xmlReader.LocalName, xmlReader.Value);
                    }
                }
                else if (xmlReader.NodeType == XmlNodeType.EndElement)
                {
                    if (xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        xmlReader.Read();
                        hasNext = true;
                        break;
                    }
                }
            }

            if (hasNext)
            {
                ConstructReturn();
            }
            return hasNext;
        }


    }
}
