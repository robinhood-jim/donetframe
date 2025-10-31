using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Office.Core;
using Frameset.Office.Excel.Meta;
using Frameset.Office.Meta;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace Frameset.Office.Util
{
    public class OpcPackage : IDisposable
    {
        internal ZipFile InputFile
        {
            get; set;
        }
        public static string WORKBOOK_MAIN_CONTENT_TYPE =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
        public static string WORKBOOK_EXCEL_MACRO_ENABLED_MAIN_CONTENT_TYPE =
                "application/vnd.ms-excel.sheet.macroEnabled.main+xml";
        public static string SHARED_STRINGS_CONTENT_TYPE =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedstrings+xml";
        public static string STYLE_CONTENT_TYPE =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml";
        public static string WORD_DOCUMENT_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
        public static string WORD_NUMBERING_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml";
        public static string WORD_STYLE_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml";
        public static string WORD_WEBSETTING_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.webSettings+xml";
        public static string WORD_FOOTERNOTE_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.footnotes+xml";
        public static string WORD_ENDNOTE_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.endnotes+xml";
        public static string WORD_HEADER_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml";
        public static string WORD_FOOTER_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml";
        public static string WORD_FONTTABLE_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml";
        public static string WORD_THEME_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.theme+xml";


        public static string CORE_PROPERTIY_CONTENTTYPE = "application/vnd.openxmlformats-package.core-properties+xml";
        public static string EXTEND_PROPERTY_CONTENTTYPE = "application/vnd.openxmlformats-officedocument.extended-properties+xml";
        public static string PATTERN = "^(.*/)([^/]+)$";
        public ZipOutputStream OutputStream
        {
            get; internal set;
        }
        public static Dictionary<string, string> IMPLICIT_NUM_FMTS = new Dictionary<string, string>() { { "1", "0" },
            {"2", "0.00" },
            {"3", "#,##0"},
            {"4", "#,##0.00"},
            {"9", "0%" },
            {"10", "0.00%" },
            {"11", "0.00E+00"},
            {"12", "# ?/?"},
            {"13", "# ??/??" },
            {"14", "mm-dd-yy"},
            { "15", "d-mmm-yy"},
            { "16", "d-mmm"},
            { "17", "mmm-yy"},
            { "18", "h:mm AM/PM"},
            { "19", "h:mm:ss AM/PM"},
            { "20", "h:mm"},
            { "21", "h:mm:ss"},
            { "22", "m/d/yy h:mm"},
            { "37", "#,##0 ;(#,##0)"},
            { "38", "#,##0 ;[Red](#,##0)"},
            { "39", "#,##0.00;(#,##0.00)"},
            { "40", "#,##0.00;[Red](#,##0.00)"},
            { "45", "mm:ss"},
            { "46", "[h]:mm:ss"},
            {"47", "mmss.0" },
            { "48", "##0.0E+0"},
            { "49", "@"}
        };
        public Dictionary<string, RelationShip> RelationShipMap
        {
            get; internal set;
        } = new Dictionary<string, RelationShip>();
        public bool WriteMode
        {
            get; internal set;
        } = false;

        public OpcPackage(string zipFile)
        {
            InputFile = new ZipFile(System.IO.File.OpenRead(zipFile));

        }
        public static OpcPackage Create(Stream stream)
        {
            return new OpcPackage(stream, true);
        }
        public static OpcPackage Read(Stream stream)
        {
            OpcPackage opc = new OpcPackage(stream);

            opc.WriteMode = false;
            return opc;

        }
        internal OpcPackage(Stream stream)
        {
            InputFile = new ZipFile(stream);
        }
        internal OpcPackage(Stream stream, bool writeTag)
        {
            OutputStream = new ZipOutputStream(stream);
            WriteMode = true;

        }
        public void DoReadInit(Action<ZipFile> action)
        {
            action.Invoke(InputFile);
        }
        internal Stream GetEntryContent(string name)
        {
            string partName = name;
            if (partName.StartsWith("/"))
            {
                partName = partName.Substring(1, partName.Length - 1);
            }
            if (InputFile != null)
            {
                ZipEntry entry = InputFile.GetEntry(partName);
                if (entry == null)
                {
                    var iter = InputFile.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        ZipEntry e1 = iter.Current as ZipEntry;
                        if (string.Equals(name, e1.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            return InputFile.GetInputStream(e1);
                        }
                    }
                    return null;
                }
                else
                {
                    return InputFile.GetInputStream(entry);
                }
            }
            else
            {
                throw new ConfigMissingException("");
            }
        }
        public void ExtractRelationShip(string relationPath, string startPath)
        {
            string xlFolder = relationPath.Substring(1, relationPath.IndexOf(startPath) - 1);

            using (XMLStreamReader reader = new XMLStreamReader(GetEntryContent(relationPath)))
            {
                while (reader.GotoElement("Relationship"))
                {
                    string id = reader.GetAttribute("Id");
                    string target = reader.GetAttribute("Target");
                    string type = reader.GetAttribute("Type");
                    // if name does not start with /, it is a relative path
                    if (!target.StartsWith("/"))
                    {
                        target = xlFolder + target;
                    } // else it is an absolute path
                    RelationShipMap.TryAdd(id, new RelationShip(id, target, type));
                }
            }
        }

        public void Dispose()
        {
            if (InputFile != null)
            {
                InputFile.Close();
            }
            if (OutputStream != null)
            {
                OutputStream.Close();
            }


        }
        public static String RelsNameFor(String entryName)
        {
            return Regex.Replace(entryName, PATTERN, "$1_rels/$2.rels");
        }
        public static CellType ParseCellType(ExcelCellProp prop)
        {
            CellType type = CellType.STRING;
            switch (prop.ColumnType)
            {
                case Constants.MetaType.BIGINT:
                case Constants.MetaType.FLOAT:
                case Constants.MetaType.DOUBLE:
                case Constants.MetaType.INTEGER:
                case Constants.MetaType.DATE:
                case Constants.MetaType.TIMESTAMP:
                    type = CellType.NUMBER;
                    break;
                case Constants.MetaType.BOOLEAN:
                    type = CellType.BOOLEAN;
                    break;
                case Constants.MetaType.FORMULA:
                    type = CellType.FORMULA;
                    break;
            }
            return type;
        }
    }
}
