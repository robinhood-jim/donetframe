using Frameset.Common.Data.Utils;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Frameset.Common.Data.Reader
{
    public class ParquetIterator<T> : AbstractDataIterator<T>
    {
        private ParquetReader preader = null!;
        private ParquetSchema schema = null!;
        private List<DataField> fields = [];
        private readonly Dictionary<DataField, DataColumn> groupMap = new Dictionary<DataField, DataColumn>();
        int groupCount;
        IReadOnlyList<IParquetRowGroupReader> groupReaders = null!;
        IParquetRowGroupReader currentReader = null!;
        int currentGroup = 0;
        long rowCount = 0;
        long readLines = 0;
        public ParquetIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(define.Path);
        }

        public ParquetIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(define.Path);
        }

        public ParquetIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(processPath);
        }

        public override sealed void Initalize(string? filePath = null)
        {
            base.Initalize(filePath);
            preader = ParquetReader.CreateAsync(inputStream).Result;
            if (MetaDefine.ColumnList.IsNullOrEmpty())
            {
                schema = preader.Schema;
                foreach (var item in schema.DataFields)
                {
                    fields.Add(item);
                }
            }
            else
            {
                schema = ParquetUtils.GetSchema(MetaDefine, fields);
            }
            groupCount = preader.RowGroupCount;
            groupReaders = preader.RowGroups;
        }
        public override bool MoveNext()
        {
            base.MoveNext();
            if (currentReader == null || readLines >= rowCount)
            {
                if (currentGroup >= groupCount)
                {
                    return false;
                }
                currentReader = groupReaders[currentGroup++];
                groupMap.Clear();
                rowCount = currentReader.RowCount;
                readLines = 0;

                for (int i = 0; i < fields.Count; i++)
                {
                    groupMap.TryAdd(fields[i], currentReader.ReadColumnAsync(fields[i]).Result);
                }
            }
            CachedValue.Clear();
            foreach (DataField field in fields)
            {
                DataColumn? dataColumn = null;
                groupMap.TryGetValue(field, out dataColumn);

                object? value = dataColumn?.Data.GetValue(readLines);
                if (value != null)
                {
                    CachedValue.TryAdd(field.Name, value);
                }
            }
            ConstructReturn();
            readLines++;
            return true;
        }

        public override async IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null)
        {
            base.MoveNext();
            if (currentGroup < groupCount)
            {
                if (currentReader == null || readLines >= rowCount)
                {
                    currentReader = groupReaders[currentGroup++];
                    groupMap.Clear();
                    rowCount = currentReader.RowCount;
                    readLines = 0;

                    for (int i = 0; i < fields.Count; i++)
                    {
                        groupMap.TryAdd(fields[i], await currentReader.ReadColumnAsync(fields[i]));
                    }
                }
                CachedValue.Clear();
                foreach (DataField field in fields)
                {
                    DataColumn? dataColumn;
                    groupMap.TryGetValue(field, out dataColumn);

                    object? value = dataColumn?.Data.GetValue(readLines);
                    if (value != null)
                    {
                        CachedValue.TryAdd(field.Name, value);
                    }
                }
                ConstructReturn();
                readLines++;
                yield return current;
            }
        }
    }
}
