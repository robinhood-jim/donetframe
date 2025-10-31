using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Office.Core;
using Frameset.Office.Element;
using Frameset.Office.Excel.Element;
using Frameset.Office.Excel.Meta;
using Frameset.Office.Meta;
using Microsoft.IdentityModel.Tokens;
using Spring.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Frameset.Office.Excel.Util
{
    public abstract class AbstractCellEnumerator<T> : IEnumerator<T>
    {
        public T Current => current;

        object IEnumerator.Current => Current;

        internal WorkBook workBook;
        internal int trackedRowIndex = 0;
        internal bool containHeaders = false;
        public static ExcelSheetProp prop
        {
            get; internal set;
        }
        internal bool multipleType = false;
        internal bool needIdentifyColumn = false;
        internal IList<Cell> cells = new List<Cell>();
        internal bool finishReadHeader = false;
        internal Dictionary<int, CellAddress> addressMap = new Dictionary<int, CellAddress>();
        internal CellProcessor processor;
        internal static bool isDate1904;
        internal XMLStreamReader r;
        internal bool reuseCurrent;
        internal T current;

        public AbstractCellEnumerator(WorkBook workBook, Stream stream, ExcelSheetProp sheetProp, bool reuseCurrent)
        {
            this.workBook = workBook;
            this.reuseCurrent = reuseCurrent;
            isDate1904 = workBook.Date1904;
            r = new XMLStreamReader(stream);

            processor = new CellProcessor();
            prop = sheetProp;
            if (prop != null)
            {
                containHeaders = prop.FillHeader;
                if (prop.CellProps.IsNullOrEmpty())
                {
                    needIdentifyColumn = true;
                }
            }
            else
            {
                containHeaders = true;
                needIdentifyColumn = true;
                prop = new ExcelSheetProp();
            }
            if (reuseCurrent)
            {
                current = System.Activator.CreateInstance<T>();
            }
            r.GotoElement("sheetData");
        }
        internal void readHeader()
        {
            int trackedColIndex = 0;
            while (r.GoTo(() => r.IsStartElement("c") || r.IsEndElement("row")))
            {
                if ("row".Equals(r.GetLocalName()))
                {
                    break;
                }
                Cell cell = parseCell(trackedColIndex++, false);
                if (needIdentifyColumn && cell.GetValue() != null)
                {
                    prop.AddCellProp(new ExcelCellProp(cell.GetValue().ToString(), cell.GetValue().ToString(), Constants.MetaType.STRING));
                }
            }
            InitCells();
        }
        internal Cell parseCell(int trackedColIndex, bool isMultiplex)
        {
            CellAddress addr = getCellAddressWithFallback(trackedColIndex);

            processor.SetValue(r, workBook, addr);

            if ("inlineStr".Equals(processor.GetCellTypeStr()))
            {
                return parseInlineStr(addr, processor, isMultiplex);
            }
            else if ("s".Equals(processor.GetCellTypeStr()))
            {
                return parseString(addr, processor, isMultiplex);
            }
            else
            {
                return parseOther(addr, processor, isMultiplex);
            }
        }
        CellAddress getCellAddressWithFallback(int trackedColIndex)
        {
            string cellRefOrNull = r.GetAttribute("r");
            CellAddress address = null;
            if (!addressMap.TryGetValue(trackedColIndex, out address))
            {
                address = cellRefOrNull != null ? new CellAddress(cellRefOrNull) : new CellAddress(trackedRowIndex, trackedColIndex);
                addressMap.TryAdd(trackedColIndex, address);
            }
            else
            {
                if (!cellRefOrNull.IsNullOrEmpty())
                {
                    address.SetAddress(cellRefOrNull);
                }
                else
                {
                    address.SetPos(trackedRowIndex, trackedColIndex);
                }
            }
            return address;
        }
        Cell parseInlineStr(CellAddress addr, CellProcessor processor, bool isMultiplex)
        {
            while (r.GoTo(() => r.IsStartElement("is") || r.IsEndElement("c") || r.IsStartElement("f")))
            {
                if ("is".Equals(r.GetLocalName()))
                {
                    processor.RawValue = r.GetValueUntilEndElement("is");
                    processor.Value = processor.RawValue;
                }
                else if ("f".Equals(r.GetLocalName()))
                {
                    processor.Formula = r.GetValueUntilEndElement("f");
                }
                else
                {
                    break;
                }
            }
            processor.CellType = processor.Formula == null ? CellType.STRING : CellType.FORMULA;
            return returnCell(isMultiplex, workBook, processor, addr);
        }
        Cell empty(CellAddress addr, CellType type)
        {
            return new Cell(workBook, type, "", addr, null, "");
        }
        Cell parseString(CellAddress addr, CellProcessor processor, bool isMultiplex)
        {
            r.GoTo(() => r.IsStartElement("v") || r.IsEndElement("c"));
            if (r.IsEndElement("c"))
            {
                return empty(addr, CellType.STRING);
            }
            string v = r.GetValueUntilEndElement("v");
            if (v.IsNullOrEmpty())
            {
                return empty(addr, CellType.STRING);
            }
            int index = Convert.ToInt32(v);
            string sharedStringValue = workBook.shardingStrings.GetValues()[index].Value;
            processor.Value = sharedStringValue;
            processor.Formula = null;
            processor.RawValue = sharedStringValue;
            processor.CellType = CellType.STRING;
            return returnCell(isMultiplex, workBook, processor, addr);
        }
        internal Cell parseOther(CellAddress addr, CellProcessor processor, bool isMultiplex)
        {
            CellType definedType = ParseType(processor.GetCellTypeStr());
            Func<string, CellAddress, object> parser = GetParserForType(definedType);


            while (r.GoTo(() => r.IsStartElement("v") || r.IsEndElement("c") || r.IsStartElement("f")))
            {
                if ("v".Equals(r.GetLocalName()))
                {
                    processor.RawValue = r.GetValueUntilEndElement("v");
                    try
                    {
                        processor.Value = "".Equals(processor.RawValue) ? null : parser.Invoke(processor.RawValue, addr);
                    }
                    catch (ExcelException e)
                    {
                        definedType = CellType.ERROR;
                    }
                }
                else if ("f".Equals(r.GetLocalName()))
                {
                    processor.Formula = r.GetValueUntilEndElement("f");
                }
                else
                {
                    break;
                }
            }
            if (processor.Formula == null && processor.Value == null && definedType == CellType.NUMBER)
            {
                processor.CellType = CellType.EMPTY;
                processor.Value = null;
                processor.Formula = null;
                return returnCell(isMultiplex, workBook, processor, addr);
            }
            else
            {
                processor.CellType = processor.Formula.IsNullOrEmpty() ? CellType.FORMULA : definedType;
                return returnTypeCell(isMultiplex, workBook, processor, addr);
            }
        }

        internal Cell returnCell(bool isMultiplex, WorkBook workBook, CellProcessor processor, CellAddress addr)
        {
            if (!isMultiplex || valueAllEmpty() || cells[addr.GetColumn()] == null)
            {
                return new Cell(workBook, processor, addr);
            }
            else
            {
                Cell cell = cells[addr.GetColumn()];
                if (processor.Value != null)
                {
                    cell.SetValue(processor.Value);
                }
                return cell;
            }
        }

        Cell returnTypeCell(bool isMultiplex, WorkBook workBook, CellProcessor processor, CellAddress addr)
        {
            if (!isMultiplex || valueAllEmpty() || cells[addr.GetColumn()] == null)
            {
                return new Cell(workBook, processor.CellType, processor.Value, addr, processor.Formula, processor.RawValue, processor.DataFormatId, processor.DataFormatString);
            }
            else
            {
                Cell cell = cells[addr.GetColumn()];
                if (processor.Value != null)
                {
                    cell.SetValue(processor.Value);
                }
                return cell;
            }
        }
        bool valueAllEmpty()
        {
            if (!CollectionUtils.IsEmpty(cells))
            {
                bool emptyTag = true;
                foreach (Cell cell in cells)
                {
                    if (cell != null)
                    {
                        emptyTag = false;
                        break;
                    }
                }
                return emptyTag;
            }
            return true;
        }
        CellType ParseType(string type)
        {
            switch (type)
            {
                case "b":
                    return CellType.BOOLEAN;
                case "e":
                    return CellType.ERROR;
                case "n":
                    return CellType.NUMBER;
                case "str":
                    return CellType.FORMULA;
                case "s":
                case "inlineStr":
                    return CellType.STRING;
                default:
                    return CellType.ERROR;
            }

        }
        Func<string, CellAddress, object> GetParserForType(CellType type)
        {
            switch (type)
            {
                case CellType.BOOLEAN:
                    return parseBoolean;
                case CellType.NUMBER:
                    return parseNumber;
                case CellType.FORMULA:
                case CellType.ERROR:
                    return defaultValue;
                default:
                    return null;
            }
        }
        internal static object parseNumber(string s, CellAddress address)
        {

            if (s.IsNullOrEmpty())
            {
                return null;
            }
            int column = address.GetColumn();
            string tmpVal = s;
            object retObj = null;

            if (!CollectionUtils.IsEmpty(prop.CellProps) && prop.CellProps[column] != null)
            {
                ExcelCellProp columnProp = prop.CellProps[column];
                switch (columnProp.ColumnType)
                {
                    case Constants.MetaType.INTEGER:
                        if (tmpVal.Contains("."))
                        {
                            int pos = tmpVal.IndexOf(".");
                            tmpVal = tmpVal.Substring(0, pos);
                        }
                        retObj = Convert.ToInt32(tmpVal);
                        break;
                    case Constants.MetaType.BIGINT:
                        if (tmpVal.Contains("."))
                        {
                            int pos = tmpVal.IndexOf(".");
                            tmpVal = tmpVal.Substring(0, pos);
                        }
                        retObj = Convert.ToInt64(tmpVal);
                        break;
                    case Constants.MetaType.FLOAT:
                        retObj = Convert.ToDecimal(tmpVal);
                        break;
                    case Constants.MetaType.DOUBLE:
                        retObj = Convert.ToDouble(tmpVal);
                        break;
                    case Constants.MetaType.DATE:
                    case Constants.MetaType.TIMESTAMP:
                        retObj = DateUtils.GetDateTime(Convert.ToDouble(s), isDate1904, false);
                        break;
                    default:
                        retObj = Convert.ToDouble(s);
                        break;

                }
            }
            else
            {
                retObj = Convert.ToDouble(s);
            }
            return retObj;
        }
        internal static string defaultValue(string s, CellAddress address)
        {
            return s;
        }

        internal static object parseBoolean(string s, CellAddress address)
        {
            if ("0".Equals(s))
            {
                return false;
            }
            else if ("1".Equals(s))
            {
                return true;
            }
            else
            {
                throw new ExcelException("Invalid boolean cell value: '" + s + "'. Expecting '0' or '1'.");
            }
        }

        public abstract void ConstructReturn();
        public abstract void ProcessCell(int trackedColIndex);
        public void Dispose()
        {
            if (r != null)
            {
                r.Dispose();
            }
        }

        public virtual bool MoveNext()
        {
            if (reuseCurrent)
            {
                current = System.Activator.CreateInstance<T>();
            }
            if (containHeaders && !finishReadHeader)
            {
                readHeader();
                finishReadHeader = true;
            }
            if (r.GoTo(() => r.IsStartElement("row") || r.IsEndElement("sheetData")))
            {
                bool isRow = "row".Equals(r.GetLocalName());
                if (isRow)
                {
                    GetNext();
                }
                return isRow;
            }
            else
            {
                return false;
            }

        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
        public virtual void InitCells()
        {
            cells = new List<Cell>(prop.CellProps.Count);
            for (int i = 0; i < prop.CellProps.Count; i++)
            {
                cells.Add(null);
            }
        }
        internal abstract void GetNext();
        public IList<Cell> GetCells()
        {
            return cells;
        }
        public CellProcessor GetCellProcessor()
        {
            return processor;
        }
    }

}
