using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    public abstract class HeapItem<K>
    {
        public int HeapIndex { get; internal set; }
        public K HeapKey { get; internal set; }
    }

    public class MyBinaryHeap<K, V>
        where V: HeapItem<K>
    {
        private HeapItem<K>[] m_array;

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
        private IComparer<K> m_comparer;

        public MyBinaryHeap()
        {
            m_array = new HeapItem<K>[128];
            m_count = 0;
            m_capacity = 128;
            m_comparer = Comparer<K>.Default;
        }

        public MyBinaryHeap(int initialCapacity)
        {
            m_array = new HeapItem<K>[initialCapacity];
            m_count = 0;
            m_capacity = initialCapacity;
            m_comparer = Comparer<K>.Default;
        }

        public MyBinaryHeap(int initialCapacity, IComparer<K> comparer)
        {
            m_array = new HeapItem<K>[initialCapacity];
            m_count = 0;
            m_capacity = initialCapacity;
            Debug.Assert(comparer != null);
            m_comparer = comparer;
        }

        public void Insert(V value, K key)
        {
            if (m_count == m_capacity)
            {
                Reallocate();
            }

            value.HeapKey = key;
            MoveItem(value, m_count);

            Up(m_count);
            m_count++;
        }

        public V GetItem(int index)
        {
            return m_array[index] as V;
        }

        public V Min()
        {
            return (V)m_array[0];
        }

        public V RemoveMin()
        {
            V toReturn = (V)m_array[0];

            if (m_count != 1)
            {
                MoveItem(m_count - 1, 0);
                m_array[m_count - 1] = null;
                m_count--;
                Down(0);
            }
            else
            {
                m_count--;
                m_array[0] = null;
            }

            return toReturn;
        }

        public V RemoveMax()
        {
            Debug.Assert(m_count > 0);

            int maxIndex = 0;

            for (int i = 1; i < m_count; ++i)
            {
                if (m_comparer.Compare(m_array[maxIndex].HeapKey, m_array[i].HeapKey) < 0)
                {
                    maxIndex = i;
                }
            }

            V toReturn = m_array[maxIndex] as V;

            if (maxIndex != m_count)
            {
                MoveItem(m_count - 1, maxIndex);
                Up(maxIndex);
            }
            m_count--;

            return toReturn;
        }

        public void Remove(V item)
        {
            if (m_count != 1)
            {
                if (m_count - 1 == item.HeapIndex)
                {
                    m_array[m_count - 1] = null;
                    m_count--;
                }
                else
                {
                    MoveItem(m_count - 1, item.HeapIndex);
                    m_array[m_count - 1] = null;
                    m_count--;

                    if (m_comparer.Compare(item.HeapKey, m_array[item.HeapIndex].HeapKey) < 0)
                        Down(item.HeapIndex);
                    else
                        Up(item.HeapIndex);
                }
            }
            else
            {
                m_count--;
                m_array[0] = null;
            }
        }

        public void Modify(V item, K newKey)
        {
            K oldKey = item.HeapKey;
            item.HeapKey = newKey;
            if (m_comparer.Compare(oldKey, newKey) <= 0)
            {
                Down(item.HeapIndex);
            }
            else
            {
                Up(item.HeapIndex);
            }
        }

        public void ModifyUp(V item, K newKey)
        {
            item.HeapKey = newKey;
            Up(item.HeapIndex);
        }

        public void ModifyDown(V item, K newKey)
        {
            item.HeapKey = newKey;
            Down(item.HeapIndex);
        }

        public void Clear()
        {
            for (int i = 0; i < m_count; ++i)
            {
                m_array[i] = null;
            }
            m_count = 0;
        }

        private void Up(int index)
        {
            if (index == 0) return;
            int parentIndex = (index - 1) / 2;
            if (m_comparer.Compare(m_array[parentIndex].HeapKey, m_array[index].HeapKey) <= 0) return;

            HeapItem<K> swap = m_array[index];
            while(true)
            {
                MoveItem(parentIndex, index);
                index = parentIndex;

                if (index == 0) break;
                parentIndex = (index - 1) / 2;
                if (m_comparer.Compare(m_array[parentIndex].HeapKey, swap.HeapKey) <= 0) break;
            }

            MoveItem(swap, index);
            return;
        }

        private void Down(int index)
        {
            if (m_count == index + 1) return;

            int left = index * 2 + 1;
            int right = left + 1;

            HeapItem<K> swap = m_array[index];

            while (right <= m_count) // While the current node has children
            {
                if (right == m_count || m_comparer.Compare(m_array[left].HeapKey, m_array[right].HeapKey) < 0) // Only the left child exists or the left child is smaller
                {
                    if (m_comparer.Compare(swap.HeapKey, m_array[left].HeapKey) <= 0)
                        break;

                    MoveItem(left, index);

                    index = left;
                    left = index * 2 + 1;
                    right = left + 1;
                }
                else // Right child exists and is smaller
                {
                    if (m_comparer.Compare(swap.HeapKey, m_array[right].HeapKey) <= 0)
                        break;

                    MoveItem(right, index);

                    index = right;
                    left = index * 2 + 1;
                    right = left + 1;
                }
            }

            MoveItem(swap, index);
        }

        private void MoveItem(int fromIndex, int toIndex)
        {
            m_array[toIndex] = m_array[fromIndex];
            m_array[toIndex].HeapIndex = toIndex;
        }

        private void MoveItem(HeapItem<K> fromItem, int toIndex)
        {
            m_array[toIndex] = fromItem;
            m_array[toIndex].HeapIndex = toIndex;
        }

        private void Reallocate()
        {
            HeapItem<K>[] newArray = new HeapItem<K>[m_capacity * 2];
            Array.Copy(m_array, newArray, m_capacity);

            m_array = newArray;
            m_capacity *= 2;
        }

        public void QueryAll(List<V> list)
        {
            foreach (var heapItem in m_array)
            {
                if (heapItem != null)
                    list.Add((V)heapItem);
            }
        }
    }
}
