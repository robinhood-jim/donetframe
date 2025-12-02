using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Xml;

namespace Frameset.Common.Data.Reader
{
    public class XmlIterator<T> : AbstractDataIterator<T>
    {
        private XmlReader xmlReader = null!;
        private string rootEleNodeName = null!;
        private string childEleNodeName = null!;
        private Dictionary<string, DataSetColumnMeta> metaMap = new Dictionary<string, DataSetColumnMeta>();

        public XmlIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.XML;
            Initalize(define.Path);
        }

        public XmlIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.XML;
            Initalize(define.Path);
        }

        public XmlIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.XML;
            Initalize(processPath);
        }

        public override sealed void Initalize(string? filePath = null)
        {
            base.Initalize(filePath);
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
            foreach (var item in MetaDefine.ColumnList)
            {
                metaMap.TryAdd(item.ColumnCode, item);
            }
        }

        public override IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null)
        {
            base.Initalize(path);
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
                            string name = xmlReader.Name;
                            DataSetColumnMeta? meta;
                            metaMap.TryGetValue(name, out meta);
                            if (meta == null)
                            {
                                throw new OperationFailedException("prop " + name + " not defined!");
                            }
                            ParseObject(meta);
                        }
                        xmlReader.MoveToElement();
                    }
                    else if (!xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        string propName = xmlReader.LocalName;
                        xmlReader.Read();
                        DataSetColumnMeta? meta;
                        metaMap.TryGetValue(propName, out meta);
                        if (meta == null)
                        {
                            throw new OperationFailedException("prop " + propName + " not defined!");
                        }
                        ParseObject(meta);
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
            CachedValue.Clear();
            bool hasNext = false;
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType != XmlNodeType.EndElement && xmlReader.NodeType != XmlNodeType.EndEntity)
                {
                    if (xmlReader.HasAttributes && xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        while (xmlReader.MoveToNextAttribute())
                        {
                            string name = xmlReader.Name;
                            DataSetColumnMeta? meta;
                            metaMap.TryGetValue(name, out meta);
                            if (meta == null)
                            {
                                throw new OperationFailedException("prop " + name + " not defined!");
                            }
                            ParseObject(meta);
                        }
                        xmlReader.MoveToElement();
                    }
                    else if (!xmlReader.LocalName.Equals(childEleNodeName))
                    {
                        string propName = xmlReader.LocalName;
                        xmlReader.Read();
                        DataSetColumnMeta? meta;
                        metaMap.TryGetValue(propName, out meta);
                        if (meta == null)
                        {
                            throw new OperationFailedException("prop " + propName + " not defined!");
                        }
                        ParseObject(meta);
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
        internal void ParseObject(DataSetColumnMeta meta)
        {
            if (meta.ColumnType != Constants.MetaType.TIMESTAMP)
            {
                CachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(xmlReader.Value, meta, dateFormatter));
            }
            else
            {
                CachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(xmlReader.Value, meta, timestampFormatter));
            }
        }

    }
}
