using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Reflection;

namespace Frameset.Common.Protobuf.Utils;

public class DynamicMessage : IMessage
{
    private MessageDefinition definition;
    private ConstructorInfo constructorInfo = typeof(MessageDescriptor).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(DescriptorProto), typeof(FileDescriptor), typeof(int), typeof(GeneratedClrTypeInfo)]);
    public Dictionary<string, object> DataContent
    {
        get; set;
    } = [];

    public MessageDescriptor Descriptor => GetMessage();

    public DynamicMessage(MessageDefinition definition)
    {
        this.definition = definition;
    }
    private MessageDescriptor GetMessage()
    {
        MessageDescriptor messageDescriptor = (MessageDescriptor)constructorInfo.Invoke([definition.descriptor, null, 0, null]);
        return messageDescriptor;

    }
    public void SetField(int num, object value)
    {
        if (definition.fieldIdMap.TryGetValue(num, out FieldDescriptorProto? field))
        {
            DataContent.TryAdd(field.Name, value);
        }
    }
    public void SetField(string fieldName, object value)
    {
        if (definition.fieldNameMap.TryGetValue(fieldName, out _))
        {
            DataContent.TryAdd(fieldName, value);
        }
    }
    public void Clear()
    {
        DataContent.Clear();
    }

    private void WriteObject(CodedOutputStream codedOutput, FieldDescriptorProto fieldDescriptor, object value)
    {
        switch (fieldDescriptor.Type)
        {
            case FieldDescriptorProto.Types.Type.Float:
                float val = Convert.ToSingle(value);
                codedOutput.WriteFloat(val);
                break;
            case FieldDescriptorProto.Types.Type.Int32:
            case FieldDescriptorProto.Types.Type.Sint32:
                int intval = Convert.ToInt32(value);
                codedOutput.WriteInt32(intval);
                break;
            case FieldDescriptorProto.Types.Type.Int64:
            case FieldDescriptorProto.Types.Type.Sint64:
                long longval = Convert.ToInt64(value);
                codedOutput.WriteInt64(longval);
                break;
            case FieldDescriptorProto.Types.Type.Fixed64:
                long fixlongval = Convert.ToInt64(value);
                codedOutput.WriteSFixed64(fixlongval);
                break;
            case FieldDescriptorProto.Types.Type.Fixed32:
                int fixintval = Convert.ToInt32(value);
                codedOutput.WriteSFixed32(fixintval);
                break;
            case FieldDescriptorProto.Types.Type.Bool:
                bool boolval = Convert.ToBoolean(value);
                codedOutput.WriteBool(boolval);
                break;
            case FieldDescriptorProto.Types.Type.String:
                string strVal = Convert.ToString(value);
                codedOutput.WriteString(strVal);
                break;
            case FieldDescriptorProto.Types.Type.Double:
                double dVal = Convert.ToDouble(value);
                codedOutput.WriteDouble(dVal);
                break;
            case FieldDescriptorProto.Types.Type.Bytes:
                if (!(value is byte[]))
                {
                    throw new ArgumentException("{0} should be a byte[]", fieldDescriptor.Name);
                }
                codedOutput.WriteBytes(ByteString.CopyFrom((byte[])value));
                break;
            case FieldDescriptorProto.Types.Type.Message:
                codedOutput.WriteRawMessage((IMessage)value);
                break;
            default:
                break;
        }
    }
    private bool ReadObject(CodedInputStream codedInput, FieldDescriptorProto fieldDescriptor)
    {
        object value = null!;
        if (codedInput.IsAtEnd)
        {
            return false;
        }
        switch (fieldDescriptor.Type)
        {
            case FieldDescriptorProto.Types.Type.Float:
                value = codedInput.ReadFloat();

                break;
            case FieldDescriptorProto.Types.Type.Int32:
            case FieldDescriptorProto.Types.Type.Sint32:
                value = codedInput.ReadInt32();
                break;
            case FieldDescriptorProto.Types.Type.Int64:
            case FieldDescriptorProto.Types.Type.Sint64:
                value = codedInput.ReadInt64();
                break;
            case FieldDescriptorProto.Types.Type.Fixed64:
                value = codedInput.ReadSFixed64();

                break;
            case FieldDescriptorProto.Types.Type.Fixed32:
                value = codedInput.ReadSFixed32();
                break;
            case FieldDescriptorProto.Types.Type.Bool:
                value = codedInput.ReadBool();
                break;
            case FieldDescriptorProto.Types.Type.String:
                value = codedInput.ReadString();
                break;
            case FieldDescriptorProto.Types.Type.Bytes:

                value = codedInput.ReadBytes();
                break;
            case FieldDescriptorProto.Types.Type.Double:
                value = codedInput.ReadDouble();
                break;
            case FieldDescriptorProto.Types.Type.Message:
                //TODO
                codedInput.ReadRawMessage((IMessage)value);
                break;
            default:
                break;
        }
        DataContent.TryAdd(fieldDescriptor.Name, value);
        return true;
    }
    private int CalculateSize(FieldDescriptorProto fieldDescriptor, object value)
    {
        int calculateSize = 0;
        switch (fieldDescriptor.Type)
        {
            case FieldDescriptorProto.Types.Type.Float:
                float val = Convert.ToSingle(value);
                calculateSize = CodedOutputStream.ComputeFloatSize(val);
                break;
            case FieldDescriptorProto.Types.Type.Int32:
            case FieldDescriptorProto.Types.Type.Sint32:
                int intval = Convert.ToInt32(value);
                calculateSize = CodedOutputStream.ComputeInt32Size(intval);
                break;
            case FieldDescriptorProto.Types.Type.Int64:
            case FieldDescriptorProto.Types.Type.Sint64:
                long longval = Convert.ToInt64(value);
                calculateSize = CodedOutputStream.ComputeInt64Size(longval);
                break;
            case FieldDescriptorProto.Types.Type.Fixed64:
                ulong fixlongval = Convert.ToUInt64(value);
                calculateSize = CodedOutputStream.ComputeFixed64Size(fixlongval);
                break;
            case FieldDescriptorProto.Types.Type.Fixed32:
                uint fixintval = Convert.ToUInt32(value);
                calculateSize = CodedOutputStream.ComputeFixed32Size(fixintval);
                break;
            case FieldDescriptorProto.Types.Type.Bool:
                bool boolval = Convert.ToBoolean(value);
                calculateSize = CodedOutputStream.ComputeBoolSize(boolval);
                break;
            case FieldDescriptorProto.Types.Type.String:
                string strVal = Convert.ToString(value);
                calculateSize = CodedOutputStream.ComputeStringSize(strVal);
                break;
            case FieldDescriptorProto.Types.Type.Bytes:
                if (!(value is byte[]))
                {
                    throw new ArgumentException("{0} should be a byte[]", fieldDescriptor.Name);
                }
                calculateSize = CodedOutputStream.ComputeBytesSize(ByteString.CopyFrom((byte[])value));
                break;
            case FieldDescriptorProto.Types.Type.Message:
                //TODO
                calculateSize = ((IMessage)value).CalculateSize();
                break;
            case FieldDescriptorProto.Types.Type.Double:
                double dVal = Convert.ToDouble(value);
                calculateSize = CodedOutputStream.ComputeDoubleSize(dVal);
                break;
            default:
                break;
        }
        return calculateSize;
    }

    public bool MergeDelimitedFrom(Stream inputStream)
    {
        DataContent.Clear();
        try
        {
            MessageExtensions.MergeDelimitedFrom(this, inputStream);
        }
        catch (Exception e)
        {
            return false;
        }
        return true;
    }



    public void MergeFrom(CodedInputStream input)
    {
        DataContent.Clear();
        foreach (FieldDescriptorProto proto in definition.descriptor.Field)
        {
            if (!ReadObject(input, proto))
            {
                break;
            }
        }

    }
    public bool ReadFrom(CodedInputStream input)
    {
        DataContent.Clear();
        foreach (FieldDescriptorProto proto in definition.descriptor.Field)
        {

            if (!ReadObject(input, proto))
            {
                return false;
            }
        }
        return true;
    }

    public void WriteTo(CodedOutputStream output)
    {
        foreach (FieldDescriptorProto proto in definition.descriptor.Field)
        {
            if (DataContent.TryGetValue(proto.Name, out object? value) && value != null)
            {
                WriteObject(output, proto, value);
            }
            else
            {
                throw new ArgumentException("field {0} is Null", proto.Name);
            }
        }
    }

    public int CalculateSize()
    {
        int totalCount = 0;
        foreach (FieldDescriptorProto proto in definition.descriptor.Field)
        {
            if (DataContent.TryGetValue(proto.Name, out object value) && value != null)
            {
                totalCount += CalculateSize(proto, value);
            }
            else
            {
                throw new ArgumentException("field {0} is Null", proto.Name);
            }
        }
        return totalCount;
    }
}
