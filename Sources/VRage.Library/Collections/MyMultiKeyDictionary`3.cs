using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    /// <summary>
    /// MyMultiKeyDictionary supports value lookups using multiple different keys.
    /// When keys can be derived from value, use MultiKeyIndex.
    /// </summary>
    public class MyMultiKeyDictionary<TKey1, TKey2, TValue> : IEnumerable<MyMultiKeyDictionary<TKey1, TKey2, TValue>.Triple>
    {
        public struct Triple
        {
            public TKey1 Key1;
            public TKey2 Key2;
            public TValue Value;

            public Triple(TKey1 key1, TKey2 key2, TValue value)
            {
                Key1 = key1;
                Key2 = key2;
                Value = value;
            }
        }

        Dictionary<TKey1, Triple> m_index1 = new Dictionary<TKey1, Triple>();
        Dictionary<TKey2, Triple> m_index2 = new Dictionary<TKey2, Triple>();

        public TValue this[TKey1 key]
        {
            get { return m_index1[key].Value; }
        }

        public TValue this[TKey2 key]
        {
            get { return m_index2[key].Value; }
        }

        public int Count
        {
            get { return m_index1.Count; }
        }

        public MyMultiKeyDictionary(int capacity = 0, EqualityComparer<TKey1> keyComparer1 = null, EqualityComparer<TKey2> keyComparer2 = null)
        {
            m_index1 = new Dictionary<TKey1, Triple>(capacity, keyComparer1);
            m_index2 = new Dictionary<TKey2, Triple>(capacity, keyComparer2);
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            var tri = new Triple(key1, key2, value);
            m_index1.Add(key1, tri);
            try
            {
                m_index2.Add(key2, tri);
            }
            catch
            {
                m_index1.Remove(key1);
                throw;
            }
        }

        public bool ContainsKey(TKey1 key1)
        {
            return m_index1.ContainsKey(key1);
        }

        public bool ContainsKey(TKey2 key2)
        {
            return m_index2.ContainsKey(key2);
        }

        public bool Remove(TKey1 key1)
        {
            Triple value;
            return m_index1.TryGetValue(key1, out value) && m_index2.Remove(value.Key2) && m_index1.Remove(key1);
        }

        public bool Remove(TKey2 key2)
        {
            Triple value;
            return m_index2.TryGetValue(key2, out value) && m_index1.Remove(value.Key1) && m_index2.Remove(key2);
        }

        public bool Remove(TKey1 key1, TKey2 key2)
        {
            return m_index1.Remove(key1) && m_index2.Remove(key2);
        }

        public bool TryRemove(TKey1 key1, out Triple removedValue)
        {
            return m_index1.TryGetValue(key1, out removedValue) && m_index2.Remove(removedValue.Key2) && m_index1.Remove(key1);
        }

        public bool TryRemove(TKey2 key2, out Triple removedValue)
        {
            return m_index2.TryGetValue(key2, out removedValue) && m_index1.Remove(removedValue.Key1) && m_index2.Remove(key2);
        }

        public bool TryRemove(TKey1 key1, out TValue removedValue)
        {
            Triple t;
            var res = m_index1.TryGetValue(key1, out t) && m_index2.Remove(t.Key2) && m_index1.Remove(key1);
            removedValue = t.Value;
            return res;
        }

        public bool TryRemove(TKey2 key2, out TValue removedValue)
        {
            Triple t;
            var res = m_index2.TryGetValue(key2, out t) && m_index1.Remove(t.Key1) && m_index2.Remove(key2);
            removedValue = t.Value;
            return res;
        }

        public bool TryGetValue(TKey1 key1, out Triple result)
        {
            return m_index1.TryGetValue(key1, out result);
        }

        public bool TryGetValue(TKey2 key2, out Triple result)
        {
            return m_index2.TryGetValue(key2, out result);
        }

        public bool TryGetValue(TKey1 key1, out TValue result)
        {
            Triple t;
            var res = m_index1.TryGetValue(key1, out t);
            result = t.Value;
            return res;
        }

        public bool TryGetValue(TKey2 key2, out TValue result)
        {
            Triple t;
            var res = m_index2.TryGetValue(key2, out t);
            result = t.Value;
            return res;
        }

        Dictionary<TKey1, Triple>.ValueCollection.Enumerator GetEnumerator()
        {
            return m_index1.Values.GetEnumerator();
        }

        IEnumerator<MyMultiKeyDictionary<TKey1, TKey2, TValue>.Triple> IEnumerable<MyMultiKeyDictionary<TKey1, TKey2, TValue>.Triple>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
