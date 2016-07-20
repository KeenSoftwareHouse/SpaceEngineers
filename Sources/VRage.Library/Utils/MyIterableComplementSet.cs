using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Library.Collections;

namespace VRage.Library.Utils
{

    /**
     * Over a given set of elements W this class maintains two subsets A and B such that
     * A ⋂ B = ∅, A ⋃ B = W, with constant time operations for moving elements from one set to the other.
     * 
     * Both subsets are individually iterable, as well as the whole W.
     * 
     * The order of elements in either set is never preserved.
     * 
     * When using this class with value types beware that they will be duplicated internally.
     * Prefer to use class types or some form of lightweight reference with this.
     */
    public class MyIterableComplementSet<T> : IEnumerable<T>
    {
        private Dictionary<T, int> m_index = new Dictionary<T, int>();
        private List<T> m_data = new List<T>();

        private int m_split = 0;

        // Add an item to the set by default.
        // Cost is O(1)
        public void Add(T item)
        {
            m_index.Add(item, m_data.Count);
            m_data.Add(item);
        }

        // Add an item to the complement.
        // Cost is O(1)
        public void AddToComplement(T item)
        {
            m_index.Add(item, m_data.Count);
            m_data.Add(item);

            MoveToComplement(item);
        }

        // Remove an item from set or complement.
        // Cost is O(1)
        public void Remove(T item)
        {
            Debug.Assert(m_index.ContainsKey(item));

            int rmIndex = m_index[item];

            if (m_split > rmIndex)
            {
                m_split--;
                var lastComplement = m_data[m_split];

                m_index[lastComplement] = rmIndex;

                m_data[rmIndex] = lastComplement;

                rmIndex = m_split;
            }

            int last = m_data.Count - 1;

            m_data[rmIndex] = m_data[last];

            m_index[m_data[last]] = rmIndex;
            m_index.Remove(item);

            m_data.RemoveAt(last);
        }

        // Move an item from set to complement
        // Cost is O(1)
        public void MoveToComplement(T item)
        {
            Debug.Assert(m_index[item] >= m_split);

            var firstSet = m_data[m_split];
            var itemIdx = m_index[item];


            m_data[m_split] = item;
            m_index[item] = m_split;

            m_data[itemIdx] = firstSet;
            m_index[firstSet] = itemIdx;

            ++m_split;
        }

        // Weather either set contains this element.
        public bool Contains(T item)
        {
            return m_index.ContainsKey(item);
        }

        // Checks if an item is in the complement set.
        public bool IsInComplement(T item)
        {
            return m_index[item] < m_split;
        }

        // Move an item from complement to set.
        // Cost is O(1)
        public void MoveToSet(T item)
        {
            Debug.Assert(m_index[item] < m_split);

            --m_split;

            var lastComplement = m_data[m_split];
            var itemIdx = m_index[item];

            m_data[m_split] = item;
            m_index[item] = m_split;

            m_data[itemIdx] = lastComplement;
            m_index[lastComplement] = itemIdx;
        }

        // Remove all elements from set
        // Cost is O(setSize)
        public void ClearSet()
        {
            for (int i = m_split; i < m_data.Count; ++i)
            {
                m_index.Remove(m_data[i]);
            }

            m_data.RemoveRange(m_split, m_data.Count - m_split);
        }

        // Removes all items from the complement set.
        // If this would be called often consider inverting your logic as ClearSet() is faster.
        // Cost is O(n)
        public void ClearComplement()
        {
            for (int i = m_split; i < m_data.Count; ++i)
            {
                m_index.Remove(m_data[i]);
            }

            m_data.RemoveRange(m_split, m_data.Count - m_split);
        }

        // Move all elements to the complement subset.
        public void AllToComplement()
        {
            m_split = m_data.Count;
        }

        // Move all elements to the non-complement subset.
        public void AllToSet()
        {
            m_split = 0;
        }

        // Enumerate the set
        public IEnumerable<T> Set()
        {
            return MyRangeIterator<T>.ForRange(m_data, m_split, m_data.Count);
        }

        // Enumerate the complement set
        public IEnumerable<T> Complement()
        {
            return MyRangeIterator<T>.ForRange(m_data, 0, m_split);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            m_split = 0;
            m_index.Clear();
            m_data.Clear();
        }
    }

    public static class MyIterableComplementSetExtensions
    {
        public static void AddOrEnsureOnComplement<T>(this MyIterableComplementSet<T> self, T item)
        {
            if (!self.Contains(item))
            {
                self.AddToComplement(item);
            }
            else if (!self.IsInComplement(item))
            {
                self.MoveToComplement(item);
            }
        }

        public static void AddOrEnsureOnSet<T>(this MyIterableComplementSet<T> self, T item)
        {
            if (!self.Contains(item))
            {
                self.Add(item);
            }
            else if (self.IsInComplement(item))
            {
                self.MoveToSet(item);
            }
        }

        public static void EnsureOnComplementIfContained<T>(this MyIterableComplementSet<T> self, T item)
        {
            if (self.Contains(item) && !self.IsInComplement(item))
            {
                self.MoveToComplement(item);
            }
        }

        public static void EnsureOnSetIfContained<T>(this MyIterableComplementSet<T> self, T item)
        {
            if (self.Contains(item) && self.IsInComplement(item))
            {
                self.MoveToSet(item);
            }
        }

        public static void RemoveIfContained<T>(this MyIterableComplementSet<T> self, T item)
        {
            if (self.Contains(item))
                self.Remove(item);
        }
    }
}
