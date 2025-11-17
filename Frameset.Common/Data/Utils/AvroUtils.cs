using Avro;
using Frameset.Core.Common;
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
        private static readonly ConstructorInfo logicConstructor = typeof(LogicalSchema).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(Schema), typeof(string), typeof(PropertyMap) });
        public static RecordSchema GetSchema(DataCollectionDefine define, string? className = null)
        {
            RecordSchema schema = null;
            string classNameStr = className ?? "com.robin.test";

            List<Field> fields = new List<Field>();
            if (!define.ColumnList.IsNullOrEmpty())
            {
                int pos = 0;

                foreach (DataSetColumnMeta meta in define.ColumnList)
                {
                    Schema baseSchema = null;
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
                            LogicalSchema logical = (LogicalSchema)logicConstructor.Invoke(new object[] { originSchema, "timestamp-millis", new PropertyMap() });
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
            return schema;
        }

    }
}
