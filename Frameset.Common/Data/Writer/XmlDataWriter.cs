using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Xml;

namespace Frameset.Common.Data.Writer
{
    public class XmlDataWriter<T> : AbstractDataWriter<T>
    {
        private string collectionNodeName;
        private string childNodeName;
        private XmlWriter xmlwriter;
        public XmlDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.XML;
            initalize();
            MetaDefine.ResourceConfig.TryGetValue("xml.collectionName", out collectionNodeName);
            MetaDefine.ResourceConfig.TryGetValue("xml.childName", out childNodeName);
            if (collectionNodeName.IsNullOrEmpty())
            {
                collectionNodeName = "Records";
            }
            if (childNodeName.IsNullOrEmpty())
            {
                childNodeName = "Record";
            }
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
            xmlwriter.WriteStartElement(childNodeName);
            foreach (DataSetColumnMeta column in MetaDefine.ColumnList)
            {
                object retVal = GetValue(value, column);
                if (retVal != null)
                {
                    xmlwriter.WriteStartElement(column.ColumnCode);
                    xmlwriter.WriteValue(GetOutput(column, retVal));
                    xmlwriter.WriteEndElement();
                }
            }
            xmlwriter.WriteEndElement();
        }
    }
}
