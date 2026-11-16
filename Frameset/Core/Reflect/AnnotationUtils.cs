using Frameset.Core.Annotation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Frameset.Core.Reflect
{
    public static class AnnotationUtils
    {
        private static Dictionary<Type, Dictionary<string, MethodParam>> reflectMap = [];
        public static Dictionary<string, MethodParam> ReflectObject(Type targetType)
        {
            if (!reflectMap.TryGetValue(targetType, out Dictionary<string, MethodParam>? dict))
            {
                PropertyInfo[] propertyInfos = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                dict = new();
                foreach (PropertyInfo prop in propertyInfos)
                {
                    string name = prop.Name;

                    MethodParam param = new()
                    {
                        ParamType = prop.PropertyType,
                        PropertyInfo = prop,
                        Getter = ReflectionUtils.GetGetter(targetType, name),
                        Setter = ReflectionUtils.GetSetter(targetType, name),
                    };
                    dict.TryAdd(name, param);
                }
                FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo fieldInfo in fields)
                {
                    string name = fieldInfo.Name;
                    MethodParam param = new MethodParam
                    {
                        ParamType = fieldInfo.FieldType,
                        Field = fieldInfo,
                        Getter = ReflectionUtils.GetGetter(targetType, name),
                        Setter = ReflectionUtils.GetSetter(targetType, name),
                    };
                }

                reflectMap.TryAdd(targetType, dict);
            }
            return dict;

        }
        private static bool ValidateEntity(Type type)
        {
            bool isEntityDef = Attribute.IsDefined(type.Module, typeof(MappingEntityAttribute)) | Attribute.IsDefined(type.Module, typeof(TableAttribute));
            if (!isEntityDef)
            {
                throw new ArgumentException("module is not a mappingEntity or EF entity");
            }
            return true;
        }

    }
    public class MethodParam
    {
        
        public Type ParamType
        {
            get; internal set;
        } = null!;
        public PropertyInfo PropertyInfo
        {
            get; internal set;
        } = null!;
        public FieldInfo Field
        {
            get; set;
        } = null!;
        //Expression Getter
        public Func<object, object?> Getter
        {
            get; internal set;
        } = null!;
        //Expression Setter
        public Action<object, object?> Setter
        {
            get; internal set;
        } = null!;

    }
}