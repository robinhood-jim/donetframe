using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Frameset.Core.Utils
{
    public static class ClassUtils
    {
        private static WeakLruDictionary<string, Type> typeCache = new();
        /// <summary>
        /// Get User defined Type (exclude system assembly) by assembly Scanner
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        public static Type? GetUserType(this string fullTypeName)
        {
            if (!typeCache.TryGetValue(fullTypeName, out Type? retType))
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                if (!assemblies.IsNullOrEmpty())
                {
                    foreach (Assembly assembly in assemblies)
                    {
                        if (StringUtils.IsAssemblySystem(assembly))
                        {
                            continue;
                        }
                        retType = assembly.GetType(fullTypeName);
                        if (retType != null)
                        {
                            typeCache.TryAdd(fullTypeName, retType);
                            break;
                        }
                    }
                }
            }
            return retType;
        }
        /// <summary>
        /// Get Type by assembly Scanner
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        public static Type? GetTypeByName(this string fullTypeName)
        {
            if (!typeCache.TryGetValue(fullTypeName, out Type? retType))
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                if (!assemblies.IsNullOrEmpty())
                {
                    foreach (Assembly assembly in assemblies)
                    {
                        retType = assembly.GetType(fullTypeName);
                        if (retType != null)
                        {
                            typeCache.TryAdd(fullTypeName, retType);
                            break;
                        }
                    }
                }
            }
            return retType;
        }
        /// <summary>
        ///  Check Type is Primitive or string decimal DateTime
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        public static bool IsFundamentalType(this Type classType)
        {
            
            Type cmpType = classType;
            if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                cmpType = classType.GetGenericArguments()[0];
            }
            return cmpType.IsPrimitive || cmpType.IsEnum || cmpType.Equals(typeof(string)) || cmpType.Equals(typeof(decimal)) || cmpType.Equals(typeof(DateTime)) || cmpType.Equals(typeof(DateTimeOffset));
        }
        /// <summary>
        /// Check  Type must be  BaseEntity
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        public static bool IsModelType(this Type classType)
        {
            Type cmpType = classType;
            if (classType.IsGenericType && classType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                cmpType = classType.GetGenericArguments()[0];
            }
            return cmpType.IsSubclassOf(typeof(BaseEntity));
        }
        public static bool IsTypeList(this Type classType)
        {
            if (classType.IsGenericType)
            {
                Type genericType = classType.GetGenericTypeDefinition();
                return genericType == typeof(IList<>) || genericType == typeof(IEnumerable<>);
            }
            return false;
        }
        public static List<Type> ScanTypesWithAttribute(this Type scanType)
        {
            Trace.Assert(scanType.IsSubclassOf(typeof(Attribute)),"type must be Attribute!");
            List<Type> retList = [];
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            Trace.Assert(entryAssembly!=null);
            AssemblyName[] assemblies = entryAssembly.GetReferencedAssemblies();
            List<AssemblyName> assemblyNames =
            [
                ..assemblies,
                entryAssembly.GetName()
            ];

            foreach (AssemblyName assemblyName in assemblyNames)
            {
                try
                {
                    Assembly assembly = Assembly.Load(assemblyName);
                    if (StringUtils.IsAssemblySystem(assembly))
                    {
                        continue;
                    }
                    retList.AddRange(assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttributes(scanType, false).Length > 0)
                        .ToList());
                }
                catch (Exception ex)
                {
                    LogUtils.Error(ex.Message);
                }
            }
            return retList;

        }
    }

}
