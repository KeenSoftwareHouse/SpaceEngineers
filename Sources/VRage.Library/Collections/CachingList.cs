using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// List wrapper that allows for addition and removal even during enumeration.
    /// Done by caching changes and allowing explicit application using Apply* methods.
    /// </summary>
    public class CachingList<T> : IEnumerable<T>
    {
        List<T> m_list = new List<T>();
        List<T> m_toAdd = new List<T>();
        List<T> m_toRemove = new List<T>();

        public int Count
        {
            get { return m_list.Count; }
        }

        public T this[int index]
        {
            get { return m_list[index]; }
        }

        public void Add(T entity)
        {
            if (m_toRemove.Contains(entity))
                m_toRemove.Remove(entity);
            else
                m_toAdd.Add(entity);
        }

        public void Remove(T entity, bool immediate = false)
        {
            if (m_toAdd.Contains(entity))
                m_toAdd.Remove(entity);
            else
                m_toRemove.Add(entity);

            if (immediate)
            {
                m_list.Remove(entity);
                m_toRemove.Remove(entity);
            }
        }

        public void ClearImmediate()
        {
            m_toAdd.Clear();
            m_toRemove.Clear();
            m_list.Clear();
        }

        public void ApplyChanges()
        {
            ApplyAdditions();
            ApplyRemovals();
        }

        public void ApplyAdditions()
        {
            m_list.AddList(m_toAdd);
            m_toAdd.Clear();
        }

        public void ApplyRemovals()
        {
            foreach (var entity in m_toRemove)
                m_list.Remove(entity);
            m_toRemove.Clear();
        }

        public List<T>.Enumerator GetEnumerator()
        {
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
