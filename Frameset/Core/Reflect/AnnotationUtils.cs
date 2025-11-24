using Frameset.Core.Annotation;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Frameset.Core.Reflect
{
    public class AnnotationUtils
    {
        private static Dictionary<Type, Dictionary<string, MethodParam>> reflectMap = new Dictionary<Type, Dictionary<string, MethodParam>>();
        public static Dictionary<string, MethodParam> ReflectObject(Type targetObj)
        {
            PropertyInfo[] propertyInfos = targetObj.GetProperties();
            Dictionary<string, MethodParam> dict = null;
            if (!reflectMap.TryGetValue(targetObj.GetType(), out dict))
            {
                dict = new Dictionary<string, MethodParam>();
                foreach (PropertyInfo prop in propertyInfos)
                {
                    string name = prop.Name;
                    MethodParam param = new()
                    {
                        GetMethod = prop.GetGetMethod(),
                        SetMethod = prop.GetSetMethod(),
                        ParamType = prop.GetGetMethod().ReturnType,
                        PropertyInfo = prop
                    };
                    dict.TryAdd(name, param);
                }
                reflectMap.TryAdd(targetObj, dict);
            }
            return dict;

        }
        private static bool validateEntity(Type type)
        {
            bool isEntityDef = Attribute.IsDefined(type.Module, typeof(MappingEntity));
            if (!isEntityDef)
            {
                throw new ArgumentException("module is not a mappingEntity");
            }

            return true;
        }
    }
    public class MethodParam
    {
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
        public PropertyInfo PropertyInfo
        {
            get; internal set;
        }

    }
}