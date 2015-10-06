using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.Library.Collections
{
    public class MyConcurrentSortableQueue<T> : IMyQueue<T>, IList<T>
    {
        private readonly List<T> m_list;
        private readonly FastResourceLock m_lock = new FastResourceLock();
        private FastResourceLockExtensions.MyExclusiveLock m_locked;

        /// <summary>
        /// Debug only!! Thread unsafe
        /// </summary>
        public ListReader<T> ListUnsafe { get { return new ListReader<T>(m_list); } }

        /// <summary>
        /// Manage lock yourself when accesing the list!
        /// </summary>
        public List<T> List { get { return m_list; } }

        public MyConcurrentSortableQueue(int reserve)
        {
            m_list = new List<T>(reserve);
        }

        public MyConcurrentSortableQueue()
        {
            m_list = new List<T>();
        }

        /// <summary>
        /// For accessing unsafe members
        /// </summary>
        public void Lock()
        {
            m_locked = m_lock.AcquireExclusiveUsing();
        }

        /// <summary>
        /// For accessing unsafe members
        /// </summary>
        public void Unlock()
        {
            m_locked.Dispose();
        }

        /// <summary>
        /// Does NOT call sort
        /// </summary>
        /// <param name="value"></param>
        public void Add(T value)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_list.Add(value);
            }
        }

        public void Sort(IComparer<T> comparer)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_list.Sort(comparer);
            }
        }

        public bool TryDequeueFront(out T value)
        {
            value = default(T);
            using (m_lock.AcquireExclusiveUsing())
            {
                if (m_list.Count == 0)
                    return false;
                value = m_list[0];
                m_list.RemoveAt(0);
            }
            return true;
        }

        public bool TryDequeueBack(out T value)
        {
            value = default(T);
            using (m_lock.AcquireExclusiveUsing())
            {
                if (m_list.Count == 0)
                    return false;
                value = m_list[m_list.Count - 1];
                m_list.RemoveAt(m_list.Count - 1);
            }
            return true;
        }

        public int Count
        {
            get
            {
                using (m_lock.AcquireSharedUsing())
                    return m_list.Count;
            }
        }

        public bool Empty
        {
            get
            {
                using (m_lock.AcquireSharedUsing())
                    return m_list.Count == 0;
            }
        }


        public int IndexOf(T item)
        {
            using (m_lock.AcquireSharedUsing())
                return m_list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            using (m_lock.AcquireSharedUsing())
                m_list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            using (m_lock.AcquireSharedUsing())
                m_list.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                using (m_lock.AcquireSharedUsing()) return m_list[index];
            }
            set
            {
                using (m_lock.AcquireSharedUsing()) m_list[index] = value;
            }
        }


        public void Clear()
        {
            using (m_lock.AcquireSharedUsing()) m_list.Clear();
        }

        public bool Contains(T item)
        {
            using (m_lock.AcquireSharedUsing()) return m_list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using (m_lock.AcquireSharedUsing()) m_list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            using (m_lock.AcquireSharedUsing()) return m_list.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MyConcurrentListSetEnumerator(this.m_list, m_lock.AcquireExclusiveUsing());
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new MyConcurrentListSetEnumerator(this.m_list, m_lock.AcquireExclusiveUsing());
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void RemoveAll(Predicate<T> callback)
        {
            using (m_lock.AcquireSharedUsing())
                for (int i = 0; i < Count; )
                {
                    if (callback(m_list[i]))
                    {
                        m_list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
        }

        struct MyConcurrentListSetEnumerator : IEnumerator<T>
        {
            IEnumerator<T> setEnumerator;
            FastResourceLockExtensions.MyExclusiveLock ilock;

            public MyConcurrentListSetEnumerator(List<T> set, FastResourceLockExtensions.MyExclusiveLock ilock)
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
    }
}
