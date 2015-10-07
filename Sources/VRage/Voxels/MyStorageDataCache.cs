using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Common.Utils;
using VRage.Utils;
using VRageMath;

namespace VRage.Voxels
{
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
        public int StepLinear
        {
            get { return m_sX; }
        }

        public Vector3I Size3D
        {
            get { return m_size3d; }
        }

        [ThreadStatic]
        private static readonly byte[] unitCache = new byte[2];

        /// <param name="start">Inclusive.</param>
        /// <param name="end">Inclusive.</param>
        public void Resize(Vector3I start, Vector3I end)
        {
            Resize((end - start) + 1);
        }

        public void Resize(Vector3I size3D)
        {
            m_size3d = size3D;
            int size = size3D.Size;
            m_sY = size3D.X * m_sX;
            m_sZ = size3D.Y * m_sY;

            m_sizeLinear = size * StepLinear;
            if (size == 1)
            {
                m_data = unitCache;
            }
            else if (m_data == null || m_data.Length < m_sizeLinear)
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

            Vector3I tempVoxelCoord;
            for (int z = -1; z <= 1; z++)
            {
                tempVoxelCoord.Z = z + p.Z;
                if ((uint)tempVoxelCoord.Z >= (uint)m_size3d.Z)
                    continue;

                for (int y = -1; y <= 1; y++)
                {
                    tempVoxelCoord.Y = y + p.Y;
                    if ((uint)tempVoxelCoord.Y >= (uint)m_size3d.Y)
                        continue;

                    for (int x = -1; x <= 1; x++)
                    {
                        tempVoxelCoord.X = x + p.X;
                        if ((uint)tempVoxelCoord.X >= (uint)m_size3d.X)
                            continue;

                        var content = Content(ref tempVoxelCoord);
                        max = Math.Max(max, content);
                        min = Math.Min(min, content);
                    }
                }
            }

            if (min == max) return false;

            int old = Content(ref p);

            byte newVal = (byte)MyUtils.GetClampInt(old + MyUtils.GetRandomInt(randomizationAdd + randomizationRemove) - randomizationRemove, min, max);

            if (newVal != old)
            {
                Content(ref p, (byte)newVal);
                return true;
            }
            return false;
        }

        public void BlockFillContent(Vector3I min, Vector3I max, byte content)
        {
            AssertPosition(ref min);
            AssertPosition(ref max);

            min.Z *= m_sZ;
            max.Z *= m_sZ;

            min.Y *= m_sY;
            max.Y *= m_sY;

            min.X *= m_sX;
            max.X *= m_sX;

            int field = (int)MyStorageDataTypeEnum.Content;

            unsafe
            {
                fixed (byte* c = &m_data[0])
                {
                    Vector3I p;
                    for (p.Z = min.Z; p.Z <= max.Z; p.Z += m_sZ)
                    {
                        int z = p.Z + field;
                        for (p.Y = min.Y; p.Y <= max.Y; p.Y += m_sY)
                        {
                            for (p.X = min.X; p.X <= max.X; p.X += m_sX)
                            {
                                c[p.X + p.Y + z] = content;
                            }
                        }
                    }
                }
            }
        }

        public void BlockFillMaterial(Vector3I min, Vector3I max, byte materialIdx)
        {
            Vector3I p;
            for (p.Z = min.Z; p.Z <= max.Z; ++p.Z)
            for (p.Y = min.Y; p.Y <= max.Y; ++p.Y)
            for (p.X = min.X; p.X <= max.X; ++p.X)
            {
                Material(ref p, materialIdx);
            }
        }

        public bool ContainsIsoSurface()
        {
            ProfilerShort.Begin("MyStorageDataCache.ContainsIsoSurface");
            try
            {
                int i = (int)MyStorageDataTypeEnum.Content;
                bool firstBelow = m_data[i] < MyVoxelConstants.VOXEL_ISO_LEVEL;
                i += StepLinear;
                for (; i < m_sizeLinear; i += StepLinear)
                {
                    bool thisBelow = m_data[i] < MyVoxelConstants.VOXEL_ISO_LEVEL;
                    if (firstBelow != thisBelow)
                        return true;
                }
                return false;
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public bool ContainsVoxelsAboveIsoLevel()
        {
            ProfilerShort.Begin("MyStorageDataCache.ContainsVoxelsAboveIsoLevel");
            try
            {
                for (int i = (int)MyStorageDataTypeEnum.Content; i < m_sizeLinear; i += StepLinear)
                {
                    if (m_data[i] > MyVoxelConstants.VOXEL_ISO_LEVEL)
                        return true;
                }
                return false;
            }
            finally
            {
                ProfilerShort.End();
            }
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
                m_maxMortonCode = source.Size3D.Size;
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