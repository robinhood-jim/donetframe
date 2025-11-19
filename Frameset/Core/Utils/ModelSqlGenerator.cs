using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Frameset.Core.Utils
{
    public class ModelSqlGenerator
    {
        public static void GenerateSql(Constants.DbType dbType, AbstractSqlDialect dialect, Stream stream, Dictionary<string, object> additionalCfgMap = null)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] allTypes = assembly.GetTypes();
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
        internal static string GenerateCreatSql(EntityContent entityContent, IList<FieldContent> fields, AbstractSqlDialect dialect, Dictionary<string, object> additionalCfgMap)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("CREATE TABLE ").Append(entityContent.GetTableName()).Append("(\n");
            foreach (FieldContent field in fields)
            {
                builder.Append(dialect.GetColumnDefine(field)).Append(",\n");
            }
            builder.Remove(builder.Length - 1, builder.Length).Append(")\n");
            if (!additionalCfgMap.IsNullOrEmpty())
            {
                dialect.AppendAdditionalScript(builder, additionalCfgMap);
            }
            return builder.ToString();

        }
    }
}
