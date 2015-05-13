using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    public class MyUniqueList<T>
    {
        List<T> m_list = new List<T>();
        HashSet<T> m_hashSet = new HashSet<T>();

        /// <summary>
        /// O(1)
        /// </summary>
        public int Count
        {
            get { return m_list.Count; }
        }

        /// <summary>
        /// O(1)
        /// </summary>
        public T this[int index]
        {
            get { return m_list[index]; }
        }

        /// <summary>
        /// O(1)
        /// </summary>
        public bool Add(T item)
        {
            if (m_hashSet.Add(item))
            {
                m_list.Add(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// O(n)
        /// </summary>
        public bool Insert(int index, T item)
        {
            if (m_hashSet.Add(item))
            {
                m_list.Insert(index, item);
                return true;
            }
            else
            {
                m_list.Remove(item);
                m_list.Insert(index, item);
                return false;
            }
        }

        /// <summary>
        /// O(n)
        /// </summary>
        public bool Remove(T item)
        {
            if (m_hashSet.Remove(item))
            {
                m_list.Remove(item);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            m_list.Clear();
            m_hashSet.Clear();
        }

        /// <summary>
        /// O(1)
        /// </summary>
        public bool Contains(T item)
        {
            return m_hashSet.Contains(item);
        }

        public UniqueListReader<T> Items
        {
            get { return new UniqueListReader<T>(this); }
        }

        public ListReader<T> ItemList
        {
            get { return new ListReader<T>(m_list); }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return m_list.GetEnumerator();
        }
    }
}
