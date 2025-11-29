using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Newtonsoft.Json;

namespace Frameset.Common.Data.Writer
{
    public class JsonDataWriter<T> : AbstractDataWriter<T>
    {
        private readonly JsonWriter jsonWriter;
        public JsonDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.JSON;
            useWriter = true;
            base.Initalize();
            jsonWriter = new JsonTextWriter(writer);
            jsonWriter.WriteStartArray();
        }

        public JsonDataWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.JSON;
            useWriter = true;
            base.Initalize();
            jsonWriter = new JsonTextWriter(writer);
            jsonWriter.WriteStartArray();
        }

        public override void FinishWrite()
        {
            if (jsonWriter != null)
            {
                jsonWriter.WriteEndArray();
                jsonWriter.Flush();
                Flush();
                jsonWriter.Close();
            }
        }

        public override void WriteRecord(T value)
        {
            jsonWriter.WriteStartObject();
            foreach (DataSetColumnMeta column in MetaDefine.ColumnList)
            {
                object? retVal = GetValue(value, column);
                if (retVal != null)
                {
                    jsonWriter.WritePropertyName(column.ColumnCode);
                    object? getValue = GetOutput(column, retVal);
                    if (getValue != null)
                    {
                        jsonWriter.WriteValue(getValue);
                    }
                }
            }
            jsonWriter.WriteEndObject();
        }

    }
}
