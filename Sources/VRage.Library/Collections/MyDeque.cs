using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public class MyDeque<T>
    {
        private T[] m_buffer;
        private int m_front;
        private int m_back;

        public MyDeque(int baseCapacity = 8)
        {
            m_buffer = new T[baseCapacity + 1];
        }

        public bool Empty
        {
            get { return m_front == m_back; }
        }

        private bool Full
        {
            get { return ((m_back + 1) % m_buffer.Length) == m_front; }
        }

        public int Count
        {
            get { return (m_back - m_front) + (m_back < m_front ? m_buffer.Length : 0); }
        }

        public void Clear()
        {
            Array.Clear(m_buffer, 0, m_buffer.Length);
            m_front = 0;
            m_back = 0;
        }

        public void EnqueueFront(T value)
        {
            EnsureCapacityForOne();
            Decrement(ref m_front);
            m_buffer[m_front] = value;
        }

        public void EnqueueBack(T value)
        {
            EnsureCapacityForOne();
            m_buffer[m_back] = value;
            Increment(ref m_back);
        }

        public T DequeueFront()
        {
            Debug.Assert(!Empty);
            T value = m_buffer[m_front];
            m_buffer[m_front] = default(T);
            Increment(ref m_front);
            return value;
        }

        public T DequeueBack()
        {
            Debug.Assert(!Empty);
            Decrement(ref m_back);
            T value = m_buffer[m_back];
            m_buffer[m_back] = default(T);
            return value;
        }

        private void Increment(ref int index)
        {
            index = (index + 1) % m_buffer.Length;
        }

        private void Decrement(ref int index)
        {
            --index;
            if (index < 0)
                index += m_buffer.Length;
        }

        private void EnsureCapacityForOne()
        {
            if (!Full)
                return;

            var tmpBuffer = new T[(m_buffer.Length - 1) * 2 + 1];
            int newBack = 0;
            for (int i = m_front; i != m_back; Increment(ref i))
            {
                tmpBuffer[newBack++] = m_buffer[i];
            }
            m_buffer = tmpBuffer;
            m_front = 0;
            m_back = newBack;
        }

    }
}
