using Avro;
using Avro.Generic;
using Avro.IO;
using Frameset.Common.Data.Utils;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using System.Text;
using System.Text.Json;

namespace Frameset.Common.Streaming.Producer
{
    public abstract class AbstractQueueProducer<T> : IDisposable
    {
        public DataCollectionDefine MetaDefine
        {
            get; internal set;
        }
        public AbstractQueueProducer(DataCollectionDefine define)
        {
            MetaDefine = define;
            string? serializer = null;
            if (MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.KAFKASERIALIZER, out serializer))
            {
                serializeType = ResourceConstants.SerialzeTypeOf(serializer);
            }
            if (ResourceConstants.SerializeType.AVRO == serializeType)
            {
                schema = AvroUtils.GetSchema(typeof(T));
                record = new(schema);
                dwriter = new(schema);
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
        }
        protected string hostUrl = null!;
        protected ResourceConstants.SerializeType serializeType = ResourceConstants.SerializeType.JSON;
        protected Encoding encoding = Encoding.UTF8;
        protected RecordSchema schema = null!;
        protected GenericRecord record = null!;
        protected Dictionary<string, MethodParam> methodMap = [];
        public abstract bool SendMessage(string queueName, string key, T message);
        protected GenericDatumWriter<GenericRecord> dwriter = null!;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposable)
        {
            if (!disposable)
            {
                return;
            }
        }
        protected byte[] Serailize(T message)
        {
            return serializeType switch
            {
                ResourceConstants.SerializeType.JSON => encoding.GetBytes(JsonSerializer.Serialize(message)),
                ResourceConstants.SerializeType.AVRO => SerializeAvro(message),
                _ => throw new NotImplementedException()
            };
        }
        protected byte[] SerializeAvro(T message)
        {
            foreach (var entry in methodMap)
            {
                object? obj = entry.Value.GetMethod.Invoke(message, []);
                if (obj != null)
                {
                    record.Add(entry.Key, obj);
                }
                else
                {
                    record.Add(entry.Key, null);
                }
            }

            byte[] bytes;
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryEncoder encoder = new(stream);
                dwriter?.Write(record, encoder);
                bytes = stream.ToArray();
            }
            return bytes;
        }
    }
}
