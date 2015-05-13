using System;
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
        private IMyStorageDataProvider m_provider;
        private MyStorageDataTypeEnum m_dataType;
        private Vector3I m_leafMin;
        private Vector3I m_leafMax;

        public MyProviderLeaf(IMyStorageDataProvider provider, MyStorageDataTypeEnum dataType, ref Vector3I leafMin, ref Vector3I leafMax)
        {
            m_provider = provider;
            m_dataType = dataType;
            m_leafMin = leafMin;
            m_leafMax = leafMax;
        }

        [Conditional("DEBUG")]
        private void AssertRangeIsInside(int lodIndex, ref Vector3I globalMin, ref Vector3I globalMax)
        {
            var leafMinInLod = m_leafMin >> lodIndex;
            var leafMaxInLod = m_leafMax >> lodIndex;
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
            get { return m_leafMin; }
        }

        bool IMyOctreeLeafNode.ReadOnly
        {
            get { return true; }
        }

        byte IMyOctreeLeafNode.GetFilteredValue()
        {
            // mk:TODO getting single value from provider for this LoD.
            return 0;
        }

        void IMyOctreeLeafNode.ReadRange(MyStorageDataCache target, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod)
        {
            var leafMinInLod = m_leafMin >> lodIndex;
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
