using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;

namespace VRage.Collections
{
    public struct HashSetReader<T>: IEnumerable<T>, IEnumerable
    {
        private readonly HashSet<T> m_hashset;

        public HashSetReader(HashSet<T> set)
        {
            Debug.Assert(set != null, "Hashset cannot be null");
            m_hashset = set;
        }

        public static implicit operator HashSetReader<T>(HashSet<T> v)
        {
            return new HashSetReader<T>(v);
        }

        public bool IsValid
        {
            get { return m_hashset != null; }
        }

        public int Count
        {
            get
            {
                return m_hashset.Count;
            }
        }

        public bool Contains(T item)
        {
            return m_hashset.Contains(item);
        }

        public HashSet<T>.Enumerator GetEnumerator()
        {
            return m_hashset.GetEnumerator();
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
