using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Allows access to queue by index
    /// Otherwise implementation is similar to regular queue
    /// </summary>
    public class MyQueue<T>
    {
        private T[] m_array;
        private int m_head;
        private int m_tail;
        private int m_size;

        public MyQueue(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("Capacity cannot be < 0", "capacity");
            this.m_array = new T[capacity];
            this.m_head = 0;
            this.m_tail = 0;
            this.m_size = 0;
        }

        public MyQueue(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentException("Collection cannot be empty", "collection");
            this.m_array = new T[4];
            this.m_size = 0;
            foreach (T obj in collection)
                this.Enqueue(obj);
        }

        public T[] DebugItems
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
            if (this.m_head < this.m_tail)
            {
                Array.Clear((Array)this.m_array, this.m_head, this.m_size);
            }
            else
            {
                Array.Clear((Array)this.m_array, this.m_head, this.m_array.Length - this.m_head);
                Array.Clear((Array)this.m_array, 0, this.m_tail);
            }
            this.m_head = 0;
            this.m_tail = 0;
            this.m_size = 0;
        }

        public int Count
        {
            get
            {
                return this.m_size;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentException("Index must be larger or equal to 0 and smaller than Count");
                return m_array[(this.m_head + index) % this.m_array.Length];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentException("Index must be larger or equal to 0 and smaller than Count");
                m_array[(this.m_head + index) % this.m_array.Length] = value;
            }
        }

        public void Enqueue(T item)
        {
            if (this.m_size == this.m_array.Length)
            {
                int capacity = (int)((long)this.m_array.Length * 200L / 100L);
                if (capacity < this.m_array.Length + 4)
                    capacity = this.m_array.Length + 4;
                this.SetCapacity(capacity);
            }
            this.m_array[this.m_tail] = item;
            this.m_tail = (this.m_tail + 1) % this.m_array.Length;
            ++this.m_size;
        }

        public T Peek()
        {
            if (this.m_size == 0)
                throw new InvalidOperationException("Queue is empty");
            return this.m_array[this.m_head];
        }

        public T Dequeue()
        {
            T obj = this.m_array[this.m_head];
            this.m_array[this.m_head] = default(T); // Clear item to prevent holding references
            this.m_head = (this.m_head + 1) % this.m_array.Length;
            --this.m_size;
            return obj;
        }

        // TODO (DI): Those mod operations can be removed to improve performance
        public bool Remove(T item)
        {
            int idx = m_head;
            for (int i = 0; i < m_size; i++, idx++)
            {
                if (m_array[idx % m_array.Length].Equals(item))
                    break;
            }

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
            return false;
        }

        private void SetCapacity(int capacity)
        {
            T[] objArray = new T[capacity];
            if (this.m_size > 0)
            {
                if (this.m_head < this.m_tail)
                {
                    Array.Copy((Array)this.m_array, this.m_head, (Array)objArray, 0, this.m_size);
                }
                else
                {
                    Array.Copy((Array)this.m_array, this.m_head, (Array)objArray, 0, this.m_array.Length - this.m_head);
                    Array.Copy((Array)this.m_array, 0, (Array)objArray, this.m_array.Length - this.m_head, this.m_tail);
                }
            }
            this.m_array = objArray;
            this.m_head = 0;
            this.m_tail = this.m_size == capacity ? 0 : this.m_size;
        }

        public void TrimExcess()
        {
            if (this.m_size >= (int)((double)this.m_array.Length * 0.9))
                return;
            this.SetCapacity(this.m_size);
        }
    }
}
