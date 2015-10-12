using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    /// <summary>
    /// Stores items in list with fixed index (no reordering).
    /// Null is used as special value and cannot be added into list.
    /// </summary>
    public class MyIndexList<T> : IEnumerable<T>
        where T : class
    {
        public struct Enumerator : IEnumerator<T>
        {
            MyIndexList<T> m_list;
            int m_index;
            int m_version;

            public Enumerator(MyIndexList<T> list)
            {
                m_list = list;
                m_index = -1;
                m_version = list.m_version;
            }
            
            public T Current
            {
                get 
                {
                    if (m_version != m_list.m_version)
                    {
                        throw new InvalidOperationException("Collection was modified after enumerator was created");
                    }
                    return m_list[m_index];
                }
            }

            public bool MoveNext()
            {
                while(true)
                {
                    m_index++;
                    if (m_index >= m_list.Count)
                        return false;
                    else if (m_list[m_index] != null)
                        return true;
                };
            }

            void IDisposable.Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            void System.Collections.IEnumerator.Reset()
            {
                m_index = -1;
                m_version = m_list.m_version;
            }
        }

        private List<T> m_list;
        private Queue<int> m_freeList;
        private int m_version;

        public int Count
        {
            get { return m_list.Count; }
        }

        public T this[int index]
        {
            get { return m_list[index]; }
        }

        /// <summary>
        /// Returns what will be next index returned by Add operation.
        /// </summary>
        public int NextIndex
        {
            get { return m_freeList.Count > 0 ? m_freeList.Peek() : m_list.Count; }
        }

        public MyIndexList(int capacity = 0)
        {
            m_list = new List<T>(capacity);
            m_freeList = new Queue<int>(capacity); // Same capacity as list
        }

        public int Add(T item)
        {
            if (item == null)
                throw new ArgumentException("Null cannot be stored in IndexList, it's used as 'empty' indicator");

            int index;
            if (m_freeList.TryDequeue(out index))
            {
                m_list[index] = item;
                m_version++;
                return index;
            }
            else
            {
                m_list.Add(item);
                m_version++;
                return m_list.Count - 1;
            }
        }

        public void Remove(int index)
        {
           if(!TryRemove(index))
               throw new InvalidOperationException(string.Format("Item at index {0} is already empty", index));
        }

        public void Remove(int index, out T removedItem)
        {
            if (!TryRemove(index, out removedItem))
                throw new InvalidOperationException(string.Format("Item at index {0} is already empty", index));
        }

        public bool TryRemove(int index)
        {
            T item;
            return TryRemove(index, out item);
        }

        public bool TryRemove(int index, out T removedItem)
        {
            removedItem = m_list[index];
            if (removedItem == null)
            {
                return false;
            }
            else
            {
                m_version++;
                m_list[index] = null;
                Debug.Assert(!m_freeList.Contains(index), "Free list will contain same index twice, error!");
                m_freeList.Enqueue(index);
                return true;
            }
        }
        
        Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
