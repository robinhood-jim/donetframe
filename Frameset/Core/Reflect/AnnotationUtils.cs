using Frameset.Core.Annotation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Frameset.Core.Reflect
{
    public class AnnotationUtils
    {
        private static Dictionary<Type, Dictionary<string, MethodParam>> reflectMap = new Dictionary<Type, Dictionary<string, MethodParam>>();
        public static Dictionary<string, MethodParam> ReflectObject(Type targetObj)
        {
            PropertyInfo[] propertyInfos = targetObj.GetProperties();
            if (!reflectMap.TryGetValue(targetObj.GetType(), out Dictionary<string, MethodParam>? dict))
            {
                dict = new();
                foreach (PropertyInfo prop in propertyInfos)
                {
                    string name = prop.Name;
                    Trace.Assert(prop.GetMethod != null && prop.SetMethod != null);
                    MethodParam param = new()
                    {
                        GetMethod = prop.GetMethod,
                        SetMethod = prop.SetMethod,
                        ParamType = prop.GetMethod.ReturnType,
                        PropertyInfo = prop
                    };
                    dict.TryAdd(name, param);
                }
                reflectMap.TryAdd(targetObj, dict);
            }
            return dict;

        }
        private static bool ValidateEntity(Type type)
        {
            bool isEntityDef = Attribute.IsDefined(type.Module, typeof(MappingEntityAttribute));
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
        } = null!;
        public MethodInfo SetMethod
        {
            get; internal set;
        } = null!;
        public Type ParamType
        {
            get; internal set;
        } = null!;
        public PropertyInfo PropertyInfo
        {
            get; internal set;
        } = null!;

    }
}