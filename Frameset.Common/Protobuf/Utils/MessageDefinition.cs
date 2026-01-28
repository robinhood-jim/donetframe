using Google.Protobuf.Reflection;
using System.Diagnostics;

namespace Frameset.Common.Protobuf.Utils
{
    public class MessageDefinition
    {
        internal readonly Dictionary<int, FieldDescriptorProto> fieldIdMap = [];
        internal readonly Dictionary<string, FieldDescriptorProto> fieldNameMap = [];
        internal DescriptorProto descriptor;
        private MessageDefinition()
        {

        }
        public static Builder NewBuilder(string msgTypeName)
        {
            return new Builder(msgTypeName);
        }
        public class Builder
        {
            private DescriptorProto proto;
            private MessageDefinition definition;

            internal Builder(string msgTypeName)
            {
                definition = new MessageDefinition();
                proto = new DescriptorProto();
                proto.Name = msgTypeName;
            }
            public Builder AddField(string label, string type, string name, int num, string defaultValue = null)
            {
                FieldDescriptorProto.Types.Type fieldType = GetTypeByName(type);
                FieldDescriptorProto.Types.Label fieldLabel = GetLabel(label);
                FieldDescriptorProto field = new FieldDescriptorProto()
                {
                    Name = name,
                    Type = fieldType,
                    Label = fieldLabel,
                    Number = num
                };
                if (!string.IsNullOrWhiteSpace(defaultValue))
                {
                    field.DefaultValue = defaultValue;
                }
                proto.Field.Add(field);
                definition.fieldIdMap.TryAdd(num, field);
                definition.fieldNameMap.TryAdd(name, field);
                return this;
            }
            public MessageDefinition Build()
            {
                definition.descriptor = proto;
                return definition;
            }
        }
        public static FieldDescriptorProto.Types.Type GetTypeByName(string typeName)
        {
            Trace.Assert(!string.IsNullOrWhiteSpace(typeName), "");
            string typeLower = typeName.ToLower();
            return typeLower switch
            {
                "double" => FieldDescriptorProto.Types.Type.Double,
                "float" => FieldDescriptorProto.Types.Type.Float,
                "int32" => FieldDescriptorProto.Types.Type.Int32,
                "int64" => FieldDescriptorProto.Types.Type.Int64,
                "uint32" => FieldDescriptorProto.Types.Type.Uint32,
                "uint64" => FieldDescriptorProto.Types.Type.Uint64,
                "sint32" => FieldDescriptorProto.Types.Type.Sint32,
                "sint64" => FieldDescriptorProto.Types.Type.Sint64,
                "fixed32" => FieldDescriptorProto.Types.Type.Fixed32,
                "fixed64" => FieldDescriptorProto.Types.Type.Fixed64,
                "sfixed32" => FieldDescriptorProto.Types.Type.Sfixed32,
                "sfixed64" => FieldDescriptorProto.Types.Type.Sfixed64,
                "bool" => FieldDescriptorProto.Types.Type.Bool,
                "string" => FieldDescriptorProto.Types.Type.String,
                "bytes" => FieldDescriptorProto.Types.Type.Bytes,
                _ => throw new NotSupportedException()
            };
        }
        public static FieldDescriptorProto.Types.Label GetLabel(string label)
        {
            Trace.Assert(!string.IsNullOrWhiteSpace(label), "");
            string labelLower = label.ToLower();
            return labelLower switch
            {
                "optional" => FieldDescriptorProto.Types.Label.Optional,
                "required" => FieldDescriptorProto.Types.Label.Required,
                "repeated" => FieldDescriptorProto.Types.Label.Repeated,
                _ => throw new NotSupportedException()
            };
        }
    }
}
