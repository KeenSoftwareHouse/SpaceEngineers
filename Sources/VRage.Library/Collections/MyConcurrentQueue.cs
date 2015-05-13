using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Simple thread-safe queue.
    /// Uses spin-lock
    /// </summary>
    public class MyConcurrentQueue<T>
    {
        Queue<T> m_queue;
        SpinLockRef m_lock = new SpinLockRef();

        public MyConcurrentQueue(int capacity = 0)
        {
            m_queue = new Queue<T>(capacity);
        }

        public int Count
        {
            get
            {
                using (m_lock.Acquire())
                {
                    return m_queue.Count;
                };
            }
        }

        public void Clear()
        {
            using (m_lock.Acquire())
            {
                m_queue.Clear();
            };
        }

        public void Enqueue(T instance)
        {
            using (m_lock.Acquire())
            {
                m_queue.Enqueue(instance);
            };
        }

        public T Dequeue()
        {
            using (m_lock.Acquire())
            {
                return m_queue.Dequeue();
            };
        }

        public bool TryDequeue(out T instance)
        {
            using (m_lock.Acquire())
            {
                if (m_queue.Count > 0)
                {
                    instance = m_queue.Dequeue();
                    return true;
                }
            };

            instance = default(T);
            return false;
        }

        public bool TryPeek(out T instance)
        {
            using (m_lock.Acquire())
            {
                if (m_queue.Count > 0)
                {
                    instance = m_queue.Peek();
                    return true;
                }
            };

            instance = default(T);
            return false;
        }
    }
}
