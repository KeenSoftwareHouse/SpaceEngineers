using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    public class CachingHashSet<T> : IEnumerable<T>
    {
        HashSet<T> m_hashSet = new HashSet<T>();
        HashSet<T> m_toAdd = new HashSet<T>();
        HashSet<T> m_toRemove = new HashSet<T>();

        public int Count
        {
            get { return m_hashSet.Count; }
        }

        public void Clear()
        {
            m_hashSet.Clear();
            m_toAdd.Clear();
            m_toRemove.Clear();
        }

        public bool Contains(T item)
        {
            return m_hashSet.Contains(item);
        }

        public void Add(T item)
        {
            if (m_toRemove.Contains(item))
                m_toRemove.Remove(item);
            else
                m_toAdd.Add(item);
        }

        public void Remove(T item, bool immediate = false)
        {
            if (m_toAdd.Contains(item))
                m_toAdd.Remove(item);
            else
                m_toRemove.Add(item);

            if (immediate)
            {
                m_hashSet.Remove(item);
                m_toRemove.Remove(item);
            }
        }

        public void ApplyChanges()
        {
            ApplyAdditions();
            ApplyRemovals();
        }

        public void ApplyAdditions()
        {
            foreach (var item in m_toAdd)
                m_hashSet.Add(item);
            m_toAdd.Clear();
        }

        public void ApplyRemovals()
        {
            foreach (var item in m_toRemove)
                m_hashSet.Remove(item);
            m_toRemove.Clear();
        }

        public HashSet<T>.Enumerator GetEnumerator()
        {
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
