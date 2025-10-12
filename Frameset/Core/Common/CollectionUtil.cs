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
}
