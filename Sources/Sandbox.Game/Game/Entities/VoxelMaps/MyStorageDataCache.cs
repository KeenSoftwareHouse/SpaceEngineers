using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Common.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.VoxelMaps
{
    public enum MyStorageDataTypeEnum
    {
        Content,
        Material
    }

    public class MyStorageDataCache
    {
        private byte[] m_data;

        // optimization for index computation (precomputed steps in each dimension)
        private int m_sZ, m_sY;
        private const int m_sX = 2;

        private Vector3I m_size3d;
        private int m_sizeLinear;

        public byte[] Data
        {
            get { return m_data; }
        }

        public int SizeLinear
        {
            get { return m_sizeLinear; }
        }
        public const int StepLinear = m_sX;

        public Vector3I Size3D
        {
            get { return m_size3d; }
        }

        /// <param name="start">Inclusive.</param>
        /// <param name="end">Inclusive.</param>
        public void Resize(ref Vector3I start, ref Vector3I end)
        {
            var size = (end - start) + 1;
            Resize(ref size);
        }

        public void Resize(ref Vector3I size3D)
        {
            m_size3d = size3D;
            int size = size3D.Size();
            m_sY = size3D.X * m_sX;
            m_sZ = size3D.Y * m_sY;

            m_sizeLinear = size * StepLinear;
            if (m_data == null || m_data.Length < m_sizeLinear)
            {
                int pow2Size = MathHelper.GetNearestBiggerPowerOfTwo(m_sizeLinear);
                Debug.Assert((pow2Size - m_sizeLinear) < m_sizeLinear);
                m_data = new byte[pow2Size];
            }
        }

        public byte Get(MyStorageDataTypeEnum type, ref Vector3I p)
        {
            AssertPosition(ref p);
            return m_data[p.X * m_sX + p.Y * m_sY + p.Z * m_sZ + (int)type];
        }

        public byte Get(MyStorageDataTypeEnum type, int linearIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            return m_data[linearIdx + (int)type];
        }

        public byte Get(MyStorageDataTypeEnum type, int x, int y, int z)
        {
            AssertPosition(x, y, z);
            return m_data[x * m_sX + y * m_sY + z * m_sZ + (int)type];
        }

        public void Set(MyStorageDataTypeEnum type, ref Vector3I p, byte value)
        {
            AssertPosition(ref p);
            m_data[p.X * m_sX + p.Y * m_sY + p.Z * m_sZ + (int)type] = value;
        }

        public void Content(ref Vector3I p, byte content)
        {
            AssertPosition(ref p);
            m_data[p.X * m_sX + p.Y * m_sY + p.Z * m_sZ + (int)MyStorageDataTypeEnum.Content] = content;
        }

        public void Content(int linearIdx, byte content)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            m_data[linearIdx + (int)MyStorageDataTypeEnum.Content] = content;
        }

        public byte Content(ref Vector3I p)
        {
            AssertPosition(ref p);
            return m_data[p.X * m_sX + p.Y * m_sY + p.Z * m_sZ + (int)MyStorageDataTypeEnum.Content];
        }

        public byte Content(int x, int y, int z)
        {
            AssertPosition(x, y, z);
            return m_data[x * m_sX + y * m_sY + z * m_sZ + (int)MyStorageDataTypeEnum.Content];
        }

        public byte Content(int linearIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            return m_data[linearIdx + (int)MyStorageDataTypeEnum.Content];
        }

        public void Material(ref Vector3I p, byte materialIdx)
        {
            AssertPosition(ref p);
            m_data[p.X * m_sX + p.Y * m_sY + p.Z * m_sZ + (int)MyStorageDataTypeEnum.Material] = materialIdx;
        }

        public byte Material(ref Vector3I p)
        {
            AssertPosition(ref p);
            return m_data[p.X * m_sX + p.Y * m_sY + p.Z * m_sZ + (int)MyStorageDataTypeEnum.Material];
        }

        public byte Material(int linearIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            return m_data[linearIdx + (int)MyStorageDataTypeEnum.Material];
        }

        public void Material(int linearIdx, byte materialIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            m_data[linearIdx + (int)MyStorageDataTypeEnum.Material] = materialIdx;
        }

        public int ComputeLinear(ref Vector3I p)
        {
            return p.X * m_sX + p.Y * m_sY + p.Z * m_sZ;
        }

        public bool WrinkleVoxelContent(ref Vector3I p, float wrinkleWeightAdd, float wrinkleWeightRemove)
        {
            int max = Int32.MinValue, min = Int32.MaxValue;
            int randomizationAdd = (int)(wrinkleWeightAdd * 255);
            int randomizationRemove = (int)(wrinkleWeightRemove * 255);

            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        Vector3I tempVoxelCoord = new Vector3I(p.X + x, p.Y + y, p.Z + z);
                        var content = Content(ref tempVoxelCoord);
                        max = Math.Max(max, content);
                        min = Math.Min(min, content);
                    }
                }
            }

            if (min == max) return false;

            int old = Content(ref p);

            byte newVal = (byte)MyVRageUtils.GetClampInt(old + MyVRageUtils.GetRandomInt(randomizationAdd + randomizationRemove) - randomizationRemove, min, max);
            newVal = MyCellStorage.Quantizer.QuantizeValue(newVal);

            if (newVal != old)
            {
                Content(ref p, (byte)newVal);
                return true;
            }
            return false;
        }

        [Conditional("DEBUG")]
        private void AssertPosition(ref Vector3I position)
        {
            Debug.Assert(0 <= position.X && position.X < m_size3d.X);
            Debug.Assert(0 <= position.Y && position.Y < m_size3d.Y);
            Debug.Assert(0 <= position.Z && position.Z < m_size3d.Z);
        }

        [Conditional("DEBUG")]
        private void AssertPosition(int x, int y, int z)
        {
            Debug.Assert(0 <= x && x < m_size3d.X);
            Debug.Assert(0 <= y && y < m_size3d.Y);
            Debug.Assert(0 <= z && z < m_size3d.Z);
        }

        public void ClearContent(byte p)
        {
            const MyStorageDataTypeEnum type = MyStorageDataTypeEnum.Content;
            for (int i = (int)type; i < m_sizeLinear; i += StepLinear)
                m_data[i] = p;
        }

        public void ClearMaterials(byte p)
        {
            const MyStorageDataTypeEnum type = MyStorageDataTypeEnum.Material;
            for (int i = (int)type; i < m_sizeLinear; i += StepLinear)
                m_data[i] = p;
        }

        public struct MortonEnumerator : IEnumerator<byte>
        {
            private MyStorageDataTypeEnum m_type;
            private MyStorageDataCache m_source;
            private int m_maxMortonCode;
            private int m_mortonCode;
            private Vector3I m_pos;
            private byte m_current;

            public MortonEnumerator(MyStorageDataCache source, MyStorageDataTypeEnum type)
            {
                Debug.Assert(source.Size3D.X == source.Size3D.Y && source.Size3D.Y == source.Size3D.Z);
                Debug.Assert(source.Size3D.IsPowerOfTwo);
                m_type = type;
                m_source = source;
                m_maxMortonCode = source.Size3D.Size();
                m_mortonCode = -1;
                m_pos = default(Vector3I);
                m_current = 0;
            }

            public byte Current
            {
                get { return m_current; }
            }

            public void Dispose() { }

            object System.Collections.IEnumerator.Current
            {
                get { return m_current; }
            }

            public bool MoveNext()
            {
                ++m_mortonCode;
                if (m_mortonCode < m_maxMortonCode)
                {
                    MyMortonCode3D.Decode(m_mortonCode, out m_pos);
                    m_current = m_source.Get(m_type, ref m_pos);
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                m_mortonCode = -1;
                m_current = 0;
            }
        }

    }

}
