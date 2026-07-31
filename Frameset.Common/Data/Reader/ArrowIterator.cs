using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Memory;
using Frameset.Common.Data.Utils;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Common.Data.Reader
{
    public class ArrowIterator<T> : AbstractDataIterator<T>
    {
        private ArrowStreamReader arrowStreamReader=null!;
        private Schema schema=null!;
        private MemoryAllocator allocator=null!;
        private RecordBatch recordBatch=null!;
        private int maxrows;
        private int currentbatchRow;

        public ArrowIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.ARROW;
            useRawStream = true;
            Initalize(define.Path);

        }
        public ArrowIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.ARROW;
            useRawStream = true;
            Initalize(processPath);
        }
        public override void Initalize(string? filePath = null)
        {
            base.Initalize(filePath);
            schema = ArrowUtils.GetSchema(MetaDefine);
            allocator = MemoryAllocator.Default.Value;
            arrowStreamReader = new ArrowStreamReader(inputStream, allocator);
        }
        public override bool MoveNext()
        {
            base.MoveNext();
            bool hasRecord = false;
            CachedValue.Clear();
            if (recordBatch == null || maxrows == 0 || currentbatchRow == 0 || currentbatchRow > maxrows)
            {
                recordBatch = arrowStreamReader.ReadNextRecordBatch();
                currentbatchRow = 0;
                maxrows = recordBatch.Length;
            }
            if (recordBatch != null)
            {
                hasRecord = true;
            }
            if (hasRecord)
            {
                IReadOnlyList<Field> fields = schema.FieldsList;
                if (!fields.IsNullOrEmpty())
                {
                    for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                    {
                        WrapValue(fields[i], MetaDefine.ColumnList[i].ColumnCode, MetaDefine.ColumnList[i].ColumnType, i, currentbatchRow);
                    }
                }
                currentbatchRow++;
                ConstructReturn();
                return true;
            }
            return false;
        }
        internal void WrapValue(Field field, string columnName, Constants.MetaType columnType, int column, int row)
        {
            IArrowArray array = recordBatch.Column(column);
            object? value=null ;
            switch (columnType)
            {
                case Constants.MetaType.LONG:
                    Int64Array longArr = (Int64Array)array;
                    value = longArr.GetValue(row);
                    break;
                case Constants.MetaType.SHORT:
                    Int16Array shortArray = (Int16Array)array;
                    value = shortArray.GetValue(row);
                    break;
                case Constants.MetaType.INTEGER:
                    Int32Array intArr = (Int32Array)array;
                    value = intArr.GetValue(row);
                    break;
                case Constants.MetaType.FLOAT:
                    FloatArray floatArr = (FloatArray)array;
                    value = floatArr.GetValue(row);
                    break;
                case Constants.MetaType.DOUBLE:
                    DoubleArray dArr = (DoubleArray)array;
                    value = dArr.GetValue(row);
                    break;
                case Constants.MetaType.TIMESTAMP:
                    Int64Array tArr = (Int64Array)array;
                    value = new DateTime(tArr.GetValue(row).Value);
                    break;
                case Constants.MetaType.DATE:
                    Int64Array daArr = (Int64Array)array;
                    value = new DateTime(daArr.GetValue(row).Value);
                    break;
                case Constants.MetaType.STRING:
                    StringArray sArr = (StringArray)array;
                    value = sArr.GetString(row);
                    break;
            }
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                CachedValue.TryAdd(columnName, value);
            }
        }
        public override IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null)
        {
            throw new NotImplementedException();
        }
    }
}
