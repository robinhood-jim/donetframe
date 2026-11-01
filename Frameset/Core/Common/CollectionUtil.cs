using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;

namespace Frameset.Core.Common
{
    public static class CollectionUtil
    {


    }
    public static class DictionaryUtils
    {
        public static void RemoveIf<K, V>(Dictionary<K, V> dict, V value) where K : notnull where V : notnull
        {
            if (!dict.IsNullOrEmpty())
            {

                foreach (var item in dict)
                {
                    if (item.Value.Equals(value))
                    {
                        dict.Remove(item.Key);
                    }
                }
            }
        }
        public static bool IsTypeDictionary(this Type classType)
        {
            if (classType.IsGenericType)
            {
                Type genericType = classType.GetGenericTypeDefinition();
                return genericType == typeof(IDictionary<,>) || genericType == typeof(Dictionary<,>);
            }
            return false;
        }
        

    }
}
