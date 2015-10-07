using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;

namespace VRage.Collections
{
    public struct DictionaryReader<K, V> : IEnumerable<KeyValuePair<K, V>>, IEnumerable
    {
        private readonly Dictionary<K, V> m_collection;

        public static readonly DictionaryReader<K, V> Empty = default(DictionaryReader<K, V>);

        public DictionaryReader(Dictionary<K, V> collection)
        {
            m_collection = collection;
        }

        public bool HasValue
        {
            get { return m_collection != null; }
        }

        public bool ContainsKey(K key)
        {
            return m_collection.ContainsKey(key);
        }

        public bool TryGetValue(K key, out V value)
        {
            return m_collection.TryGetValue(key, out value);
        }

        public int Count()
        {
            return m_collection.Count;
        }

        public V this[K key]
        {
            get
            {
                return m_collection[key];
            }
        }

        public Dictionary<K, V>.Enumerator GetEnumerator()
        {
            return m_collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator DictionaryReader<K, V>(Dictionary<K, V> v)
        {
            return new DictionaryReader<K, V>(v);
        }
    }

    public struct DictionaryValuesReader<K, V> : IEnumerable<V>, IEnumerable
    {
        private readonly Dictionary<K, V> m_collection;

        public DictionaryValuesReader(Dictionary<K, V> collection)
        {
            m_collection = collection;
        }

        public int Count
        {
            get { return m_collection.Count; }
        }

        public V this[K key] 
        {
            get 
            {
                return m_collection[key]; 
            }
        }

        public bool TryGetValue(K key, out V result)
        {
            return m_collection.TryGetValue(key, out result);
        }

        public Dictionary<K, V>.ValueCollection.Enumerator GetEnumerator()
        {
            return m_collection.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<V> IEnumerable<V>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator DictionaryValuesReader<K, V>(Dictionary<K, V> v)
        {
            return new DictionaryValuesReader<K, V>(v);
        }
    }

    public struct DictionaryKeysReader<K, V> : IEnumerable<K>, IEnumerable
    {
        private readonly Dictionary<K, V> m_collection;

        public DictionaryKeysReader(Dictionary<K, V> collection)
        {
            m_collection = collection;
        }

        public Dictionary<K, V>.KeyCollection.Enumerator GetEnumerator()
        {
            return m_collection.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<K> IEnumerable<K>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count()
        {
            return m_collection.Count;
        }
    }

}
