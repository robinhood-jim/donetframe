using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Frameset.Core.Dao.Utils
{

    public static class EntityReflectUtils
    {
        private static readonly Dictionary<Type, EntityContent> entityContentMap = [];
        private static readonly Dictionary<Type, IList<FieldContent>> fieldsListMap = [];
        private static readonly Dictionary<Type, FieldContent> pkFieldMap = [];

        public static EntityContent GetEntityInfo(Type entityType)
        {
            AssertUtils.IsTrue(entityType.IsSubclassOf(typeof(BaseEntity)));
            EntityContent content = null;
            if (!entityContentMap.TryGetValue(entityType, out content))
            {
                MappingEntityAttribute entity = (MappingEntityAttribute)entityType.GetCustomAttribute(typeof(MappingEntityAttribute));
                if (entity != null)
                {
                    string tableName = entity.TableName;
                    string schema = entity.Schema != null ? entity.Schema : null;
                    string dsName = entity.DsName;
                    content = new EntityContent(entityType, tableName, schema, dsName);
                    content.IfExplicit = entity.IfExplicit;
                }
                else
                {
                    TableAttribute attribute = (TableAttribute)entityType.GetCustomAttribute(typeof(TableAttribute));
                    if (attribute != null)
                    {
                        content = new EntityContent(entityType, attribute.Name, attribute.Schema, null);
                    }
                }

                if (content != null)
                {
                    entityContentMap.Add(entityType, content);
                }
            }
            return content;
        }
        /**
         * Support MappingField and EF Core  Column Define
         */
        public static IList<FieldContent> GetFieldsContent(Type entityType)
        {
            AssertUtils.IsTrue(entityType.IsSubclassOf(typeof(BaseEntity)));
            bool extendBaseModel = entityType.IsSubclassOf(typeof(BaseModel));
            IList<FieldContent> fields = null;
            EntityContent entityContent = GetEntityInfo(entityType);
            if (!fieldsListMap.TryGetValue(entityType, out fields))
            {
                fields = new List<FieldContent>();
                PropertyInfo[] propertyInfos = entityType.GetProperties();
                foreach (PropertyInfo prop in propertyInfos)
                {
                    object[] attributes = prop.GetCustomAttributes(false);
                    string propName = prop.Name;
                 
                    if (attributes.Length > 0)
                    {
                        FieldBuilder builder = new FieldBuilder();
                        builder.Property(prop);
                        for (int i = 0; i < attributes.Length; i++)
                        {

                            if (attributes[i].GetType().Equals(typeof(MappingFieldAttribute)))
                            {
                                MappingFieldAttribute mappingField = attributes[i] as MappingFieldAttribute;
                                if (mappingField.Exist)
                                {
                                    Constants.MetaType dataType = AdjustType(prop.PropertyType);
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
                                    builder.Required(mappingField.IfRequired).DataType(mappingField.DataType).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);

                                }
                                break;
                            }
                            else if (attributes[i].GetType().Equals(typeof(ColumnAttribute)))
                            {
                                ColumnAttribute a = attributes[i] as ColumnAttribute;
                                builder.PropertyName(propName).FieldName(!string.IsNullOrWhiteSpace(a.Name) ? a.Name : Frameset.Core.Utils.StringUtils.CamelCaseLowConvert(propName)).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);

                            }
                            else if (attributes[i].GetType().Equals(typeof(DatabaseGeneratedAttribute)))
                            {
                                DatabaseGeneratedAttribute a = attributes[i] as DatabaseGeneratedAttribute;
                                if (a.DatabaseGeneratedOption.Equals(DatabaseGeneratedOption.Identity))
                                {
                                    builder.Increment(true);
                                }
                                else if (a.DatabaseGeneratedOption.Equals(DatabaseGeneratedOption.Computed))
                                {

                                }
                            }
                            else if (attributes[i].GetType().Equals(typeof(RequiredAttribute)))
                            {
                                builder.Required(true);
                            }
                            else if (attributes[i].GetType().Equals(typeof(KeyAttribute)))
                            {
                                builder.Primary(true);
                            }
                            else if (attributes[i].GetType().Equals(typeof(NotMappedAttribute)))
                            {
                                builder.NotMapped();
                            }
                            else if (attributes[i].GetType().Equals(typeof(ManyToOneAttribute)))
                            {
                                ManyToOneAttribute attr = (ManyToOneAttribute)attributes[i];
                                builder.ManyToOne(attr.ParentType);
                                entityContent.ParentEntitys.Add(attr.ParentType);
                            }
                        }
                        if (builder.Acceptable())
                        {
                            fields.Add(builder.Build());
                        }
                        else if (!entityContent.IfExplicit)
                        {
                            builder.PropertyName(propName).FieldName(Core.Utils.StringUtils.CamelCaseLowConvert(propName)).DataType(AdjustType(prop.PropertyType)).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);
                            fields.Add(builder.Build());
                        }
                    }
                    else if (!entityContent.IfExplicit)
                    {
                        FieldBuilder builder = new FieldBuilder();
                        builder.PropertyName(propName).FieldName(Core.Utils.StringUtils.CamelCaseLowConvert(propName)).DataType(AdjustType(prop.PropertyType)).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);

                        fields.Add(builder.Build());
                    }

                }

                if (!CollectionUtils.IsEmpty(fields))
                {
                    fieldsListMap.Add(entityType, fields);
                }
            }
            
            return fields;
        }
        public static Dictionary<string, FieldContent> GetFieldsMap(Type entityType)
        {
            AssertUtils.IsTrue(entityType.IsSubclassOf(typeof(BaseEntity)));
            IList<FieldContent> fields = GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
            {
                return fields.GroupBy(p => p.PropertyName).ToDictionary(k => k.Key, v => v.First());
            }
            else
            {
                return [];
            }

        }
        public static FieldContent GetPriamryKey(Type entityType)
        {
            AssertUtils.IsTrue(entityType.IsSubclassOf(typeof(BaseEntity)));
            IList<FieldContent> fields = GetFieldsContent(entityType);
            if (!pkFieldMap.TryGetValue(entityType, out FieldContent? content))
            {
                if (!fields.IsNullOrEmpty())
                {
                    content = fields.First(x => x.IfPrimary);
                    if (content != null)
                    {
                        pkFieldMap.TryAdd(entityType, content);
                    }
                }
            }
            return content;
        }


        public static Constants.MetaType AdjustType(Type type)
        {
            Constants.MetaType dataType = Constants.MetaType.INTEGER;
            if (type.Equals(typeof(int)))
            {
                dataType = Constants.MetaType.INTEGER;
            }
            else if (type.Equals(typeof(short)))
            {
                dataType = Constants.MetaType.SHORT;
            }
            else if (type.Equals(typeof(float)))
            {
                dataType = Constants.MetaType.FLOAT;
            }
            else if (type.Equals(typeof(double)))
            {
                dataType = Constants.MetaType.DOUBLE;
            }
            else if (type.Equals(typeof(DateTime)))
            {
                dataType = Constants.MetaType.DATE;
            }
            else if (type.Equals(typeof(DateTimeOffset)))
            {
                dataType = Constants.MetaType.TIMESTAMP;
            }
            else if (type.Equals(typeof(byte[])))
            {
                dataType = Constants.MetaType.BLOB;
            }
            else if (type.Equals(typeof(string)))
            {
                dataType = Constants.MetaType.STRING;
            }
            return dataType;

        }

    }
}
