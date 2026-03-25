using Frameset.Common.Data.Utils;
using Frameset.Common.FileSystem;
using Frameset.Common.Protobuf.Utils;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;

namespace Frameset.Common.Data.Reader
{
    public class ProtobufIterator<T> : AbstractDataIterator<T>
    {
        private MessageDefinition definition = null!;
        private DynamicMessage message = null!;

        public ProtobufIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.PARQUET;
            Initalize(define.Path);
        }

        public ProtobufIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.PARQUET;
            Initalize(define.Path);
        }
        public override bool MoveNext()
        {
            base.MoveNext();
            if (message.MergeDelimitedFromNew(inputStream))
            {
                CachedValue.Clear();
                foreach (var entry in message.DataContent)
                {
                    CachedValue.TryAdd(entry.Key, entry.Value);
                }
                ConstructReturn();
                return true;
            }
            return false;
        }
        public override sealed void Initalize(string? filePath = null)
        {
            base.Initalize(filePath);
            MessageDefinition.Builder builder = MessageDefinition.NewBuilder("test");
            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                DataSetColumnMeta columnMeta = MetaDefine.ColumnList[i];
                builder.AddField("required", ProtobufUtils.GetTypeStr(columnMeta.ColumnType), columnMeta.ColumnCode, i + 1);
            }
            definition = builder.Build();
            message = new DynamicMessage(definition);
        }

        public override IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null)
        {
            throw new NotImplementedException();
        }
    }
}
