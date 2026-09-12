using Frameset.Core.Common;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;


namespace Frameset.Core.Utils
{
    public class BeanUtils
    {
        private static readonly WeakLruDictionary<Type, Dictionary<string, PropertyInfo>> typeProperties = new(200);
        public static void CopyProperties(object source, object target, string[] ignoreColumns = null)
        {
            Trace.Assert(source != null && target != null, "");
            bool issourceDict = source.GetType().IsAssignableFrom(typeof(Dictionary<string, object>));
            bool istargetDict = target.GetType().IsAssignableFrom(typeof(Dictionary<string, object>));
            if (source.GetType().Equals(target.GetType()))
            {
                CopySameType(source, target, ignoreColumns);
            }
            else if (!issourceDict && !istargetDict)
            {
                SetProperties(source, target, ignoreColumns);
            }
            else
            {
                List<string> ignoreColumnList = ignoreColumns.IsNullOrEmpty() ? [] : [.. ignoreColumns];
                if (issourceDict)
                {
                    Dictionary<string, object> sourceMap = source as Dictionary<string, object>;
                    if (!istargetDict)
                    {
                        foreach (KeyValuePair<string, object> pair in sourceMap)
                        {
                            if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                            {
                                continue;
                            }
                            if (pair.Value != null)
                            {
                                SetProperty(pair.Value, pair.Key, target);
                            }
                        }
                    }
                    else
                    {
                        Dictionary<string, object> targetDict = target as Dictionary<string, object>;
                        foreach (KeyValuePair<string, object> pair in sourceMap)
                        {
                            if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                            {
                                continue;
                            }
                            targetDict[pair.Key] = pair.Value;
                        }
                    }
                }
                else
                {
                    Dictionary<string, PropertyInfo> propDict = GetPropertyInfos(source.GetType());
                    Dictionary<string, object> targetDict = target as Dictionary<string, object>;
                    foreach (KeyValuePair<string, PropertyInfo> pair in propDict)
                    {
                        if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                        {
                            continue;
                        }
                        object value = pair.Value.GetMethod.Invoke(source, []);
                        if (value != null)
                        {
                            targetDict[pair.Key] = value;
                        }
                    }
                }
            }
        }
        public static void CopyPropertiesWithMap(object source, object target, Dictionary<string, string> mappingDict, string[] ignoreColumns = null)
        {
            bool issourceDict = source.GetType().IsAssignableFrom(typeof(Dictionary<string, object>));
            bool istargetDict = target.GetType().IsAssignableFrom(typeof(Dictionary<string, object>));
            List<string> ignoreColumnList = ignoreColumns.IsNullOrEmpty() ? [] : [.. ignoreColumns];
            if (!issourceDict && !istargetDict)
            {
                Dictionary<string, PropertyInfo> propDict = GetPropertyInfos(source.GetType());
                if (!propDict.IsNullOrEmpty())
                {
                    foreach (KeyValuePair<string, PropertyInfo> pair in propDict)
                    {
                        if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                        {
                            continue;
                        }
                        object sourceVal = pair.Value.GetMethod.Invoke(source, []);
                        if (sourceVal != null && mappingDict.TryGetValue(pair.Key, out string mappingColumn))
                        {
                            SetProperty(sourceVal, mappingColumn, target);
                        }
                    }
                }
            }
            else
            {
                if (issourceDict)
                {
                    Dictionary<string, object> sourceMap = source as Dictionary<string, object>;
                    if (!istargetDict)
                    {
                        foreach (KeyValuePair<string, object> pair in sourceMap)
                        {
                            if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                            {
                                continue;
                            }
                            if (pair.Value != null && mappingDict.TryGetValue(pair.Key, out string mappingColumn))
                            {
                                SetProperty(pair.Value, mappingColumn, target);
                            }
                        }
                    }
                    else
                    {
                        Dictionary<string, object> targetDict = target as Dictionary<string, object>;
                        foreach (KeyValuePair<string, object> pair in sourceMap)
                        {
                            if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                            {
                                continue;
                            }
                            if (pair.Value != null && mappingDict.TryGetValue(pair.Key, out string mappingColumn))
                            {
                                targetDict[mappingColumn] = pair.Value;
                            }
                        }

                    }
                }
                else
                {
                    Dictionary<string, PropertyInfo> propDict = GetPropertyInfos(source.GetType());
                    Dictionary<string, object> targetDict = target as Dictionary<string, object>;
                    foreach (KeyValuePair<string, PropertyInfo> pair in propDict)
                    {
                        if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                        {
                            continue;
                        }
                        object value = pair.Value.GetMethod.Invoke(source, []);
                        if (value != null && mappingDict.TryGetValue(pair.Key, out string mappingColumn))
                        {
                            targetDict[mappingColumn] = value;
                        }
                    }
                }
            }
        }
        public static Dictionary<string, PropertyInfo> GetPropertyInfos(Type classType)
        {
            Trace.Assert(classType != null, "");
            if (!typeProperties.TryGetValue(classType, out Dictionary<string, PropertyInfo> propDict))
            {
                PropertyInfo[] infos = classType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                if (!infos.IsNullOrEmpty())
                {
                    propDict = infos.ToDictionary(x => x.Name, x => x);
                    typeProperties.TryAdd(classType, propDict);
                }
                else
                {
                    throw new NotSupportedException("type does not contain properties");
                }
            }
            return propDict;
        }
        private static void CopySameType(object source, object target, string[] ignoreColumns)
        {
            Dictionary<string, PropertyInfo> properties = GetPropertyInfos(source.GetType());
            List<string> ignoreColumnList = ignoreColumns.IsNullOrEmpty() ? [] : [.. ignoreColumns];
            foreach (KeyValuePair<string, PropertyInfo> pair in properties)
            {
                if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                {
                    continue;
                }
                object sourceVal = pair.Value.GetMethod.Invoke(source, []);
                if (sourceVal != null)
                {
                    pair.Value.SetMethod.Invoke(target, [sourceVal]);
                }

            }
        }
        private static void SetProperties(object input, object target, string[] ignoreColumns)
        {
            Dictionary<string, PropertyInfo> propDict = GetPropertyInfos(input.GetType());
            List<string> ignoreColumnList = ignoreColumns.IsNullOrEmpty() ? [] : [.. ignoreColumns];
            if (!propDict.IsNullOrEmpty())
            {
                foreach (KeyValuePair<string, PropertyInfo> pair in propDict)
                {
                    if (!ignoreColumns.IsNullOrEmpty() && ignoreColumnList.Contains(pair.Key))
                    {
                        continue;
                    }
                    SetProperty(pair.Value.GetMethod.Invoke(input, []), pair.Key, target);
                }
            }
        }
        private static void SetProperty(object input, string propertyName, object targetObj)
        {
            if (input != null && !string.IsNullOrWhiteSpace(input.ToString()))
            {
                Dictionary<string, PropertyInfo> targetProperties = GetPropertyInfos(targetObj.GetType());
                if (targetProperties.TryGetValue(propertyName, out PropertyInfo propertyInfo))
                {
                    propertyInfo.SetMethod.Invoke(targetObj, [ConvertUtil.ParseByType(propertyInfo.GetMethod.ReturnType, input)]);
                }
            }
        }
    }
}
