using Google.Protobuf.Reflection;
using ProtoBuf;

namespace Frameset.Common.Protobuf.Utils
{
    public class DynamicSchema
    {
        private DescriptorProto fileDescriptor;
        private MessageDefinition messageDefinition;
        private ProtoWriter protoWriter;
        private ProtoReader protoReader;
        private DynamicSchema(MessageDefinition definition)
        {
            messageDefinition = definition;
            this.fileDescriptor = definition.descriptor;
            //Dictionary<string, FieldDescriptor> fieldDescMap = parse(fileDescriptor);

        }

        public DescriptorProto GetDescriptorProto()
        {
            return fileDescriptor;
        }

        /*private Dictionary<string,FieldDescriptor> parse(FileDescriptorSet fileDescriptorSet)
        {
            HashSet<string> protoNames = new();
            foreach(FileDescriptorProto proto in fileDescriptorSet.File)
            {
                if (protoNames.Contains(proto.Name))
                {
                    throw new MethodNotSupportedException("");
                }
                protoNames.Add(proto.Name);
                
            }
            Dictionary<string, FileDescriptor> resovledDescMap = [];
            while (resovledDescMap.Count < fileDescriptor.File.Count)
            {
                foreach(FileDescriptorProto proto1 in fileDescriptor.File)
                {
                    if (resovledDescMap.ContainsKey(proto1.Name))
                    {
                        continue;
                    }
                    RepeatedField<string> dependecies= proto1.Dependency;
                    List<FileDescriptor> resovledFdList = [];
                    foreach(string depName in dependecies)
                    {
                        if (!protoNames.Contains(depName))
                        {
                            throw new MethodNotSupportedException("");
                        }
                        resovledDescMap.TryGetValue(depName, out FileDescriptor fd);
                        if (fd != null)
                        {
                            resovledFdList.Add(fd);
                        }
                    }
                    if (resovledFdList.Count == dependecies.Count)
                    {
                        FileDescriptor[] fds = new FileDescriptor[resovledFdList.Count];
                        FileDescriptor descriptor = FileDescriptor.FromGeneratedCode(null, resovledFdList);

                    }
                }
            }

        }*/

        public class Builder
        {
            private FileDescriptorSet fileDescriptor;
            internal readonly string protoName;
            internal readonly string packageName;
            internal MessageDefinition messageDefinition;
            internal DescriptorProto descriptor;


            public static Builder NewBuilder(string protoName, string packageName)
            {
                return new Builder(protoName, packageName);
            }
            internal Builder(string protoName, string packageName)
            {
                this.packageName = packageName;
                this.protoName = protoName;
                fileDescriptor = new FileDescriptorSet();
            }
            public Builder AddMessageDefinition(MessageDefinition definition)
            {
                this.messageDefinition = messageDefinition;
                this.descriptor = messageDefinition.descriptor;

                return this;
            }
            public Builder AddMessageDefinition(FileDescriptorProto msgDefine, string name, string packageName)
            {
                msgDefine.Name = name;
                msgDefine.Package = packageName;
                fileDescriptor.File.Add(msgDefine);
                return this;
            }
            public DynamicSchema Build()
            {
                return new DynamicSchema(messageDefinition);
            }

        }
    }
}
