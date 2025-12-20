using Frameset.Office.Core;
using Frameset.Office.Meta;
using Frameset.Office.Word.Element;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Frameset.Office.Word.Util
{
    public sealed class BodyElementEnumerator : IEnumerator<IBodyElement>
    {
        private readonly Document document;
        private readonly XMLStreamReader reader;
        private string paragraphId;
        private string rsidRDefault;
        private string rsidR;
        private string runId;
        private IBodyElement bodyElement;
        public static readonly string NAMESPACEMAIN = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        public static readonly string NAMESPACEW14 = "http://schemas.microsoft.com/office/word/2010/wordml";

        public BodyElementEnumerator(Document document, Stream inputStream)
        {
            this.document = document;
            reader = new XMLStreamReader(inputStream);
            reader.GotoElement("body");
        }

        public IBodyElement Current => bodyElement;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool MoveNext()
        {
            List<PictureData> pictureDatas = [];
            bodyElement = null;
            List<XRunElement> elements = [];
            StringBuilder builder = new StringBuilder();
            while (reader.HasNext())
            {
                if (reader.IsStartElement("p"))
                {
                    paragraphId = reader.GetAttribute(NAMESPACEW14, "paraId");
                    rsidRDefault = reader.GetAttribute(NAMESPACEMAIN, "rsidRDefault");
                    rsidR = reader.GetAttribute(NAMESPACEMAIN, "rsidR");
                    reader.GoTo(() => reader.IsStartElement("r") || reader.IsStartElement("t"));
                }
                if (reader.IsEndElement("p"))
                {
                    bodyElement = new ParagraphElement(paragraphId, rsidR, rsidRDefault, runId, elements);
                    reader.Next();
                    break;
                }
                if (reader.IsStartElement("r"))
                {
                    runId = null;
                    if (reader.GetAttributeCount() > 0)
                    {
                        runId = reader.GetAttributeAt(0);
                    }
                    XRunElement element1 = new XRunElement(runId);
                    if (builder.Length > 0)
                    {
                        builder.Remove(0, builder.Length);
                    }
                    reader.DoUntilEnd("r", r2 =>
                    {
                        string name = reader.GetLocalName();
                        if (string.Equals("t", name, StringComparison.OrdinalIgnoreCase))
                        {
                            builder.Append(reader.GetValueUntilEndElement("t"));
                        }
                        else if (string.Equals("blip", name, StringComparison.OrdinalIgnoreCase))
                        {
                            PictureData pictureData = new PictureData();
                            pictureData.Rid = r2.GetAttribute(0);
                            document.GetOpcPackage().RelationShipMap.TryGetValue(pictureData.Rid, out RelationShip relationShip);
                            pictureData.Path = relationShip?.Target;
                            pictureDatas.Add(pictureData);
                        }
                    });
                    if (builder.Length > 0)
                    {
                        element1.Content = builder.ToString();
                    }
                    if (!pictureDatas.IsNullOrEmpty())
                    {
                        element1.PictureDatas = pictureDatas;
                    }
                    elements.Add(element1);
                }
                else if (reader.IsStartElement("tbl"))
                {
                    TableElement tableElement = ParseTable();
                    if (tableElement != null)
                    {
                        bodyElement = tableElement;
                        break;
                    }
                }
                else
                {
                    reader.Next();
                }
            }
            return bodyElement != null;
        }
        private TableElement ParseTable()
        {
            List<string> header = [];
            List<string> tmpValues = [];
            List<List<string>> values = [];
            StringBuilder builder = new StringBuilder();
            bool firstRow = true;
            if (reader.IsStartElement("tbl"))
            {
                while (reader.HasNext())
                {
                    reader.Next();
                    if (reader.IsStartElement("tr") && tmpValues.Count > 0)
                    {
                        tmpValues = new();
                    }
                    if (reader.IsStartElement("tc"))
                    {
                        if (builder.Length > 0)
                        {
                            builder.Remove(0, builder.Length);
                        }
                    }
                    else if (reader.IsStartElement("t"))
                    {
                        builder.Append(reader.GetValueUntilEndElement("t"));
                    }
                    else if (reader.IsEndElement("tc"))
                    {
                        tmpValues.Add(builder.ToString());
                    }
                    else if (reader.IsEndElement("tr"))
                    {
                        if (firstRow)
                        {
                            header.AddRange(tmpValues);
                            firstRow = false;
                        }
                        else
                        {
                            values.Add(tmpValues);
                        }
                    }
                    else if (reader.IsEndElement("tbl"))
                    {
                        reader.Next();
                        break;
                    }
                }
            }
            if (!header.IsNullOrEmpty() || !values.IsNullOrEmpty())
            {
                return new TableElement(header, values);
            }
            else
            {
                return null;
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
