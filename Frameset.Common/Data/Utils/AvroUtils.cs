using Avro;
using Frameset.Common.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;

namespace Frameset.Common.Data.Utils
{
    public class AvroUtils
    {
        internal AvroUtils()
        {

        }
        private static readonly ConstructorInfo? logicConstructor = typeof(LogicalSchema).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(Schema), typeof(string), typeof(PropertyMap) });
        public static RecordSchema GetSchema(DataCollectionDefine define, string? className = null)
        {
            RecordSchema schema = null!;
            string classNameStr = className ?? "com.robin.test";

            List<Field> fields = new();
            if (!define.ColumnList.IsNullOrEmpty())
            {
                int pos = 0;

                foreach (DataSetColumnMeta meta in define.ColumnList)
                {
                    Schema? baseSchema = null;
                    switch (meta.ColumnType)
                    {
                        case Constants.MetaType.INTEGER:
                            baseSchema = PrimitiveSchema.Create(Schema.Type.Int, new PropertyMap());
                            break;
                        case Constants.MetaType.BIGINT:
                            baseSchema = PrimitiveSchema.Create(Schema.Type.Long, new PropertyMap());
                            break;
                        case Constants.MetaType.SHORT:
                            baseSchema = PrimitiveSchema.Create(Schema.Type.Int, new PropertyMap());
                            break;
                        case Constants.MetaType.DOUBLE:
                            baseSchema = PrimitiveSchema.Create(Schema.Type.Double, new PropertyMap());
                            break;
                        case Constants.MetaType.FLOAT:
                            baseSchema = PrimitiveSchema.Create(Schema.Type.Float, new PropertyMap());
                            break;
                        case Constants.MetaType.TIMESTAMP:
                            Schema originSchema = PrimitiveSchema.Create(Schema.Type.Long, new PropertyMap());
                            LogicalSchema logical = (LogicalSchema)logicConstructor?.Invoke(new object[] { originSchema, "timestamp-millis", new PropertyMap() });
                            baseSchema = logical;
                            break;
                        case Constants.MetaType.STRING:
                            baseSchema = PrimitiveSchema.Create(Schema.Type.String, new PropertyMap());
                            break;

                    }
                    Field field = new Field(baseSchema, meta.ColumnCode, pos);
                    fields.Add(field);
                    pos++;

                }
                schema = RecordSchema.Create(classNameStr, fields);
            }
            else
            {
                throw new OperationFailedException("ColumnList Missing");
            }
            return schema;
        }
        public static RecordSchema GetSchema(Type type)
        {
            string? classNameStr = type.FullName;
            var attributeEnum = type.GetCustomAttributes(typeof(ProtoNumberAttribute));
            PropertyInfo[] infos = type.GetProperties();
            if (infos.IsNullOrEmpty())
            {
                throw new OperationFailedException("PropertyInfo is null");
            }
            List<Field> fields = new();
            if (!attributeEnum.IsNullOrEmpty())
            {
                foreach (PropertyInfo info in infos)
                {
                    Attribute? sourceAttribute = info.GetCustomAttribute(typeof(ProtoNumberAttribute));
                    if (sourceAttribute != null)
                    {
                        ProtoNumberAttribute attribute = (ProtoNumberAttribute)sourceAttribute;
                        Schema baseSchema = GetBaseSchema(info);
                        Field field = new Field(baseSchema, info?.Name, attribute.Number);
                        fields.Add(field);
                    }
                }
                return RecordSchema.Create(classNameStr, fields);
            }
            else
            {
                int pos = 0;
                foreach (PropertyInfo info in infos)
                {
                    Schema baseSchema = GetBaseSchema(info);
                    Field field = new Field(baseSchema, info?.Name, pos);
                    fields.Add(field);
                    pos++;
                }
                return RecordSchema.Create(classNameStr, fields);
            }

        }

        private static Schema GetBaseSchema(PropertyInfo info)
        {
            return Type.GetTypeCode(info?.GetMethod?.ReturnType) switch
            {
                TypeCode.Int32 => PrimitiveSchema.Create(Schema.Type.Int, new PropertyMap()),
                TypeCode.Int64 => PrimitiveSchema.Create(Schema.Type.Long, new PropertyMap()),
                TypeCode.Int16 => PrimitiveSchema.Create(Schema.Type.Int, new PropertyMap()),
                TypeCode.Double => PrimitiveSchema.Create(Schema.Type.Double, new PropertyMap()),
                TypeCode.DateTime => GetDateTimeFormat(),
                _ => PrimitiveSchema.Create(Schema.Type.String, new PropertyMap())

            };
        }

        private static Schema GetDateTimeFormat()
        {
            Schema originSchema = PrimitiveSchema.Create(Schema.Type.Long, new PropertyMap());
            return (LogicalSchema)logicConstructor.Invoke(new object[] { originSchema, "timestamp-millis", new PropertyMap() });
        }


    }
}
