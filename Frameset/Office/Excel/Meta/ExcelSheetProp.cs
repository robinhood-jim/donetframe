using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Office.Annotation;
using Frameset.Office.Element;
using Microsoft.IdentityModel.Tokens;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Frameset.Office.Excel.Meta
{
    public class ExcelSheetProp
    {
        internal ExcelSheetProp()
        {

        }
        public string SheetName
        {
            get; internal set;
        }
        public int StartRow
        {
            get; internal set;
        } = 2;
        public int StartCol
        {
            get; internal set;
        } = 1;
        public int TableId
        {
            get; internal set;
        }
        public bool FillHeader
        {
            get; internal set;
        } = true;
        public string TemplateFile
        {
            get; internal set;
        }
        public IList<ExcelCellProp> CellProps
        {
            get; internal set;
        } = new List<ExcelCellProp>();
        public int MaxRows
        {
            get; internal set;
        } = 500000;
        public int WriteBufferSize
        {
            get; set;
        } = 0;
        public SheetVisibility Visibility
        {
            get; internal set;
        }
        internal Dictionary<string, FieldContent> fieldInfoMap = new();
        public Type EntityType
        {
            get; internal set;
        }
        public void AddCellProp(string columnName, string columnCode, Constants.MetaType columnType)
        {
            CellProps.Add(new ExcelCellProp(columnName, columnCode, columnType));

        }
        public void AddCellProp(string columnName, string columnCode, Constants.MetaType columnType, string formula)
        {
            CellProps.Add(new ExcelCellProp(columnName, columnCode, columnType, formula));

        }
        public void AddCellProp(string columnName, string columnCode, Constants.MetaType columnType, bool needMerge)
        {
            CellProps.Add(new ExcelCellProp(columnName, columnCode, columnType, needMerge));

        }
        public void AddCellProp(ExcelCellProp cellprop)
        {
            CellProps.Add(cellprop);
        }
        public static ExcelSheetProp FromEntityDefine(Type entityType)
        {
            ExcelSheetProp propDef = new ExcelSheetProp();
            propDef.EntityType = entityType;
            object[] attributes = entityType.GetCustomAttributes(false);
            Dictionary<int, ExcelCellProp> cellMap = new();
            if (attributes.Length > 0)
            {
                for (int i = 0; i < attributes.Length; i++)
                {
                    if (attributes[i].GetType().Equals(typeof(ExcelSheetAttribute)))
                    {
                        ExcelSheetAttribute sheetAttribute = attributes[i] as ExcelSheetAttribute;
                        propDef.MaxRows = sheetAttribute.MaxRows != -1 ? sheetAttribute.MaxRows : 500000;
                        propDef.SheetName ??= sheetAttribute.Name ?? "sheet1";
                        propDef.FillHeader = sheetAttribute.FillHeader;
                        propDef.Visibility = sheetAttribute.Visibility ?? SheetVisibility.VISIBLE;
                    }
                }
            }
            int totalNum = 0;
            PropertyInfo[] propertyInfos = entityType.GetProperties();
            if (propertyInfos.Count() > 0)
            {
                foreach (PropertyInfo prop in propertyInfos)
                {
                    object[] propAttr = prop.GetCustomAttributes(false);
                    string propName = prop.Name;
                    string columnName = prop.Name;

                    if (propAttr.Length > 0)
                    {
                        ExcelCellProp cellProp = new ExcelCellProp();
                        cellProp.ColumnCode = propName;
                        Constants.MetaType metaType = EntityReflectUtils.AdjustType(prop.PropertyType);
                        cellProp.ColumnType = metaType;
                        for (int i = 0; i < propAttr.Length; i++)
                        {
                            parseAttribute(propAttr, cellProp, columnName, totalNum);
                        }
                        FieldBuilder builder = new();
                        builder.Property(prop).PropertyName(propName).FieldName(columnName).DataType(metaType).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);
                        propDef.fieldInfoMap.TryAdd(propName, builder.Build());
                        cellMap.TryAdd(cellProp.Index, cellProp);
                        totalNum++;
                    }
                }
                FieldInfo[] fieldInfos = entityType.GetFields();
                if (fieldInfos.Count() > 0)
                {
                    foreach (FieldInfo field in fieldInfos)
                    {
                        object[] propAttr = field.GetCustomAttributes(false);
                        string propName = field.Name;
                        string columnName = field.Name;
                        if (propAttr.Count() > 0)
                        {
                            ExcelCellProp cellProp = new ExcelCellProp();
                            cellProp.ColumnCode = propName;
                            Constants.MetaType metaType = EntityReflectUtils.AdjustType(field.GetType());
                            cellProp.ColumnType = metaType;
                            for (int i = 0; i < propAttr.Length; i++)
                            {
                                parseAttribute(propAttr, cellProp, columnName, totalNum);
                            }

                            FieldBuilder builder = new();
                            builder.PropertyName(propName).FieldName(columnName).FieldInfo(field).DataType(cellProp.ColumnType);
                            propDef.fieldInfoMap.TryAdd(propName, builder.Build());
                            cellMap.TryAdd(cellProp.Index, cellProp);
                            totalNum++;
                        }

                    }
                }
                foreach (var item in cellMap.OrderBy(it => it.Key).ToList())
                {
                    propDef.AddCellProp(item.Value);
                }
            }
            return propDef;
        }
        public static ExcelSheetProp FromDataReader(IDataReader reader, string timeFormat = "yyyy-MM-dd", Dictionary<string, string> nameMapping = null)
        {
            Trace.Assert(reader.FieldCount > 0, "empty reader");
            ExcelSheetProp prop = new ExcelSheetProp();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string propName = reader.GetName(i);
                string columName = propName;
                Constants.MetaType metaType = EntityReflectUtils.AdjustType(reader.GetFieldType(i));
                if (!nameMapping.IsNullOrEmpty())
                {
                    nameMapping.TryGetValue(propName, out columName);
                }
                ExcelCellProp cellProp = new(columName, propName, metaType);
                if (Constants.MetaType.DATE.Equals(metaType) || Constants.MetaType.TIMESTAMP.Equals(metaType))
                {
                    cellProp.Format = timeFormat;
                }
                prop.AddCellProp(cellProp);
            }
            return prop;
        }

        internal static void parseAttribute(object[] propAttr, ExcelCellProp cellProp, string columnName, int totalNum)
        {
            int orderPos = totalNum;
            for (int i = 0; i < propAttr.Length; i++)
            {

                if (propAttr[i].GetType().Equals(typeof(ExcelColumnAttribute)))
                {
                    ExcelColumnAttribute a = propAttr[i] as ExcelColumnAttribute;
                    cellProp.ColumnName = a.ColumnName ?? columnName;
                    orderPos = a.Order != -1 ? a.Order : totalNum;
                    cellProp.Format = a.Format;
                    cellProp.FormatId = a.FormatId;
                    cellProp.Width = a.Width;
                    cellProp.Formula = a.Formula;
                    if (!cellProp.Formula.IsNullOrEmpty())
                    {
                        cellProp.ColumnType = Constants.MetaType.FORMULA;
                    }
                }
                else if (propAttr[i].GetType().Equals(typeof(ExcelColumnNameAttribute)))
                {
                    cellProp.ColumnName = (propAttr[i] as ExcelColumnNameAttribute).ColumnName;
                }
                else if (propAttr[i].Equals(typeof(ExcelColumnIndexAttribute)))
                {
                    orderPos = (propAttr[i] as ExcelColumnIndexAttribute).Index;
                }
                else if (propAttr[i].Equals(typeof(ExcelFormulaAttribute)))
                {
                    cellProp.Formula = (propAttr[i] as ExcelFormulaAttribute).Formula;
                }
            }
            cellProp.Index = orderPos;
        }
        public static ExcelSheetProp FromDataReader()
        {
            return new ExcelSheetProp();
        }
        public Dictionary<string, FieldContent> GetFieldMap()
        {
            return fieldInfoMap;
        }
    }
    public class SheetPropBuilder
    {
        private ExcelSheetProp prop = new ExcelSheetProp();
        internal SheetPropBuilder()
        {

        }
        public static SheetPropBuilder NewBuilder()
        {
            SheetPropBuilder b = new();
            return b;
        }
        public SheetPropBuilder AddCellProp(string columnName, string columnCode, Constants.MetaType columnType)
        {
            prop.CellProps.Add(new(columnName, columnCode, columnType));
            return this;
        }
        public SheetPropBuilder AddCellProp(string columnName, string columnCode, Constants.MetaType columnType, string formula)
        {
            prop.CellProps.Add(new(columnName, columnCode, columnType, formula));
            return this;
        }
        public SheetPropBuilder AddCellProp(string columnName, string columnCode, Constants.MetaType columnType, bool needMerge)
        {
            prop.CellProps.Add(new(columnName, columnCode, columnType, needMerge));
            return this;
        }
        public SheetPropBuilder AddCellProp(ExcelCellProp cellprop)
        {
            prop.CellProps.Add(cellprop);
            return this;
        }
        public SheetPropBuilder StartRow(int startRow)
        {
            AssertUtils.IsTrue(startRow > 0, "");
            prop.StartRow = startRow;
            return this;
        }
        public SheetPropBuilder StartCol(int startCol)
        {
            AssertUtils.IsTrue(startCol > 0, "");
            prop.StartCol = startCol;
            return this;
        }
        public SheetPropBuilder SheetName(string sheetName)
        {
            AssertUtils.IsTrue(!sheetName.IsNullOrEmpty(), "");
            prop.SheetName = sheetName;
            return this;
        }
        public SheetPropBuilder TemplateFile(string templateFile)
        {
            AssertUtils.IsTrue(!templateFile.IsNullOrEmpty(), "");
            prop.TemplateFile = templateFile;
            return this;
        }
        public SheetPropBuilder MaxRows(int maxRows)
        {
            AssertUtils.IsTrue(maxRows > 0, "");
            prop.MaxRows = maxRows;
            return this;
        }
        public ExcelSheetProp Build()
        {
            return prop;
        }

    }
    public class ExcelCellProp
    {
        public string ColumnName
        {
            get; internal set;
        }
        public string ColumnCode
        {
            get; internal set;
        }
        public Constants.MetaType ColumnType
        {
            get; internal set;
        }
        public string Formula
        {
            get; internal set;
        }
        public string Format
        {
            get; internal set;
        }
        public bool NeedMerge
        {
            get; internal set;
        }
        public int FormatId
        {
            get; internal set;
        }
        public double Width
        {
            get; internal set;
        }
        public int Index
        {
            get; set;
        }
        internal ExcelCellProp()
        {

        }

        public ExcelCellProp(string columnName, string columnCode, Constants.MetaType columnType, bool needMerge)
        {
            this.ColumnCode = columnCode;
            this.ColumnName = columnName;
            this.ColumnType = columnType;
            this.NeedMerge = needMerge;
        }
        public ExcelCellProp(string columnName, string columnCode, Constants.MetaType columnType, string formula)
        {
            this.ColumnCode = columnCode;
            this.ColumnName = columnName;
            this.ColumnType = columnType;
            this.Formula = formula;
        }

        public ExcelCellProp(string columnName, string columnCode, Constants.MetaType columnType)
        {
            this.ColumnCode = columnCode;
            this.ColumnName = columnName;
            this.ColumnType = columnType;
            this.NeedMerge = false;
        }

    }
}
