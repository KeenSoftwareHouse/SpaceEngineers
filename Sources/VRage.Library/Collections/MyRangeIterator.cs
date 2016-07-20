using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public struct MyRangeIterator<T> : IEnumerator<T>
    {
        public struct Enumerable : IEnumerable<T>
        {
            private MyRangeIterator<T> m_enumerator;

            public Enumerable(MyRangeIterator<T> enume)
            {
                m_enumerator = enume;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_enumerator;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private T[] m_array;

        private int m_start;
        private int m_current;
        private int m_end;

        public static Enumerable ForRange(T[] array, int start, int end)
        {
            return new Enumerable(new MyRangeIterator<T>(array, start, end));
        }

        public static Enumerable ForRange(List<T> list, int start, int end)
        {
            return new Enumerable(new MyRangeIterator<T>(list, start, end));
        }

        // end is C++ style past end index
        public MyRangeIterator(T[] array, int start, int end)
        {
            m_array = array;
            m_start = start;
            m_current = start - 1;
            m_end = end - 1;
        }

        public MyRangeIterator(List<T> list, int start, int end)
        {
            m_array = list.GetInternalArray();
            m_start = start;
            m_current = start - 1;
            m_end = end - 1;
        }

        public void Dispose()
        {
            m_array = null;
        }

        public bool MoveNext()
        {
            if (m_current != m_end)
            {
                m_current++;
                return true;
            }
            else return false;
        }

        public void Reset()
        {
            m_current = m_start - 1;
        }

        public T Current
        {
            get
            {
                return m_array[m_current];
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}
