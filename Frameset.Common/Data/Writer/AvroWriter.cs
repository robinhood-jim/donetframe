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
    public class AvroWriter<T> : AbstractDataWriter<T>
    {
        private RecordSchema schema;
        private DatumWriter<GenericRecord> datumWriter;
        private DataFileWriter<GenericRecord> fileWriter;
        public AvroWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawOutputStream = true;
            initalize();
        }
        internal override void initalize()
        {
            base.initalize();
            schema = AvroUtils.GetSchema(MetaDefine);
            datumWriter = new GenericDatumWriter<GenericRecord>(schema);
            Codec codec = Codec.CreateCodec(Codec.Type.Null);
            CompressType compressType = GetCompressType();
            switch (compressType)
            {
                case CompressType.BZ2:
                    codec = Codec.CreateCodec(Codec.Type.BZip2);
                    break;
                case CompressType.ZIP:
                    codec = Codec.CreateCodec(Codec.Type.Deflate);
                    break;
                case CompressType.XZ:
                    codec = Codec.CreateCodec(Codec.Type.XZ);
                    break;
                case CompressType.ZSTD:
                    codec = Codec.CreateCodec(Codec.Type.Zstandard);
                    break;
                case CompressType.SNAPPY:
                    codec = Codec.CreateCodec(Codec.Type.Snappy);
                    break;
            }

            fileWriter = (DataFileWriter<GenericRecord>)DataFileWriter<GenericRecord>.OpenWriter(datumWriter, outputStream, codec);

        }

        public override void FinishWrite()
        {
            fileWriter.Flush();
            fileWriter.Close();
        }

        public override void WriteRecord(T value)
        {
            GenericRecord record = new GenericRecord(schema);
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
