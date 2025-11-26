using Avro;
using Avro.File;
using Avro.Generic;
using Frameset.Common.Data.Utils;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;


namespace Frameset.Common.Data.Writer
{
    public class AvroDataWriter<T> : AbstractDataWriter<T>
    {
        private RecordSchema schema;
        private DatumWriter<GenericRecord> datumWriter;
        private DataFileWriter<GenericRecord> fileWriter;
        public AvroDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawOutputStream = true;
            Initalize();
        }

        public AvroDataWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawOutputStream = true;
            Initalize();
        }

        internal override void Initalize()
        {
            base.Initalize();
            schema = AvroUtils.GetSchema(MetaDefine);
            datumWriter = new GenericDatumWriter<GenericRecord>(schema);
            CompressType compressType = GetCompressType();
            Codec codec = GetCodec(compressType);
            fileWriter = (DataFileWriter<GenericRecord>)DataFileWriter<GenericRecord>.OpenWriter(datumWriter, outputStream, codec);

        }

        private static Codec GetCodec(CompressType compressType)
        {
            return compressType switch
            {
                CompressType.BZ2 => Codec.CreateCodec(Codec.Type.BZip2),
                CompressType.ZIP => Codec.CreateCodec(Codec.Type.Deflate),
                CompressType.XZ => Codec.CreateCodec(Codec.Type.XZ),
                CompressType.ZSTD => Codec.CreateCodec(Codec.Type.Zstandard),
                CompressType.SNAPPY => Codec.CreateCodec(Codec.Type.Snappy),
                _ => Codec.CreateCodec(Codec.Type.Null)
            };
        }

        public override void FinishWrite()
        {
            fileWriter.Flush();
            Flush();
            fileWriter.Close();
        }

        public override void WriteRecord(T value)
        {
            GenericRecord record = new(schema);
            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                object retVal = GetValue(value, MetaDefine.ColumnList[i]);
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
                            ts = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(retVal.ToString())).LocalDateTime;
                        }

                        record.Add(MetaDefine.ColumnList[i].ColumnCode, ts);
                    }
                    else
                    {
                        record.Add(MetaDefine.ColumnList[i].ColumnCode, retVal);
                    }
                }
            }
            fileWriter.Append(record);
        }
    }
}
