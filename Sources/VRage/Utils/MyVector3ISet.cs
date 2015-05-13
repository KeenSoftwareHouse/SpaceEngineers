using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;

namespace VRage.Utils
{
    /// <summary>
    /// A data structure for a set of Vector3I coordinates optimized for sets with high spatial coherence (hence the name)
    /// </summary>
    public class MyVector3ISet : IEnumerable<Vector3I>
    {
        public struct Enumerator : IEnumerator<Vector3I>
        {
            private Dictionary<Vector3I, ulong>.Enumerator m_dictEnum;
            private int m_shift;
            private ulong m_currentData;
            private MyVector3ISet m_parent;

            private int m_timestamp;

            public Enumerator(MyVector3ISet set)
            {
                m_parent = set;
                m_dictEnum = new Dictionary<Vector3I,ulong>.Enumerator();
                m_shift = 0;
                m_currentData = 0;
                m_timestamp = 0;
                Init();
            }

            private void Init()
            {
                m_dictEnum = m_parent.m_chunks.GetEnumerator();
                m_shift = 63;
                m_currentData = 0;
                m_timestamp = m_parent.Timestamp;
            }

            public Vector3I Current
            {
                get
                {
                    CheckTimestamp();

                    int x = m_shift & 0x3;
                    int y = (m_shift >> 2) & 0x3;
                    int z = (m_shift >> 4);
                    return m_dictEnum.Current.Key * 4 + new Vector3I(x, y, z);
                }
            }

            public bool MoveNext()
            {
                CheckTimestamp();

                while (MoveNextInternal())
                {
                    if ((m_currentData & ((ulong)1 << m_shift)) != 0) return true;
                }
                return false;
            }

            private bool MoveNextInternal()
            {
                if (m_shift == 63)
                {
                    m_shift = 0;
                    if (!m_dictEnum.MoveNext()) return false;
                    m_currentData = m_dictEnum.Current.Value;
                    return true;
                }
                else
                {
                    m_shift++;
                    return true;
                }
            }

            public void Dispose() { }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public void Reset() { Init(); }

            [Conditional("DEBUG")]
            private void CheckTimestamp()
            {
                if (m_timestamp != m_parent.Timestamp)
                    throw new System.InvalidOperationException("A Vector3I set collection was modified during iteration using an enumerator!");
            }
        }

        private Dictionary<Vector3I, ulong> m_chunks;

        /// <summary>
        /// For detection of modification of the set during iteration. Every modifying operation on the set should increase the timestamp
        /// </summary>
        private int Timestamp { get; set; }

        public bool Empty
        {
            get
            {
                return m_chunks.Count == 0;
            }
        }

        public MyVector3ISet()
        {
            m_chunks = new Dictionary<Vector3I, ulong>();
            Timestamp = 0;
        }

        public bool Contains(ref Vector3I position)
        {
            ulong data = 0;
            Vector3I index = new Vector3I(position.X >> 2, position.Y >> 2, position.Z >> 2);
            if (m_chunks.TryGetValue(index, out data))
            {
                return (data & GetMask(ref position)) != 0;
            }
            return false;
        }

        public bool Contains(Vector3I position)
        {
            return Contains(ref position);
        }

        public void Add(ref Vector3I position)
        {
            Vector3I index = new Vector3I(position.X >> 2, position.Y >> 2, position.Z >> 2);
            ulong data = 0;
            m_chunks.TryGetValue(index, out data);
            data |= GetMask(ref position);
            m_chunks[index] = data;

            Timestamp++;
        }

        public void Add(Vector3I position)
        {
            Add(ref position);
        }

        public void Remove(ref Vector3I position)
        {
            Vector3I index = new Vector3I(position.X >> 2, position.Y >> 2, position.Z >> 2);
            ulong data = 0;
            m_chunks.TryGetValue(index, out data);
            data &= ~GetMask(ref position);
            if (data == 0)
                m_chunks.Remove(index);
            else
                m_chunks[index] = data;

            Timestamp++;
        }

        public void Remove(Vector3I position)
        {
            Remove(ref position);
        }

        /// <summary>
        /// Makes a union of this set and the other set and saves it in this set
        /// </summary>
        public void Union(MyVector3ISet otherSet)
        {
            foreach (var chunk in otherSet.m_chunks)
            {
                ulong data = 0;
                m_chunks.TryGetValue(chunk.Key, out data);
                data |= chunk.Value;
                m_chunks[chunk.Key] = data;
            }

            Timestamp++;
        }

        public void Clear()
        {
            m_chunks.Clear();

            Timestamp++;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private static ulong GetMask(ref Vector3I position)
        {
            return (ulong)1 << (((position.Z & 0x3) << 4) + ((position.Y & 0x3) << 2) + (position.X & 0x3));
        }

        IEnumerator<Vector3I> IEnumerable<Vector3I>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
