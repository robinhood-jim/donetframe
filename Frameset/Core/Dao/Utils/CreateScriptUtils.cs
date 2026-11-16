using Frameset.Core.Annotation;
using Frameset.Core.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Frameset.Core.Dao.Utils
{
    public class CreateScriptUtils
    {
        public static string GenerateScript(IAdoDao dao)
        {
            List<Type> scanTypes = ClassUtils.ScanTypesWithAttribute(typeof(MappingEntityAttribute), (t) => t.GetCustomAttributes(typeof(MappingEntityAttribute), false).Length > 0 || t.GetCustomAttributes(typeof(TableAttribute), false).Length > 0);
            StringBuilder builder = new();
            foreach (Type type in scanTypes)
            {
                EntityContent entityContent = EntityReflectUtils.GetEntityInfo(type);
                List<FieldContent> fieldContents = EntityReflectUtils.GetFieldsContent(type);
                builder.Append(GenerateDDM(dao, entityContent, fieldContents));
                builder.Append(";\n");
            }
            return builder.ToString();

        }
        public static string GenerateDDM(IAdoDao dao, EntityContent content, List<FieldContent> fields)
        {
            StringBuilder builder = new();
            builder.Append("CREATE TABLE ").Append(content.GetTableName()).Append("(");
            foreach (FieldContent fieldContent in fields)
            {
                builder.Append("\t").Append(dao.GetDialect().GenerateFieldDefine(fieldContent)).Append(",\n");
            }
            builder.Remove(builder.Length - 2, 2);
            builder.Append(')');
            return builder.ToString();
        }

    }
}
