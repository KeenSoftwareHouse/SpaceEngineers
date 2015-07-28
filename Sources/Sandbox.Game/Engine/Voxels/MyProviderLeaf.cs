﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    class MyProviderLeaf : IMyOctreeLeafNode
    {
        [ThreadStatic]
        private static MyStorageDataCache m_filteredValueBuffer;
        private static MyStorageDataCache FilteredValueBuffer
        {
            get
            {
                if (m_filteredValueBuffer == null)
                {
                    m_filteredValueBuffer = new MyStorageDataCache();
                    m_filteredValueBuffer.Resize(Vector3I.One);
                }

                return m_filteredValueBuffer;
            }
        }

        private IMyStorageDataProvider m_provider;
        private MyStorageDataTypeEnum m_dataType;
        private MyCellCoord m_cell;

        public MyProviderLeaf(IMyStorageDataProvider provider, MyStorageDataTypeEnum dataType, ref MyCellCoord cell)
        {
            m_provider = provider;
            m_dataType = dataType;
            m_cell = cell;
        }

        [Conditional("DEBUG")]
        private void AssertRangeIsInside(int lodIndex, ref Vector3I globalMin, ref Vector3I globalMax)
        {
            Debug.Assert(m_cell.Lod >= lodIndex);
            var lodShift = m_cell.Lod - lodIndex;
            var lodSize = 1 << lodShift;
            var leafMinInLod = m_cell.CoordInLod << lodShift;
            var leafMaxInLod = leafMinInLod + (lodSize - 1);
            Debug.Assert(globalMin.IsInsideInclusive(ref leafMinInLod, ref leafMaxInLod));
            Debug.Assert(globalMax.IsInsideInclusive(ref leafMinInLod, ref leafMaxInLod));
        }

        MyOctreeStorage.ChunkTypeEnum IMyOctreeLeafNode.SerializedChunkType
        {
            get
            {
                return m_dataType == MyStorageDataTypeEnum.Content
                      ? MyOctreeStorage.ChunkTypeEnum.ContentLeafProvider
                      : MyOctreeStorage.ChunkTypeEnum.MaterialLeafProvider;
            }
        }

        int IMyOctreeLeafNode.SerializedChunkSize
        {
            get { return 0; }
        }

        Vector3I IMyOctreeLeafNode.VoxelRangeMin
        {
            get { return m_cell.CoordInLod << m_cell.Lod; }
        }

        bool IMyOctreeLeafNode.ReadOnly
        {
            get { return true; }
        }

        byte IMyOctreeLeafNode.GetFilteredValue()
        {
            var filteredValueBuffer = FilteredValueBuffer;
            Debug.Assert(filteredValueBuffer.Size3D == Vector3I.One);
            m_provider.ReadRange(filteredValueBuffer, m_dataType, ref Vector3I.Zero, m_cell.Lod, ref m_cell.CoordInLod, ref m_cell.CoordInLod);
            return filteredValueBuffer.Content(0);
        }

        void IMyOctreeLeafNode.ReadRange(MyStorageDataCache target, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod)
        {
            var lodShift = m_cell.Lod - lodIndex;
            var leafMinInLod = m_cell.CoordInLod << lodShift;
            var min = minInLod + leafMinInLod;
            var max = maxInLod + leafMinInLod;
            AssertRangeIsInside(lodIndex, ref min, ref max);
            ProfilerShort.Begin("MyProviderLeaf.ReadRange");
            m_provider.ReadRange(target, m_dataType, ref writeOffset, lodIndex, ref min, ref max);
            ProfilerShort.End();
        }

        void IMyOctreeLeafNode.WriteRange(MyStorageDataCache source, ref Vector3I readOffset, ref Vector3I min, ref Vector3I max)
        {
            throw new NotSupportedException();
        }

        void IMyOctreeLeafNode.OnDataProviderChanged(IMyStorageDataProvider newProvider)
        {
            m_provider = newProvider;
        }

        void IMyOctreeLeafNode.ReplaceValues(Dictionary<byte, byte> oldToNewValueMap)
        {
            // Do nothing. Done explicitly on provider.
        }
    }
}
