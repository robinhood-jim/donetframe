using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Office.Excel;
using Frameset.Office.Excel.Meta;
using Frameset.Office.Excel.Util;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Common.Data.Reader
{
    public class XlsxIterator<T> : AbstractDataIterator<T>
    {
        private SingleWorkBook workBook=null!;
        private ExcelSheetProp sheetProp=null!;
        private SheetPropBuilder propBuilder=null!;
        private MapEnumerator enumerator=null!;
        private int sheetNums;
        private int sheetPos;
        public XlsxIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.XLSX;
            Initalize(define.Path);
        }

        public XlsxIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.XLSX;
            Initalize(define.Path);
        }
        public XlsxIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.XLSX;
            Initalize(processPath);
        }
        public override sealed void Initalize(string? filePath = null)
        {
            base.Initalize(filePath);
            propBuilder = SheetPropBuilder.NewBuilder();
            if (!MetaDefine.ColumnList.IsNullOrEmpty())
            {
                foreach (DataSetColumnMeta meta in MetaDefine.ColumnList)
                {
                    propBuilder.AddCellProp(meta.ColumnName, meta.ColumnCode, meta.ColumnType);
                }
            }
            sheetProp = propBuilder.Build();
            workBook = new SingleWorkBook(inputStream, false, sheetProp);
            sheetNums = workBook.GetSheetNum();
        }
        public override bool MoveNext()
        {
            base.MoveNext();
            if (enumerator == null)
            {
                enumerator = workBook.GetMapEnumerator(workBook.GetSheet(sheetPos), sheetProp);
            }
            CachedValue.Clear();
            while (!enumerator.MoveNext() && sheetPos < sheetNums)
            {
                enumerator.Dispose();
                sheetPos++;
                enumerator = workBook.GetMapEnumerator(workBook.GetSheet(sheetPos), sheetProp);
            }
            if (sheetPos < sheetNums)
            {
                Dictionary<string, object> valueMap = enumerator.Current;
                foreach (KeyValuePair<string, object> pair in valueMap)
                {
                    CachedValue.TryAdd(pair.Key, pair.Value);
                }
                ConstructReturn();
                return true;
            }
            return false;
        }
        public override IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null)
        {
            throw new NotImplementedException();
        }
    }
}
