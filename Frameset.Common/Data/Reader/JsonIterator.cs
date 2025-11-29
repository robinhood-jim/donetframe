using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace Frameset.Common.Data.Reader
{
    public class JsonIterator<T> : AbstractDataIterator<T>
    {

        private JsonTextReader jsonReader=null!;
        private Dictionary<string, DataSetColumnMeta> metaMap = [];

        public JsonIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            Initalize(define.Path);
        }

        public JsonIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            Initalize(define.Path);
        }

        public JsonIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            Initalize(processPath);
        }

        public override sealed void Initalize(string? filePath = null)
        {
            base.Initalize(filePath);
            jsonReader = new JsonTextReader(reader);
            if (jsonReader != null)
            {
                jsonReader.Read();
                Trace.Assert(jsonReader.TokenType == JsonToken.StartArray, "json does not start as Array");
            }
            foreach (var item in MetaDefine.ColumnList)
            {
                metaMap.TryAdd(item.ColumnCode, item);
            }

        }

        public override async IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null)
        {
            base.Initalize(path);

            await foreach (var map in System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<Dictionary<string, object>>(inputStream))
            {
                CachedValue.Clear();
                for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                {
                    DataSetColumnMeta meta = MetaDefine.ColumnList[i];
                    object? value = null;
                    map?.TryGetValue(meta.ColumnCode, out value);
                    CachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(value, meta, dateFormatter));

                }
                ConstructReturn();
                yield return current;

            }

        }
        public override bool MoveNext()
        {
            base.MoveNext();
            bool hasNext = false;
            try
            {
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        CachedValue.Clear();
                    }
                    else if (jsonReader.TokenType == JsonToken.EndObject)
                    {
                        ConstructReturn();
                        hasNext = true;
                        break;
                    }
                    else if (jsonReader.TokenType == JsonToken.PropertyName)
                    {
                        string propName = jsonReader.Value?.ToString();
                        jsonReader.Read();
                        object? value = jsonReader.Value;
                        DataSetColumnMeta? meta;
                        metaMap.TryGetValue(propName, out meta);
                        if (meta != null && value!=null)
                        {
                            if (meta.ColumnType != Constants.MetaType.TIMESTAMP)
                            {
                                CachedValue.TryAdd(propName, ConvertUtil.ConvertStringToTargetObject(value, meta, dateFormatter));
                            }
                            else
                            {
                                CachedValue.TryAdd(propName, ConvertUtil.ConvertStringToTargetObject(value, meta, timestampFormatter));
                            }
                        }
                        else
                        {
                            throw new OperationFailedException("prop " + propName + " not defined!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception {Message}", ex.Message);
            }
            return hasNext;
        }
    }
}
