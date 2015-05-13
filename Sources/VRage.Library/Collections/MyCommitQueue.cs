using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Basic copy-on-commit implementation, later it will be faster by using one queue with 2 tails
    /// </summary>
    public class MyCommitQueue<T>
    {
        Queue<T> m_commited = new Queue<T>();
        SpinLock m_commitLock = new SpinLock();

        List<T> m_dirty = new List<T>();
        SpinLock m_dirtyLock = new SpinLock();

        public int Count
        {
            get 
            {
                m_commitLock.Enter();
                try
                {
                    return m_commited.Count;
                }
                finally
                {
                    m_commitLock.Exit();
                }
            }
        }

        public int UncommitedCount
        {
            get
            {
                m_dirtyLock.Enter();
                try
                {
                    return m_dirty.Count;
                }
                finally
                {
                    m_dirtyLock.Exit();
                }
            }
        }

        public void Enqueue(T obj)
        {
            m_dirtyLock.Enter();
            try
            {
                m_dirty.Add(obj);
            }
            finally
            {
                m_dirtyLock.Exit();
            }
        }

        public void Commit()
        {
            m_dirtyLock.Enter();
            try
            {
                m_commitLock.Enter();
                try
                {
                    foreach (var e in m_dirty)
                    {
                        m_commited.Enqueue(e);
                    }
                }
                finally
                {
                    m_commitLock.Exit();
                }
                m_dirty.Clear();
            }
            finally
            {
                m_dirtyLock.Exit();
            }
        }

        public bool TryDequeue(out T obj)
        {
            m_commitLock.Enter();
            try
            {
                if(m_commited.Count > 0)
                {
                    obj = m_commited.Dequeue();
                    return true;
                }
            }
            finally
            {
                m_commitLock.Exit();
            }
            obj = default(T);
            return false;
        }

        /*
        private static T[] m_emptyArray = new T[0];

        private T[] m_array = m_emptyArray;
        private int m_head;
        private int m_tail;
        private int m_size;

        public int Count
        {
            get
            {
                return this.m_size;
            }
        }

        public MyCommitQueue(int capacity)
        {
            if (capacity < 0)
                throw new InvalidOperationException("Capacity cannot be < 0");
            this.m_array = new T[capacity];
            this.m_head = 0;
            this.m_tail = 0;
            this.m_size = 0;
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

        public T Dequeue()
        {
            if (this.m_size == 0)
                throw new InvalidOperationException("Queue is empty");
            T obj = this.m_array[this.m_head];
            this.m_array[this.m_head] = default(T);
            this.m_head = (this.m_head + 1) % this.m_array.Length;
            --this.m_size;
            return obj;
        }

        public T Peek()
        {
            if (this.m_size == 0)
                throw new InvalidOperationException("Queue is empty");
            return this.m_array[this.m_head];
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
         * */
    }
}
