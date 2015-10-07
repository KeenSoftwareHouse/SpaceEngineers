using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dictionary, K key)
        {
            V val;
            dictionary.TryGetValue(key, out val);
            return val;
        }

        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dictionary, K key, V defaultValue)
        {
            V val;
            return dictionary.TryGetValue(key, out val) ? val : defaultValue;
        }

        public static KeyValuePair<K, V> FirstPair<K, V>(this Dictionary<K, V> dictionary)
        {
            var e = dictionary.GetEnumerator();
            e.MoveNext();
            return e.Current;
        }
    }
}
