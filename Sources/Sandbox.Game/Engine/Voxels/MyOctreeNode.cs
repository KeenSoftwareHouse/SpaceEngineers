using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.Gui;
using VRage.Voxels;
using VRageMath;
using TLeafData = System.Byte;

namespace Sandbox.Engine.Voxels
{
    unsafe struct MyOctreeNode
    {
        public const int CHILD_COUNT = 8;

        public const int SERIALIZED_SIZE = CHILD_COUNT * sizeof(TLeafData) + 1;

        /// <summary>
        /// Computes filtered value given 8 values in child.
        /// </summary>
        /// <param name="pData">Pointer to 8 values. Do NOT go further than that.</param>
        public delegate TLeafData FilterFunction(TLeafData* pData);

        private static readonly Dictionary<byte, int> m_histogram = new Dictionary<TLeafData, int>();

        public static readonly FilterFunction ContentFilter;
        public static readonly FilterFunction MaterialFilter;

        static MyOctreeNode()
        {
            ContentFilter = SignedDistanceFilterInternal;
            MaterialFilter = HistogramFilterInternal;
        }

        public byte ChildMask;
        public fixed TLeafData Data[CHILD_COUNT];

        public MyOctreeNode(TLeafData allContent)
        {
            ChildMask = 0;
            SetAllData(allContent);
        }

        public bool HasChildren
        {
            get { return ChildMask != 0; }
        }

        public void ClearChildren()
        {
            ChildMask = 0;
        }

        public bool HasChild(int childIndex)
        {
            AssertChildIndex(childIndex);
            return (ChildMask & (1 << childIndex)) != 0;
        }

        public void SetChild(int childIndex, bool childPresent)
        {
            AssertChildIndex(childIndex);
            var mask = 1 << childIndex;
            if (childPresent)
                ChildMask |= (byte)mask;
            else
                ChildMask &= (byte)~mask;

            Debug.Assert(HasChild(childIndex) == childPresent);
        }

        public void SetAllData(TLeafData value)
        {
            fixed (TLeafData* ptr = Data) { SetAllData(ptr, value); }
        }

        public static unsafe void SetAllData(TLeafData* dst, TLeafData value)
        {
            for (int i = 0; i < CHILD_COUNT; ++i)
                dst[i] = value;
        }

        public void SetData(int childIndex, TLeafData data)
        {
            AssertChildIndex(childIndex);
            fixed (TLeafData* ptr = Data)
                ptr[childIndex] = data;
        }

        public TLeafData GetData(int cellIndex)
        {
            AssertChildIndex(cellIndex);
            fixed (TLeafData* ptr = Data)
                return ptr[cellIndex];
        }

        public TLeafData ComputeFilteredValue(FilterFunction filter)
        {
            fixed (TLeafData* ptr = Data) { return filter(ptr); }
        }

        public bool AllDataSame()
        {
            fixed (TLeafData* pData = Data) { return AllDataSame(pData); }
        }

        public static unsafe bool AllDataSame(TLeafData* pData)
        {
            TLeafData refValue = pData[0];
            for (int i = 1; i < CHILD_COUNT; ++i)
            {
                if (pData[i] != refValue)
                    return false;
            }
            return true;
        }

        public bool AllDataSame(TLeafData value)
        {
            fixed (TLeafData* pData = Data) { return AllDataSame(pData, value); }
        }

        public static unsafe bool AllDataSame(TLeafData* pData, TLeafData value)
        {
            for (int i = 1; i < CHILD_COUNT; ++i)
            {
                if (pData[i] != value)
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder tmp = new StringBuilder(20);
            tmp.Append("0x").Append(ChildMask.ToString("X2")).Append(": ");
            fixed (TLeafData* ptr = Data)
            {
                for (int i = 0; i < CHILD_COUNT; ++i)
                {
                    if (i != 0)
                        tmp.Append(", ");
                    tmp.Append(ptr[i]);
                }
            }
            return tmp.ToString();
        }

        [Conditional("DEBUG")]
        private void AssertChildIndex(int cellIndex)
        {
            Debug.Assert(0 <= cellIndex && cellIndex < CHILD_COUNT);
        }

        private static float ToSignedDistance(TLeafData value)
        {
            return (value / (float)TLeafData.MaxValue) * 2f - 1f;
        }

        private static TLeafData FromSignedDistance(float value)
        {
            return (TLeafData)((value * 0.5f + 0.5f) * TLeafData.MaxValue + 0.5f);
        }

        /// <summary>
        /// Treats value as normalized signed distance in given LOD. Since LOD size doubles, distance halves for all cases except max value.
        /// </summary>
        private static unsafe TLeafData SignedDistanceFilterInternal(TLeafData* pData)
        {
            float signedDist = ToSignedDistance(pData[0]);
            var average = AverageValueFilterInternal(pData);
            var averageSDist = ToSignedDistance(average);

            if (averageSDist != signedDist || (signedDist != 1f && signedDist != -1f))
            {
                signedDist *= 0.5f; // distance is halved in higher lod since sample size is doubled
                // maybe the average could also be taken into account to get an idea on which side of surface this sample is ...
            }
            return FromSignedDistance(signedDist);
        }

        /// <summary>
        /// Chooses average of all values.
        /// </summary>
        private static unsafe TLeafData AverageValueFilterInternal(TLeafData* pData)
        {
            float sum = 0;
            for (int i = 0; i < CHILD_COUNT; ++i)
                sum += ToSignedDistance(pData[i]);

            sum /= CHILD_COUNT;
            if (sum != 1f && sum != -1f)
            {
                sum *= 0.5f; // distance is halved in higher lod since sample size is doubled
            }
            return FromSignedDistance(sum);
        }

        /// <summary>
        /// Chooses value which is the closest to isosurface level. Whether
        /// from above, or from below is chosen depending on which is majority.
        /// </summary>
        private static unsafe TLeafData IsoSurfaceFilterInternal(TLeafData* pData)
        {
            TLeafData bestBelow = 0;
            TLeafData bestAbove = TLeafData.MaxValue;
            int aboveCount = 0;
            int belowCount = 0;
            for (int i = 0; i < CHILD_COUNT; ++i)
            {
                var data = pData[i];
                if (data < MyVoxelConstants.VOXEL_ISO_LEVEL)
                {
                    ++belowCount;
                    if (data > bestBelow)
                        bestBelow = data;
                }
                else
                {
                    ++aboveCount;
                    if (data < bestAbove)
                        bestAbove = data;
                }
            }

            var res = (belowCount > aboveCount) ? bestBelow : bestAbove;

            float cornerSignedDistance = (res / (float)TLeafData.MaxValue) * 2f - 1f;
            if (cornerSignedDistance != 1f && cornerSignedDistance != -1f)
            {
                cornerSignedDistance *= 0.5f; // distance is halved in higher lod since sample size is doubled
            }
            cornerSignedDistance = cornerSignedDistance * 0.5f + 0.5f;
            return (TLeafData)(cornerSignedDistance * TLeafData.MaxValue);
        }

        /// <summary>
        /// Chooses the most common value.
        /// </summary>
        private static unsafe TLeafData HistogramFilterInternal(TLeafData* pdata)
        {
            for (int i = 0; i < CHILD_COUNT; ++i)
            {
                TLeafData key = pdata[i];
                int count;
                m_histogram.TryGetValue(key, out count);
                ++count;
                m_histogram[key] = count;
            }

            TLeafData mostCommon = default(TLeafData);
            int mostCommonCount = 0;
            foreach (var entry in m_histogram)
            {
                if (entry.Value > mostCommonCount)
                {
                    mostCommonCount = entry.Value;
                    mostCommon = entry.Key;
                }
            }
            m_histogram.Clear();
            return mostCommon;
        }

        public bool AnyAboveIso()
        {
            fixed (byte* data = Data)
                for (int i = 0; i < CHILD_COUNT; ++i)
                {
                    if (data[i] > MyVoxelConstants.VOXEL_ISO_LEVEL) return true;
                }

            return false;
        }
    }

}
