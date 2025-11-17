using Avro;
using Avro.File;
using Avro.Generic;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;

namespace Frameset.Common.Data.Reader
{
    public class AvroIterator<T> : AbstractDataIterator<T>
    {
        private RecordSchema schema;
        private IFileReader<GenericRecord> fileReader;


        public AvroIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            initalize(define.Path);
        }

        public AvroIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            initalize(define.Path);
        }
        public override void initalize(string path = null)
        {
            base.initalize(path);
            fileReader = DataFileReader<GenericRecord>.OpenReader(inputStream);
            schema = (RecordSchema)fileReader.GetSchema();
        }
        public override bool MoveNext()
        {
            base.MoveNext();
            cachedValue.Clear();
            if (fileReader.HasNext())
            {
                GenericRecord record = fileReader.Next();
                List<Field> fields = schema.Fields;
                foreach (Field field in fields)
                {
                    object value = record.GetValue(field.Pos);
                    if (value != null)
                    {
                        cachedValue.TryAdd(field.Name, value);
                    }
                }
                ConstructReturn();
                return true;
            }
            return false;
        }

        public override async IAsyncEnumerable<T> ReadAsync(string path = null, string filterSql = null)
        {
            initalize(path);
            base.MoveNext();
            cachedValue.Clear();
            while (fileReader.HasNext())
            {
                GenericRecord record = fileReader.Next();
                List<Field> fields = schema.Fields;
                foreach (Field field in fields)
                {
                    object value = record.GetValue(field.Pos);
                    if (value != null)
                    {
                        cachedValue.TryAdd(field.Name, value);
                    }
                }
                ConstructReturn();
                yield return current;
            }
        }


    }
}
