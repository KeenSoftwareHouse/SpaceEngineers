using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime;

namespace VRage.Collections
{
    public struct UniqueListReader<T>: IEnumerable<T>, IEnumerable
    {
        public static UniqueListReader<T> Empty = new UniqueListReader<T>(new MyUniqueList<T>());

        private readonly MyUniqueList<T> m_list;

        public UniqueListReader(MyUniqueList<T> list)
        {
            m_list = list;
        }

        public static implicit operator ListReader<T>(UniqueListReader<T> list)
        {
            return list.m_list.ItemList;
        }

        public static implicit operator UniqueListReader<T>(MyUniqueList<T> list)
        {
            return new UniqueListReader<T>(list);
        }

        public int Count
        {
            get { return m_list.Count; }
        }

        public T ItemAt(int index)
        {
            return m_list[index];
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
