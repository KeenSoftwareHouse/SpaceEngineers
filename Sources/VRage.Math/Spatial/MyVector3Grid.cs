using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageMath.Spatial
{
    public class MyVector3Grid<T>
    {
        private struct Entry
        {
            public Vector3 Point;
            public T Data;
            public int NextEntry; // Next entry in this bin of the grid (or next free entry)

            public override string ToString()
            {
                return Point.ToString() + ", -> " + NextEntry.ToString() + ", Data: " + Data.ToString();
            }
        }

        // Note that this will be invalidated when the collection changes (unless otherwise noted)
        public struct SphereQuery
        {
            private MyVector3Grid<T> m_parent;
            private Vector3 m_point;
            private float m_distSq;

            private int m_previousIndex;
            private int m_storageIndex;

            private Vector3I_RangeIterator m_rangeIterator;

            public SphereQuery(MyVector3Grid<T> parent, ref Vector3 point, float dist)
            {
                m_parent = parent;
                m_point = point;
                m_distSq = dist * dist;

                Vector3 offset = new Vector3(dist);
                Vector3I start = m_parent.GetBinIndex(point - offset);
                Vector3I end = m_parent.GetBinIndex(point + offset);

                m_rangeIterator = new Vector3I_RangeIterator(ref start, ref end);

                m_previousIndex = -1;
                m_storageIndex = -1;
            }

            public T Current
            {
                get { return m_parent.m_storage[m_storageIndex].Data; }
            }

            public Vector3I CurrentBin
            {
                get { return m_rangeIterator.Current; }
            }

            public int PreviousIndex
            {
                get { return m_previousIndex; }
            }

            public int StorageIndex
            {
                get { return m_storageIndex; }
            }

            /// <summary>
            /// Removes the current entry and returns true whether there is another entry.
            /// May invalidate indices and queries in the same bin.
            /// To remove values from more queries while ensuring their validity use MyVector3Grid.RemoveTwo(...).
            /// </summary>
            public bool RemoveCurrent()
            {
                Debug.Assert(m_storageIndex != -1);
                m_storageIndex = m_parent.RemoveEntry(m_storageIndex);
                if (m_previousIndex == -1)
                {
                    if (m_storageIndex == -1)
                    {
                        m_parent.m_bins.Remove(m_rangeIterator.Current);
                    }
                    else
                    {
                        m_parent.m_bins[m_rangeIterator.Current] = m_storageIndex;
                    }
                }
                else
                {
                    Entry previousEntry = m_parent.m_storage[m_previousIndex];
                    previousEntry.NextEntry = m_storageIndex;
                    m_parent.m_storage[m_previousIndex] = previousEntry;
                }

                return FindFirstAcceptableEntry();
            }

            public bool MoveNext()
            {
                if (m_storageIndex == -1)
                {
                    if (!FindNextNonemptyBin()) return false;
                }
                else
                {
                    m_previousIndex = m_storageIndex;
                    m_storageIndex = m_parent.m_storage[m_storageIndex].NextEntry;
                }

                return FindFirstAcceptableEntry();
            }

            private bool FindFirstAcceptableEntry()
            {
                while (true)
                {
                    while (m_storageIndex != -1)
                    {
                        Entry current = m_parent.m_storage[m_storageIndex];
                        if ((current.Point - m_point).LengthSquared() < m_distSq) return true;
                        m_previousIndex = m_storageIndex;
                        m_storageIndex = current.NextEntry;
                    }

                    m_rangeIterator.MoveNext();

                    if (!FindNextNonemptyBin()) return false;
                }
            }

            private bool FindNextNonemptyBin()
            {
                m_previousIndex = -1;

                if (!m_rangeIterator.IsValid()) return false;

                Vector3I bin = m_rangeIterator.Current;
                while (!m_parent.m_bins.TryGetValue(bin, out m_storageIndex))
                {
                    m_rangeIterator.GetNext(out bin);
                    if (!m_rangeIterator.IsValid()) return false;
                }

                return true;
            }
        }


        private float m_cellSize;
        private float m_divisor;
        private int m_nextFreeEntry;

        private int m_count;

        List<Entry> m_storage;
        Dictionary<Vector3I, int> m_bins;

        private IEqualityComparer<T> m_equalityComparer;
        public int Count { get { return m_count; } }

        public int InvalidIndex { get { return -1; } }

        public MyVector3Grid(float cellSize) : this(cellSize, EqualityComparer<T>.Default) { }

        public MyVector3Grid(float cellSize, IEqualityComparer<T> comparer)
        {
            m_cellSize = cellSize;
            m_divisor = 1.0f / m_cellSize;

            m_storage = new List<Entry>();
            m_bins = new Dictionary<Vector3I, int>();

            m_equalityComparer = comparer;

            Clear();
        }

        public void Clear()
        {
            m_nextFreeEntry = 0;
            m_count = 0;
            m_storage.Clear();
            m_bins.Clear();
        }

        /// <summary>
        /// Clears the storage faster than clear. Only use for value type T
        /// </summary>
        public void ClearFast()
        {
            m_nextFreeEntry = 0;
            m_count = 0;
            m_storage.SetSize(0);
            m_bins.Clear();
        }

        public void AddPoint(ref Vector3 point, T data)
        {
            Vector3I binIndex = GetBinIndex(ref point);
            int storagePtr;
            if (!m_bins.TryGetValue(binIndex, out storagePtr))
            {
                int newEntry = AddNewEntry(ref point, data);
                m_bins.Add(binIndex, newEntry);
            }
            else
            {
                Entry currentEntry = m_storage[storagePtr];
                int nextEntryPtr = currentEntry.NextEntry;
                while (nextEntryPtr != InvalidIndex)
                {
                    storagePtr = nextEntryPtr;
                    currentEntry = m_storage[storagePtr];
                    nextEntryPtr = currentEntry.NextEntry;
                }

                int newEntry = AddNewEntry(ref point, data);
                currentEntry.NextEntry = newEntry;
                m_storage[storagePtr] = currentEntry;
            }
        }

        // May invalidate indices and queries in the same bin!
        // Also, beware that it removes ALL points with the given coord!
        public void RemovePoint(ref Vector3 point)
        {
            Vector3I binIndex = GetBinIndex(ref point);
            int currentPtr;
            if (m_bins.TryGetValue(binIndex, out currentPtr))
            {
                int previousPtr = InvalidIndex;
                int firstPtr = currentPtr;

                Entry previousEntry = default(Entry);

                while (currentPtr != InvalidIndex)
                {
                    Entry currentEntry = m_storage[currentPtr];
                    if (currentEntry.Point == point)
                    {
                        int nextEntry = RemoveEntry(currentPtr);
                        if (firstPtr == currentPtr)
                        {
                            firstPtr = nextEntry;
                        }
                        else
                        {
                            previousEntry.NextEntry = nextEntry;
                            m_storage[previousPtr] = previousEntry;
                        }
                        currentPtr = nextEntry;
                    }
                    else
                    {
                        previousPtr = currentPtr;
                        previousEntry = currentEntry;
                        currentPtr = currentEntry.NextEntry;
                    }
                }

                if (firstPtr == InvalidIndex)
                {
                    m_bins.Remove(binIndex);
                }
                else
                {
                    m_bins[binIndex] = firstPtr;
                }
            }
            else
            {
                Debug.Assert(false, "Could not find any entry at the given position!");
            }
        }

        // Moves the data at the given index to a new position
        public void MovePoint(int index, ref Vector3 newPosition)
        {
            Entry entry = m_storage[index];
            Vector3I oldBinIndex = GetBinIndex(m_storage[index].Point);
            Vector3I newBinIndex = GetBinIndex(ref newPosition);

            if (oldBinIndex == newBinIndex)
            {
                entry.Point = newPosition;
                m_storage[index] = entry;
            }
            else
            {
                int firstPtr = m_bins[oldBinIndex];
                if (index == firstPtr)
                {
                    int nextPtr = RemoveEntry(index);
                    if (nextPtr == InvalidIndex)
                    {
                        m_bins.Remove(oldBinIndex);
                    }
                    else
                    {
                        m_bins[oldBinIndex] = nextPtr;
                    }
                }
                else
                {
                    int currentPtr = firstPtr;
                    bool removed = false;
                    while (currentPtr != InvalidIndex)
                    {
                        Entry currentEntry = m_storage[currentPtr];
                        int nextPtr = currentEntry.NextEntry;

                        if (nextPtr == index)
                        {
                            currentEntry.NextEntry = RemoveEntry(index);
                            m_storage[currentPtr] = currentEntry;
                            removed = true;
                            break;
                        }

                        currentPtr = nextPtr;
                    }
                    Debug.Assert(removed, "Did not remove the entry in the old bin!");
                }

                AddPoint(ref newPosition, entry.Data);
            }
        }

        /// <summary>
        /// Returns the index of the point containing the given data on the given position
        /// The index is only valid as long as the grid does not change!
        /// </summary>
        public int FindPointIndex(ref Vector3 point, T data)
        {
            Vector3I binIndex = GetBinIndex(ref point);
            int storagePtr = InvalidIndex;
            m_bins.TryGetValue(binIndex, out storagePtr);

            while (storagePtr != InvalidIndex)
            {
                Entry entry = m_storage[storagePtr];
                if (entry.Point == point && m_equalityComparer.Equals(entry.Data, data))
                    return storagePtr;

                storagePtr = entry.NextEntry;
            }

            return storagePtr;
        }

        /// <summary>
        /// Returns the data stored at the given index
        /// </summary>
        public T GetData(int index)
        {
            return m_storage[index].Data;
        }

        /// <summary>
        /// Returns the point at which the data is stored at the given index
        /// </summary>
        public Vector3 GetPoint(int index)
        {
            return m_storage[index].Point;
        }

        /// <summary>
        /// Returns a query for iterating over points inside a sphere of radius dist around the given point
        /// </summary>
        public SphereQuery QueryPointsSphere(ref Vector3 point, float dist)
        {
            return new SphereQuery(this, ref point, dist);
        }

        /// <summary>
        /// Removes values pointed at by en0 and en1 and ensures that the queries both stay consistent
        /// </summary>
        public void RemoveTwo(ref SphereQuery en0, ref SphereQuery en1)
        {
            if (en0.CurrentBin == en1.CurrentBin)
            {
                if (en0.StorageIndex == en1.PreviousIndex)
                {
                    en1.RemoveCurrent();
                    en0.RemoveCurrent();
                    en1 = en0;
                }
                else if (en1.StorageIndex == en0.PreviousIndex)
                {
                    en0.RemoveCurrent();
                    en1.RemoveCurrent();
                    en0 = en1;
                }
                else if (en0.StorageIndex == en1.StorageIndex)
                {
                    en0.RemoveCurrent();
                    en1 = en0;
                }
                else
                {
                    en0.RemoveCurrent();
                    en1.RemoveCurrent();
                }
            }
            else
            {
                en0.RemoveCurrent();
                en1.RemoveCurrent();
            }
        }

        public Dictionary<Vector3I, int>.Enumerator EnumerateBins()
        {
            return m_bins.GetEnumerator();
        }

        public int GetNextBinIndex(int currentIndex)
        {
            if (currentIndex == InvalidIndex) return InvalidIndex;
            Debug.Assert(currentIndex < m_storage.Count);
            return m_storage[currentIndex].NextEntry;
        }

        public void GetLocalBinBB(ref Vector3I binPosition, out BoundingBoxD output)
        {
            output.Min = binPosition * m_cellSize;
            output.Max = output.Min + new Vector3(m_cellSize);
        }

        public void CollectStorage(int startingIndex, ref List<T> output)
        {
            CheckIndexIsValid(startingIndex);

            output.Clear();

            var entry = m_storage[startingIndex];
            output.Add(entry.Data);
            while (entry.NextEntry != InvalidIndex)
            {
                entry = m_storage[entry.NextEntry];
                output.Add(entry.Data);
            }
        }

        public void CollectEntireStorage(List<T> output)
        {
            output.Clear();
            foreach (var binEntry in m_bins)
            {
                int storagePtr = binEntry.Value;
                do
                {
                    var entry = m_storage[storagePtr];
                    output.Add(entry.Data);
                    storagePtr = entry.NextEntry;
                } while (storagePtr != InvalidIndex);
            }
        }

        private Vector3I GetBinIndex(ref Vector3 point)
        {
            Vector3I binIndex = Vector3I.Floor(point * m_divisor);
            return binIndex;
        }

        private Vector3I GetBinIndex(Vector3 point)
        {
            return GetBinIndex(ref point);
        }

        private int AddNewEntry(ref Vector3 point, T data)
        {
            m_count++;
            if (m_nextFreeEntry == m_storage.Count)
            {
                m_storage.Add(new Entry() { Point = point, Data = data, NextEntry = InvalidIndex });
                return m_nextFreeEntry++;
            }
            else
            {
                Debug.Assert((uint)m_nextFreeEntry <= m_storage.Count);
                if ((uint)m_nextFreeEntry > m_storage.Count) return -1;

                int newEntry = m_nextFreeEntry;
                m_nextFreeEntry = m_storage[m_nextFreeEntry].NextEntry;
                m_storage[newEntry] = new Entry() { Point = point, Data = data, NextEntry = InvalidIndex };
                return newEntry;
            }
        }

        /// <summary>
        /// Removes entry with the given index and returns the index of the next entry (or -1 if the removed entry was the last one)
        /// </summary>
        private int RemoveEntry(int toRemove)
        {
            CheckIndexIsValid(toRemove);

            m_count--;

            Entry removedEntry = m_storage[toRemove];
            int nextEntry = removedEntry.NextEntry;

            removedEntry.NextEntry = m_nextFreeEntry;
            removedEntry.Data = default(T); // To avoid keeping references in case T is a class type
            m_nextFreeEntry = toRemove;
            m_storage[toRemove] = removedEntry;

            return nextEntry;
        }

        [Conditional("DEBUG")]
        private void CheckIndexIsValid(int index)
        {
            Debug.Assert((index >= 0 && index < m_storage.Count), "MyVector3Grid storage index overflow!");
            int freeIndex = m_nextFreeEntry;
            while (freeIndex != InvalidIndex && freeIndex != m_storage.Count)
            {
                Debug.Assert(index != freeIndex, "MyVector3Grid storage index was freed!");
                freeIndex = m_storage[freeIndex].NextEntry;
            }
        }
    }
}
