using Frameset.Core.Exceptions;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;

namespace Frameset.Office.Util
{
    public class OpcPackage
    {
        private static ZipFile file;
        public static string WORKBOOK_MAIN_CONTENT_TYPE =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
        public static string WORKBOOK_EXCEL_MACRO_ENABLED_MAIN_CONTENT_TYPE =
                "application/vnd.ms-excel.sheet.macroEnabled.main+xml";
        public static string SHARED_stringS_CONTENT_TYPE =
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

        public OpcPackage(string zipFile)
        {
            file = new ZipFile(File.OpenRead(zipFile));

        }
        public OpcPackage(Stream stream)
        {
            file = new ZipFile(stream);
        }
        public void doReadInit(Action<ZipFile> action)
        {
            action.Invoke(file);
        }
        internal Stream GetEntryContent(string name)
        {
            if (file != null)
            {
                ZipEntry entry = file.GetEntry(name);
                if (entry == null)
                {
                    var iter = file.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        ZipEntry e1 = iter.Current as ZipEntry;
                        if (string.Equals(name, e1.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            return file.GetInputStream(e1);
                        }
                    }
                    return null;
                }
                else
                {
                    return file.GetInputStream(entry);
                }
            }
            else
            {
                throw new ConfigMissingException("");
            }
        }
    }
}
