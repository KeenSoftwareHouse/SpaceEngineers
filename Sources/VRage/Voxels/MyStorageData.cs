using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using VRage.Common.Utils;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace VRage.Voxels
{
    [Serializable]
    public class MyStorageData
    {
        private byte[][] m_dataByType;

        // optimization for index computation (precomputed steps in each dimension)
        private int m_sZ, m_sY;
        private const int m_sX = 1;

        private Vector3I m_size3d;
        private int m_sizeLinear;
        private int m_dataSizeLinear = -1;

        public byte[] this[MyStorageDataTypeEnum type]
        {
            get
            {
                Debug.Assert(((int)m_storedTypes & (1 << (int)type)) != 0);
                return m_dataByType[(int)type];
            }
            set
            {
                Debug.Assert(value.Length >= m_sizeLinear && (m_dataSizeLinear == -1 || value.Length == m_dataSizeLinear));

                if (m_dataSizeLinear == -1) m_dataSizeLinear = value.Length;

                m_dataByType[(int)type] = value;
            }
        }

        public int SizeLinear
        {
            get { return m_sizeLinear; }
        }
        public int StepLinear
        {
            get { return m_sX; }
        }

        public int StepX
        {
            get { return m_sX; }
        }

        public int StepY
        {
            get { return m_sY; }
        }

        public int StepZ
        {
            get { return m_sZ; }
        }

        public Vector3I Size3D
        {
            get { return m_size3d; }
        }

        private MyStorageDataTypeFlags m_storedTypes;

        public MyStorageData(MyStorageDataTypeFlags typesToStore = MyStorageDataTypeFlags.ContentAndMaterial)
        {
            m_storedTypes = typesToStore;

            m_dataByType = new byte[(int)MyStorageDataTypeEnum.NUM_STORAGE_DATA_TYPES][];
        }

        public MyStorageData(Vector3I size, byte[] content = null, byte[] material = null, byte[] occlusion = null)
        {
            m_dataByType = new byte[(int)MyStorageDataTypeEnum.NUM_STORAGE_DATA_TYPES][];

            Resize(size);

            if (content != null)
            {
                m_storedTypes |= MyStorageDataTypeFlags.Content;
                this[MyStorageDataTypeEnum.Content] = content;
            }

            if (material != null)
            {
                m_storedTypes |= MyStorageDataTypeFlags.Material;
                this[MyStorageDataTypeEnum.Material] = material;
            }

            if (occlusion != null)
            {
                m_storedTypes |= MyStorageDataTypeFlags.Occlusion;
                this[MyStorageDataTypeEnum.Occlusion] = occlusion;
            }
        }

        /**
         * Wreather this storage data should keep occlusion values.
         */
        public bool StoreOcclusion
        {
            get { return m_dataByType[(int)MyStorageDataTypeEnum.Occlusion] != null; }
            set
            {
                var oi = (int)MyStorageDataTypeEnum.Occlusion;
                if (value && m_dataByType[oi] == null)
                {
                    m_dataByType[oi] = new byte[m_dataSizeLinear];
                    m_storedTypes |= MyStorageDataTypeFlags.Occlusion;
                }
                else if (!value && m_dataByType[oi] != null)
                {
                    m_dataByType[oi] = null;
                    m_storedTypes &= ~MyStorageDataTypeFlags.Occlusion;
                }
            }
        }

        /// <param name="start">Inclusive.</param>
        /// <param name="end">Inclusive.</param>
        public void Resize(Vector3I start, Vector3I end)
        {
            Resize(end - start + 1);
        }

        public void Resize(Vector3I size3D)
        {
            m_size3d = size3D;
            int size = size3D.Size;
            m_sY = size3D.X * m_sX;
            m_sZ = size3D.Y * m_sY;

            m_sizeLinear = size * StepLinear;

            for (int i = 0; i < m_dataByType.Length; ++i)
            {
                if ((m_dataByType[i] == null || m_dataByType[i].Length < m_sizeLinear) && m_storedTypes.Requests((MyStorageDataTypeEnum)i))
                {
                    int pow2Size = MathHelper.GetNearestBiggerPowerOfTwo(m_sizeLinear);
                    m_dataSizeLinear = pow2Size;
                    Debug.Assert(pow2Size - m_sizeLinear < m_sizeLinear);
                    m_dataByType[i] = new byte[pow2Size];
                }

            }
        }

        public byte Get(MyStorageDataTypeEnum type, ref Vector3I p)
        {
            AssertPosition(ref p);
            return this[type][p.X * m_sX + p.Y * m_sY + p.Z * m_sZ];
        }

        public byte Get(MyStorageDataTypeEnum type, int linearIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            return this[type][linearIdx];
        }

        public byte Get(MyStorageDataTypeEnum type, int x, int y, int z)
        {
            AssertPosition(x, y, z);
            return this[type][x * m_sX + y * m_sY + z * m_sZ];
        }

        public void Set(MyStorageDataTypeEnum type, ref Vector3I p, byte value)
        {
            AssertPosition(ref p);
            this[type][p.X * m_sX + p.Y * m_sY + p.Z * m_sZ] = value;
        }

        public void Content(ref Vector3I p, byte content)
        {
            AssertPosition(ref p);
            this[MyStorageDataTypeEnum.Content][p.X * m_sX + p.Y * m_sY + p.Z * m_sZ] = content;
        }

        public void Content(int linearIdx, byte content)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            this[MyStorageDataTypeEnum.Content][linearIdx] = content;
        }

        public byte Content(ref Vector3I p)
        {
            AssertPosition(ref p);
            return this[MyStorageDataTypeEnum.Content][p.X * m_sX + p.Y * m_sY + p.Z * m_sZ];
        }

        public byte Content(int x, int y, int z)
        {
            AssertPosition(x, y, z);
            return this[MyStorageDataTypeEnum.Content][x * m_sX + y * m_sY + z * m_sZ];
        }

        public byte Content(int linearIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            return this[MyStorageDataTypeEnum.Content][linearIdx];
        }

        public void Material(ref Vector3I p, byte materialIdx)
        {
            AssertPosition(ref p);
            this[MyStorageDataTypeEnum.Material][p.X * m_sX + p.Y * m_sY + p.Z * m_sZ] = materialIdx;
        }

        public byte Material(ref Vector3I p)
        {
            AssertPosition(ref p);
            return this[MyStorageDataTypeEnum.Material][p.X * m_sX + p.Y * m_sY + p.Z * m_sZ];
        }

        public byte Material(int linearIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            return this[MyStorageDataTypeEnum.Material][linearIdx];
        }

        public void Material(int linearIdx, byte materialIdx)
        {
            Debug.Assert(linearIdx < m_sizeLinear);
            this[MyStorageDataTypeEnum.Material][linearIdx] = materialIdx;
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

        public void BlockFill(MyStorageDataTypeEnum type, Vector3I min, Vector3I max, byte content)
        {
            AssertPosition(ref min);
            AssertPosition(ref max);

            min.Z *= m_sZ;
            max.Z *= m_sZ;

            min.Y *= m_sY;
            max.Y *= m_sY;

            min.X *= m_sX;
            max.X *= m_sX;

            unsafe
            {
                fixed (byte* c = &this[type][0])
                {
                    Vector3I p;
                    for (p.Z = min.Z; p.Z <= max.Z; p.Z += m_sZ)
                    {
                        int z = p.Z;
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

            unsafe
            {
                fixed (byte* c = &this[MyStorageDataTypeEnum.Content][0])
                {
                    Vector3I p;
                    for (p.Z = min.Z; p.Z <= max.Z; p.Z += m_sZ)
                    {
                        int z = p.Z;
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

        // This is broken but since it's not used I will just disable it.
#if false
        public void BlockFillContentExceptCell(Vector3I min, Vector3I max, byte content, Vector3I cellMin, Vector3I cellMax)
        {
            AssertPosition(ref min);
            AssertPosition(ref max);

            min.Z *= m_sZ;
            max.Z *= m_sZ;
            cellMin.Z *= m_sZ;
            cellMax.Z *= m_sZ;

            min.Y *= m_sY;
            max.Y *= m_sY;
            cellMin.Y *= m_sY;
            cellMax.Y *= m_sY;

            min.X *= m_sX;
            max.X *= m_sX;
            cellMin.X *= m_sX;
            cellMax.X *= m_sX;

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
                                if (p.Z < cellMin.Z || p.Z > cellMax.Z
                                    || p.Y < cellMin.Y || p.Y > cellMax.Y
                                    || p.X < cellMin.X || p.X > cellMax.X)
                                    c[p.X + p.Y + z] = content;
                            }
                        }
                    }
                }
            }
        }
#endif

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
            ProfilerShort.Begin("MyStorageData.ContainsIsoSurface");
            try
            {
                var content = this[MyStorageDataTypeEnum.Content];
                bool firstBelow = content[0] < MyVoxelConstants.VOXEL_ISO_LEVEL;
                for (int i = 1; i < m_sizeLinear; i += StepLinear)
                {
                    bool thisBelow = content[i] < MyVoxelConstants.VOXEL_ISO_LEVEL;
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
            ProfilerShort.Begin("MyStorageData.ContainsVoxelsAboveIsoLevel");
            var content = this[MyStorageDataTypeEnum.Content];
            try
            {
                for (int i = 0; i < m_sizeLinear; i += StepLinear)
                {
                    if (content[i] > MyVoxelConstants.VOXEL_ISO_LEVEL)
                        return true;
                }
                return false;
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public int ValueWhenAllEqual(MyStorageDataTypeEnum dataType)
        {
            var data = this[dataType];
            byte first = data[0];
            for (int i = 1; i < m_sizeLinear; i += StepLinear)
            {
                if (first != data[i]) return -1;
            }
            return first;
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
            var content = this[MyStorageDataTypeEnum.Content];
            for (int i = 0; i < m_sizeLinear; i += StepLinear)
                content[i] = p;
        }

        public void ClearMaterials(byte p)
        {
            var material = this[MyStorageDataTypeEnum.Material];
            for (int i = 0; i < m_sizeLinear; i += StepLinear)
                material[i] = p;
        }

        public void Clear(MyStorageDataTypeEnum type, byte p)
        {
            var data = this[type];
            for (int i = 0; i < m_sizeLinear; i += StepLinear)
                data[i] = p;
        }

        public struct MortonEnumerator : IEnumerator<byte>
        {
            private MyStorageDataTypeEnum m_type;
            private MyStorageData m_source;
            private int m_maxMortonCode;
            private int m_mortonCode;
            private Vector3I m_pos;
            private byte m_current;

            public MortonEnumerator(MyStorageData source, MyStorageDataTypeEnum type)
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

        public string ToBase64()
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            return System.Convert.ToBase64String(stream.GetBuffer());
        }

        public static MyStorageData FromBase64(string str)
        {
            MemoryStream stream = new MemoryStream(System.Convert.FromBase64String(str));
            BinaryFormatter formatter = new BinaryFormatter();
            return (MyStorageData)formatter.Deserialize(stream);
        }
    }
}