using Frameset.Office.Core;
using Frameset.Office.Util;
using Frameset.Office.Word.Element;
using Frameset.Office.Word.Util;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;

namespace Frameset.Office.Word
{
    public sealed class Document : IDisposable
    {
        private readonly OpcPackage opcPackage;
        private string documentPath;
        private string fontTablePath;
        private string stylePath;
        private readonly List<string> headerPaths = [];
        private readonly List<string> footerPaths = [];
        private readonly List<string> themePaths = [];
        private readonly PrDefaultRpr defaultRpr = new PrDefaultRpr();
        private readonly List<LsdException> lsdExceptions = [];
        public Document(Stream inputStream)
        {
            opcPackage = OpcPackage.Read(inputStream);
            opcPackage.DoReadInit(f =>
            {
                ExtractParts();
                ExtractStyle();
                opcPackage.ExtractRelationShip(OpcPackage.RelsNameFor(documentPath), "_rel");
            });

        }
        private void ExtractParts()
        {
            string contentTypePath = "[Content_Types].xml";
            using (XMLStreamReader reader = new XMLStreamReader(opcPackage.GetEntryContent(contentTypePath)))
            {
                while (reader.GoTo(() => reader.IsStartElement("Override")))
                {
                    string contentType = reader.GetAttribute("ContentType");
                    if (string.Equals(OpcPackage.WORD_DOCUMENT_CONTENT_TYPE, contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        documentPath = reader.GetAttribute("PartName");
                    }
                    else if (string.Equals(OpcPackage.WORD_FONTTABLE_CONTENT_TYPE, contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        fontTablePath = reader.GetAttribute("PartName");
                    }
                    else if (string.Equals(OpcPackage.WORD_STYLE_CONTENT_TYPE, contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        stylePath = reader.GetAttribute("PartName");
                    }
                    else if (string.Equals(OpcPackage.WORD_HEADER_CONTENT_TYPE, contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        headerPaths.Add(reader.GetAttribute("PartName"));
                    }
                    else if (string.Equals(OpcPackage.WORD_FOOTER_CONTENT_TYPE, contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        footerPaths.Add(reader.GetAttribute("PartName"));
                    }
                    else if (string.Equals(OpcPackage.WORD_THEME_CONTENT_TYPE, contentType, StringComparison.OrdinalIgnoreCase))
                    {
                        themePaths.Add(reader.GetAttribute("PartName"));
                    }
                }
                if (documentPath.IsNullOrEmpty())
                {
                    documentPath = "word/document.xml";
                }
            }
        }
        private void ExtractStyle()
        {
            using (XMLStreamReader reader = new XMLStreamReader(opcPackage.GetEntryContent(stylePath)))
            {
                reader.GotoElement("docDefaults");
                while (reader.GotoElement("rPr"))
                {
                    while (reader.GotoElement("rFonts"))
                    {
                        reader.DoInAttributes(r =>
                        {
                            for (int i = 0; i < r.AttributeCount; i++)
                            {
                                string value = r.GetAttribute(i);
                                string name = r.LocalName;
                                defaultRpr.Fonts.TryAdd(name, value);
                            }
                        });
                    }
                    while (reader.GotoElement("lang"))
                    {
                        reader.DoInAttributes(r =>
                        {
                            for (int i = 0; i < r.AttributeCount; i++)
                            {
                                string value = r.GetAttribute(i);
                                string name = r.LocalName;
                                defaultRpr.Langs.TryAdd(name, value);
                            }
                        });
                    }
                }
                reader.GotoElement("latentStyles");
                while (reader.GotoElement("lsdException"))
                {
                    reader.DoInAttributes(r =>
                    {
                        LsdException.Builder builder = LsdException.Builder.newBuilder();
                        for (int i = 0; i < r.AttributeCount; i++)
                        {
                            string value = r.GetAttribute(i);
                            string name = r.LocalName;

                            switch (name)
                            {
                                case "name":
                                    builder.Name(value);
                                    break;
                                case "qFormat":
                                    builder.QFormat(value);
                                    break;
                                case "semiHidden":
                                    builder.SemiHidden(value);
                                    break;
                                case "unhideWhenUsed":
                                    builder.UnhideWhenUsed(value);
                                    break;
                                case "uiPriority":
                                    builder.UiPriority(value);
                                    break;
                            }
                            lsdExceptions.Add(builder.Build());
                        }
                    });
                }
                while (reader.GotoElement("style"))
                {
                    Style style = new Style();
                    reader.DoInAttributes(r =>
                    {
                        for (int i = 0; i < r.AttributeCount; i++)
                        {
                            string value = r.GetAttribute(i);
                            string name = r.LocalName;
                            switch (name)
                            {
                                case "type":
                                    style.TypeDef = value;
                                    break;
                                case "default":
                                    style.DefaultVal = value;
                                    break;
                                case "styleId":
                                    style.StyleId = value;
                                    break;
                            }
                        }
                        reader.DoUntilEnd("style", r1 =>
                        {
                            String name = r.LocalName;
                            switch (name)
                            {
                                case "name":
                                    style.Name = r.GetAttribute(0);
                                    break;
                                case "baseOn":
                                    style.BaseOn = r.GetAttribute(0);
                                    break;
                                case "qFormat":
                                    if (r.AttributeCount > 0)
                                    {
                                        style.QFormat = r.GetAttribute(0);
                                    }
                                    break;
                                case "uiPriority":
                                    if (r.AttributeCount > 0)
                                    {
                                        style.UiPriority = r.GetAttribute(0);
                                    }
                                    break;
                                case "pPr":
                                    reader.DoUntilEnd("pPr", r2 =>
                                    {
                                        String name2 = r2.LocalName;
                                        string value = r.GetAttribute(0);
                                        string name = r.LocalName;
                                        switch (name2)
                                        {
                                            case "rFonts":
                                                style.Rpr.Fonts.TryAdd(name, value);
                                                break;
                                            case "lang":
                                                style.Rpr.Langs.TryAdd(name, value);
                                                break;
                                            case "sz":
                                                style.Rpr.Sz = value;
                                                break;
                                            case "szCs":
                                                style.Rpr.SzCs = value;
                                                break;
                                        }
                                    });
                                    break;
                            }
                        });

                    });
                }
            }
        }

        public void Dispose()
        {

            GC.SuppressFinalize(this);
        }
        public OpcPackage GetOpcPackage()
        {
            return opcPackage;
        }
        public BodyElementEnumerator GetEnumerator()
        {
            Stream inStream = opcPackage.GetEntryContent(documentPath);
            return new BodyElementEnumerator(this, inStream);
        }
    }
}
