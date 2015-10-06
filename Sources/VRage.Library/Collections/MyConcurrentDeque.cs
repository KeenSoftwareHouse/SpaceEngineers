using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;

namespace VRage.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public class MyConcurrentDeque<T> : IMyQueue<T>
    {
        private readonly MyDeque<T> m_deque = new MyDeque<T>();
        private readonly FastResourceLock m_lock = new FastResourceLock();

        public bool Empty
        {
            get
            {
                using (m_lock.AcquireSharedUsing())
                {
                    return m_deque.Empty;
                }
            }
        }

        public int Count
        {
            get
            {
                using (m_lock.AcquireSharedUsing())
                {
                    return m_deque.Count;
                }
            }
        }

        public void Clear()
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_deque.Clear();
            }
        }

        public void EnqueueFront(T value)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_deque.EnqueueFront(value);
            }
        }

        public void EnqueueBack(T value)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_deque.EnqueueBack(value);
            }
        }

        public bool TryDequeueFront(out T value)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                if (m_deque.Empty)
                {
                    value = default(T);
                    return false;
                }
                value = m_deque.DequeueFront();
                return true;
            }
        }

        public bool TryDequeueBack(out T value)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                if (m_deque.Empty)
                {
                    value = default(T);
                    return false;
                }
                value = m_deque.DequeueBack();
                return true;
            }
        }

    }
}
