using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VRage.Collections
{
    public class LRUCache<TKey, TValue>
    {
        private static HashSet<int> m_debugEntrySet = new HashSet<int>();

        private int m_first;
        private int m_last;
        private readonly IEqualityComparer<TKey> m_comparer;
        private readonly Dictionary<TKey, int> m_entryLookup;
        private readonly CacheEntry[] m_cacheEntries;
        private readonly FastResourceLock m_lock = new FastResourceLock();

        public LRUCache(int cacheSize, IEqualityComparer<TKey> comparer = null)
        {
            m_comparer = comparer ?? EqualityComparer<TKey>.Default;
            m_cacheEntries = new CacheEntry[cacheSize];
            m_entryLookup = new Dictionary<TKey, int>(cacheSize, m_comparer);

            ResetInternal();
        }

        public void Reset()
        {
            if (m_entryLookup.Count > 0)
            {
                ResetInternal();
            }
        }

        void ResetInternal()
        {
            CacheEntry defaultEntry;
            defaultEntry.Data = default(TValue);
            defaultEntry.Key = default(TKey);
            for (int i = 0; i < m_cacheEntries.Length; ++i)
            {
                // all nodes initially point to their neighbors
                defaultEntry.Prev = i - 1;
                defaultEntry.Next = i + 1;
                m_cacheEntries[i] = defaultEntry;
            }
            m_first = 0;
            m_last = m_cacheEntries.Length - 1;
            Debug.Assert(m_cacheEntries[m_first].Prev == INVALID_ENTRY);
            m_cacheEntries[m_last].Next = INVALID_ENTRY;
            m_entryLookup.Clear();
        }

        public TValue Read(TKey key)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                AssertConsistent();

                try
                {
                    int cacheIndex;
                    if (m_entryLookup.TryGetValue(key, out cacheIndex))
                    {
                        if (cacheIndex != m_first)
                        {
                            Remove(cacheIndex);
                            AddFirst(cacheIndex);
                        }
                        return m_cacheEntries[cacheIndex].Data;
                    }

                    return default(TValue);
                }
                finally
                {
                    AssertConsistent();
                }
            }
        }

        public void Write(TKey key, TValue value)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                AssertConsistent();

                int cacheIndex;
                if (m_entryLookup.TryGetValue(key, out cacheIndex))
                {
                    Debug.Assert(m_comparer.Equals(key, m_cacheEntries[cacheIndex].Key));
                    m_cacheEntries[cacheIndex].Data = value;
                }
                else
                {
                    int swappedIndex = m_last;

                    RemoveLast();
                    m_entryLookup.Remove(m_cacheEntries[swappedIndex].Key);

                    m_cacheEntries[swappedIndex].Key = key;
                    m_cacheEntries[swappedIndex].Data = value;

                    AddFirst(swappedIndex);
                    m_entryLookup.Add(key, swappedIndex);
                }

                AssertConsistent();
            }
        }

        private void RemoveLast()
        {
            int newLastIdx = m_cacheEntries[m_last].Prev;

            Debug.Assert(m_cacheEntries[m_last].Prev != INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[m_last].Next == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[newLastIdx].Prev != INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[newLastIdx].Next != INVALID_ENTRY);

            m_cacheEntries[newLastIdx].Next = INVALID_ENTRY;
            m_cacheEntries[m_last].Prev = INVALID_ENTRY;

            Debug.Assert(m_cacheEntries[m_last].Prev == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[m_last].Next == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[newLastIdx].Prev != INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[newLastIdx].Next == INVALID_ENTRY);

            m_last = newLastIdx;
        }

        private void Remove(int entryIndex)
        {
            Debug.Assert((uint)entryIndex < (uint)m_cacheEntries.Length);

            int prevIdx = m_cacheEntries[entryIndex].Prev;
            int nextIdx = m_cacheEntries[entryIndex].Next;

            if (prevIdx != INVALID_ENTRY)
                m_cacheEntries[prevIdx].Next = m_cacheEntries[entryIndex].Next;
            else
                m_first = m_cacheEntries[entryIndex].Next;

            if (nextIdx != INVALID_ENTRY)
                m_cacheEntries[nextIdx].Prev = m_cacheEntries[entryIndex].Prev;
            else
                m_last = m_cacheEntries[entryIndex].Prev;

            m_cacheEntries[entryIndex].Prev = INVALID_ENTRY;
            m_cacheEntries[entryIndex].Next = INVALID_ENTRY;
        }

        private void AddFirst(int entryIndex)
        {
            Debug.Assert(m_cacheEntries[entryIndex].Prev == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[entryIndex].Next == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[m_first].Prev == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[m_first].Next != INVALID_ENTRY);

            m_cacheEntries[m_first].Prev = entryIndex;
            m_cacheEntries[entryIndex].Next = m_first;

            Debug.Assert(m_cacheEntries[m_first].Prev != INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[m_first].Next != INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[entryIndex].Prev == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[entryIndex].Next != INVALID_ENTRY);

            m_first = entryIndex;
        }

        /// <summary>
        /// Verifies that all assumptions are met (linked list is connected and all lookups are correct).
        /// FULLDEBUG is only here to disable this. Enable by changing to DEBUG if you suspect problems.
        /// </summary>
        [Conditional("FULLDEBUG")]
        private void AssertConsistent()
        {
            Debug.Assert(m_cacheEntries[m_first].Prev == INVALID_ENTRY);
            Debug.Assert(m_cacheEntries[m_last].Next == INVALID_ENTRY);

            for (int test = 0; test < 3; ++test)
            {
                for (int i = 0; i < m_cacheEntries.Length; ++i)
                    m_debugEntrySet.Add(i);

                switch (test)
                {
                    case 0:
                        {
                            int current = m_first;
                            while (current != INVALID_ENTRY)
                            {
                                bool removed = m_debugEntrySet.Remove(current);
                                Debug.Assert(removed);
                                current = m_cacheEntries[current].Next;
                            }
                            Debug.Assert(m_debugEntrySet.Count == 0);
                        }
                        break;

                    case 1:
                        {
                            int current = m_last;
                            while (current != INVALID_ENTRY)
                            {
                                bool removed = m_debugEntrySet.Remove(current);
                                Debug.Assert(removed);
                                current = m_cacheEntries[current].Prev;
                            }
                            Debug.Assert(m_debugEntrySet.Count == 0);
                        }
                        break;

                    case 2:
                        {
                            foreach (var entry in m_entryLookup)
                            {
                                Debug.Assert(m_comparer.Equals(entry.Key, m_cacheEntries[entry.Value].Key));
                                bool removed = m_debugEntrySet.Remove(entry.Value);
                                Debug.Assert(removed);
                            }
                            Debug.Assert(m_debugEntrySet.Count + m_entryLookup.Count == m_cacheEntries.Length);
                            m_debugEntrySet.Clear();
                        }
                        break;
                }
            }
        }

        const int INVALID_ENTRY = -1;

        [DebuggerDisplay("Prev={Prev}, Next={Next}, Key={Key}, Data={Data}")]
        struct CacheEntry
        {
            public int Prev;
            public int Next;

            public TValue Data;
            public TKey Key;
        }

    }

}
