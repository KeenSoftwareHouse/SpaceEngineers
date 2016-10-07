using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Collections;
using ParallelTasks;

namespace VRage.Generics
{
    public class MyObjectsPool<T> where T : class, new()
    {
        MyConcurrentQueue<T> m_unused;
        HashSet<T> m_active;
        HashSet<T> m_marked;
        SpinLockRef m_activeLock = new SpinLockRef();

        //  Count of items allowed to store in this pool.
        int m_baseCapacity;

        //public QueueReader<T> Unused
        //{
        //    get { return new QueueReader<T>(m_unused); }
        //}

        public HashSetReader<T> Active
        {
            get { return new HashSetReader<T>(m_active); }
        }

        public int ActiveCount
        {
            get { return m_active.Count; }
        }

        public int BaseCapacity
        {
            get { return m_baseCapacity; }
        }

        public int Capacity
        {
            get { return m_unused.Count + m_active.Count; }
        }

        private MyObjectsPool()
        {
        }

        public MyObjectsPool(int baseCapacity)
        {
            //  Pool should contain at least one preallocated item!
            Debug.Assert(baseCapacity > 0);

            m_baseCapacity = baseCapacity;
            m_unused = new MyConcurrentQueue<T>(m_baseCapacity);
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
            bool create = (m_unused.Count == 0);
            if (create)
                item = new T();
            else
                item = m_unused.Dequeue();
            using (m_activeLock.Acquire())
            {
                m_active.Add(item);
            }
            return create;
        }

        //  Allocates new object in the pool and returns reference to it.
        //  If pool doesn't have free object (it's full), null is returned. But this shouldn't happen if capacity is chosen carefully.
        public T Allocate(bool nullAllowed = false)
        {
            T item = default(T);
            if (m_unused.Count > 0)
            {
                item = m_unused.Dequeue();
                using (m_activeLock.Acquire())
                {
                    m_active.Add(item);
                }
            }
            Debug.Assert(nullAllowed ? true : item != null, "MyObjectsPool is full and cannot allocate any other item!");
            return item;
        }

        //  Deallocates object imediatelly. This is the version that accepts object, and then it find its node.
        //  IMPORTANT: Don't call while iterating this object pool!
        public void Deallocate(T item)
        {
            Debug.Assert(m_active.Contains(item), "Deallocating item which is not in active set of the pool.");
            using (m_activeLock.Acquire())
            {
                m_active.Remove(item);
            }
            m_unused.Enqueue(item);
        }

        //  Marks object for deallocation, but doesn't remove it immediately. Call it during iterating the pool.
        public void MarkForDeallocate(T item)
        {
            Debug.Assert(m_active.Contains(item), "Marking item which is not in active set of the pool.");
            m_marked.Add(item);
        }

        // Marks all active items for deallocation.
        public void MarkAllActiveForDeallocate()
        {
            m_marked.UnionWith(m_active);
        }

        //  Deallocates objects marked for deallocation. If same object was marked twice or more times for
        //  deallocation, this method will handle it and deallocate it only once (rest is ignored).
        //  IMPORTANT: Call only when not iterating the pool!!!
        public void DeallocateAllMarked()
        {
            foreach (var marked in m_marked)
            {
                Deallocate(marked);
            }
            m_marked.Clear();
        }
        
        //  Deallocates all objects
        //  IMPORTANT: Call only when not iterating the pool!!!
        public void DeallocateAll()
        {
            foreach (var active in m_active)
            {
                m_unused.Enqueue(active);
            }
            m_active.Clear();
            m_marked.Clear();
        }

        //public void TrimToBaseCapacity()
        //{
        //    while (Capacity > BaseCapacity && m_unused.Count > 0)
        //    {
        //        m_unused.Dequeue();
        //    }
        //    m_unused.TrimExcess();
        //    m_active.TrimExcess();
        //    m_marked.TrimExcess();
        //    Debug.Assert(Capacity == BaseCapacity, "Could not trim to base capacity (possibly due to active objects).");
        //}
    }
}
