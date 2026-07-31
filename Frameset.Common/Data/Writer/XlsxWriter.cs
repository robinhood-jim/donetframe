using System.Diagnostics;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Office.Excel;
using Frameset.Office.Excel.Meta;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Common.Data.Writer
{
    public class XlsxWriter<T> : AbstractDataWriter<T>
    {
        private SingleWorkBook workBook=null!;
        private ExcelSheetProp sheetProp=null!;
        private SheetPropBuilder propBuilder=null!;
        private WorkSheet workSheet=null!;

        public XlsxWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.XLSX;
            Initalize();
        }
        public XlsxWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.XLSX;
            Initalize();
        }
        internal override void Initalize()
        {
            base.Initalize();
            propBuilder = SheetPropBuilder.NewBuilder();
            if (!MetaDefine.ColumnList.IsNullOrEmpty())
            {
                foreach (DataSetColumnMeta meta in MetaDefine.ColumnList)
                {
                    propBuilder.AddCellProp(meta.ColumnName, meta.ColumnCode, meta.ColumnType);
                }
            }
            sheetProp = propBuilder.Build();
            workBook = new SingleWorkBook(outputStream, true, sheetProp);
            workSheet = workBook.CreateSheet("sheet1", sheetProp);
        }

        public override void FinishWrite()
        {
            if (workSheet != null)
            {
                workSheet.Finish();
            }
            if (workBook != null)
            {
                workBook.Finish();
            }
        }

        public override void WriteRecord(T value)
        {
            if (useDictOutput)
            {
                Dictionary<string, object> tmp = value as Dictionary<string, object>;
                workSheet.WriteRow(tmp);
            }
            else
            {
                Dictionary<string, object> tmpDict = [];
                for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                {
                    object? retVal = GetValue(value, MetaDefine.ColumnList[i]);
                    if (retVal != null)
                    {
                        tmpDict.TryAdd(MetaDefine.ColumnList[i].ColumnCode, retVal);
                    }
                }
                workSheet.WriteRow(tmpDict);
            }
        }
    }
}
