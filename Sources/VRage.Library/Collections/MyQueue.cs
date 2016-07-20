using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Allows access to queue by index
    /// Otherwise implementation is similar to regular queue
    /// </summary>
    public class MyQueue<T>
    {
        protected T[] m_array;
        protected int m_head;
        protected int m_tail;
        protected int m_size;

        public MyQueue(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("Capacity cannot be < 0", "capacity");
            m_array = new T[capacity];
            m_head = 0;
            m_tail = 0;
            m_size = 0;
        }

        public MyQueue(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentException("Collection cannot be empty", "collection");
            m_array = new T[4];
            m_size = 0;
            foreach (T obj in collection)
                Enqueue(obj);
        }

        public T[] InternalArray
        {
            get
            {
                T[] result = new T[Count];
                for (int i = 0; i < Count; i++)
                {
                    result[i] = this[i];
                }
                return result;
            }
        }

        public void Clear()
        {
            if (m_head < m_tail)
            {
                Array.Clear(m_array, m_head, m_size);
            }
            else
            {
                Array.Clear(m_array, m_head, m_array.Length - m_head);
                Array.Clear(m_array, 0, m_tail);
            }

            m_head = 0;
            m_tail = 0;
            m_size = 0;
        }

        public int Count
        {
            get
            {
                return m_size;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentException("Index must be larger or equal to 0 and smaller than Count");
                return m_array[(m_head + index) % m_array.Length];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentException("Index must be larger or equal to 0 and smaller than Count");
                m_array[(m_head + index) % m_array.Length] = value;
            }
        }

        public void Enqueue(T item)
        {
            if (m_size == m_array.Length)
            {
                int capacity = (int)(m_array.Length * 200L / 100L);
                if (capacity < m_array.Length + 4)
                    capacity = m_array.Length + 4;
                SetCapacity(capacity);
            }
            m_array[m_tail] = item;
            m_tail = (m_tail + 1) % m_array.Length;
            ++m_size;
        }

        public T Peek()
        {
            if (m_size == 0)
                throw new InvalidOperationException("Queue is empty");
            return m_array[m_head];
        }

        public T Dequeue()
        {
            T obj = m_array[m_head];
            m_array[m_head] = default(T); // Clear item to prevent holding references
            m_head = (m_head + 1) % m_array.Length;
            --m_size;
            return obj;
        }

        public bool Contains(T item)
        {
            int idx = m_head;
            for (int i = 0; i < m_size; i++, idx++)
            {
                if (m_array[idx % m_array.Length].Equals(item))
                    return true;
            }

            return false;
        }

        public bool Remove(T item)
        {
            int idx = m_head;
            int i;
            for (i = 0; i < m_size; i++, idx++)
            {
                if (m_array[idx % m_array.Length].Equals(item))
                    break;
            }

            if (i == m_size) return false;
            Remove(idx);

            return true;
        }

        // TODO (DI): Those mod operations can be removed to improve performance
        public void Remove(int idx)
        {
            Debug.Assert(idx >= m_head || idx < m_tail);

            int mod = idx % m_array.Length;
            int next;
            int last = (m_tail + m_array.Length - 1) % m_array.Length;
            while (mod != last)
            {
                next = (mod + 1) % m_array.Length;
                m_array[mod] = m_array[next];
                mod = next;
            }
            m_array[last] = default(T);

            m_tail = last;
            --m_size;
        }

        protected void SetCapacity(int capacity)
        {
            T[] objArray = new T[capacity];
            if (m_size > 0)
            {
                if (m_head < m_tail)
                {
                    Array.Copy(m_array, m_head, objArray, 0, m_size);
                }
                else
                {
                    Array.Copy(m_array, m_head, objArray, 0, m_array.Length - m_head);
                    Array.Copy(m_array, 0, objArray, m_array.Length - m_head, m_tail);
                }
            }
            m_array = objArray;
            m_head = 0;
            m_tail = m_size == capacity ? 0 : m_size;
        }

        public void TrimExcess()
        {
            if (m_size >= (int)(m_array.Length * 0.9))
                return;
            SetCapacity(m_size);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('[');

            if (Count > 0)
            {
                sb.Append(this[Count - 1]);

                for (int i = Count - 2; i >= 0; --i)
                {
                    sb.Append(", ");
                    sb.Append(this[i]);
                }
            }

            sb.Append(']');

            return sb.ToString();
        }
    }
}
