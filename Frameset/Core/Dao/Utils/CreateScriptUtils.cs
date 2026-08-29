using Frameset.Core.Annotation;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Frameset.Core.Dao.Utils
{
    public class CreateScriptUtils
    {
        public static string GenerateScript(IJdbcDao dao)
        {
            List<Type> scanTypes = ScanModelTypes();
            StringBuilder builder = new();
            foreach (Type type in scanTypes)
            {
                EntityContent entityContent = EntityReflectUtils.GetEntityInfo(type);
                List<FieldContent> fieldContents = (List<FieldContent>)EntityReflectUtils.GetFieldsContent(type);
                builder.Append(GenerateDDM(dao, entityContent, fieldContents));
                builder.Append(";\n");
            }
            return builder.ToString();

        }
        public static string GenerateDDM(IJdbcDao dao, EntityContent content, List<FieldContent> fields)
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

        protected static List<Type> ScanModelTypes()
        {
            List<Type> retList = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    retList.AddRange(assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && (t.GetCustomAttributes(typeof(MappingEntityAttribute), false).Length > 0 || t.GetCustomAttributes(typeof(TableAttribute), false).Length > 0))
                                .ToList());
                }
                catch (Exception ex)
                {
                    Log.Error("{Message}", ex.Message);
                }
            }
            return retList;

        }
    }
}
