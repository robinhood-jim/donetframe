using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Model;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Frameset.Core.Dao.Utils
{

    public class EntityReflectUtils
    {
        private static Dictionary<Type, EntityContent> entityContentMap = new Dictionary<Type, EntityContent>();
        private static Dictionary<Type, IList<FieldContent>> fieldsListMap = new Dictionary<Type, IList<FieldContent>>();

        public static EntityContent GetEntityInfo(Type entityType)
        {
            AssertUtils.IsTrue(entityType.IsSubclassOf(typeof(BaseEntity)));
            EntityContent content = null;
            if (!entityContentMap.TryGetValue(entityType, out content))
            {
                object[] attributes = entityType.GetCustomAttributes(false);
                if (attributes.Length > 0)
                {
                    for (int i = 0; i < attributes.Length; i++)
                    {
                        if (attributes[i].GetType().Equals(typeof(MappingEntity)))
                        {
                            MappingEntity entity = attributes[i] as MappingEntity;

                            string tableName = entity.TableName;
                            string schema = entity.Schema != null ? entity.Schema : null;
                            string dsName = entity.DsName;
                            content = new EntityContent(entityType, tableName, schema, dsName);
                            content.IfExplicit = entity.IfExplicit;
                            break;
                        }
                        else if (attributes[i].GetType().Equals(typeof(TableAttribute)))
                        {
                            //EF Core Table 
                            TableAttribute a = attributes[i] as TableAttribute;
                            content = new EntityContent(entityType, a.Name, a.Schema, null);

                        }
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
                    FieldInfo fieldInfo = entityType.GetField(propName);
                    if (attributes.Length > 0)
                    {
                        FieldBuilder builder = new FieldBuilder();
                        for (int i = 0; i < attributes.Length; i++)
                        {

                            if (attributes[i].GetType().Equals(typeof(MappingField)))
                            {
                                MappingField mappingField = attributes[i] as MappingField;
                                if (mappingField.Exist)
                                {
                                    Constants.MetaType dataType = adjustType(prop.GetType());
                                    builder.PropertyName(propName).FieldName(!string.IsNullOrWhiteSpace(mappingField.Field) ? mappingField.Field : Frameset.Core.Utils.StringUtils.camelCaseLowConvert(propName));
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
                                    builder.Required(mappingField.IfRequired).DataType(mappingField.DataType).FieldInfo(fieldInfo).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);

                                }
                                break;
                            }
                            else if (attributes[i].GetType().Equals(typeof(ColumnAttribute)))
                            {
                                ColumnAttribute a = attributes[i] as ColumnAttribute;
                                builder.PropertyName(propName).FieldName(!string.IsNullOrWhiteSpace(a.Name) ? a.Name : Frameset.Core.Utils.StringUtils.camelCaseLowConvert(propName)).FieldInfo(fieldInfo).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);

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

                        }
                        if (builder.Acceptable())
                        {
                            fields.Add(builder.Build());
                        }
                    }
                    else if (!entityContent.IfExplicit)
                    {
                        FieldBuilder builder = new FieldBuilder();
                        builder.PropertyName(propName).FieldName(Frameset.Core.Utils.StringUtils.camelCaseLowConvert(propName)).DataType(adjustType(prop.GetType())).FieldInfo(fieldInfo).GetMethod(prop.GetMethod).SetMethod(prop.SetMethod);

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
            bool extendBaseModel = entityType.IsSubclassOf(typeof(BaseModel));
            IList<FieldContent> fields = GetFieldsContent(entityType);
            if (!CollectionUtils.IsEmpty(fields))
            {
                return fields.GroupBy(p => p.PropertyName).ToDictionary(k => k.Key, v => v.First());
            }
            else
            {
                return null;
            }

        }

        public static Constants.MetaType adjustType(Type type)
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
