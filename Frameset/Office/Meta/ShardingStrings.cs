using Frameset.Office.Core;
using Frameset.Office.Element;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Frameset.Office.Meta
{
    public class ShardingStrings : IWritableElement
    {
        public IList<ShardingString> Values
        {
            get; internal set;
        } = new List<ShardingString>();
        internal Stream inputStream;
        internal ShardingStrings(Stream inputStream)
        {
            this.inputStream = inputStream;
        }
        public ShardingStrings()
        {

        }
        public static ShardingStrings FromStream(Stream inputStream)
        {
            ShardingStrings s = new ShardingStrings(inputStream);
            s.Constrcut();
            return s;

        }
        internal void Constrcut()
        {
            int pos = 0;
            using (XMLStreamReader reader = new XMLStreamReader(inputStream))
            {
                while (reader.HasNext())
                {
                    if (!reader.IsStartElement("si"))
                    {
                        reader.GotoElement("si");
                    }
                    StringBuilder builder = new StringBuilder();
                    while (reader.GoTo(() => reader.IsStartElement("t") || reader.IsStartElement("rPh") || reader.IsEndElement("si")))
                    {
                        if (reader.IsStartElement("t"))
                        {
                            builder.Append(reader.GetValueUntilEndElement("t"));
                        }
                        else if (reader.IsEndElement("si"))
                        {
                            break;
                        }
                        else if (reader.IsStartElement("rPh"))
                        {
                            reader.GoTo(() => reader.IsEndElement("rPh"));
                        }
                    }
                    if (builder.Length > 0)
                    {
                        Values.Add(new ShardingString(builder.ToString(), pos++));
                    }
                }
            }
        }
        public IList<ShardingString> GetValues()
        {
            return Values;
        }

        public void WriteOut(XmlBufferWriter writer)
        {
            writer.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n")
                .Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"10\" uniqueCount=\"").Append(Values.Count + "\">\n");
            foreach (ShardingString value in Values)
            {
                writer.Append("<si><t>").AppendEscaped(value.Value).Append("</t></si>");
            }
            writer.Append("\n</sst>");
        }
    }
    public class ShardingString
    {
        internal string Value
        {
            get; set;
        }
        internal int Index
        {
            get; set;
        }
        public ShardingString(string value, int index)
        {
            this.Value = value;
            this.Index = index;
        }
    }
}
