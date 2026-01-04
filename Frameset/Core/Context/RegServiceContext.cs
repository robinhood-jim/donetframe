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
    /// <summary>
    /// Register Service Context
    /// </summary>
    public static class RegServiceContext
    {
        //以接口为Type的注册对象池
        private static readonly Dictionary<Type, object> registerMap = [];
        //以实现类为Type的注册对象池
        private static readonly Dictionary<Type, object> targetTypeMap = [];
        //生命周期
        private static readonly Dictionary<Type, ServiceLifetime> lifeTimeMap = [];
        //注解定义的接口和实现类对应
        private static readonly Dictionary<Type, Type> interfaceTargetMap = [];
        //具体实现类存在否
        private static readonly Dictionary<Type, int> wiredTypeMap = [];
        private static IServiceProvider provider;

        public static void SetContext(IServiceProvider _provider)
        {
            provider = _provider;
        }
        /// <summary>
        /// Register Transient Type
        /// </summary>
        /// <param name="baseInterface"></param>
        /// <param name="subType"></param>
        /// <exception cref="OperationFailedException"></exception>
        public static void Register(Type baseInterface, Type subType)
        {
            if (!interfaceTargetMap.TryGetValue(baseInterface, out Type _))
            {
                interfaceTargetMap.TryAdd(baseInterface, subType);
                lifeTimeMap.TryAdd(subType, ServiceLifetime.Scoped);
            }
            else
            {
                throw new OperationFailedException("type already register!");
            }
        }
        /// <summary>
        /// Register Type with Object
        /// </summary>
        /// <param name="baseInterface"></param>
        /// <param name="valueObj"></param>
        /// <param name="lifetime"></param>
        public static void Register(Type baseInterface, object valueObj, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (!registerMap.TryGetValue(baseInterface, out object _) && lifetime == ServiceLifetime.Singleton)
            {
                registerMap.TryAdd(baseInterface, valueObj);
                targetTypeMap.TryAdd(valueObj.GetType(), valueObj);
            }
            lifeTimeMap.TryAdd(baseInterface, lifetime);
        }
        /// <summary>
        /// GetRequired
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        /// <exception cref="OperationFailedException"></exception>
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
                        retObj = GetServiceRequired(interfaceType, subType, lifeTime, []);
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
                        retObj = GetServiceRequired(interfaceType, targetType, ServiceLifetime.Singleton, []);
                        registerMap.TryAdd(interfaceType, retObj);
                        targetTypeMap.TryAdd(retObj.GetType(), retObj);
                    }
                    else
                    {
                        if (provider != null)
                        {
                            retObj = provider.GetRequiredService(interfaceType);
                        }
                        if (retObj == null)
                        {
                            throw new OperationFailedException("type does not register!");
                        }
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
        /// <summary>
        /// GetRequired
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="OperationFailedException"></exception>
        public static T GetBean<T>()
        {
            return (T)GetBean(typeof(T));
        }
        /// <summary>
        /// Customer IOC Scanner
        /// </summary>
        /// <param name="type">Attribute Type</param>
        /// <param name="action">User Defined Action</param>
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
                        GetServiceRequired(i, impl, ServiceLifetime.Singleton, []);
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
                    GetServiceRequired(interfaceType, impl, lifetime, []);
                    break;
                case ServiceLifetime.Transient:
                case ServiceLifetime.Scoped:
                    Register(interfaceType, impl, lifetime);
                    break;
                default:
                    break;
            }
        }

        private static object GetServiceRequired(Type baseType, Type wireType, ServiceLifetime lifeTime, Dictionary<Type, int> scannedTypes)
        {
            if (scannedTypes.TryGetValue(wireType, out int _))
            {
                throw new OperationFailedException("dependency cycle exists");
            }
            object retObj = null;
            if (registerMap.TryGetValue(baseType, out retObj) && retObj != null)
            {
                return retObj;
            }
            if (IsClassNoParamConstruct(wireType))
            {
                if (!targetTypeMap.TryGetValue(wireType, out retObj))
                {
                    retObj = Activator.CreateInstance(wireType);
                }
                //Get Resource Annotation
                AutoWireInject(retObj, wireType, lifeTime, scannedTypes);
            }
            else
            {
                retObj = ConstructInject(wireType, lifeTime, scannedTypes);
                //AutoWireInject(retObj, wireType, lifeTime, scannedTypes);
            }
            if (retObj != null)
            {
                Register(baseType, retObj, lifeTime);
            }
            return retObj;
        }
        private static object ConstructInject(Type implementType, ServiceLifetime lifeTime, Dictionary<Type, int> scannnedTypes)
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
                        paramObj[i] = GetServiceRequired(parameter.ParameterType, tagetType, lifeTime, scannnedTypes);
                        scannnedTypes.TryAdd(parameter.ParameterType, 1);
                    }
                    retObj = constructorInfo.Invoke(paramObj);
                    break;
                }
            }
            return retObj;
        }
        private static void AutoWireInject(object originObj, Type wiredType, ServiceLifetime lifeTime, Dictionary<Type, int> scannedTypes)
        {
            if (wiredTypeMap.TryGetValue(wiredType, out _))
            {
                return;
            }
            var fields = wiredType.GetTypeInfo().DeclaredFields.Where(field => Attribute.IsDefined(field, typeof(ResourceAttribute))).GetEnumerator();
            while (fields.MoveNext())
            {
                FieldInfo info = fields.Current;
                ResourceAttribute attribute = (ResourceAttribute)info.GetCustomAttribute(typeof(ResourceAttribute));
                if (info.GetValue(originObj) == null)
                {
                    Type implementType = GetParameterType(info.FieldType);
                    object constructObj = null;
                    if (implementType == typeof(DbContext) || implementType.IsSubclassOf(typeof(DbContext)))
                    {
                        string contextName = attribute.ResourceName ?? DbContextFactory.CONTEXTDEFAULTNAME;
                        constructObj = DbContextFactory.GetContext(contextName);
                    }
                    else
                    {
                        constructObj = GetServiceRequired(info.FieldType, implementType, lifeTime, scannedTypes);
                    }
                    scannedTypes.TryAdd(info.FieldType, 1);
                    info.SetValue(originObj, constructObj);
                }
            }
            wiredTypeMap.TryAdd(wiredType, 1);
        }
        private static Type GetParameterType(Type baseType)
        {
            if (!interfaceTargetMap.TryGetValue(baseType, out Type implementType))
            {
                if (baseType.IsGenericType && baseType.FullName.StartsWith("Frameset.Core.Repo.IBaseRepository"))
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
