using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    public struct StackReader<T> : IEnumerable<T>, IEnumerable
    {
        private readonly Stack<T> m_collection;

        public StackReader(Stack<T> collection)
        {
            m_collection = collection;
        }

        public Stack<T>.Enumerator GetEnumerator()
        {
            return m_collection.GetEnumerator();
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
