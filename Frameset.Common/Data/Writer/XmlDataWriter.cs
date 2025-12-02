using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Xml;

namespace Frameset.Common.Data.Writer
{
    public class XmlDataWriter<T> : AbstractDataWriter<T>
    {
        private readonly string? collectionNodeName;
        private readonly string? entityName;
        private readonly XmlWriter xmlwriter;
        public XmlDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.XML;
            Initalize();
            if (IsReturnDictionary())
            {
                MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.XMLCOLLECTIONNAME, out collectionNodeName);
                MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.XMLENTITYIONNAME, out entityName);
                if (collectionNodeName.IsNullOrEmpty())
                {
                    collectionNodeName = ResourceConstants.XMLDEFUALTCOLLECTIONAME;
                }
                if (entityName.IsNullOrEmpty())
                {
                    entityName = ResourceConstants.XMLDEFAULTENTITYNAME;
                }
            }
            else
            {
                collectionNodeName = typeof(T).Name + "s";
                entityName = typeof(T).Name;
            }
            xmlwriter = XmlWriter.Create(outputStream);
            xmlwriter.WriteStartDocument();
            xmlwriter.WriteStartElement(collectionNodeName);
        }

        public XmlDataWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.XML;
            Initalize();
            collectionNodeName = typeof(T).Name + "s";
            entityName = typeof(T).Name;
            xmlwriter = XmlWriter.Create(outputStream);
            xmlwriter.WriteStartDocument();
            xmlwriter.WriteStartElement(collectionNodeName);
        }

        public override void FinishWrite()
        {
            xmlwriter.WriteEndElement();
            xmlwriter.Flush();
            xmlwriter.Close();
        }

        public override void WriteRecord(T value)
        {
            xmlwriter.WriteStartElement(entityName);
            foreach (DataSetColumnMeta column in MetaDefine.ColumnList)
            {
                object? retVal = GetValue(value, column);
                if (retVal != null)
                {
                    object? getValue = GetOutput(column, retVal);
                    if (getValue != null)
                    {
                        xmlwriter.WriteStartElement(column.ColumnCode);
                        xmlwriter.WriteValue(getValue);
                        xmlwriter.WriteEndElement();
                    }
                }
            }
            xmlwriter.WriteEndElement();
        }
    }
}
