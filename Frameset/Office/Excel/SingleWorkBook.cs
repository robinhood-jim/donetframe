using Frameset.Core.Exceptions;
using Frameset.Office.Excel.Meta;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace Frameset.Office.Excel
{
    public class SingleWorkBook : WorkBook
    {

        public SingleWorkBook(string filePath, ExcelSheetProp prop) : base(filePath, prop)
        {

        }
        public SingleWorkBook(Stream stream, bool readWriteTag, ExcelSheetProp prop) : base(stream, readWriteTag, prop)
        {

        }

        public SingleWorkBook(Stream stream, bool writeTag, Type entityType) : base(stream, writeTag, entityType)
        {
        }

        public SingleWorkBook(Stream stream, bool writeTag, IDataReader reader, string timeFormat = "yyyy-MM-dd", Dictionary<string, string> nameMapping = null) : base(stream, writeTag, reader,timeFormat,nameMapping)
        {
        }

        public bool WriteRow(Dictionary<string, object> dict)
        {
            if (currentSheet == null)
            {
                throw new ConfigMissingException("Please call BeginWrite first");
            }
            if (totalRow > 0 && (totalRow % MaxRows == 0 || (maxSheetSize > 0 && GetSheet(currentSheet.Index).GetWriter().ShouldClose(maxSheetSize, thresholdSize))))
            {
                if (Log.IsEnabled(LogEventLevel.Debug))
                {
                    Log.Debug(" Flush and Close sheet " + currentSheet.Index);
                }
                currentSheet.Finish();
                BeginWrite();
            }
            currentSheet.WriteRow(dict);
            totalRow++;
            return true;
        }
        public bool WriteEntity(object obj)
        {
            Trace.Assert(obj.GetType().Equals(SheetProp.EntityType),"must call ExcelSheetProp.FromEntity first");
            if (currentSheet == null)
            {
                throw new ConfigMissingException("Please call BeginWrite first");
            }
            if (totalRow > 0 && (totalRow % MaxRows == 0 || (maxSheetSize > 0 && GetSheet(currentSheet.Index).GetWriter().ShouldClose(maxSheetSize, thresholdSize))))
            {
                if (Log.IsEnabled(LogEventLevel.Debug))
                {
                    Log.Debug(" Flush and Close sheet " + currentSheet.Index);
                }
                currentSheet.Finish();
                BeginWrite();
            }
            currentSheet.WriteEntity(obj);
            totalRow++;
            return true;
        }
    }
}
