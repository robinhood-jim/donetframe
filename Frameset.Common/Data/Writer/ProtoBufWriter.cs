using Frameset.Common.Data.Utils;
using Frameset.Common.FileSystem;
using Frameset.Common.Protobuf.Utils;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Google.Protobuf;

namespace Frameset.Common.Data.Writer
{
    public class ProtoBufWriter<T> : AbstractDataWriter<T>
    {
        private DynamicMessage message;
        private MessageDefinition definition;

        public ProtoBufWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.PROTOBUF;
            Initalize();
        }

        public ProtoBufWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.PROTOBUF;
            Initalize();
        }

        internal override void Initalize()
        {
            base.Initalize();
            MessageDefinition.Builder builder = MessageDefinition.NewBuilder("test");
            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                DataSetColumnMeta columnMeta = MetaDefine.ColumnList[i];
                builder.AddField("required", ProtobufUtils.GetTypeStr(columnMeta), columnMeta.ColumnCode, i + 1);
            }
            definition = builder.Build();
            message = new DynamicMessage(definition);

        }

        public override void FinishWrite()
        {
            Flush();
        }

        public override void WriteRecord(T value)
        {
            message.DataContent.Clear();
            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                object? retVal = GetValue(value, MetaDefine.ColumnList[i]);
                if (retVal != null)
                {
                    if (MetaDefine.ColumnList[i].ColumnType == Constants.MetaType.TIMESTAMP)
                    {
                        object ts;
                        if (retVal is DateTime)
                        {
                            ts = retVal;
                        }
                        else if (retVal is DateTimeOffset)
                        {
                            ts = retVal;
                        }
                        else
                        {
                            ts = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(retVal?.ToString())).LocalDateTime;
                        }

                        message.DataContent.TryAdd(MetaDefine.ColumnList[i].ColumnCode, ts);
                    }
                    else
                    {
                        message.DataContent.TryAdd(MetaDefine.ColumnList[i].ColumnCode, retVal);
                    }
                }
            }
            message.WriteDelimitedTo(outputStream);
        }
    }
}
