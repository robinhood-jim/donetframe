using Frameset.Core.Exceptions;
using Frameset.Core.Utils;
using Frameset.Office.Core;
using Frameset.Office.Element;
using Frameset.Office.Excel.Meta;
using Frameset.Office.Excel.Util;
using Frameset.Office.Meta;
using Frameset.Office.Util;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace Frameset.Office.Excel
{
    public class WorkBook : IDisposable
    {
        public int ActiveTab
        {
            get; internal set;
        } = 0;
        public bool Finished
        {
            get; internal set;
        } = false;
        public string ApplicationName
        {
            get; internal set;
        } = "rapidoffice";
        public string ApplicationVersion
        {
            get; internal set;
        } = "1.0";
        public IList<string> Formats
        {
            get; internal set;
        } = new List<string>();
        OpcPackage opcPackage;
        internal IList<WorkSheet> sheets = new List<WorkSheet>();
        internal Dictionary<string, WorkSheet> sheetMap = new Dictionary<string, WorkSheet>();
        public bool Date1904
        {
            get; internal set;
        } = false;
        internal ShardingStrings shardingStrings;

        private Dictionary<int, XmlBufferWriter> sheetWriterMap = new Dictionary<int, XmlBufferWriter>();
        internal XmlBufferWriter writer;
        static string sheetIdPrefix = "rId";
        internal int maxSheetSize = 0;
        internal string localTmpPath = null;
        internal CustomProperty properties = new CustomProperty();

        internal StyleHolder Holder
        {
            get; set;
        } = new StyleHolder();
        internal Dictionary<string, ShardingString> shardingStringMap = new Dictionary<string, ShardingString>();
        internal string workBookPath;
        internal string shardingStringsPath;
        internal string stylePath;
        internal string appPath;
        public Dictionary<string, string> FormatMap
        {
            get; internal set;
        } = new Dictionary<string, string>();
        internal IList<string> formatIdList = new List<string>();
        internal Dictionary<string, RelationShip> relationShipTypeMap = new Dictionary<string, RelationShip>();
        internal bool WriteTag
        {
            get; set;
        } = false;
        public ExcelSheetProp SheetProp
        {
            get; internal set;
        }
        internal int totalRow;
        public int MaxRows
        {
            get; set;
        } = WorkSheet.MAX_ROWS;
        protected WorkSheet currentSheet;
        protected int thresholdSize = 8 * 1024;

        public WorkBook(string fileName, ExcelSheetProp prop)
        {
            opcPackage = new OpcPackage(fileName);
            shardingStrings = new ShardingStrings();
            opcPackage.DoReadInit((zipFile) =>
            {
                extractPart();
                extractStyle(stylePath);
                opcPackage.ExtractRelationShip(OpcPackage.RelsNameFor(workBookPath), "_rel");
            });
            beginRead();
        }
        public WorkBook(Stream stream, bool writeTag, ExcelSheetProp prop)
        {
            SheetProp = prop;
            doInit(stream, writeTag);
        }
        public WorkBook(Stream stream, bool writeTag, Type entityType)
        {
            SheetProp = ExcelSheetProp.FromEntityDefine(entityType);
            doInit(stream, writeTag);
        }
        public WorkBook(Stream stream, bool writeTag, IDataReader reader, string timeFormat = "yyyy-MM-dd", Dictionary<string, string> nameMapping = null)
        {
            SheetProp = ExcelSheetProp.FromDataReader(reader, timeFormat, nameMapping);
            doInit(stream, writeTag);
        }
        private void doInit(Stream stream, bool writeTag)
        {
            shardingStrings = new ShardingStrings();
            if (writeTag)
            {
                opcPackage = OpcPackage.Create(stream);
                writer = new XmlBufferWriter(opcPackage.OutputStream, SheetProp.WriteBufferSize);
            }
            else
            {
                opcPackage = OpcPackage.Read(stream);
                opcPackage.DoReadInit((zipFile) =>
                {
                    extractPart();
                    extractStyle(stylePath);
                    opcPackage.ExtractRelationShip(OpcPackage.RelsNameFor(workBookPath), "_rel");
                });
                beginRead();
            }

            WriteTag = writeTag;
        }
        public void BeginWrite()
        {
            int sheetNum = GetSheetNum() + 1;
            if (currentSheet == null)
            {
                currentSheet = CreateSheet(SheetProp.SheetName ?? "sheet" + sheetNum, SheetProp);
            }
            else
            {
                currentSheet = CreateSheet(SheetProp.SheetName + sheetNum ?? "sheet" + sheetNum, SheetProp);
            }
        }
        void extractPart()
        {
            string contentTypePath = "[Content_Types].xml";
            using (XMLStreamReader reader = new XMLStreamReader(opcPackage.GetEntryContent(contentTypePath)))
            {
                while (reader.GoTo(() => reader.IsStartElement("Override")))
                {
                    string contentType = reader.GetAttribute("ContentType");
                    if (OpcPackage.WORKBOOK_MAIN_CONTENT_TYPE.Equals(contentType, StringComparison.OrdinalIgnoreCase) || OpcPackage.WORKBOOK_EXCEL_MACRO_ENABLED_MAIN_CONTENT_TYPE.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        workBookPath = reader.GetAttribute("PartName");
                    }
                    else if (OpcPackage.SHARED_STRINGS_CONTENT_TYPE.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        shardingStringsPath = reader.GetAttribute("PartName");
                    }
                    else if (OpcPackage.STYLE_CONTENT_TYPE.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        stylePath = reader.GetAttribute("PartName");
                    }
                    else if (OpcPackage.EXTEND_PROPERTY_CONTENTTYPE.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        appPath = reader.GetAttribute("PartName");
                    }
                }
            }
        }
        void extractStyle(string stylePath)
        {
            using (XMLStreamReader reader = new XMLStreamReader(opcPackage.GetEntryContent(stylePath)))
            {
                AtomicBoolean insideCellXfs = new AtomicBoolean(false);
                while (reader.GoTo(() => reader.IsStartElement("numFmt") || reader.IsStartElement("xf") ||
                    reader.IsStartElement("cellXfs") || reader.IsEndElement("cellXfs")))
                {
                    if (reader.IsStartElement("cellXfs"))
                    {
                        insideCellXfs.Set(true);
                    }
                    else if (reader.IsEndElement("cellXfs"))
                    {
                        insideCellXfs.Set(false);
                    }
                    if ("numFmt".Equals(reader.GetLocalName()))
                    {
                        string formatCode = reader.GetAttribute("formatCode");
                        FormatMap.TryAdd(reader.GetAttribute("numFmtId"), formatCode);
                    }
                    else if (insideCellXfs.Get() && reader.IsStartElement("xf"))
                    {
                        string numFmtId = reader.GetAttribute("numFmtId");
                        formatIdList.Add(numFmtId);
                        string formatstr = null;
                        if (OpcPackage.IMPLICIT_NUM_FMTS.TryGetValue(numFmtId, out formatstr))
                        {
                            FormatMap.TryAdd(numFmtId, formatstr);
                        }
                        else
                        {
                            FormatMap.TryAdd(numFmtId, reader.GetText());
                        }

                    }
                }
            }
        }
        void beginRead()
        {
            using (XMLStreamReader reader = new XMLStreamReader(opcPackage.GetEntryContent(workBookPath)))
            {
                while (reader.GoTo(() => reader.IsStartElement("sheets") || reader.IsStartElement("workbookPr") ||
                    reader.IsStartElement("workbookView") || reader.IsEndElement("workbook")))
                {
                    if ("workbookView".Equals(reader.GetLocalName()))
                    {
                        String activeTab = reader.GetAttribute("activeTab");
                        if (activeTab != null)
                        {
                            this.ActiveTab = Convert.ToInt16(activeTab);
                        }
                    }
                    else if ("sheets".Equals(reader.GetLocalName()))
                    {
                        reader.Foreach("sheet", "sheets", this.parseSheet);
                    }
                    else if ("workbookPr".Equals(reader.GetLocalName()))
                    {
                        String date1904Value = reader.GetAttribute("date1904");
                        Date1904 = Convert.ToBoolean(date1904Value);
                    }
                    else
                    {
                        break;
                    }
                }
                shardingStrings = ShardingStrings.FromStream(opcPackage.GetEntryContent(shardingStringsPath));
            }
        }
        private void beginFlush()
        {

            writeFileContent("[Content_Types].xml", (w) =>
            {
                w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>");

                w.Append("<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
                foreach (WorkSheet ws in sheets)
                {
                    int index = getIndex(ws);
                    w.Append("<Override PartName=\"/xl/worksheets/sheet").Append(index).Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");

                }
                w.Append("<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>");
                w.Append("<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>");

                w.Append("</Types>");
            });
            writeFileContent("_rels/.rels", (w) =>
            {
                w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                w.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                w.Append("<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/>");
                w.Append("<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>");
                w.Append("<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>");

                w.Append("</Relationships>");
            });
            writeFileContent("xl/_rels/workbook.xml.rels", (w) =>
            {
                w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Target=\"sharedStrings.xml\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\"/><Relationship Id=\"rId2\" Target=\"styles.xml\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\"/>");
                foreach (WorkSheet ws in sheets)
                {
                    w.Append("<Relationship Id=\"rId").Append(getIndex(ws) + 2).Append("\" Target=\"worksheets/sheet").Append(getIndex(ws)).Append(".xml\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\"/>");
                }
                w.Append("</Relationships>");
            });
            writeFileContent("xl/sharedStrings.xml", shardingStrings.WriteOut);
            writeProperty();
            writeWorkBookFile();
            writeFileContent("xl/styles.xml", Holder.WriteOut);

        }
        void writeProperty()
        {
            writeFileContent("docProps/app.xml", (w) =>
            {
                w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                w.Append("<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\">");
                w.Append("<Application>").AppendEscaped(ApplicationName).Append("</Application>");

                if (!properties.Manager.IsNullOrEmpty())
                {
                    w.Append("<Manager>");
                    w.AppendEscaped(properties.Manager);
                    w.Append("</Manager>");
                }
                if (!properties.Company.IsNullOrEmpty())
                {
                    w.Append("<Company>");
                    w.AppendEscaped(properties.Company);
                    w.Append("</Company>");
                }

                if (!properties.HyperlinkBase.IsNullOrEmpty())
                {
                    w.Append("<HyperlinkBase>");
                    w.AppendEscaped(properties.HyperlinkBase);
                    w.Append("</HyperlinkBase>");
                }
                if (ApplicationVersion != null)
                {
                    w.Append("<AppVersion>");
                    w.AppendEscaped(ApplicationVersion);
                    w.Append("</AppVersion>");
                }
                w.Append("</Properties>");
            });
            writeFileContent("docProps/core.xml", (w) =>
            {
                w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
                w.Append("<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                String title = properties.Title;
                if (title != null)
                {
                    w.Append("<dc:title>");
                    w.AppendEscaped(title);
                    w.Append("</dc:title>");
                }
                String subject = properties.Subject;
                if (subject != null)
                {
                    w.Append("<dc:subject>");
                    w.AppendEscaped(subject);
                    w.Append("</dc:subject>");
                }
                w.Append("<dc:creator>");
                w.AppendEscaped(ApplicationName);
                w.Append("</dc:creator>");
                String keywords = properties.Keywords;
                if (keywords != null)
                {
                    w.Append("<cp:keywords>");
                    w.AppendEscaped(keywords);
                    w.Append("</cp:keywords>");
                }
                String description = properties.Description;
                if (description != null)
                {
                    w.Append("<dc:description>");
                    w.AppendEscaped(description);
                    w.Append("</dc:description>");
                }
                w.Append("<dcterms:created xsi:type=\"dcterms:W3CDTF\">");
                w.Append(DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.SSSX"));
                w.Append("</dcterms:created>");
                String category = properties.Category;
                if (category != null)
                {
                    w.Append("<cp:category>");
                    w.AppendEscaped(category);
                    w.Append("</cp:category>");
                }
                w.Append("</cp:coreProperties>");
            });
        }
        private void writeWorkbookSheet(XmlBufferWriter w, WorkSheet ws)
        {
            w.Append("<sheet name=\"").AppendEscaped(ws.Name).Append("\" r:id=\"rId").Append(getIndex(ws) + 2)
                    .Append("\" sheetId=\"").Append(getIndex(ws));

            w.Append("\"/>");
        }
        void writeWorkBookFile()
        {
            writeFileContent("xl/workbook.xml", (w) =>
            {
                w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<workbook " +
                        "xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                        "<workbookPr date1904=\"" + Date1904 + "\"/>" +
                        "<bookViews>" +
                        "<workbookView activeTab=\"" + ActiveTab + "\"/>" +
                        "</bookViews>" +
                        "<sheets>");

                foreach (WorkSheet ws in sheets)
                {
                    writeWorkbookSheet(w, ws);
                }
                w.Append("</sheets>");
                w.Append("</workbook>");
            });
        }
        void writeFileContent(String partName, Action<XmlBufferWriter> consumer)
        {
            lock (opcPackage.OutputStream)
            {
                beginPart(partName);
                consumer.Invoke(writer);
                writer.Flush();
                opcPackage.OutputStream.CloseEntry();
            }
        }
        void beginPart(String partName)
        {
            ZipEntry entry = new ZipEntry(partName);
            if (opcPackage.OutputStream != null)
            {
                opcPackage.OutputStream.PutNextEntry(entry);
                opcPackage.OutputStream.Flush();
            }
        }
        int getIndex(WorkSheet ws)
        {
            lock (sheets)
            {
                return sheets.IndexOf(ws) + 1;
            }
        }
        void parseSheet(XMLStreamReader r)
        {
            string name = r.GetAttribute("name");
            string id = r.GetAttribute("http://schemas.openxmlformats.org/officeDocument/2006/relationships", "id");
            String sheetId = r.GetAttribute("sheetId");
            SheetVisibility sheetVisibility;
            if ("veryHidden".Equals(r.GetAttribute("state")))
            {
                sheetVisibility = SheetVisibility.VERY_HIDDEN;
            }
            else if ("hidden".Equals(r.GetAttribute("state")))
            {
                sheetVisibility = SheetVisibility.HIDDEN;
            }
            else
            {
                sheetVisibility = SheetVisibility.VISIBLE;
            }
            int index = sheets.Count;
            WorkSheet sheet = new WorkSheet(this, index, id, sheetId, name, sheetVisibility);
            sheets.Add(sheet);
            sheetMap.TryAdd(name, sheet);
        }
        public void Finish()
        {
            if (Finished)
            {
                return;
            }
            if (sheets.IsNullOrEmpty())
            {
                throw new IllegalArgumentException("A workbook must contain at least one worksheet.");
            }
            if (WriteTag)
            {
                beginFlush();
                foreach (WorkSheet ws in sheets)
                {
                    ws.Finish();
                    ZipOutputStream outputStream = opcPackage.OutputStream;
                    beginPart("xl/worksheets/sheet" + ws.Index + ".xml");
                    using (Stream inputStream = new FileStream(localTmpPath + Path.DirectorySeparatorChar + "sheet" + ws.Index + ".xml", FileMode.Open))
                    {
                        inputStream.CopyTo(outputStream, 8192);
                    }
                    File.Delete(localTmpPath + Path.DirectorySeparatorChar + "sheet" + ws.Index + ".xml");
                    outputStream.CloseEntry();
                }
                Directory.Delete(localTmpPath);
            }

            Finished = true;

        }
        public MapEnumerator GetMapEnumerator(WorkSheet sheet, ExcelSheetProp prop)
        {
            return new MapEnumerator(this, getSheetContent(sheet), prop);
        }
        internal Stream getSheetContent(WorkSheet sheet)
        {
            RelationShip relationShip = null;
            if (opcPackage.RelationShipMap.TryGetValue(sheet.Id, out relationShip))
            {
                return opcPackage.GetEntryContent(relationShip.Target);
            }
            else
            {
                throw new FileNotFoundException("sheet " + sheet.Name + " not found!");
            }

        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposable)
        {
            if (!disposable)
            {
                return;
            }
            Finish();
            if (opcPackage != null)
            {
                opcPackage.Dispose();
            }
        }
        internal ShardingString AddShardingString(String value)
        {
            ShardingString s1 = null;
            if (!shardingStringMap.TryGetValue(value, out s1))
            {
                s1 = new ShardingString(value, shardingStrings.Values.Count);
                shardingStrings.Values.Add(s1);
                shardingStringMap.TryAdd(value, s1);
            }
            return s1;
        }
        void beginSheetWrite(WorkSheet sheet, ExcelSheetProp prop)
        {
            int id = sheet.Index;
            Stream outputStream;

            if (localTmpPath == null)
            {
                localTmpPath = Path.GetTempPath() + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                Directory.CreateDirectory(localTmpPath);
            }
            String localSheetPath = localTmpPath + Path.DirectorySeparatorChar + "sheet" + id + ".xml";
            outputStream = new FileStream(localSheetPath, FileMode.CreateNew);
            sheetWriterMap.TryAdd(id, new XmlBufferWriter(outputStream, SheetProp.WriteBufferSize));
            sheet.WriteHeader(sheetWriterMap[id]);
            if (prop.FillHeader)
            {
                sheet.WriteTitle(sheetWriterMap[id], prop);
            }
            sheet.SetWriter(sheetWriterMap[id]);
        }
        public WorkSheet CreateSheet(String sheetName, ExcelSheetProp prop)
        {
            return CreateSheet(sheetName, prop, null);
        }
        public WorkSheet CreateSheet(String sheetName, ExcelSheetProp prop, Action<WorkSheet> consumer)
        {
            int idx = sheets.Count;
            idx++;
            WorkSheet sheet = new WorkSheet(this, prop, idx, sheetIdPrefix + idx, sheetIdPrefix + idx, sheetName, SheetVisibility.VISIBLE);
            sheet.SetDefaultStyles(consumer);
            sheets.Add(sheet);
            sheetMap.TryAdd(sheetName, sheet);
            beginSheetWrite(sheet, prop);
            return sheet;
        }
        public WorkSheet GetSheet(String sheetName)
        {
            WorkSheet sheet;
            sheetMap.TryGetValue(sheetName, out sheet);

            return sheet;
        }
        public WorkSheet GetSheet(int id)
        {
            return id < 0 || id > sheets.Count ? null : sheets[id];
        }
        public int GetSheetNum()
        {
            return !sheets.IsNullOrEmpty() ? sheets.Count : 0;
        }
        public void WriteRecords(IDataReader reader)
        {
            Trace.Assert(!SheetProp.CellProps.IsNullOrEmpty(), "not init properly!Please call ExcelSheetProp.FromDataReader first");
            if (currentSheet == null)
            {
                throw new ConfigMissingException("Please call BeginWrite first");
            }
            while (reader.Read())
            {
                if (totalRow > 0 && (totalRow % MaxRows == 0 || (maxSheetSize > 0 && GetSheet(currentSheet.Index).GetWriter().ShouldClose(maxSheetSize, thresholdSize))))
                {
                    if (Log.IsEnabled(LogEventLevel.Debug))
                    {
                        Log.Debug(" Flush and Close sheet " + currentSheet.Index);
                    }
                    currentSheet.Finish();
                    BeginWrite();
                }
                Dictionary<string, object> valueMap = new Dictionary<string, object>();
                for (int col = 0; col < reader.FieldCount; col++)
                {
                    valueMap.TryAdd(reader.GetName(col), reader[col]);
                }
                currentSheet.WriteRow(valueMap);
            }
        }

    }
}
