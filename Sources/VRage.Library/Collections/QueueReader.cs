using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    public struct QueueReader<T> : IEnumerable<T>, IEnumerable
    {
        private readonly Queue<T> m_collection;

        public int Count
        {
            get { return m_collection.Count; }
        }

        public QueueReader(Queue<T> collection)
        {
            m_collection = collection;
        }

        public Queue<T>.Enumerator GetEnumerator()
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
