using Microsoft.IdentityModel.Tokens;
using System;
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
        public static Type? GetUserType(string fullTypeName)
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
        public static Type? GetType(string fullTypeName)
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
    }
}
