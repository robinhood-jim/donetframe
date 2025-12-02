using Microsoft.IdentityModel.Tokens;
using Spring.Util;
using System.Collections.Generic;

namespace Frameset.Core.Common
{
    public class CollectionUtil<K, V>
    {
        public static bool isNotEmpty(Dictionary<K, V> dict, K key)
        {
            return dict.ContainsKey(key) && dict[key] != null;
        }
        public static bool isNotEmpty(IList<K> list)
        {
            return !CollectionUtils.IsEmpty(list);
        }
    }
    public class DictionaryUtils
    {
        public static void RemoveIf<K, V>(Dictionary<K, V> dict, V value)
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
    }
}
