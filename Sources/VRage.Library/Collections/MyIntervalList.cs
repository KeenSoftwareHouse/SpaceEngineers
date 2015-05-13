using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// <para>A set of integer numbers optimized for sets with long consecutive runs. Each interval is stored as two values in m_list: the lower and the upper bound.</para>
    /// <para>For example, the set of numbers 2, 3, 4, 5, 7, 9, 10, 11, 12, 13 (or alternatively in the interval notation &lt;2, 5&gt; U &lt;7, 7&gt; U &lt;9, 13&gt;)
    /// is saved as a list { 2, 5, 7, 7, 9, 13 }</para>
    /// </summary>
    public class MyIntervalList
    {
        public struct Enumerator
        {
            private int m_interval;
            private int m_dist;
            private int m_lowerBound;
            private int m_upperBound;
            private MyIntervalList m_parent;

            public Enumerator(MyIntervalList parent)
            {
                m_interval = -1;
                m_dist = 0;
                m_lowerBound = 0;
                m_upperBound = 0;
                m_parent = parent;
            }

            public int Current { get { return m_lowerBound + m_dist; } }

            public bool MoveNext()
            {
                if (m_interval == -1)
                    return MoveNextInterval();

                if (m_lowerBound + m_dist >= m_upperBound)
                    return MoveNextInterval();

                m_dist++;
                return true;
            }

            private bool MoveNextInterval()
            {
                m_interval++;
                if (m_interval >= m_parent.IntervalCount) return false;

                m_dist = 0;
                m_lowerBound = m_parent.m_list[m_interval * 2];
                m_upperBound = m_parent.m_list[m_interval * 2 + 1];
                return true;
            }
        }

        private List<int> m_list;

        private int m_count = 0;
        public int Count
        {
            get
            {
                return m_count;
            }
        }

        public int IntervalCount
        {
            get
            {
                Debug.Assert(m_list.Count % 2 == 0);
                return m_list.Count / 2;
            }
        }

        public MyIntervalList()
        {
            m_list = new List<int>(8);
        }

        private MyIntervalList(int capacity)
        {
            m_list = new List<int>(capacity);
        }

        public override string ToString()
        {
            string retval = "";
            for (int i = 0; i < m_list.Count; i += 2)
            {
                if (i != 0) retval += "; ";
                retval += "<" + m_list[i] + "," + m_list[i + 1] + ">";
            }
            return retval;
        }

        public int IndexOf(int value)
        {
            // This could be improved by divide and conquer to be O(log(IntervalCount)) instead of O(IntervalCount)
            int retval = 0;
            for (int i = 0; i < m_list.Count; i += 2)
            {
                if (value < m_list[i])
                {
                    return -1;
                }
                if (value <= m_list[i + 1])
                {
                    return retval + value - m_list[i];
                }

                retval += m_list[i + 1] - m_list[i] + 1;
            }

            return -1;
        }

        public int this[int index]
        {
            get
            {
                // This could be improved by divide and conquer to be O(log(IntervalCount)) instead of O(IntervalCount)
                if (index < 0 || index >= m_count)
                {
                    throw new IndexOutOfRangeException("Index " + index + " is out of range in MyIntervalList. Valid indices are in range <0, Count)");
                }

                int counter = index;
                for (int i = 0; i < m_list.Count; i += 2)
                {
                    int currentRange = m_list[i + 1] - m_list[i] + 1;
                    if (counter < currentRange)
                    {
                        return m_list[i] + counter;
                    }

                    counter -= currentRange;
                }

                Debug.Assert(false, "Should not get here");
                return 0;
            }
        }

        /// <summary>
        /// Add a value to the list
        /// </summary>
        public void Add(int value)
        {
            if (value == int.MinValue)
            {
                if (m_list.Count == 0)
                {
                    InsertInterval(0, value, value);
                }
                else if (m_list[0] == int.MinValue + 1)
                {
                    ExtendIntervalDown(0);
                }
                else if (m_list[0] != int.MinValue)
                {
                    InsertInterval(0, value, value);
                }
                return;
            }

            if (value == int.MaxValue)
            {
                int last = m_list.Count - 2;
                if (last < 0)
                {
                    InsertInterval(0, value, value);
                }
                else if (m_list[last + 1] == int.MaxValue - 1)
                {
                    ExtendIntervalUp(last);
                }
                else if (m_list[last + 1] != int.MaxValue)
                {
                    InsertInterval(m_list.Count, value, value);
                }
                return;
            }

            // This could be improved by divide and conquer to be O(log(IntervalCount)) instead of O(IntervalCount)
            for (int i = 0; i < m_list.Count; i += 2)
            {
                if (value + 1 < m_list[i])
                {
                    InsertInterval(i, value, value);
                    return;
                }

                if (value - 1 > m_list[i + 1]) continue;

                if (value + 1 == m_list[i])
                {
                    ExtendIntervalDown(i);
                    return;
                }

                if (value - 1 == m_list[i + 1])
                {
                    ExtendIntervalUp(i);
                    return;
                }

                // Now, the value must lie in the current interval, so we can safely return, doing nothing
                return;
            }

            InsertInterval(m_list.Count, value, value);
        }

        public void Clear()
        {
            m_list.Clear();
            m_count = 0;
        }

        public MyIntervalList GetCopy()
        {
            var copy = new MyIntervalList(m_list.Count);

            for (int i = 0; i < m_list.Count; ++i)
            {
                copy.m_list.Add(m_list[i]);
            }
            copy.m_count = m_count;

            return copy;
        }

        public bool Contains(int value)
        {
            // This could be improved by divide and conquer to be O(log(IntervalCount)) instead of O(IntervalCount)
            for (int i = 0; i < m_list.Count; i += 2)
            {
                if (value < m_list[i]) return false;
                if (value <= m_list[i + 1]) return true;
            }
            return false;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private void InsertInterval(int listPosition, int min, int max)
        {
            Debug.Assert(listPosition % 2 == 0);
            Debug.Assert(listPosition <= m_list.Count);

            if (listPosition == m_list.Count)
            {
                m_list.Add(min);
                m_list.Add(max);

                m_count += max - min + 1;
                return;
            }

            int i = m_list.Count - 2;
            m_list.Add(m_list[i]);
            m_list.Add(m_list[i + 1]);
            for (; i > listPosition; i -= 2)
            {
                m_list[i] = m_list[i - 2];
                m_list[i + 1] = m_list[i - 1];
            }

            m_list[i] = min;
            m_list[i + 1] = max;

            m_count += max - min + 1;
        }

        private void ExtendIntervalDown(int i)
        {
            Debug.Assert(i % 2 == 0);

            m_list[i]--;
            m_count++;

            if (i != 0)
                TryMergeIntervals(i - 1, i);
        }

        private void ExtendIntervalUp(int i)
        {
            Debug.Assert(i % 2 == 0);

            m_list[i + 1]++;
            m_count++;

            if (i < m_list.Count - 2)
                TryMergeIntervals(i + 1, i + 2);
        }

        private void TryMergeIntervals(int i1, int i2)
        {
            Debug.Assert(i1 % 2 == 1);
            Debug.Assert(i2 % 2 == 0);
            Debug.Assert(m_list[i1] < m_list[i2]);

            if (m_list[i1] + 1 != m_list[i2]) return;

            for (int i = i1; i < m_list.Count - 2; ++i)
            {
                m_list[i] = m_list[i + 2];
            }
            m_list.RemoveAt(m_list.Count - 1);
            m_list.RemoveAt(m_list.Count - 1);
        }
    }
}
