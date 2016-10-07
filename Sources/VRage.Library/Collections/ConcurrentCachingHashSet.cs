using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParallelTasks;

namespace VRage.Collections
{
    public class ConcurrentCachingHashSet<T> : IEnumerable<T>
    {
        private readonly HashSet<T> m_hashSet = new HashSet<T>();
        private readonly HashSet<T> m_toAdd = new HashSet<T>();
        private readonly HashSet<T> m_toRemove = new HashSet<T>();

        private readonly SpinLockRef m_setLock = new SpinLockRef();
        private readonly SpinLockRef m_changelistLock = new SpinLockRef();

        public int Count
        {
            get
            {
                using (m_setLock.Acquire()) return m_hashSet.Count;
            }
        }

        public void Clear()
        {
            using (m_setLock.Acquire())
            using (m_changelistLock.Acquire())
            {
                m_hashSet.Clear();
                m_toAdd.Clear();
                m_toRemove.Clear();
            }
        }

        public bool Contains(T item)
        {
            using (m_setLock.Acquire())
                return m_hashSet.Contains(item);
        }

        public void Add(T item)
        {
            using (m_changelistLock.Acquire())
            {
                if (m_toRemove.Contains(item))
                    m_toRemove.Remove(item);
                else
                    m_toAdd.Add(item);
            }
        }

        public void Remove(T item, bool immediate = false)
        {
            using (m_changelistLock.Acquire())
            {
                if (m_toAdd.Contains(item))
                    m_toAdd.Remove(item);
                else
                    m_toRemove.Add(item);
            }

            if (immediate)
            {
                using (m_setLock.Acquire())
                using (m_changelistLock.Acquire())
                {
                    m_hashSet.Remove(item);
                    m_toRemove.Remove(item);
                }
            }
        }

        public void ApplyChanges()
        {
            ApplyAdditions();
            ApplyRemovals();
        }

        public void ApplyAdditions()
        {
            using (m_setLock.Acquire())
            using (m_changelistLock.Acquire())
            {
                foreach (var item in m_toAdd)
                    m_hashSet.Add(item);
                m_toAdd.Clear();
            }
        }

        public void ApplyRemovals()
        {
            using (m_setLock.Acquire())
            using (m_changelistLock.Acquire())
            {
                foreach (var item in m_toRemove)
                    m_hashSet.Remove(item);
                m_toRemove.Clear();
            }
        }

        public HashSet<T>.Enumerator GetEnumerator()
        {
            using (m_setLock.Acquire())
                return m_hashSet.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return string.Format("Count = {0}; ToAdd = {1}; ToRemove = {2}", m_hashSet.Count, m_toAdd.Count, m_toRemove.Count);
        }
    }
}
