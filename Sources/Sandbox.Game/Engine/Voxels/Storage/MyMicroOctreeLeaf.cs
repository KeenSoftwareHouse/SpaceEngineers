using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.Game;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
    class MyMicroOctreeLeaf : IMyOctreeLeafNode
    {
        const bool DEBUG_WRITES = MyFinalBuildConstants.IS_DEBUG;

        private MySparseOctree m_octree;
        private MyStorageDataTypeEnum m_dataType;
        private Vector3I m_voxelRangeMin;

        public unsafe MyMicroOctreeLeaf(MyStorageDataTypeEnum dataType, int height, Vector3I voxelRangeMin)
        {
            Debug.Assert(dataType == MyStorageDataTypeEnum.Content ||
                         dataType == MyStorageDataTypeEnum.Material);

            m_octree = new MySparseOctree(height, dataType == MyStorageDataTypeEnum.Content
                ? MyOctreeNode.ContentFilter
                : MyOctreeNode.MaterialFilter);
            m_dataType = dataType;
            m_voxelRangeMin = voxelRangeMin;
        }

        internal void BuildFrom(MyStorageData source)
        {
            Debug.Assert(source.Size3D == new Vector3I(m_octree.TreeWidth));
            var enumer = new MyStorageData.MortonEnumerator(source, m_dataType);
            m_octree.Build(enumer);
        }

        internal void BuildFrom(byte singleValue)
        {
            m_octree.Build(singleValue);
        }

        internal void WriteTo(Stream stream)
        {
            m_octree.WriteTo(stream);
        }

        internal unsafe void ReadFrom(MyOctreeStorage.ChunkHeader header, Stream stream)
        {
            if (m_octree == null)
            {
                Debug.Assert(header.ChunkType == MyOctreeStorage.ChunkTypeEnum.ContentLeafOctree ||
                             header.ChunkType == MyOctreeStorage.ChunkTypeEnum.MaterialLeafOctree);
                m_octree = new MySparseOctree(0, header.ChunkType == MyOctreeStorage.ChunkTypeEnum.ContentLeafOctree
                    ? MyOctreeNode.ContentFilter
                    : MyOctreeNode.MaterialFilter);
            }
            m_octree.ReadFrom(header, stream);
        }

        internal bool TryGetUniformValue(out byte uniformValue)
        {
            if (m_octree.IsAllSame)
            {
                uniformValue = m_octree.GetFilteredValue();
                return true;
            }
            else
            {
                uniformValue = 0;
                return false;
            }
        }

        internal void DebugDraw(MyDebugDrawBatchAABB batch, Vector3 worldPos, MyVoxelDebugDrawMode mode)
        {
            m_octree.DebugDraw(batch, worldPos, mode);
        }

        Vector3I IMyOctreeLeafNode.VoxelRangeMin
        {
            get { return m_voxelRangeMin; }
        }

        bool IMyOctreeLeafNode.ReadOnly
        {
            get { return false; }
        }

        void IMyOctreeLeafNode.ReadRange(MyStorageData target, MyStorageDataTypeFlags types, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod, ref MyVoxelRequestFlags flags)
        {
            m_octree.ReadRange(target, m_dataType, ref writeOffset, lodIndex, ref minInLod, ref maxInLod);
        }

        void IMyOctreeLeafNode.WriteRange(MyStorageData source, ref Vector3I readOffset, ref Vector3I min, ref Vector3I max)
        {
            m_octree.WriteRange(source, m_dataType, ref readOffset, ref min, ref max);
            if (DEBUG_WRITES)
            {
                var tmp = new MyStorageData();
                tmp.Resize(min, max);
                m_octree.ReadRange(tmp, m_dataType, ref Vector3I.Zero, 0, ref min, ref max);
                Vector3I p = Vector3I.Zero;
                var cacheEnd = max - min;
                int errorCounter = 0;
                for (var it = new Vector3I_RangeIterator(ref Vector3I.Zero, ref cacheEnd);
                    it.IsValid(); it.GetNext(out p))
                {
                    var read = readOffset + p;
                    if (source.Get(m_dataType, ref read) != tmp.Get(m_dataType, ref p))
                        ++errorCounter;
                }
                Debug.Assert(errorCounter == 0, string.Format("{0} errors writing to leaf octree.", errorCounter));
            }
        }

        byte IMyOctreeLeafNode.GetFilteredValue()
        {
            return m_octree.GetFilteredValue();
        }

        void IMyOctreeLeafNode.OnDataProviderChanged(IMyStorageDataProvider newProvider)
        {
            // do nothing, doesn't depend on provider
        }

        MyOctreeStorage.ChunkTypeEnum IMyOctreeLeafNode.SerializedChunkType
        {
            get
            {
                return m_dataType == MyStorageDataTypeEnum.Content
                      ? MyOctreeStorage.ChunkTypeEnum.ContentLeafOctree
                      : MyOctreeStorage.ChunkTypeEnum.MaterialLeafOctree;
            }
        }

        int IMyOctreeLeafNode.SerializedChunkSize
        {
            get { return m_octree.SerializedSize; }
        }

        void IMyOctreeLeafNode.ReplaceValues(Dictionary<byte, byte> oldToNewValueMap)
        {
            m_octree.ReplaceValues(oldToNewValueMap);
        }


        public ContainmentType Intersect(ref BoundingBoxI box, bool lazy)
        {
            BoundingBoxI localCoords = box;
            localCoords.Translate(-m_voxelRangeMin);

            return m_octree.Intersect(ref localCoords, lazy);
        }

        public bool Intersect(ref LineD line, out double startOffset, out double endOffset)
        {
            line.From -= (Vector3D)m_voxelRangeMin;
            line.To -= (Vector3D)m_voxelRangeMin;

            if (m_octree.Intersect(ref line, out startOffset, out endOffset))
            {
                line.From += (Vector3D)m_voxelRangeMin;
                line.To += (Vector3D)m_voxelRangeMin;
                return true;
            }

            return false;
        }
    }
}