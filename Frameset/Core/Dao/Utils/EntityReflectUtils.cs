using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Model;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Frameset.Core.Dao.Utils
{

    public static class EntityReflectUtils
    {
        private static readonly Dictionary<Type, EntityContent> entityContentMap = [];
        private static readonly Dictionary<Type, List<FieldContent>> fieldsListMap = [];
        private static readonly Dictionary<Type, Dictionary<Type, string>> relationMap = [];
        private static readonly Dictionary<Type, FieldContent> pkFieldMap = [];
        private static readonly Dictionary<Type, TableLogicColumn> logicMap = [];


        public static EntityContent GetEntityInfo(Type entityType)
        {
            Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)));

            if (!entityContentMap.TryGetValue(entityType, out EntityContent? content))
            {
                if (entityType.GetCustomAttribute(typeof(MappingEntityAttribute)) is MappingEntityAttribute entity)
                {
                    string tableName = entity.TableName;
                    string schema = entity.Schema;
                    string dsName = entity.DsName;
                    content = new(entityType, tableName, schema, dsName);
                    content.IfExplicit = entity.IfExplicit;
                }
                else if (entityType.GetCustomAttribute(typeof(TableAttribute)) is TableAttribute attribute)
                {
                    content = new(entityType, attribute.Name, attribute.Schema, null);
                }

                if (content != null)
                {
                    entityContentMap.Add(entityType, content);
                }
            }
            Trace.Assert(content != null);
            return content;
        }
        /**
         * Support MappingField and EF Core  Column Define
         */
        public static List<FieldContent> GetFieldsContent(Type entityType)
        {
            Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)));

            EntityContent entityContent = GetEntityInfo(entityType);
            if (!fieldsListMap.TryGetValue(entityType, out List<FieldContent>? fields))
            {
                fields = [];
                PropertyInfo[] propertyInfos = entityType.GetProperties();
                foreach (PropertyInfo prop in propertyInfos)
                {
                    object[] attributes = prop.GetCustomAttributes(false);
                    string propName = prop.Name;
                    string fieldName = Core.Utils.StringUtils.CamelCaseLowConvert(propName);
                    Constants.MetaType dataType = Constants.MetaTypeOfType(prop.PropertyType);
                    FieldBuilder builder = new(entityType);
                    builder.Property(prop);
                    
                    if (attributes.Length > 0)
                    {
                        for (int i = 0; i < attributes.Length; i++)
                        {
                            if (attributes[i].GetType() == typeof(MappingFieldAttribute))
                            {
                                MappingFieldAttribute? mappingField = attributes[i] as MappingFieldAttribute;
                                Trace.Assert(mappingField != null);
                                if (mappingField.Exist)
                                {
                                    if (!Constants.MetaType.NONE.Equals(mappingField.DataType))
                                    {
                                        dataType = mappingField.DataType;
                                    }
                                    builder.PropertyName(propName).FieldName(!string.IsNullOrWhiteSpace(mappingField.Field) ? mappingField.Field : Frameset.Core.Utils.StringUtils.CamelCaseLowConvert(propName));
                                    if (mappingField.IfPrimary)
                                    {
                                        builder.Primary(true);
                                    }
                                    if (!string.IsNullOrWhiteSpace(mappingField.SequenceName))
                                    {
                                        builder.SequenceName(mappingField.SequenceName);
                                    }
                                    if (mappingField.IfIncrement)
                                    {
                                        builder.Increment(true);
                                    }
                                    Trace.Assert(prop.GetMethod != null && prop.SetMethod != null);
                                    builder.Required(mappingField.IfRequired).DataType(dataType);

                                }
                            }
                            else if (attributes[i].GetType() == typeof(ColumnAttribute))
                            {
                                ColumnAttribute? a = attributes[i] as ColumnAttribute;
                                Trace.Assert(a != null);
                                fieldName = !string.IsNullOrWhiteSpace(a.Name) ? a.Name : Core.Utils.StringUtils.CamelCaseLowConvert(propName);
                                Trace.Assert(prop.DeclaringType != null && prop.GetMethod != null && prop.SetMethod != null);
                                builder.PropertyName(propName).FieldName(fieldName).DataType(Constants.MetaTypeOfType(prop.DeclaringType));

                            }
                            else if (attributes[i].GetType() == typeof(DatabaseGeneratedAttribute))
                            {
                                DatabaseGeneratedAttribute? a = attributes[i] as DatabaseGeneratedAttribute;
                                Trace.Assert(a != null);
                                if (a.DatabaseGeneratedOption.Equals(DatabaseGeneratedOption.Identity))
                                {
                                    builder.Increment(true);
                                }
                                else if (a.DatabaseGeneratedOption.Equals(DatabaseGeneratedOption.Computed))
                                {
                                    builder.Computed();
                                }
                            }
                            else if (attributes[i].GetType() == typeof(RequiredAttribute))
                            {
                                builder.Required(true);
                            }
                            else if (attributes[i].GetType() == typeof(KeyAttribute))
                            {
                                builder.Primary(true);
                            }
                            else if (attributes[i].GetType() == typeof(NotMappedAttribute))
                            {
                                builder.NotMapped();
                            }
                            else if (attributes[i].GetType() == typeof(ManyToOneAttribute))
                            {
                                ManyToOneAttribute attr = (ManyToOneAttribute)attributes[i];
                                builder.ManyToOne(attr);
                                entityContent.ParentEntitys.Add(attr.ParentType);
                            }
                            else if (attributes[i].GetType() == typeof(OneToManyAttribute))
                            {
                                OneToManyAttribute attr = (OneToManyAttribute)attributes[i];
                                builder.OneToMany(attr);
                            }
                            else if (attributes[i].GetType() == typeof(LogicColumnAttribute))
                            {
                                //support Custom Logic column defined
                                LogicColumnAttribute attr = (LogicColumnAttribute)attributes[i];
                                builder.LogicColumn(attr);
                            }
                            else if (attributes[i].GetType() == typeof(ColumnFormulaAttribute))
                            {
                                ColumnFormulaAttribute attr = (ColumnFormulaAttribute)attributes[i];
                                builder.Formula(attr.Value);
                            }
                            builder.DataType(dataType);
                        }
                        
                        if (builder.Acceptable())
                        {
                            builder.Getter(ReflectionUtils.GetGetter(entityType, propName)).Setter(ReflectionUtils.GetSetter(entityType, propName));
                            fields.Add(builder.Build());
                        }
                     
                    }
                    else if (!entityContent.IfExplicit)
                    {
                        builder.Getter(ReflectionUtils.GetGetter(entityType, propName)).Setter(ReflectionUtils.GetSetter(entityType, propName));
                        builder.PropertyName(propName).DataType(dataType).FieldName(Core.Utils.StringUtils.CamelCaseLowConvert(propName)).DataType(Constants.MetaTypeOfType(prop.PropertyType));
                        fields.Add(builder.Build());
                    }
                   
                }

                if (!fields.IsNullOrEmpty())
                {
                    fieldsListMap.Add(entityType, fields);
                }
            }

            return fields;
        }


        public static Dictionary<string, FieldContent> GetFieldsMap(Type entityType)
        {
            Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)));
            List<FieldContent> fields = GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
            {
                return fields.GroupBy(p => p.PropertyName).ToDictionary(k => k.Key, v => v.First());
            }
            else
            {
                return [];
            }

        }
        public static FieldContent GetPrimaryKey(Type entityType)
        {
            Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)));
            List<FieldContent> fields = GetFieldsContent(entityType);
            if (!pkFieldMap.TryGetValue(entityType, out FieldContent? content))
            {
                if (!fields.IsNullOrEmpty())
                {
                    content = fields.First(x => x.IfPrimary);
                    pkFieldMap.TryAdd(entityType, content);
                }
            }
            Trace.Assert(content != null);
            return content;
        }
        /// <summary>
        /// Get LogicColumn config from JdbcDao config and LogicColumnAttribute both
        /// </summary>
        /// <param name="dao"></param>
        /// <param name="entityType"></param>
        /// <returns> if not null,return LogicColumn config</returns>
        public static TableLogicColumn? GetLogicColumnAndValue(IAdoDao dao, Type entityType)
        {
            if (!logicMap.TryGetValue(entityType, out TableLogicColumn? tableLogicColumn))
            {
                List<FieldContent> fields = GetFieldsContent(entityType);
                FieldContent? logicContent = fields.Find(x => x.IfLogicColumn);
                if (logicContent != null)
                {
                    tableLogicColumn = new TableLogicColumn()
                    {
                        FieldName = logicContent.FieldName,
                        ValidValue = logicContent.ValidValue,
                        InvalidValue = logicContent.InvalidValue
                    };
                }
                else if (!string.IsNullOrEmpty(dao.GetLogicColumn()))
                {
                    tableLogicColumn = new TableLogicColumn()
                    {
                        FieldName = dao.GetLogicColumn(),
                        ValidValue = dao.GetValidValue(),
                        InvalidValue = dao.GetInvalidValue()
                    };
                }
                if (tableLogicColumn != null)
                {
                    logicMap.TryAdd(entityType, tableLogicColumn);
                }
            }
            return tableLogicColumn;
        }


        public static Dictionary<Type, Dictionary<Type, string>> GetRelationMap()
        {
            return relationMap;
        }

    }
}
