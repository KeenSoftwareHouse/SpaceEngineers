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
    public class MyConcurrentHashSet<T> : IEnumerable<T>
    {
        HashSet<T> m_set;
        SpinLockRef m_lock = new SpinLockRef();

        public MyConcurrentHashSet()
        {
            m_set = new HashSet<T>();
        }

        public int Count
        {
            get
            {
                using (m_lock.Acquire())
                {
                    return m_set.Count;
                }
            }
        }

        public void Clear()
        {
            using (m_lock.Acquire())
            {
                m_set.Clear();
            }
        }

        public void Add(T instance)
        {
            using (m_lock.Acquire())
            {
                m_set.Add(instance);
            }
        }

        public void Remove(T value)
        {
            using (m_lock.Acquire())
            {
                m_set.Remove(value);
            }
        }

        public bool Contains(T value)
        {
            bool isContained = false;
            using (m_lock.Acquire())
            {
                isContained = m_set.Contains(value);
            }
            return isContained;
        }

        struct MyConcurrentHashSetEnumerator : IEnumerator<T>
        {
            IEnumerator<T> setEnumerator;
            SpinLockRef.Token ilock;

            public MyConcurrentHashSetEnumerator(HashSet<T> set, SpinLockRef.Token ilock)
            {
                setEnumerator = set.GetEnumerator();
                this.ilock = ilock;
            }

            public T Current
            {
                get { return setEnumerator.Current; }
            }

            public void Dispose()
            {
                setEnumerator.Dispose();
                ilock.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return setEnumerator.Current; }
            }

            public bool MoveNext()
            {
                return setEnumerator.MoveNext();
            }

            public void Reset()
            {
                setEnumerator.Reset();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MyConcurrentHashSetEnumerator(m_set, m_lock.Acquire());
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new MyConcurrentHashSetEnumerator(m_set, m_lock.Acquire());
        }
    }
}
