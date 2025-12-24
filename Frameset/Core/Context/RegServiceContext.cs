using Frameset.Core.Annotation;
using Frameset.Core.Exceptions;
using Frameset.Core.Repo;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Frameset.Core.Context
{
    public static class RegServiceContext
    {
        private static readonly Dictionary<Type, object> registerMap = [];
        private static readonly Dictionary<Type, object> targetTypeMap = [];
        private static readonly Dictionary<Type, ServiceLifetime> lifeTimeMap = [];
        private static readonly Dictionary<Type, Type> interfaceTargetMap = [];
        private static readonly Dictionary<Type, int> wiredTypeMap = [];
        public static void Register(Type baseInterface, Type subType)
        {
            if (!interfaceTargetMap.TryGetValue(baseInterface, out Type defineType))
            {
                interfaceTargetMap.TryAdd(baseInterface, subType);
                lifeTimeMap.TryAdd(subType, ServiceLifetime.Scoped);
            }
            else
            {
                throw new OperationFailedException("type already register!");
            }
        }
        public static void Register(Type baseInterface, object valueObj, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (!registerMap.TryGetValue(baseInterface, out object registerObj))
            {
                registerMap.TryAdd(baseInterface, valueObj);
                lifeTimeMap.TryAdd(baseInterface, lifetime);
            }
        }
        public static object GetBean(Type interfaceType)
        {
            object retObj = null;

            if (!registerMap.TryGetValue(interfaceType, out retObj))
            {
                if (interfaceTargetMap.TryGetValue(interfaceType, out Type subType))
                {
                    lifeTimeMap.TryGetValue(subType, out ServiceLifetime lifeTime);
                    if (!targetTypeMap.TryGetValue(subType, out retObj))
                    {
                        retObj = Activator.CreateInstance(subType);
                        if (lifeTime.Equals(ServiceLifetime.Singleton))
                        {
                            registerMap.TryAdd(interfaceType, retObj);
                            targetTypeMap.TryAdd(retObj.GetType(), retObj);
                        }
                    }
                }
                else
                {
                    Type targetType = GetParameterType(interfaceType);
                    if (targetType != null)
                    {
                        retObj = Activator.CreateInstance(targetType);
                        registerMap.TryAdd(interfaceType, retObj);
                        targetTypeMap.TryAdd(retObj.GetType(), retObj);
                    }
                    else
                    {
                        throw new OperationFailedException("type does not register!");
                    }
                }
            }
            if (interfaceType.IsAssignableFrom(retObj.GetType()))
            {
                return retObj;
            }
            else
            {
                throw new OperationFailedException("type is not subType!");
            }
        }
        public static T GetBean<T>()
        {
            object retObj = null;
            Type interfaceType = typeof(T);
            if (!registerMap.TryGetValue(interfaceType, out retObj))
            {
                if (interfaceTargetMap.TryGetValue(interfaceType, out Type subType))
                {
                    lifeTimeMap.TryGetValue(subType, out ServiceLifetime lifeTime);
                    if (!targetTypeMap.TryGetValue(subType, out retObj))
                    {
                        retObj = Activator.CreateInstance<T>();
                        if (lifeTime.Equals(ServiceLifetime.Singleton))
                        {
                            registerMap.TryAdd(interfaceType, retObj);
                            targetTypeMap.TryAdd(retObj.GetType(), retObj);
                        }
                    }
                }
                else
                {
                    Type targetType = GetParameterType(interfaceType);
                    if (targetType != null)
                    {
                        retObj = Activator.CreateInstance(targetType);
                        registerMap.TryAdd(interfaceType, retObj);
                        targetTypeMap.TryAdd(retObj.GetType(), retObj);
                    }
                    else
                    {
                        throw new OperationFailedException("type does not register!");
                    }
                }
            }
            if (interfaceType.IsAssignableFrom(retObj.GetType()))
            {
                return (T)retObj;
            }
            else
            {
                throw new OperationFailedException("type is not subType!");
            }
        }
        /// <summary>
        /// Customer IOC Register
        /// </summary>
        /// <param name="type">Attribute Type</param>
        /// <param name="action"></param>
        public static void ScanServices(Type type, Action<Type, Type> action = null)
        {
            Trace.Assert(type.IsSubclassOf(typeof(Attribute)), "");
            List<Type> types = ScanType(type);
            //Add interface and implement by assembly
            types.ForEach(impl =>
            {
                Type[] interfaces = impl.GetInterfaces();
                foreach (Type interfaceType in interfaces)
                {
                    interfaceTargetMap.TryAdd(interfaceType, impl);
                }
            });
            types.ForEach(impl =>
            {
                //获取该类所继承的所有接口
                Type[] interfaces = impl.GetInterfaces();
                //获取该类注入的生命周期
                Attribute? attribute = impl.GetCustomAttribute(type, false);
                interfaces.ToList().ForEach(i =>
                {
                    if (type.Equals(typeof(ServiceAttribute)))
                    {
                        ScanService(attribute, i, impl);
                    }
                    else if (action != null)
                    {
                        action.Invoke(i, impl);
                    }
                    else
                    {
                        object singletonObj = ConstructRequired(i, impl, []);
                        Register(i, singletonObj);
                    }
                });
            });
            Log.Information("successful load IOC Frame with {Num} Beans", targetTypeMap.Count);
        }
        private static void ScanService(Attribute attribute, Type interfaceType, Type impl)
        {
            ServiceLifetime lifetime = (attribute as ServiceAttribute).LifeTime;
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    object singletonObj = ConstructRequired(interfaceType, impl, []);
                    Register(interfaceType, singletonObj);
                    break;
                case ServiceLifetime.Transient:
                case ServiceLifetime.Scoped:
                    Register(interfaceType, impl, lifetime);
                    break;
                default:
                    break;
            }
        }
        private static object ConstructInject(Type implementType, Dictionary<Type, int> scannnedTypes)
        {
            ConstructorInfo[] constructorInfos = implementType.GetConstructors();
            object retObj = null;
            foreach (ConstructorInfo constructorInfo in constructorInfos)
            {
                if (!constructorInfo.IsAbstract && constructorInfo.IsPublic)
                {
                    ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
                    object[] paramObj = new object[parameterInfos.Length];
                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        ParameterInfo parameter = parameterInfos[i];
                        Type tagetType = GetParameterType(parameter.ParameterType);
                        paramObj[i] = ConstructRequired(parameter.ParameterType, tagetType, scannnedTypes);
                        scannnedTypes.TryAdd(parameter.ParameterType, 1);
                    }
                    retObj = constructorInfo.Invoke(paramObj);
                    break;
                }
            }
            return retObj;
        }
        private static object ConstructRequired(Type baseType, Type wireType, Dictionary<Type, int> scannedTypes)
        {
            if (scannedTypes.TryGetValue(wireType, out int nums))
            {
                throw new OperationFailedException("dependency cycle exists");
            }
            object retObj = null;
            if (IsClassNoParamConstruct(wireType))
            {
                try
                {
                    retObj = GetBean(baseType);
                    //Get Resource Annotation
                    AutoWireInject(retObj, wireType, scannedTypes);
                }
                finally
                {

                }

            }
            else
            {
                retObj = ConstructInject(wireType, scannedTypes);
                AutoWireInject(retObj, wireType, scannedTypes);
            }
            return retObj;
        }
        private static void AutoWireInject(object originObj, Type wiredType, Dictionary<Type, int> scannedTypes)
        {
            if (wiredTypeMap.TryGetValue(wiredType, out _))
            {
                return;
            }
            var fields = wiredType.GetTypeInfo().DeclaredFields.Where(field => Attribute.IsDefined(field, typeof(ResourceAttribute))).GetEnumerator();
            while (fields.MoveNext())
            {
                FieldInfo info = fields.Current;
                if (info.GetValue(originObj) == null)
                {
                    Type implementType = GetParameterType(info.FieldType);
                    object constructObj = ConstructRequired(info.FieldType, implementType, scannedTypes);
                    info.SetValue(originObj, constructObj);
                }
            }
            wiredTypeMap.TryAdd(wiredType, 1);
        }
        private static Type GetParameterType(Type baseType)
        {
            if (!interfaceTargetMap.TryGetValue(baseType, out Type implementType))
            {
                if (baseType.IsGenericType || baseType.GetGenericTypeDefinition().Equals(typeof(IBaseRepository<,>)))
                {
                    Type type = typeof(BaseRepository<,>);
                    Type[] types = baseType.GetGenericArguments();
                    Type constructType = type.MakeGenericType(types[0], types[1]);
                    implementType = constructType;
                }
            }
            return implementType;
        }
        public static bool IsClassNoParamConstruct(Type implementType)
        {
            ConstructorInfo[] constructorInfos = implementType.GetConstructors();
            bool isNoParam = false;
            foreach (ConstructorInfo constructorInfo in constructorInfos)
            {
                if (!constructorInfo.IsAbstract && constructorInfo.IsPublic && constructorInfo.GetParameters().Length == 0)
                {
                    isNoParam = true;
                    break;
                }
            }
            return isNoParam;
        }
        private static List<Type> ScanType(Type type)
        {
            List<Type> retList = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    retList.AddRange(assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttributes(type, false).Length > 0)
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
