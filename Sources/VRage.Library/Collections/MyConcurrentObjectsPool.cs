using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;

namespace VRage.Generics
{
    public class MyConcurrentObjectsPool<T> where T : class, new()
    {
        FastResourceLock m_lock = new FastResourceLock();
        MyQueue<T> m_unused;
        HashSet<T> m_active;
        HashSet<T> m_marked;
        
        //  Count of items allowed to store in this pool.
        int m_baseCapacity;

        public void ApplyActionOnAllActives(Action<T> action)
        {
            using (m_lock.AcquireSharedUsing())
            {
                foreach (var active in m_active)
                {
                    action.Invoke(active);
                }
            }
        }

        public int ActiveCount
        {
            get
            {
                using (m_lock.AcquireSharedUsing())
                {
                    return m_active.Count;
                }
            }
        }

        public int BaseCapacity
        {
            get
            {
                using (m_lock.AcquireSharedUsing())
                {
                    m_lock.AcquireShared();
                    return m_baseCapacity;
                }
            }
        }

        public int Capacity
        {
            get
            {
                using (m_lock.AcquireSharedUsing())
                {
                    m_lock.AcquireShared();
                    return m_unused.Count + m_active.Count;
                }
            }
        }

        private MyConcurrentObjectsPool()
        {
        }

        public MyConcurrentObjectsPool(int baseCapacity)
        {
            //  Pool should contain at least one preallocated item!
            Debug.Assert(baseCapacity > 0);

            m_baseCapacity = baseCapacity;
            m_unused = new MyQueue<T>(m_baseCapacity);
            m_active = new HashSet<T>();
            m_marked = new HashSet<T>();

            for (int i = 0; i < m_baseCapacity; i++)
            {
                m_unused.Enqueue(new T());
            }
        }

        /// <summary>
        /// Returns true when new item was allocated
        /// </summary>
        public bool AllocateOrCreate(out T item)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                bool create = (m_unused.Count == 0);
                if (create)
                    item = new T();
                else
                    item = m_unused.Dequeue();
                m_active.Add(item);
                return create;
            }
        }

        //  Allocates new object in the pool and returns reference to it.
        //  If pool doesn't have free object (it's full), null is returned. But this shouldn't happen if capacity is chosen carefully.
        public T Allocate(bool nullAllowed = false)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                T item = default(T);
                if (m_unused.Count > 0)
                {
                    item = m_unused.Dequeue();
                    m_active.Add(item);
                }
                Debug.Assert(nullAllowed ? true : item != null,
                    "MyObjectsPool is full and cannot allocate any other item!");
                return item;
            }
        }

        //  Deallocates object imediatelly. This is the version that accepts object, and then it find its node.
        //  IMPORTANT: Don't call while iterating this object pool!
        public void Deallocate(T item)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                Debug.Assert(m_active.Contains(item), "Deallocating item which is not in active set of the pool.");
                m_active.Remove(item);
                m_unused.Enqueue(item);
            }
        }

        //  Marks object for deallocation, but doesn't remove it immediately. Call it during iterating the pool.
        public void MarkForDeallocate(T item)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                Debug.Assert(m_active.Contains(item), "Marking item which is not in active set of the pool.");
                m_marked.Add(item);
            }
        }

        //  Deallocates objects marked for deallocation. If same object was marked twice or more times for
        //  deallocation, this method will handle it and deallocate it only once (rest is ignored).
        public void DeallocateAllMarked()
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                foreach (var marked in m_marked)
                {
                    Deallocate(marked);
                }
                m_marked.Clear();
            }
        }
        
        //  Deallocates all objects
        public void DeallocateAll()
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                foreach (var active in m_active)
                {
                    m_unused.Enqueue(active);
                }
                m_active.Clear();
                m_marked.Clear();
            }
        }
    }
}
