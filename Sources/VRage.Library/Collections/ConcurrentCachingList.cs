using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ParallelTasks;

namespace VRage.Collections
{
    /// <summary>
    /// List wrapper that allows for addition and removal even during enumeration.
    /// Done by caching changes and allowing explicit application using Apply* methods.
    /// 
    /// This version has individual locks for cached and non-cached versions, allowing
    /// each to be managed efficiently even across multiple threads
    /// </summary>
    public class ConcurrentCachingList<T> : IEnumerable<T>
    {
        private readonly List<T> m_list = new List<T>();
        private readonly List<T> m_toAdd = new List<T>();
        private readonly List<T> m_toRemove = new List<T>();

        private SpinLockRef m_listLock = new SpinLockRef();
        private SpinLockRef m_cacheLock = new SpinLockRef();

        public ConcurrentCachingList() { }

        public ConcurrentCachingList(int capacity)
        {
            m_list = new List<T>(capacity);
        }

        public int Count
        {
            get
            {
                using (m_listLock.Acquire())
                    return m_list.Count;
            }
        }

        public T this[int index]
        {
            get { using (m_listLock.Acquire()) return m_list[index]; }
        }

        public void Add(T entity)
        {
            using (m_cacheLock.Acquire())
            {
                if (m_toRemove.Contains(entity))
                    m_toRemove.Remove(entity);
                else
                    m_toAdd.Add(entity);
            }
        }

        public void Remove(T entity, bool immediate = false)
        {
            using (m_cacheLock.Acquire())
            {
                if (m_toAdd.Contains(entity))
                    m_toAdd.Remove(entity);
                else
                    m_toRemove.Add(entity);
            }

            if (immediate)
            {
                using (m_listLock.Acquire())
                using (m_cacheLock.Acquire())
                {
                    m_list.Remove(entity);
                    m_toRemove.Remove(entity);
                }
            }
        }

        /// <summary>
        /// Immediately removes an element at the specified index.
        /// </summary>
        /// <param name="index">Index of the element to remove immediately.</param>
        public void RemoveAtImmediately(int index)
        {
            using (m_listLock.Acquire())
            {
                if (index < 0 || index >= m_list.Count) return;
                m_list.RemoveAt(index);
            }
        }

        public void ClearList()
        {
            using (m_listLock.Acquire())
                m_list.Clear();
        }

        public void ClearImmediate()
        {
            using (m_listLock.Acquire())
            using (m_cacheLock.Acquire())
            {
                m_toAdd.Clear();
                m_toRemove.Clear();
                m_list.Clear();
            }
        }

        public void ApplyChanges()
        {
            ApplyAdditions();
            ApplyRemovals();
        }

        public void ApplyAdditions()
        {
            using (m_listLock.Acquire())
            using (m_cacheLock.Acquire())
            {
                m_list.AddList(m_toAdd);
                m_toAdd.Clear();
            }
        }

        public void ApplyRemovals()
        {
            using (m_listLock.Acquire())
            using (m_cacheLock.Acquire())
            {
                foreach (var entity in m_toRemove)
                    m_list.Remove(entity);
                m_toRemove.Clear();
            }
        }

        public void Sort(IComparer<T> comparer)
        {
            using (m_listLock.Acquire())
                m_list.Sort(comparer);
        }

        public List<T>.Enumerator GetEnumerator()
        {
            using (m_listLock.Acquire())
                return m_list.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [Conditional("DEBUG")]
        public void DebugCheckEmpty()
        {
            Debug.Assert(m_list.Count == 0);
            Debug.Assert(m_toAdd.Count == 0);
            Debug.Assert(m_toRemove.Count == 0);
        }

        public override string ToString()
        {
            return string.Format("Count = {0}; ToAdd = {1}; ToRemove = {2}", m_list.Count, m_toAdd.Count, m_toRemove.Count);
        }
    }
}
