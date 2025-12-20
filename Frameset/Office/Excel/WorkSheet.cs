using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Office.Core;
using Frameset.Office.Element;
using Frameset.Office.Excel.Element;
using Frameset.Office.Excel.Meta;
using Frameset.Office.Excel.Util;
using Frameset.Office.Meta;
using Frameset.Office.Util;
using Microsoft.IdentityModel.Tokens;
using Spring.Collections;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Frameset.Office.Excel
{
    public class WorkSheet
    {
        public static readonly int MAX_ROWS = 1_048_576;
        public string Id
        {
            get; internal set;
        }
        public int Index
        {
            get; internal set;
        }

        public string Name
        {
            get; internal set;
        }
        public string SheetId
        {
            get; internal set;
        }
        public SheetVisibility Visibility
        {
            get; internal set;
        }
        public WorkBook WorkBookObj
        {
            get; internal set;
        }
        public bool Finished
        {
            get; set;
        } = false;
        public Set HiddenRows
        {
            get; internal set;
        } = new HashedSet();

        public Set HiddenColumns
        {
            get; internal set;
        } = new HashedSet();
        public IList<DataValidation> DataValidations
        {
            get; internal set;
        } = new List<DataValidation>();
        public Dictionary<int, double> ColWidths
        {
            get; internal set;
        } = new Dictionary<int, double>();

        public bool FitToPage
        {
            get; internal set;
        } = false;
        public bool AutoPageBreaks
        {
            get; internal set;
        } = false;
        public Cell[] CurrentCells
        {
            get; internal set;
        }
        public int CurrentRowNum
        {
            get; internal set;
        } = 1;
        public List<int> Styles
        {
            get; internal set;
        } = new List<int>();
        public ExcelSheetProp Prop
        {
            get; internal set;
        }
        public bool IfHidden
        {
            get; internal set;
        } = false;
        Font defaultFont;
        Fill defaultFill;
        Border defaultBorder;
        Alignment defaultAlignment;
        internal XmlBufferWriter w;
        internal WorkSheet(WorkBook workBook, ExcelSheetProp prop, int index, string id, string sheetId, string name, SheetVisibility visibility) : this(workBook, index, id, sheetId, name, visibility)
        {
            this.Prop = prop;
        }
        internal WorkSheet(WorkBook workBook, int index, string id, string sheetId, string name, SheetVisibility visibility)
        {
            this.Index = index;
            this.Id = id;
            this.Name = name;
            this.Visibility = visibility;
            this.SheetId = sheetId;
            this.WorkBookObj = workBook;
        }
        public WorkSheet(WorkBook workBook, string name)
        {
            this.WorkBookObj = workBook;
            this.Name = name;
        }
        internal void SetWriter(XmlBufferWriter writer)
        {
            this.w = writer;
        }
        public XmlBufferWriter GetWriter()
        {
            return w;
        }
        internal void WriteHeader(XmlBufferWriter w)
        {

            w.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            w.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
            w.Append("<sheetPr filterMode=\"" + "false" + "\">");

            w.Append("<pageSetUpPr fitToPage=\"" + FitToPage + "\" " + "autoPageBreaks=\"" + AutoPageBreaks + "\"/></sheetPr>");
            w.Append("<dimension ref=\"A1\"/>");
            w.Append("<sheetViews><sheetView workbookViewId=\"0\">");
            w.Append("</sheetView>");
            w.Append("</sheetViews><sheetFormatPr defaultRowHeight=\"15.0\"/>");
            w.Append(GetColumnDefine());
            w.Append("<sheetData>");
        }
        public Cell GetCell(int col, ExcelSheetProp prop)
        {
            if (CurrentCells == null)
            {
                CurrentCells = new Cell[prop.CellProps.Count];
                for (int i = 0; i < prop.CellProps.Count; i++)
                {
                    Cell cell = new Cell(WorkBookObj, OpcPackage.ParseCellType(prop.CellProps[i]), new CellAddress(CurrentRowNum, i + 1));
                    cell.SetStyle(Styles[i]);
                    if (prop.CellProps[i].ColumnType.Equals(Constants.MetaType.FORMULA))
                    {
                        cell.SetValue(new Formula(prop.CellProps[i].Formula));
                    }
                    CurrentCells[i] = cell;
                }
            }
            if (col <= prop.CellProps.Count)
            {
                return CurrentCells[col];
            }
            else
            {
                throw new ExcelException("row over excel define columns");
            }
        }
        internal void WriteTitle(XmlBufferWriter w, ExcelSheetProp prop)
        {
            w.Append("<row r=\"").Append(CurrentRowNum).Append("\" s=\"1\" >");
            for (int i = 0; i < prop.CellProps.Count; i++)
            {
                ShardingString s1 = WorkBookObj.AddShardingString(prop.CellProps[i].ColumnName);
                w.Append("<c r=\"").Append(CellUtils.ColToString(i)).Append(CurrentRowNum).Append("\" t=\"s\" s=\"" + Styles[i] + "\">")
                        .Append("<v>").Append(s1.Index).Append("</v></c>");
            }
            w.Append("</row>");
            CurrentRowNum++;
        }
        public void WriteRow(Dictionary<string, object> valueMap)
        {

            if (CurrentRowNum == MAX_ROWS)
            {
                throw new ExcelException("sheet over row limit!");
            }
            if (w != null)
            {
                for (int i = 0; i < Prop.CellProps.Count; i++)
                {
                    ExcelCellProp columnProp = Prop.CellProps[i];
                    object rValue = null;
                    if (valueMap.TryGetValue(columnProp.ColumnCode, out rValue))
                    {
                        Cell cell = GetCell(i, Prop);
                        SetValue(cell, columnProp, rValue);
                    }
                    else
                    {
                        if (GetCell(i, Prop).CellType != CellType.FORMULA)
                        {
                            GetCell(i, Prop).SetValue(null);
                        }
                        else
                        {
                            Formula formula = (Formula)GetCell(i, Prop).GetValue();
                            formula.setExpression(CellUtils.ReturnFormulaWithPos(columnProp.Formula, CurrentRowNum));

                        }
                    }
                }
                WriteRow(IfHidden, (byte)0, 0.0);
                CurrentRowNum++;
            }
        }

        internal void WriteRow(bool isHidden, byte groupLevel, double rowHeight)
        {

            w.Append("<row r=\"").Append(CurrentRowNum).Append("\"");
            if (isHidden)
            {
                w.Append(" hidden=\"true\"");
            }
            if (rowHeight != 0.0)
            {
                w.Append(" ht=\"")
                        .Append(rowHeight)
                        .Append("\"")
                        .Append(" customHeight=\"1\"");
            }
            if (groupLevel != 0)
            {
                w.Append(" outlineLevel=\"")
                        .Append(groupLevel)
                        .Append("\"");
            }
            w.Append(">");
            for (int c = 0; c < CurrentCells.Length; ++c)
            {
                if (CurrentCells[c] != null)
                {
                    CurrentCells[c].Write(w, CurrentRowNum, c);
                }
            }
            w.Append("</row>");

        }
        public void WriteEntity(object obj)
        {
            Trace.Assert(obj.GetType().Equals(Prop.EntityType));
            Dictionary<string, FieldContent> fieldMap = Prop.GetFieldMap();
            Trace.Assert(!fieldMap.IsNullOrEmpty(), "no property or field found");
            if (CurrentRowNum == MAX_ROWS)
            {
                throw new ExcelException("sheet over row limit!");
            }
            if (w != null)
            {
                for (int i = 0; i < Prop.CellProps.Count; i++)
                {
                    ExcelCellProp columnProp = Prop.CellProps[i];
                    object rValue = null;
                    FieldContent content = null;
                    if (fieldMap.TryGetValue(columnProp.ColumnCode, out content))
                    {
                        rValue = content.GetMethod.Invoke(obj, null);

                        Cell cell = GetCell(i, Prop);
                        SetValue(cell, columnProp, rValue);
                    }
                }
                WriteRow(IfHidden, (byte)0, 0.0);
                CurrentRowNum++;
            }

        }

        internal void SetValue(Cell cell, ExcelCellProp prop, object value)
        {
            switch (prop.ColumnType)
            {
                case Constants.MetaType.DOUBLE:
                case Constants.MetaType.FLOAT:
                    double dVal = 0;
                    if (double.TryParse(value.ToString(), out dVal))
                    {
                        cell.SetValue(dVal);
                    }
                    break;
                case Constants.MetaType.BIGINT:
                    long lVal = 0;
                    if (long.TryParse(value.ToString(), out lVal))
                    {
                        cell.SetValue(lVal);
                    }
                    break;
                case Constants.MetaType.INTEGER:
                    int iVal = 0;
                    if (int.TryParse(value.ToString(), out iVal))
                    {
                        cell.SetValue(iVal);
                    }
                    break;
                case Constants.MetaType.DATE:
                case Constants.MetaType.TIMESTAMP:
                    if (value.GetType().Equals(typeof(DateTime)))
                    {
                        cell.SetValue(DateUtils.ConvertDateTime((DateTime)value));
                    }
                    else if (value.GetType().Equals(typeof(long)))
                    {
                        cell.SetValue(DateUtils.ConvertDateTime(DateTimeOffset.FromUnixTimeMilliseconds((long)value)));
                    }
                    else
                    {
                        long timeTs = 0;
                        if (long.TryParse(value.ToString(), out timeTs))
                        {
                            cell.SetValue(DateUtils.ConvertDateTime(DateTimeOffset.FromUnixTimeMilliseconds(timeTs).LocalDateTime));
                        }
                    }
                    break;
                case Constants.MetaType.FORMULA:
                    Formula formula = (Formula)cell.GetValue();
                    formula.setExpression(CellUtils.ReturnFormulaWithPos(prop.Formula, CurrentRowNum));
                    break;
                case Constants.MetaType.STRING:
                    cell.SetValue(value);
                    break;
            }
        }
        internal static string GetColumnDefine()
        {
            return "<cols><col min=\"1\" max=\"1\" customWidth=\"true\"></col></cols>";
        }
        public void Finish()
        {
            if (Finished)
            {
                return;
            }


            w.Append("</sheetData>");
            //写入DataValidation
            if (!CollectionUtils.IsEmpty(DataValidations))
            {
                foreach (DataValidation validation in DataValidations)
                {
                    validation.WriteOut(w);
                }
            }
            w.Append("</worksheet>");

            w.Dispose();
            Finished = true;
        }
        internal void SetDefaultStyles(Action<WorkSheet> consumer)
        {
            AssertUtils.ArgumentNotNull(Prop, "");
            Styles = new List<int>(Prop.CellProps.Count);
            if (consumer != null)
            {
                consumer.Invoke(this);
            }
            else
            {
                defaultFont = GetDefaultFont();
                defaultFill = GetDefaultFill();
                defaultBorder = GetDefaultBorder();
                defaultAlignment = GetDefaultAlignment();
                foreach (ExcelCellProp cellProp in Prop.CellProps)
                {
                    Styles.Add(StyleHolder.MergeCellStyle(0, GetFormatWithType(cellProp), defaultFont, defaultFill, defaultBorder, defaultAlignment));
                }
            }
        }
        public Font GetDefaultFont()
        {
            Font font = new Font(false, false, false, CellUtils.GetDefaultFontName(), 12, null, false);
            return font;
        }

        internal void SetDefaultFont(Font defaultFont)
        {
            this.defaultFont = defaultFont;
        }
        public Fill GetDefaultFill()
        {
            return Fill.BLACK;
        }

        internal void SetDefaultFill(Fill defaultFill)
        {
            this.defaultFill = defaultFill;
        }

        public Border GetDefaultBorder()
        {
            return Border.BLACK;
        }

        internal void SetDefaultBorder(Border defaultBorder)
        {
            this.defaultBorder = defaultBorder;
        }

        public Alignment GetDefaultAlignment()
        {
            return new Alignment("center", "center", false, 0, 0);
        }

        internal void SetDefaultAlignment(Alignment defaultAlignment)
        {
            this.defaultAlignment = defaultAlignment;
        }
        public static string GetFormatWithType(ExcelCellProp columnProp)
        {
            AssertUtils.ArgumentNotNull(columnProp, "");
            string numFmtStr = null;
            switch (columnProp.ColumnType)
            {
                case Constants.MetaType.BIGINT:
                case Constants.MetaType.INTEGER:
                case Constants.MetaType.BOOLEAN:
                    numFmtStr = columnProp.Format.IsNullOrEmpty() ? OpcPackage.IMPLICIT_NUM_FMTS["1"] : columnProp.Format;
                    break;
                case Constants.MetaType.DOUBLE:
                case Constants.MetaType.FLOAT:
                    numFmtStr = columnProp.Format.IsNullOrEmpty() ? OpcPackage.IMPLICIT_NUM_FMTS["2"] : columnProp.Format;
                    break;
                case Constants.MetaType.DATE:
                    numFmtStr = columnProp.Format.IsNullOrEmpty() ? "yyyy-MM-dd" : columnProp.Format;
                    break;
                case Constants.MetaType.TIMESTAMP:
                    numFmtStr = columnProp.Format.IsNullOrEmpty() ? "yyyy-MM-dd hh:mm:ss" : columnProp.Format;
                    break;
                case Constants.MetaType.FORMULA:
                    numFmtStr = OpcPackage.IMPLICIT_NUM_FMTS["2"];
                    break;
                default:
                    numFmtStr = OpcPackage.IMPLICIT_NUM_FMTS["1"];
                    break;

            }
            return numFmtStr;
        }

    }
}
