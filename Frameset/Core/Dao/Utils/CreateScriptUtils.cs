using Frameset.Core.Annotation;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Frameset.Core.Utils;

namespace Frameset.Core.Dao.Utils
{
    public class CreateScriptUtils
    {
        public static string GenerateScript(IAdoDao dao)
        {
            List<Type> scanTypes = ScanModelTypes();
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

        protected static List<Type> ScanModelTypes()
        {
            List<Type> retList = [];
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            Trace.Assert(entryAssembly!=null);
            AssemblyName[] assemblies = entryAssembly.GetReferencedAssemblies();
            List<AssemblyName> assemblyNames = new List<AssemblyName>(assemblies);
            assemblyNames.Add(entryAssembly.GetName());
            foreach (AssemblyName assemblyName in assemblyNames)
            {
                try
                {
                    Assembly assembly = Assembly.Load(assemblyName);
                    if (StringUtils.IsAssemblySystem(assembly))
                    {
                        continue;
                    }
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
