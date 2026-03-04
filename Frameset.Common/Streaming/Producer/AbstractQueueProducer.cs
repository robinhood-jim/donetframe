using Avro;
using Avro.Generic;
using Avro.IO;
using Frameset.Common.Data.Utils;
using Frameset.Common.Protobuf.Utils;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Google.Protobuf;
using MessagePack;
using System.Diagnostics;
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
            messageType = typeof(T);
            if (MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.MQMSGSERIALIZER, out serializer))
            {
                serializeType = ResourceConstants.SerialzeTypeOf(serializer);
            }
            if (ResourceConstants.SerializeType.AVRO == serializeType)
            {
                schema = AvroUtils.GetSchema(messageType);
                record = new(schema);
                dwriter = new(schema);
                methodMap = AnnotationUtils.ReflectObject(messageType);
            }
            else if (ResourceConstants.SerializeType.PROTOBUF == serializeType)
            {
                methodMap = AnnotationUtils.ReflectObject(messageType);
                dynamicMessage = ProtobufUtils.ConstructDynamicMessage(methodMap, messageType);
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
        protected DynamicMessage dynamicMessage = null!;
        protected Type messageType;
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
                ResourceConstants.SerializeType.PROTOBUF => SerializeProtoBuf(message),
                ResourceConstants.SerializeType.MESSAGEPACK => SerializeMessagePack(message),
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
            using (MemoryStream stream = new())
            {
                BinaryEncoder encoder = new(stream);
                dwriter?.Write(record, encoder);
                bytes = stream.ToArray();
            }
            return bytes;
        }
        protected byte[] SerializeProtoBuf(T message)
        {
            Trace.Assert(dynamicMessage != null, "");
            dynamicMessage.ParseFrom<T>(message);
            using (MemoryStream stream = new())
            {
                using (CodedOutputStream outputStream = new CodedOutputStream(stream))
                {
                    dynamicMessage.WriteTo(outputStream);
                    return stream.ToArray();
                }
            }
        }
        protected byte[] SerializeMessagePack(T message)
        {
            return MessagePackSerializer.Serialize(message, MessagePackSerializerOptions.Standard);
        }
    }
}
