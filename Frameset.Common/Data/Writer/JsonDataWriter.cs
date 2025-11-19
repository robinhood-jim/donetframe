using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Newtonsoft.Json;

namespace Frameset.Common.Data.Writer
{
    public class JsonDataWriter<T> : AbstractDataWriter<T>
    {
        private JsonWriter jsonWriter;
        public JsonDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.JSON;
            useWriter = true;
            base.initalize();
            jsonWriter = new JsonTextWriter(writer);
            jsonWriter.WriteStartArray();
        }

        public override void FinishWrite()
        {
            if (jsonWriter != null)
            {
                jsonWriter.WriteEndArray();
                jsonWriter.Flush();
                jsonWriter.Close();
            }
        }

        public override void WriteRecord(T value)
        {
            jsonWriter.WriteStartObject();
            foreach (DataSetColumnMeta column in MetaDefine.ColumnList)
            {
                object retVal = GetValue(value, column);
                if (retVal != null)
                {
                    jsonWriter.WritePropertyName(column.ColumnCode);
                    jsonWriter.WriteValue(GetOutput(column, retVal));
                }
            }
            jsonWriter.WriteEndObject();
        }

    }
}
