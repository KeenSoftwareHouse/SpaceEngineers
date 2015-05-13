using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Simple thread-safe pool.
    /// Can store external objects by calling return.
    /// Creates new instances when empty.
    /// </summary>
    public class MyConcurrentPool<T>
        where T : new()
    {
        Stack<T> m_instances;
        SpinLock m_lock;
        int m_instancesCreated = 0;

        public MyConcurrentPool(int defaultCapacity = 0, bool preallocate = false)
        {
            m_lock = new SpinLock();
            m_instances = new Stack<T>(defaultCapacity);
            
            if (preallocate)
            {
                m_instancesCreated = defaultCapacity;
                for (int i = 0; i < defaultCapacity; i++)
                {
                    m_instances.Push(new T());
                }
            }
        }

        public int Count
        {
            get
            {
                m_lock.Enter();
                try
                {
                    return m_instances.Count;
                }
                finally
                {
                    m_lock.Exit();
                }
            }
        }

        public int InstancesCreated
        {
            get
            {
                return m_instancesCreated;
            }
        }

        public T Get()
        {
            m_lock.Enter();
            try
            {
                if (m_instances.Count > 0)
                {
                    return m_instances.Pop();
                }
            }
            finally
            {
                m_lock.Exit();
            }
            System.Threading.Interlocked.Increment(ref m_instancesCreated);
            return new T();
        }

        public void Return(T instance)
        {
            m_lock.Enter();

            try
            {
                m_instances.Push(instance);
            }
            finally
            {
                m_lock.Exit();
            }
        }
    }
}
