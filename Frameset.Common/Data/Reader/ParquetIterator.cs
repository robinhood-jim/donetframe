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
        private ParquetReader preader;
        private ParquetSchema schema;
        private List<DataField> fields = new List<DataField>();
        private Dictionary<DataField, DataColumn> groupMap = new Dictionary<DataField, DataColumn>();
        int groupCount;
        IReadOnlyList<IParquetRowGroupReader> groupReaders;
        IParquetRowGroupReader currentReader;
        int currentGroup = 0;
        long rowCount = 0;
        long readLines = 0;
        public ParquetIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            initalize(define.Path);
        }

        public ParquetIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            initalize(define.Path);
        }

        public ParquetIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            initalize(processPath);
        }

        public override void initalize(string path)
        {
            base.initalize(path);
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
            cachedValue.Clear();
            foreach (DataField field in fields)
            {
                object value = groupMap[field].Data.GetValue(readLines);
                if (value != null)
                {
                    cachedValue.TryAdd(field.Name, value);
                }
            }
            ConstructReturn();
            readLines++;
            return true;
        }

        public override async IAsyncEnumerable<T> ReadAsync(string path = null, string filterSql = null)
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
                cachedValue.Clear();
                foreach (DataField field in fields)
                {
                    object value = groupMap[field].Data.GetValue(readLines);
                    if (value != null)
                    {
                        cachedValue.TryAdd(field.Name, value);
                    }
                }
                ConstructReturn();
                readLines++;
                yield return current;
            }
        }
    }
}
