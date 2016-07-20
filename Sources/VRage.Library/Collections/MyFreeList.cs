using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public class MyFreeList<T>
    {
        private T[] m_list;
        private int m_size;
        private Queue<int> m_freePositions;

        public MyFreeList(int capacity = 16)
        {
            m_list = new T[16];

            m_freePositions = new Queue<int>(capacity / 2);
        }

        public int Allocate()
        {
            int pos;
            if (m_freePositions.Count > 0)
            {
                pos = m_freePositions.Dequeue();
            }
            else
            {
                if (m_size == m_list.Length)
                    Array.Resize(ref m_list, m_list.Length << 1);

                pos = m_size++;
            }

            return pos;
        }

        public void Free(int position)
        {
            m_list[position] = default(T);

            if (position == m_size) m_size--;
            else m_freePositions.Enqueue(position);
        }

        public T this[int index]
        {
            get { return m_list[index]; }
            set { m_list[index] = value; }
        }

        public int Size
        {
            get { return m_list.Length; }
        }
    }
}
