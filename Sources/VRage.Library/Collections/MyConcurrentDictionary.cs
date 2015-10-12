using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Simple thread-safe queue.
    /// Uses spin-lock
    /// </summary>
    public class MyConcurrentDictionary<TKey, TValue>
    {
        Dictionary<TKey, TValue> m_dictionary;
        SpinLockRef m_lock = new SpinLockRef();

        public int Count
        {
            get
            {
                using (m_lock.Acquire())
                {
                    return m_dictionary.Count;
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                using (m_lock.Acquire())
                {
                    return m_dictionary[key];
                }
            }
            set
            {
                using (m_lock.Acquire())
                {
                    m_dictionary[key] = value;
                }
            }
        }

        public MyConcurrentDictionary(int capacity = 0, EqualityComparer<TKey> comparer = null)
        {
            m_dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        public TValue ChangeKey(TKey oldKey, TKey newKey)
        {
            using (m_lock.Acquire())
            {
                var result = m_dictionary[oldKey];
                m_dictionary.Remove(oldKey);
                m_dictionary[newKey] = result;
                return result;
            }
        }

        public void Clear()
        {
            using (m_lock.Acquire())
            {
                m_dictionary.Clear();
            }
        }

        public void Add(TKey key, TValue value)
        {
            using (m_lock.Acquire())
            {
                m_dictionary.Add(key, value);
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            using (m_lock.Acquire())
            {
                if(!m_dictionary.ContainsKey(key))
                {
                    m_dictionary.Add(key, value);
                    return true;
                }
                return false;
            }
        }

        public bool ContainsKey(TKey key)
        {
            using (m_lock.Acquire())
            {
                return m_dictionary.ContainsKey(key);
            }
        }

        public bool ContainsValue(TValue value)
        {
            using (m_lock.Acquire())
            {
                return m_dictionary.ContainsValue(value);
            }
        }

        public bool Remove(TKey key)
        {
            using (m_lock.Acquire())
            {
                return m_dictionary.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            using (m_lock.Acquire())
            {
                return m_dictionary.TryGetValue(key, out value);
            }
        }

        public void GetValues(List<TValue> result)
        {
            using (m_lock.Acquire())
            {
                foreach (var item in m_dictionary.Values)
                    result.Add(item);
            }
        }
    }
}
