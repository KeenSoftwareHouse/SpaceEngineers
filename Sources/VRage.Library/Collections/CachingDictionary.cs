using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Dictionary wrapper that allows for addition and removal even during enumeration.
    /// Done by caching changes and allowing explicit application using Apply* methods.
    /// </summary>
    public class CachingDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        private Dictionary<K, V> m_dictionary = new Dictionary<K, V>();
        private List<KeyValuePair<K, V>> m_additionsAndModifications = new List<KeyValuePair<K, V>>();
        private List<K> m_removals = new List<K>();

        private static K m_keyToCompare;
        private static K KeyToCompare { set { m_keyToCompare = value; } }

        private static Predicate<K> m_keyEquals = KeyEquals;
        private static Predicate<KeyValuePair<K, V>> m_keyValueEquals = KeyValueEquals;

        public V this[K key]
        {
            get
            {
                return m_dictionary[key];
            }
            set
            {
                Add(key, value);
            }
        }

        public void Add(K key, V value, bool immediate = false)
        {
            if (immediate)
            {
                m_dictionary[key] = value;
            }
            else
            {
                m_additionsAndModifications.Add(new KeyValuePair<K, V>(key, value));
                m_keyToCompare = key;
                m_removals.RemoveAll(m_keyEquals);
            }
        }

        public void Remove(K key, bool immediate = false)
        {
            if (immediate)
            {
                m_dictionary.Remove(key);
            }
            else
            {
                m_removals.Add(key);
                m_keyToCompare = key;
                m_additionsAndModifications.RemoveAll(m_keyValueEquals);
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            return m_dictionary.TryGetValue(key, out value);
        }

        public bool ContainsKey(K key)
        {
            return m_dictionary.ContainsKey(key);
        }

        public Dictionary<K, V>.KeyCollection Keys
        {
            get
            {
                return m_dictionary.Keys;
            }
        }

        public Dictionary<K, V>.ValueCollection Values
        {
            get
            {
                return m_dictionary.Values;
            }
        }

        public void Clear()
        {
            m_dictionary.Clear();
            m_additionsAndModifications.Clear();
            m_removals.Clear();
        }

        public void ApplyChanges()
        {
            ApplyAdditionsAndModifications();
            ApplyRemovals();
        }

        public void ApplyAdditionsAndModifications()
        {
            foreach (var pair in m_additionsAndModifications)
            {
                m_dictionary[pair.Key] = pair.Value;
            }
            m_additionsAndModifications.Clear();
        }

        public void ApplyRemovals()
        {
            foreach (var key in m_removals)
            {
                m_dictionary.Remove(key);
            }
            m_removals.Clear();
        }

        private static bool KeyEquals(K key)
        {
            return System.Collections.Generic.EqualityComparer<K>.Default.Equals(key, m_keyToCompare);
        }

        private static bool KeyValueEquals(KeyValuePair<K, V> keyValue)
        {
            return System.Collections.Generic.EqualityComparer<K>.Default.Equals(keyValue.Key, m_keyToCompare);
        }

        public Dictionary<K, V>.Enumerator GetEnumerator()
        {
            return m_dictionary.GetEnumerator();
        }

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
