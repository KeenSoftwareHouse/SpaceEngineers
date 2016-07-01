using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    public class MyBinaryStructHeap<TKey, TValue>
        where TValue: struct
    {
        public struct HeapItem
        {
            public TKey Key { get; internal set; }
            public TValue Value { get; internal set; }

            public override string ToString()
            {
                return Key.ToString() + ": " + Value.ToString();
            }
        }

        private HeapItem[] m_array;

        private int m_count;
        public int Count
        {
            get
            {
                return m_count;
            }
        }

        public bool Full
        {
            get
            {
                return m_count == m_capacity;
            }
        }

        private int m_capacity;
        private IComparer<TKey> m_comparer;

        public MyBinaryStructHeap(int initialCapacity = 128, IComparer<TKey> comparer = null)
        {
            m_array = new HeapItem[initialCapacity];
            m_count = 0;
            m_capacity = initialCapacity;
            m_comparer = comparer ?? Comparer<TKey>.Default;
        }

        public void Insert(TValue value, TKey key)
        {
            if (m_count == m_capacity)
            {
                Reallocate();
            }

            HeapItem item = new HeapItem()
            {
                Key = key,
                Value = value
            };

            m_array[m_count] = item;

            Up(m_count);
            m_count++;
        }

        public TValue Min()
        {
            return m_array[0].Value;
        }

        public TKey MinKey()
        {
            return m_array[0].Key;
        }

        public TValue RemoveMin()
        {
            TValue toReturn = m_array[0].Value;

            if (m_count != 1)
            {
                MoveItem(m_count - 1, 0);
                m_array[m_count - 1].Key = default(TKey);
                m_array[m_count - 1].Value = default(TValue);
                m_count--;
                Down(0);
            }
            else
            {
                m_count--;
                m_array[0].Key = default(TKey);
                m_array[0].Value = default(TValue);
            }

            return toReturn;
        }

        public TValue RemoveMax()
        {
            Debug.Assert(m_count > 0);

            int maxIndex = 0;

            for (int i = 1; i < m_count; ++i)
            {
                if (m_comparer.Compare(m_array[maxIndex].Key, m_array[i].Key) < 0)
                {
                    maxIndex = i;
                }
            }

            TValue toReturn = m_array[maxIndex].Value;

            if (maxIndex != m_count)
            {
                MoveItem(m_count - 1, maxIndex);
                Up(maxIndex);
            }
            m_count--;

            return toReturn;
        }

        public TValue Remove(TValue value, IEqualityComparer<TValue> comparer = null)
        {
            if (m_count == 0)
                return default(TValue);

            if (comparer == null)
                comparer = EqualityComparer<TValue>.Default;

            int itemIndex = 0;

            for (int i = 0; i < m_count; ++i)
            {
                if (comparer.Equals(value, m_array[i].Value))
                {
                    itemIndex = i;
                }
            }

            TValue removed;

            if (itemIndex != m_count)
            {
                removed = m_array[itemIndex].Value;

                MoveItem(m_count - 1, itemIndex);
                Up(itemIndex);
                Down(itemIndex);

                m_count--;
            }
            else
            {
                removed = default(TValue);
            }

            return removed;
        }

        public TValue Remove(TKey key)
        {
            Debug.Assert(m_count > 0);

            int itemIndex = 0;

            for (int i = 1; i < m_count; ++i)
            {
                if (m_comparer.Compare(key, m_array[i].Key) == 0)
                {
                    itemIndex = i;
                }
            }

            TValue removed;

            if (itemIndex != m_count)
            {
                removed = m_array[itemIndex].Value;

                MoveItem(m_count - 1, itemIndex);
                Up(itemIndex);
                Down(itemIndex);
            }
            else
            {
                removed = default(TValue);
            }

            m_count--;

            return removed;
        }

        public void Clear()
        {
            for (int i = 0; i < m_count; ++i)
            {
                m_array[i].Key = default(TKey);
                m_array[i].Value = default(TValue);
            }
            m_count = 0;
        }

        private void Up(int index)
        {
            if (index == 0) return;
            int parentIndex = (index - 1) / 2;
            if (m_comparer.Compare(m_array[parentIndex].Key, m_array[index].Key) <= 0) return;

            HeapItem swap = m_array[index];
            while(true)
            {
                MoveItem(parentIndex, index);
                index = parentIndex;

                if (index == 0) break;
                parentIndex = (index - 1) / 2;
                if (m_comparer.Compare(m_array[parentIndex].Key, swap.Key) <= 0) break;
            }

            MoveItem(ref swap, index);
            return;
        }

        private void Down(int index)
        {
            if (m_count == index + 1) return;

            int left = index * 2 + 1;
            int right = left + 1;

            HeapItem swap = m_array[index];

            while (right <= m_count) // While the current node has children
            {
                if (right == m_count || m_comparer.Compare(m_array[left].Key, m_array[right].Key) < 0) // Only the left child exists or the left child is smaller
                {
                    if (m_comparer.Compare(swap.Key, m_array[left].Key) <= 0)
                        break;

                    MoveItem(left, index);

                    index = left;
                    left = index * 2 + 1;
                    right = left + 1;
                }
                else // Right child exists and is smaller
                {
                    if (m_comparer.Compare(swap.Key, m_array[right].Key) <= 0)
                        break;

                    MoveItem(right, index);

                    index = right;
                    left = index * 2 + 1;
                    right = left + 1;
                }
            }

            MoveItem(ref swap, index);
        }

        private void MoveItem(int fromIndex, int toIndex)
        {
            m_array[toIndex] = m_array[fromIndex];
        }

        private void MoveItem(ref HeapItem fromItem, int toIndex)
        {
            m_array[toIndex] = fromItem;
        }

        private void Reallocate()
        {
            HeapItem[] newArray = new HeapItem[m_capacity * 2];
            Array.Copy(m_array, newArray, m_capacity);

            m_array = newArray;
            m_capacity *= 2;
        }
    }
}
