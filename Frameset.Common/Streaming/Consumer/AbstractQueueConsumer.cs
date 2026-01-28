using Avro;
using Avro.Generic;
using Avro.IO;
using Frameset.Common.Data.Utils;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Frameset.Common.Streaming.Consumer
{
    public abstract class AbstractQueueConsumer<T> : IDisposable
    {
        public DataCollectionDefine MetaDefine
        {
            get; internal set;
        }
        public AbstractQueueConsumer(DataCollectionDefine define)
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
                dreader = new(schema, schema);
                methodMap = AnnotationUtils.ReflectObject(typeof(T));
            }
        }
        protected string hostUrl = null!;
        protected ResourceConstants.SerializeType serializeType = ResourceConstants.SerializeType.JSON;
        protected Encoding encoding = Encoding.UTF8;
        protected RecordSchema schema = null!;
        protected GenericRecord record = null!;
        protected Dictionary<string, MethodParam> methodMap = [];
        public abstract List<T> PoolMessage(Action<T> action);
        protected GenericDatumReader<GenericRecord> dreader = null!;
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
        protected T DSerailize(byte[] message)
        {
            Trace.Assert(message != null);
            return serializeType switch
            {
                ResourceConstants.SerializeType.JSON => JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(message)),

                ResourceConstants.SerializeType.AVRO => AvroDSerailize(message),
                _ => throw new NotImplementedException()
            };

        }
        private T AvroDSerailize(byte[] message)
        {
            BinaryDecoder decoder = new BinaryDecoder(new MemoryStream(message));
            GenericRecord genericRecord = dreader.Read(record, decoder);
            if (genericRecord != null)
            {
                dynamic? retObj = System.Activator.CreateInstance<T>();
                int pos = 0;
                foreach (var entry in methodMap)
                {
                    object obj = record.GetValue(pos++);

                    if (obj != null)
                    {
                        entry.Value.SetMethod.Invoke(retObj, new object[] { obj });
                    }
                }
                return retObj;
            }
            else
            {
                throw new OperationFailedException("schema parse error!");
            }
        }
    }
}
