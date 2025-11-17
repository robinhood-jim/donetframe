using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Spring.Globalization.Formatters;
using System.Text.Json;

namespace Frameset.Common.Data.Reader
{
    public class JsonIterator<T> : AbstractDataIterator<T>
    {
        internal DateTimeFormatter dateFormat = new DateTimeFormatter("year-{month:full}-{day:full}");
        IAsyncEnumerable<Dictionary<string, object>> iterator;
        public JsonIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = false;
        }

        public JsonIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = false;
        }

        public override void initalize(string filePath = null)
        {
            base.initalize(filePath);
            iterator = JsonSerializer.DeserializeAsyncEnumerable<Dictionary<string, object>>(inputStream);
        }

        public override async IAsyncEnumerable<T> ReadAsync(string path, string filterSql = null)
        {
            base.initalize(path);

            await foreach (var map in JsonSerializer.DeserializeAsyncEnumerable<Dictionary<string, object>>(inputStream))
            {
                cachedValue.Clear();
                for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                {
                    DataSetColumnMeta meta = MetaDefine.ColumnList[i];
                    object value;
                    map.TryGetValue(meta.ColumnCode, out value);
                    cachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(value, meta, dateFormat));

                }
                ConstructReturn();
                yield return current;

            }

        }
        public override bool MoveNext()
        {
            base.MoveNext();
            bool hasNext = iterator.GetAsyncEnumerator().MoveNextAsync().Result;
            if (hasNext)
            {
                Dictionary<string, object> map = iterator.GetAsyncEnumerator().Current;
                cachedValue.Clear();
                for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                {
                    DataSetColumnMeta meta = MetaDefine.ColumnList[i];
                    object value;
                    map.TryGetValue(meta.ColumnCode, out value);
                    cachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(value, meta, dateFormat));
                }
                ConstructReturn();
            }
            return hasNext;
        }
    }
}
