using Frameset.Core.Annotation;
using Frameset.Core.Common;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Frameset.Core.Dao.Utils
{
    public class FieldContent
    {
        private string _fieldName;
        private string _propertyName;
        public Type EntityType
        {
            get; set;
        }
        public string FieldName
        {
            get => _fieldName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("fieldName must not be null!");
                }
                _fieldName = value;
            }
        }
        public string PropertyName
        {
            get => _propertyName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("propertyName must not be null!");
                }
                _propertyName = value;
            }
        }
        public Constants.MetaType DataType
        {
            get; set;

        }
        public bool Required
        {
            get; set;
        } = false;
        public FieldInfo FieldInfomation
        {
            get; set;
        }
        public PropertyInfo PropertyInfomation
        {
            get; internal set;
        }
        public MethodInfo GetMethod
        {
            get; internal set;
        }
        public MethodInfo SetMethod
        {
            get; internal set;
        }
        public Type ParamType
        {
            get; internal set;
        }
        public int Precise
        {
            get; set;
        }
        public int Scale
        {
            get; set;
        }
        public bool IfPrimary
        {
            get; internal set;
        } = false;
        public bool IfIncrement
        {
            get; internal set;
        } = false;
        public bool IfSequence
        {
            get; internal set;
        } = false;
        public string SequenceName
        {
            get; internal set;
        }
        public bool Exist
        {
            get; internal set;
        } = true;
        public int Length
        {
            get; set;
        }
        public Type ParentEntity
        {
            get; set;
        }
        public bool IsManyToOne
        {
            get; internal set;
        } = false;
        public bool IsOneToMany
        {
            get; internal set;
        } = false;
        public string RealtionColumn
        {
            get; set;
        }
        public Type SubType
        {
            get; set;
        }
        public CascadeType Cascade
        {
            get; set;
        }
    }
    public class FieldBuilder
    {
        private readonly FieldContent content = new FieldContent();
        public FieldBuilder(Type entityType)
        {
            content.EntityType = entityType;
        }
        public FieldBuilder PropertyName(string propName)
        {
            content.PropertyName = propName;
            return this;
        }
        public FieldBuilder Property(PropertyInfo info)
        {
            content.PropertyInfomation = info;
            return this;
        }
        public FieldBuilder FieldName(string fieldName)
        {
            content.FieldName = fieldName;
            return this;
        }
        public FieldBuilder DataType(Constants.MetaType dataType)
        {
            content.DataType = dataType;
            return this;
        }
        public FieldBuilder Required(bool required)
        {
            content.Required = required;
            return this;
        }
        public FieldBuilder FieldInfo(FieldInfo fieldInfo)
        {
            content.FieldInfomation = fieldInfo;
            return this;
        }
        public FieldBuilder GetMethod(MethodInfo getMethod)
        {
            content.GetMethod = getMethod;
            Type baseType = getMethod.ReturnType;
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                content.ParamType = baseType.GetGenericArguments()[0];
            }
            else
            {
                content.ParamType = baseType;
            }
            return this;
        }
        public FieldBuilder SetMethod(MethodInfo setMethod)
        {
            content.SetMethod = setMethod;
            return this;
        }
        public FieldBuilder Precise(int precise)
        {
            content.Precise = precise;
            return this;
        }
        public FieldBuilder Scale(int scale)
        {
            content.Scale = scale;
            return this;
        }
        public FieldContent Build()
        {
            return content;
        }
        public FieldBuilder Primary(bool priamry)
        {
            content.IfPrimary = priamry;
            return this;
        }
        public FieldBuilder SequenceName(string sequenceName)
        {
            content.IfSequence = true;
            content.Required = true;
            content.SequenceName = sequenceName;
            return this;
        }
        public FieldBuilder Increment(bool ifIncrement)
        {
            content.Required = ifIncrement;
            content.IfIncrement = ifIncrement;
            return this;
        }
        public FieldBuilder NotMapped()
        {
            content.Exist = false;
            return this;
        }
        public FieldBuilder ManyToOne(ManyToOneAttribute attribute)
        {
            content.IsManyToOne = true;
            content.ParentEntity = attribute.ParentType;
            content.RealtionColumn = attribute.ColumnName;
            if (EntityReflectUtils.GetRelationMap().TryGetValue(attribute.ParentType, out Dictionary<Type, string> childMap))
            {
                childMap.TryAdd(content.EntityType, attribute.ColumnName);
            }
            else
            {
                EntityReflectUtils.GetRelationMap().TryAdd(attribute.ParentType, new() { { content.EntityType, attribute.ColumnName } });
            }
            return this;
        }
        public FieldBuilder OneToMany(OneToManyAttribute attribute)
        {
            content.IsOneToMany = true;
            content.SubType = attribute.SubType;
            content.Cascade = attribute.Cascade;
            return this;
        }
        public bool Acceptable()
        {
            return content.Exist && !string.IsNullOrEmpty(content.FieldName) && !string.IsNullOrEmpty(content.PropertyName) && content.GetMethod != null;
        }

    }
}
