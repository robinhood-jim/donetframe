using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Frameset.Core.Utils
{
    public static class ModelSqlGenerator
    {
        public static void GenerateSql(Constants.DbType dbType, AbstractSqlDialect dialect, Stream stream, Dictionary<string, object> additionalCfgMap = null)
        {
            List<Type> allTypes = ScanModelTypes();
            using (StreamWriter writer = new StreamWriter(stream))
            {
                foreach (Type type in allTypes)
                {
                    EntityContent content = EntityReflectUtils.GetEntityInfo(type);
                    if (type != null)
                    {
                        IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(type);
                        if (!fields.IsNullOrEmpty())
                        {
                            writer.WriteLine(GenerateCreatSql(content, fields, dialect, additionalCfgMap));
                        }
                    }
                }
            }
        }
        public static List<Type> ScanModelTypes()
        {
            List<Type> retList = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    retList.AddRange(assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && (t.GetCustomAttributes(typeof(MappingEntityAttribute), false).Length > 0 || t.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute), false).Length > 0))
                                .ToList());
                }
                catch (Exception ex)
                {
                    Log.Error("{Message}", ex.Message);
                }
            }
            return retList;

        }
        internal static string GenerateCreatSql(EntityContent entityContent, IList<FieldContent> fields, AbstractSqlDialect dialect, Dictionary<string, object> additionalCfgMap)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("CREATE TABLE ").Append(entityContent.GetTableName()).Append("(\n");
            foreach (FieldContent field in fields)
            {
                builder.Append(dialect.GetFieldDefineScript(field)).Append(",\n");
            }
            builder.Remove(builder.Length - 2, 2).Append(");\n");
            if (!additionalCfgMap.IsNullOrEmpty())
            {
                dialect.AppendAdditionalScript(builder, additionalCfgMap);
            }
            return builder.ToString();

        }
    }
}
