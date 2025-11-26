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
            Initalize(define.Path);
        }

        public AvroIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(define.Path);
        }

        public AvroIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(processPath);
        }

        public override void Initalize(string path = null)
        {
            base.Initalize(path);
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
            Initalize(path);
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
