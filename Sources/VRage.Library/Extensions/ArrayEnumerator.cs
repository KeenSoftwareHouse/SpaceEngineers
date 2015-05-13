using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Extensions
{
    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        T[] m_array;
        int m_currentIndex;

        public ArrayEnumerator(T[] array)
        {
            m_array = array;
            m_currentIndex = -1;
        }

        public T Current
        {
            get { return m_array[m_currentIndex]; }
        }

        public void Dispose() { }

        object IEnumerator.Current
        {
            get
            {
                Debug.Fail("Possible boxing!");
                return Current;
            }
        }

        public bool MoveNext()
        {
            ++m_currentIndex;
            return m_currentIndex < m_array.Length;
        }

        public void Reset()
        {
            m_currentIndex = -1;
        }
    }

    public struct ArrayEnumerable<T, TEnumerator> : IEnumerable<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        TEnumerator m_enumerator;
        
        public ArrayEnumerable(TEnumerator enumerator)
        {
            m_enumerator = enumerator;
        }

        public TEnumerator GetEnumerator()
        {
            // Enumerator is struct, return copy
            return m_enumerator;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            Debug.Fail("Boxing!");
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Debug.Fail("Boxing!");
            return GetEnumerator();
        }
    }
}
