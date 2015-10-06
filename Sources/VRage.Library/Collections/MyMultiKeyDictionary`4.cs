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
    public class MyMultiKeyDictionary<TKey1, TKey2, TKey3, TValue> : IEnumerable<MyMultiKeyDictionary<TKey1, TKey2, TKey3, TValue>.Quadruple>
    {
        public struct Quadruple
        {
            public TKey1 Key1;
            public TKey2 Key2;
            public TKey3 Key3;
            public TValue Value;

            public Quadruple(TKey1 key1, TKey2 key2, TKey3 key3, TValue value)
            {
                Key1 = key1;
                Key2 = key2;
                Key3 = key3;
                Value = value;
            }
        }

        Dictionary<TKey1, Quadruple> m_index1 = new Dictionary<TKey1, Quadruple>();
        Dictionary<TKey2, Quadruple> m_index2 = new Dictionary<TKey2, Quadruple>();
        Dictionary<TKey3, Quadruple> m_index3 = new Dictionary<TKey3, Quadruple>();

        public TValue this[TKey1 key]
        {
            get { return m_index1[key].Value; }
        }

        public TValue this[TKey2 key]
        {
            get { return m_index2[key].Value; }
        }

        public TValue this[TKey3 key]
        {
            get { return m_index3[key].Value; }
        }

        public int Count
        {
            get { return m_index1.Count; }
        }

        public MyMultiKeyDictionary(int capacity = 0, EqualityComparer<TKey1> keyComparer1 = null, EqualityComparer<TKey2> keyComparer2 = null, EqualityComparer<TKey3> keyComparer3 = null)
        {
            m_index1 = new Dictionary<TKey1, Quadruple>(capacity, keyComparer1);
            m_index2 = new Dictionary<TKey2, Quadruple>(capacity, keyComparer2);
            m_index3 = new Dictionary<TKey3, Quadruple>(capacity, keyComparer3);
        }

        public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TValue value)
        {
            var quad = new Quadruple(key1, key2, key3, value);
            m_index1.Add(key1, quad);
            try
            {
                m_index2.Add(key2, quad);
                try
                {
                    m_index3.Add(key3, quad);
                }
                catch
                {
                    m_index2.Remove(key2);
                    throw;
                }
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

        public bool ContainsKey(TKey3 key3)
        {
            return m_index3.ContainsKey(key3);
        }

        public bool Remove(TKey1 key1)
        {
            Quadruple quad;
            return m_index1.TryGetValue(key1, out quad) && m_index3.Remove(quad.Key3) && m_index2.Remove(quad.Key2) && m_index1.Remove(key1);
        }

        public bool Remove(TKey2 key2)
        {
            Quadruple quad;
            return m_index2.TryGetValue(key2, out quad) && m_index3.Remove(quad.Key3) && m_index1.Remove(quad.Key1) && m_index2.Remove(key2);
        }

        public bool Remove(TKey3 key3)
        {
            Quadruple quad;
            return m_index3.TryGetValue(key3, out quad) && m_index1.Remove(quad.Key1) && m_index2.Remove(quad.Key2) && m_index3.Remove(key3);
        }

        public bool Remove(TKey1 key1, TKey2 key2, TKey3 key3)
        {
            return m_index1.Remove(key1) && m_index2.Remove(key2) && m_index3.Remove(key3);
        }

        public bool TryRemove(TKey1 key1, out TValue removedValue)
        {
            Quadruple t;
            var res = m_index1.TryGetValue(key1, out t) && m_index3.Remove(t.Key3) && m_index2.Remove(t.Key2) && m_index1.Remove(key1);
            removedValue = t.Value;
            return res;
        }

        public bool TryRemove(TKey2 key2, out TValue removedValue)
        {
            Quadruple t;
            var res = m_index2.TryGetValue(key2, out t) && m_index3.Remove(t.Key3) && m_index1.Remove(t.Key1) && m_index2.Remove(key2);
            removedValue = t.Value;
            return res;
        }

        public bool TryRemove(TKey3 key3, out TValue removedValue)
        {
            Quadruple t;
            var res = m_index3.TryGetValue(key3, out t) && m_index1.Remove(t.Key1) && m_index2.Remove(t.Key2) && m_index3.Remove(key3);
            removedValue = t.Value;
            return res;
        }

        public bool TryRemove(TKey1 key1, out Quadruple removedValue)
        {
            return m_index1.TryGetValue(key1, out removedValue) && m_index3.Remove(removedValue.Key3) && m_index2.Remove(removedValue.Key2) && m_index1.Remove(key1);
        }

        public bool TryRemove(TKey2 key2, out Quadruple removedValue)
        {
            return m_index2.TryGetValue(key2, out removedValue) && m_index3.Remove(removedValue.Key3) && m_index1.Remove(removedValue.Key1) && m_index2.Remove(key2);
        }

        public bool TryRemove(TKey3 key3, out Quadruple removedValue)
        {
            return m_index3.TryGetValue(key3, out removedValue) && m_index1.Remove(removedValue.Key1) && m_index2.Remove(removedValue.Key2) && m_index3.Remove(key3);
        }

        public bool TryGetValue(TKey1 key1, out Quadruple result)
        {
            return m_index1.TryGetValue(key1, out result);
        }

        public bool TryGetValue(TKey2 key2, out Quadruple result)
        {
            return m_index2.TryGetValue(key2, out result);
        }

        public bool TryGetValue(TKey3 key3, out Quadruple result)
        {
            return m_index3.TryGetValue(key3, out result);
        }

        public bool TryGetValue(TKey1 key1, out TValue result)
        {
            Quadruple t;
            var res = m_index1.TryGetValue(key1, out t);
            result = t.Value;
            return res;
        }

        public bool TryGetValue(TKey2 key2, out TValue result)
        {
            Quadruple t;
            var res = m_index2.TryGetValue(key2, out t);
            result = t.Value;
            return res;
        }

        public bool TryGetValue(TKey3 key3, out TValue result)
        {
            Quadruple t;
            var res = m_index3.TryGetValue(key3, out t);
            result = t.Value;
            return res;
        }

        Dictionary<TKey1, Quadruple>.ValueCollection.Enumerator GetEnumerator()
        {
            return m_index1.Values.GetEnumerator();
        }

        IEnumerator<MyMultiKeyDictionary<TKey1, TKey2, TKey3, TValue>.Quadruple> IEnumerable<MyMultiKeyDictionary<TKey1, TKey2, TKey3, TValue>.Quadruple>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
