using Frameset.Common.Annotation;
using Frameset.Common.Protobuf.Utils;
using Frameset.Core.Common;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;

namespace Frameset.Common.Data.Utils
{
    public class ProtobufUtils
    {

        public static string GetTypeStr(Constants.MetaType columnType)
        {
            return columnType switch
            {
                Constants.MetaType.LONG => "int64",
                Constants.MetaType.SHORT => "int32",
                Constants.MetaType.INTEGER => "int32",
                Constants.MetaType.FLOAT => "float",
                Constants.MetaType.DOUBLE => "double",
                Constants.MetaType.TIMESTAMP => "int64",
                Constants.MetaType.DATE => "int64",
                Constants.MetaType.BLOB => "bytes",
                Constants.MetaType.BOOLEAN => "bool",
                Constants.MetaType.STRING => "string",
                _ => "string"
            };
        }
        public static DynamicMessage ConstructDynamicMessage(Dictionary<string, MethodParam> methodMap, Type messageType)
        {
            var props = messageType.GetProperties();
            MessageDefinition.Builder builder = MessageDefinition.NewBuilder("test");
            var annotationEnum = messageType.GetCustomAttributes(typeof(ProtoNumberAttribute));
            if (!annotationEnum.IsNullOrEmpty())
            {
                foreach (PropertyInfo info in props)
                {
                    Attribute? sourceAttribute = info.GetCustomAttribute(typeof(ProtoNumberAttribute));

                    if (sourceAttribute != null)
                    {
                        ProtoNumberAttribute attribute = (ProtoNumberAttribute)sourceAttribute;
                        builder.AddField("required", Constants.GetMetaTypeProtoBufType(info.PropertyType), info.Name, attribute.Number);
                    }
                }
            }
            else
            {
                int rownum = 1;
                foreach (PropertyInfo info in props)
                {
                    builder.AddField("required", Constants.GetMetaTypeProtoBufType(info.PropertyType), info.Name, rownum);
                    rownum++;
                }
            }
            return new DynamicMessage(builder.Build());
        }
    }
}
