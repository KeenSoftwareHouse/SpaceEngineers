using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRageMath
{
    public unsafe struct BoxCornerEnumerator : IEnumerator<Vector3>, IEnumerable<Vector3>
    {
        struct TwoVectors
        {
            public Vector3 Min;
            public Vector3 Max;
        }

        static Vector3B* m_indices;

        static BoxCornerEnumerator()
        {
            m_indices = (Vector3B*)(void*)Marshal.AllocHGlobal(sizeof(byte) * 3 * 8);
            m_indices[0] = new Vector3B(0, 4, 5);
            m_indices[1] = new Vector3B(3, 4, 5);
            m_indices[2] = new Vector3B(3, 1, 5);
            m_indices[3] = new Vector3B(0, 1, 5);
            m_indices[4] = new Vector3B(0, 4, 2);
            m_indices[5] = new Vector3B(3, 4, 2);
            m_indices[6] = new Vector3B(3, 1, 2);
            m_indices[7] = new Vector3B(0, 1, 2);
        }

        TwoVectors m_minMax;
        int m_index;

        public BoxCornerEnumerator(Vector3 min, Vector3 max)
        {
            m_minMax.Min = min;
            m_minMax.Max = max;
            m_index = -1;
        }

        public Vector3 Current
        {
            get
            {
                TwoVectors minMax = m_minMax;
                float* ptr = (float*)&minMax;
                var ind = m_indices[m_index];
                return new Vector3(ptr[ind.X], ptr[ind.Y], ptr[ind.Z]);
            }
        }

        public bool MoveNext()
        {
            return ++m_index < 8;
        }

        void IDisposable.Dispose() { }
        object System.Collections.IEnumerator.Current { get { return Current; } }
        void System.Collections.IEnumerator.Reset() { m_index = -1; }

        public BoxCornerEnumerator GetEnumerator()
        {
            return this;
        }

        IEnumerator<Vector3> IEnumerable<Vector3>.GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }
    }
}
