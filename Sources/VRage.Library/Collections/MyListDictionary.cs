using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.Library.Collections
{
    /// <summary>
    /// Collection which stores multiple elements under same key by using list.
    /// Collection does not allow removing single value, only all items with same key.
    /// </summary>
    public class MyListDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>>
    {
        Stack<List<TValue>> m_listCache = new Stack<List<TValue>>();
        Dictionary<TKey, List<TValue>> m_dictionary = new Dictionary<TKey, List<TValue>>();

        List<TValue> ObtainList()
        {
            if (m_listCache.Count > 0)
                return m_listCache.Pop();
            else
                return new List<TValue>();
        }

        void ReturnList(List<TValue> list)
        {
            list.Clear();
            m_listCache.Push(list);
        }

        public List<TValue> GetList(TKey key)
        {
            return m_dictionary.GetValueOrDefault(key);
        }

        public List<TValue> GetOrAddList(TKey key)
        {
            List<TValue> list;
            if (!m_dictionary.TryGetValue(key, out list))
            {
                list = ObtainList();
                m_dictionary.Add(key, list);
            }
            return list;
        }

        public Dictionary<TKey, List<TValue>>.ValueCollection Values
        {
            get { return m_dictionary.Values; }
        }
        public void Add(TKey key, TValue value)
        {
            GetOrAddList(key).Add(value);
        }

        public bool Remove(TKey key)
        {
            List<TValue> list;
            if (m_dictionary.TryGetValue(key, out list))
            {
                m_dictionary.Remove(key);
                ReturnList(list);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var pair in m_dictionary)
            {
                ReturnList(pair.Value);
            }
            m_dictionary.Clear();
        }

        public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
        {
            return m_dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
