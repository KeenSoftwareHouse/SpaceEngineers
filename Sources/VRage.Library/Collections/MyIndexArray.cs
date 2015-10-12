using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    /// <summary>
    /// Automatically resizing array when accessing index.
    /// </summary>
    public class MyIndexArray<T>
    {
        T[] m_internalArray;

        public float MinimumGrowFactor = 2;

        public T[] InternalArray
        {
            get { return m_internalArray; }
        }

        public int Length
        {
            get { return m_internalArray.Length; }
        }

        public T this[int index]
        {
            get
            {
                return index >= m_internalArray.Length ? default(T) : m_internalArray[index];
            }
            set
            {
                var oldSize = m_internalArray.Length;
                if (index >= oldSize)
                {
                    int newSize = Math.Max((int)Math.Ceiling(MinimumGrowFactor * oldSize), index + 1);
                    Array.Resize(ref m_internalArray, newSize);
                }
                m_internalArray[index] = value;
            }
        }

        public MyIndexArray(int defaultCapacity = 0)
        {
            m_internalArray = defaultCapacity > 0 ? new T[defaultCapacity] : EmptyArray<T>.Value;
        }

        public void Clear()
        {
            Array.Clear(m_internalArray, 0, m_internalArray.Length);
        }

        public void ClearItem(int index)
        {
            m_internalArray[index] = default(T);
        }

        /// <summary>
        /// Trims end of array which contains default elements.
        /// </summary>
        public void TrimExcess(float minimumShrinkFactor = 0.5f, IEqualityComparer<T> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<T>.Default;

            int i;
            for (i = m_internalArray.Length - 1; i >= 0; i--)
            {
                if (!comparer.Equals(m_internalArray[i], default(T)))
                    break;
            }
            int newSize = i + 1;
            if (newSize <= m_internalArray.Length * minimumShrinkFactor)
            {
                Array.Resize(ref m_internalArray, newSize);
            }
        }
    }
}
