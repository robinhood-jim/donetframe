using Microsoft.IdentityModel.Tokens;

using System.Collections.Generic;

namespace Frameset.Core.Common
{
    public class CollectionUtil<K, V>
    {

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
