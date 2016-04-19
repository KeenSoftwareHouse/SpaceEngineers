using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageRender
{
    class MyFreelist<T> where T : struct
    {
        private T[] m_entities;
        private int[] m_next;
        private int m_sizeLimit;
        private int m_nextFree;
        private int m_holesCount;

        private T m_defaultValue;

        public int Size { get; private set; }
        public int Capacity { get { return m_sizeLimit; } }

        public int FilledSize { get { return Size + m_holesCount; } }

        public void Clear()
        {
            for (int i = 0; i < m_sizeLimit; i++)
            {
                m_entities[i] = m_defaultValue;
                m_next[i] = i + 1;
            }

            m_nextFree = 0;
            m_holesCount = 0;
            Size = 0;
        }

        public MyFreelist(int sizeLimit, T defaultValue = default(T))
        {
            m_defaultValue = defaultValue;
            m_sizeLimit = sizeLimit;

            m_next = new int[sizeLimit];
            m_entities = new T[sizeLimit];
            Clear();
        }

        public T[] Data { get { return m_entities; } }

        public int Allocate()
        {
            var free = m_nextFree;

            if (free == m_sizeLimit)
            {
                var newSize = (int)(Capacity * (Capacity < 1024 ? 2 : 1.5f));
                Array.Resize(ref m_next, newSize);
                Array.Resize(ref m_entities, newSize);

                for (int i = m_sizeLimit; i < newSize; i++)
                {
                    m_next[i] = i + 1;
                }

                m_sizeLimit = newSize;
            }

            m_nextFree = m_next[free];
            m_next[free] = -1;
            if (free < FilledSize - 1)
                m_holesCount--;

            Size++;

            return free;
        }

        public void Free(int index)
        {
            Debug.Assert(Size > 0);
            m_next[index] = m_nextFree;
            m_nextFree = index;
            m_entities[index] = m_defaultValue;
            if (Size == 1)
                m_holesCount = 0;
            else if (index < FilledSize - 1)
                m_holesCount++;

            Size--;
        }
    }
}
