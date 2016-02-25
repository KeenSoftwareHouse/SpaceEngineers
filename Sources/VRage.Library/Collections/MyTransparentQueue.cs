using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.Collections
{

    /**
     * Helper queue class that exposes the internals of the queue.
     * 
     * Useful when custom operations are necessary but one does not want to extend the queue itself to keep code organised.
     */
    public class MyTransparentQueue<T> : MyQueue<T>
    {
        public MyTransparentQueue(int capacity)
            : base(capacity)
        {
        }

        public MyTransparentQueue(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /**
         * Returns true if the queue had to be resized.
         */
        public new bool Enqueue(T item)
        {
            bool expanded = false;
            if (m_size == m_array.Length)
            {
                int capacity = (int)(m_array.Length * 200L / 100L);
                if (capacity < m_array.Length + 4)
                    capacity = m_array.Length + 4;
                SetCapacity(capacity);
                expanded = true;
            }
            m_array[m_tail] = item;
            m_tail = (m_tail + 1) % m_array.Length;
            ++m_size;
            return expanded;
        }

        public new T this[int index]
        {
            get { return m_array[index]; }
            set { m_array[index] = value; }
        }

        public void AdvanceHead(int amount)
        {
            Debug.Assert(amount < m_size && amount > 0);

            m_head = (m_head + amount) % m_array.Length;

            m_size -= amount;
        }

        public int Head
        {
            get { return m_head; }
        }

        public int Tail
        {
            get { return m_tail; }
            set { m_tail = value; }
        }

        public int ArraySize
        {
            get { return m_array.Length; }
        }

        public int QueueIndexForArray(int arrayIndex)
        {
            return (arrayIndex - m_head + m_array.Length) % m_array.Length;
        }
    }
}
