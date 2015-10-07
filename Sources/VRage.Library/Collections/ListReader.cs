using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime;

namespace VRage.Collections
{
    public struct ListReader<T>: IEnumerable<T>, IEnumerable
    {
        public static ListReader<T> Empty = new ListReader<T>(new List<T>(0));

        private readonly List<T> m_list;

        public ListReader(List<T> list)
        {
            m_list = list ?? Empty.m_list;
        }

        public static implicit operator ListReader<T>(List<T> list)
        {
            return new ListReader<T>(list);
        }
        
        public int Count
        {
            get { return m_list.Count; }
        }

        public T this[int index]
        {
            get { return m_list[index]; }
        }

        public T ItemAt(int index)
        {
            return m_list[index];
        }

        public int IndexOf(T item)
        {
            return m_list.IndexOf(item);
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return m_list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
