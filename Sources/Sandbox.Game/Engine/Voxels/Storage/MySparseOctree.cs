using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRage.Profiler;
using VRage.Voxels;
using VRageMath;
using TLeafData = System.Byte;
using TNodeKey = System.UInt32;

namespace Sandbox.Engine.Voxels
{
    partial class MySparseOctree
    {
        private readonly Dictionary<TNodeKey, MyOctreeNode> m_nodes = new Dictionary<TNodeKey, MyOctreeNode>();
        private MyOctreeNode.FilterFunction m_nodeFilter;

        private int m_treeHeight;
        private int m_treeWidth; // Number of leaves in each dimension
        private TLeafData m_defaultContent;

        public int TreeWidth
        {
            get { return m_treeWidth; }
        }

        public bool IsAllSame
        {
            get
            {
                var root = m_nodes[ComputeRootKey()];
                unsafe { return !root.HasChildren && MyOctreeNode.AllDataSame(root.Data); }
            }
        }

        public unsafe MySparseOctree(int height, MyOctreeNode.FilterFunction nodeFilter, TLeafData defaultContent = default(TLeafData))
        {
            Debug.Assert(height < MyCellCoord.MAX_LOD_COUNT, String.Format("Height must be < {0}", MyCellCoord.MAX_LOD_COUNT));
            m_treeHeight = height;
            m_treeWidth = 1 << height;
            m_defaultContent = defaultContent;
            m_nodeFilter = nodeFilter;
        }

        #region Octree construction

        struct StackData<T> where T : struct, IEnumerator<TLeafData>
        {
            public T Data;
            public MyCellCoord Cell;
            public MyOctreeNode DefaultNode;
        }

        public void Build<TDataEnum>(TDataEnum data) where TDataEnum : struct, IEnumerator<TLeafData>
        {
            m_nodes.Clear();
            MyOctreeNode rootNode;
            StackData<TDataEnum> stack;
            stack.Data = data;
            stack.Cell = new MyCellCoord(m_treeHeight - 1, ref Vector3I.Zero);
            stack.DefaultNode = new MyOctreeNode(m_defaultContent);
            BuildNode(ref stack, out rootNode);
            m_nodes[ComputeRootKey()] = rootNode;
            Debug.Assert(stack.Data.MoveNext() == false);

            if (false)
            {
                CheckData(data);
            }
        }

        private unsafe void BuildNode<TDataEnum>(ref StackData<TDataEnum> stack, out MyOctreeNode builtNode) where TDataEnum : struct, IEnumerator<TLeafData>
        {
            var currentNode = stack.DefaultNode;
            if (stack.Cell.Lod == 0)
            { // bottom level containing leaf data
                for (int i = 0; i < 8; ++i)
                {
                    bool movedNext = stack.Data.MoveNext();
                    Debug.Assert(movedNext);
                    currentNode.Data[i] = stack.Data.Current;
                }
            }
            else
            {
                --stack.Cell.Lod;
                Vector3I currentPosition = stack.Cell.CoordInLod;
                Vector3I childBase = currentPosition << 1;
                Vector3I childOffset;
                MyOctreeNode childNode;
                for (int i = 0; i < 8; ++i)
                {
                    ComputeChildCoord(i, out childOffset);
                    stack.Cell.CoordInLod = childBase + childOffset;
                    BuildNode(ref stack, out childNode);
                    if (!childNode.HasChildren && MyOctreeNode.AllDataSame(childNode.Data))
                    {
                        currentNode.SetChild(i, false);
                        currentNode.Data[i] = childNode.Data[0];
                    }
                    else
                    {
                        currentNode.SetChild(i, true);
                        currentNode.Data[i] = m_nodeFilter(childNode.Data, stack.Cell.Lod);
                        m_nodes.Add(stack.Cell.PackId32(), childNode);
                    }
                }
                ++stack.Cell.Lod;
                stack.Cell.CoordInLod = currentPosition;
            }

            builtNode = currentNode;
        }

        public unsafe void Build(TLeafData singleValue)
        {
            m_nodes.Clear();

            MyOctreeNode rootNode;
            rootNode.ChildMask = 0;
            for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
            {
                rootNode.Data[i] = singleValue;
            }

            m_nodes[ComputeRootKey()] = rootNode;
        }

        #endregion

        internal unsafe TLeafData GetFilteredValue()
        {
            var root = m_nodes[ComputeRootKey()];
            return m_nodeFilter(root.Data, m_treeHeight);
        }

        internal unsafe void ReadRange(MyStorageData target, MyStorageDataTypeEnum type, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod)
        {
            ProfilerShort.Begin("MySparseOctree2.ReadRangeToContent"); try
            {

                int stackIdx = 0;
                int stackSize = MySparseOctree.EstimateStackSize(m_treeHeight);
                MyCellCoord* stack = stackalloc MyCellCoord[stackSize];
                MyCellCoord data = new MyCellCoord(m_treeHeight - 1, ref Vector3I.Zero);
                stack[stackIdx++] = data;

                MyOctreeNode node;
                Vector3I childPosRelative, min, max, nodePositionInChild;
                int lodDiff;
                while (stackIdx > 0)
                {
                    Debug.Assert(stackIdx <= stackSize);
                    data = stack[--stackIdx];
                    node = m_nodes[data.PackId32()];

                    lodDiff = data.Lod - lodIndex;
                    min = minInLod >> lodDiff;
                    max = maxInLod >> lodDiff;
                    nodePositionInChild = data.CoordInLod << 1;
                    min -= nodePositionInChild;
                    max -= nodePositionInChild;
                    for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                    {
                        ComputeChildCoord(i, out childPosRelative);
                        if (!childPosRelative.IsInsideInclusive(ref min, ref max))
                            continue;
                        if (lodIndex < data.Lod && node.HasChild(i))
                        {
                            Debug.Assert(stackIdx < stackSize);
                            stack[stackIdx++] = new MyCellCoord(data.Lod - 1, nodePositionInChild + childPosRelative);
                        }
                        else
                        {
                            var nodeData = node.Data[i];
                            var childMin = nodePositionInChild + childPosRelative;
                            if (lodDiff == 0)
                            {
                                var write = writeOffset + childMin - minInLod;
                                target.Set(type, ref write, nodeData);
                            }
                            else
                            {
                                childMin <<= lodDiff;
                                var childMax = childMin + (1 << lodDiff) - 1;
                                Vector3I.Max(ref childMin, ref minInLod, out childMin);
                                Vector3I.Min(ref childMax, ref maxInLod, out childMax);
                                for (int z = childMin.Z; z <= childMax.Z; ++z)
                                    for (int y = childMin.Y; y <= childMax.Y; ++y)
                                        for (int x = childMin.X; x <= childMax.X; ++x)
                                        {
                                            var write = writeOffset;
                                            write.X += x - minInLod.X;
                                            write.Y += y - minInLod.Y;
                                            write.Z += z - minInLod.Z;
                                            target.Set(type, ref write, nodeData);
                                        }
                            }
                        }
                    }
                }

            }
            finally { ProfilerShort.End(); }
        }

        internal void WriteRange(MyStorageData source, MyStorageDataTypeEnum type, ref Vector3I readOffset, ref Vector3I min, ref Vector3I max)
        {
            ProfilerShort.Begin("MySparseOctree2.WriteRange");
            WriteRange(new MyCellCoord(m_treeHeight - 1, Vector3I.Zero), m_defaultContent, source, type, ref readOffset, ref min, ref max);
            ProfilerShort.End();
        }

        private unsafe void WriteRange(
            MyCellCoord cell,
            TLeafData defaultData,
            MyStorageData source,
            MyStorageDataTypeEnum type,
            ref Vector3I readOffset,
            ref Vector3I min,
            ref Vector3I max)
        {
            var nodeKey = cell.PackId32();
            MyOctreeNode node;

            if (!m_nodes.TryGetValue(nodeKey, out node))
            {
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                    node.Data[i] = defaultData;
            }

            if (cell.Lod == 0)
            {
                var childBase = cell.CoordInLod << 1;
                Vector3I childOffset;
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    ComputeChildCoord(i, out childOffset);
                    var child = childBase + childOffset;
                    if (!child.IsInsideInclusive(ref min, ref max))
                        continue;

                    child -= min;
                    child += readOffset;
                    node.Data[i] = source.Get(type, ref child);
                }
                m_nodes[nodeKey] = node;
            }
            else
            {
                var childBase = cell.CoordInLod << 1;
                Vector3I childOffset;
                var minInChild = (min >> cell.Lod) - childBase;
                var maxInChild = (max >> cell.Lod) - childBase;
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    ComputeChildCoord(i, out childOffset);
                    if (!childOffset.IsInsideInclusive(ref minInChild, ref maxInChild))
                        continue;

                    var childCell = new MyCellCoord(cell.Lod - 1, childBase + childOffset);
                    WriteRange(childCell, node.Data[i], source, type, ref readOffset, ref min, ref max);
                    var childKey = childCell.PackId32();
                    var childNode = m_nodes[childKey];
                    if (!childNode.HasChildren && MyOctreeNode.AllDataSame(childNode.Data))
                    {
                        node.SetChild(i, false);
                        node.Data[i] = childNode.Data[0];
                        m_nodes.Remove(childKey);
                    }
                    else
                    {
                        node.SetChild(i, true);
                        node.Data[i] = m_nodeFilter(childNode.Data, cell.Lod);
                    }
                }

                m_nodes[nodeKey] = node;
            }

        }

        #region Debug checks

        [Conditional("DEBUG")]
        private void CheckData<T>(T data) where T : struct, IEnumerator<TLeafData>
        {
            CheckData(ref data, new MyCellCoord(m_treeHeight - 1, ref Vector3I.Zero));
            Debug.Assert(!data.MoveNext());
        }

        [Conditional("DEBUG")]
        private unsafe void CheckData<T>(ref T data, MyCellCoord cell) where T : struct, IEnumerator<TLeafData>
        {
            var key = cell.PackId32();
            var node = m_nodes[key];
            for (int i = 0; i < 8; ++i)
            {
                if (node.HasChild(i))
                {
                    Vector3I childOffset;
                    ComputeChildCoord(i, out childOffset);
                    CheckData(ref data, new MyCellCoord(cell.Lod - 1, (cell.CoordInLod << 1) + childOffset));
                }
                else
                {
                    int numNodes = 1 << (3 * cell.Lod);
                    for (int j = 0; j < numNodes; ++j)
                    {
                        Debug.Assert(data.MoveNext());
                        Debug.Assert(node.Data[i] == data.Current);
                    }
                }
            }
        }

        #endregion

        private TNodeKey ComputeRootKey()
        {
            return new MyCellCoord(m_treeHeight - 1, ref Vector3I.Zero).PackId32();
        }

        private void ComputeChildCoord(int childIdx, out Vector3I relativeCoord)
        {
            Debug.Assert(childIdx < 8);
            relativeCoord.X = (childIdx >> 0) & 1;
            relativeCoord.Y = (childIdx >> 1) & 1;
            relativeCoord.Z = (childIdx >> 2) & 1;
        }

        internal unsafe void DebugDraw(VRageRender.MyDebugDrawBatchAABB batch, Vector3 worldPos, MyVoxelDebugDrawMode mode)
        {
            switch (mode)
            {
                case MyVoxelDebugDrawMode.Content_MicroNodes:
                    foreach (var entry in m_nodes)
                    {
                        Vector3I childOffset;
                        BoundingBoxD bb;
                        MyCellCoord cell = new MyCellCoord();
                        cell.SetUnpack(entry.Key);
                        var data = entry.Value;
                        for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                        {
                            if (data.HasChild(i) && cell.Lod != 0)
                                continue;
                            ComputeChildCoord(i, out childOffset);
                            var childPos = (cell.CoordInLod << (cell.Lod + 1)) + (childOffset << cell.Lod);
                            bb.Min = worldPos + childPos * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                            bb.Max = bb.Min + MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << cell.Lod);
                            batch.Add(ref bb);
                        }
                    }
                    break;

                case MyVoxelDebugDrawMode.Content_MicroNodesScaled:
                    foreach (var entry in m_nodes)
                    {
                        MyCellCoord cell = new MyCellCoord();
                        Vector3I childOffset;
                        BoundingBoxD bb;
                        cell.SetUnpack(entry.Key);
                        var data = entry.Value;
                        for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                        {
                            if (data.HasChild(i))
                                continue;
                            ComputeChildCoord(i, out childOffset);
                            float ratio = data.GetData(i) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;
                            if (ratio == 0f)
                                continue;

                            ratio = (float)Math.Pow((double)ratio * MyVoxelConstants.VOXEL_VOLUME_IN_METERS, 0.3333);
                            var childPos = (cell.CoordInLod << (cell.Lod + 1)) + (childOffset << cell.Lod);
                            var lodSize = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << cell.Lod);
                            var center = worldPos + childPos * MyVoxelConstants.VOXEL_SIZE_IN_METRES + 0.5f * lodSize;
                            bb.Min = center - 0.5f * ratio * lodSize;
                            bb.Max = center + 0.5f * ratio * lodSize;
                            batch.Add(ref bb);
                        }
                    }
                    break;
            }
        }

        public int SerializedSize
        {
            get { return sizeof(int) + sizeof(TLeafData) + m_nodes.Count * (sizeof(TNodeKey) + MyOctreeNode.SERIALIZED_SIZE); }
        }

        internal void WriteTo(Stream stream)
        {
            stream.WriteNoAlloc(m_treeHeight);
            stream.WriteNoAlloc(m_defaultContent);
            foreach (var entry in m_nodes)
            {
                stream.WriteNoAlloc(entry.Key);
                var node = entry.Value;
                stream.WriteNoAlloc(node.ChildMask);
                unsafe { stream.WriteNoAlloc(node.Data, 0, MyOctreeNode.CHILD_COUNT); }
            }
        }

        internal void ReadFrom(MyOctreeStorage.ChunkHeader header, Stream stream)
        {
            m_treeHeight = stream.ReadInt32();
            m_treeWidth = 1 << m_treeHeight;
            m_defaultContent = stream.ReadByteNoAlloc();

            header.Size -= sizeof(int) + sizeof(TLeafData);
            int nodesCount = header.Size / (sizeof(TNodeKey) + MyOctreeNode.SERIALIZED_SIZE);

            m_nodes.Clear();
            TNodeKey key;
            MyOctreeNode node;
            for (int i = 0; i < nodesCount; ++i)
            {
                key = stream.ReadUInt32();
                node.ChildMask = stream.ReadByteNoAlloc();
                unsafe { stream.ReadNoAlloc(node.Data, 0, MyOctreeNode.CHILD_COUNT); }
                m_nodes.Add(key, node);
            }
        }

        internal static int EstimateStackSize(int treeHeight)
        {
            return (treeHeight - 1) * 7 + 8;
        }

        public void ReplaceValues(Dictionary<TLeafData, TLeafData> oldToNewValueMap)
        {
            ReplaceValues(m_nodes, oldToNewValueMap);
        }

        public static unsafe void ReplaceValues<TKey>(Dictionary<TKey, MyOctreeNode> nodeCollection, Dictionary<TLeafData, TLeafData> oldToNewValueMap)
        {
            foreach (var key in nodeCollection.Keys.ToArray())
            {
                var node = nodeCollection[key];
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    byte newValue;
                    if (oldToNewValueMap.TryGetValue(node.Data[i], out newValue))
                        node.Data[i] = newValue;
                }
                nodeCollection[key] = node;
            }
        }

        internal unsafe ContainmentType Intersect(ref BoundingBoxI box, bool lazy)
        {
            int stackIdx = 0;
            int stackSize = MySparseOctree.EstimateStackSize(m_treeHeight);
            MyCellCoord* stack = stackalloc MyCellCoord[stackSize];
            MyCellCoord data = new MyCellCoord(m_treeHeight - 1, ref Vector3I.Zero);
            stack[stackIdx++] = data;

            Vector3I minInLod = box.Min;
            Vector3I maxInLod = box.Max;

            MyOctreeNode node;
            Vector3I childPosRelative, min, max, nodePositionInChild;
            int lodDiff;

            // TODO(DI): Add support for checking for containment somehow, this needs neighbourhood information which kinda sucks.

            ContainmentType cont = ContainmentType.Disjoint;

            while (stackIdx > 0)
            {
                Debug.Assert(stackIdx <= stackSize);
                data = stack[--stackIdx];
                node = m_nodes[data.PackId32()];

                lodDiff = data.Lod;
                min = minInLod >> lodDiff;
                max = maxInLod >> lodDiff;
                nodePositionInChild = data.CoordInLod << 1;
                min -= nodePositionInChild;
                max -= nodePositionInChild;
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    ComputeChildCoord(i, out childPosRelative);
                    if (!childPosRelative.IsInsideInclusive(ref min, ref max))
                        continue;
                    if (data.Lod > 0 && node.HasChild(i))
                        {
                            Debug.Assert(stackIdx < stackSize);
                            stack[stackIdx++] = new MyCellCoord(data.Lod - 1, nodePositionInChild + childPosRelative);
                        }
                    else
                    {
                        var nodeData = node.Data[i];
                        if (lodDiff == 0)
                        {
                            if (nodeData != 0) return ContainmentType.Intersects;
                        }
                        else
                        {
                            BoundingBoxI nodeBox;
                            nodeBox.Min = nodePositionInChild + childPosRelative;
                            nodeBox.Min <<= lodDiff;
                            nodeBox.Max = nodeBox.Min + (1 << lodDiff) - 1;
                            Vector3I.Max(ref nodeBox.Min, ref minInLod, out nodeBox.Min);
                            Vector3I.Min(ref nodeBox.Max, ref maxInLod, out nodeBox.Max);

                            bool res;
                            nodeBox.Intersects(ref nodeBox, out res);

                            if (res) return ContainmentType.Intersects;
                        }
                    }
                }
            }

            return cont;
        }

        internal unsafe bool Intersect(ref LineD line, out double startOffset, out double endOffset)
        {
            startOffset = 0;
            endOffset = 1;
            return true;
            /*
            int stackIdx = 0;
            int stackSize = MySparseOctree.EstimateStackSize(m_treeHeight);
            MyCellCoord* stack = stackalloc MyCellCoord[stackSize];
            MyCellCoord data = new MyCellCoord(m_treeHeight - 1, ref Vector3I.Zero);
            stack[stackIdx++] = data;

            Vector3I minInLod = box.Min;
            Vector3I maxInLod = box.Max;

            MyOctreeNode node;
            Vector3I childPosRelative, min, max, nodePositionInChild;
            int lodDiff;

            // TODO(DI): Add support for checking for containment somehow, this needs neighbourhood information which kinda sucks.

            ContainmentType cont = ContainmentType.Disjoint;

            while (stackIdx > 0)
            {
                Debug.Assert(stackIdx <= stackSize);
                data = stack[--stackIdx];
                node = m_nodes[data.PackId32()];

                lodDiff = data.Lod;
                min = minInLod >> lodDiff;
                max = maxInLod >> lodDiff;
                nodePositionInChild = data.CoordInLod << 1;
                min -= nodePositionInChild;
                max -= nodePositionInChild;
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    ComputeChildCoord(i, out childPosRelative);
                    if (!childPosRelative.IsInsideInclusive(ref min, ref max))
                        continue;
                    if (data.Lod > 0 && node.HasChild(i))
                    {
                        Debug.Assert(stackIdx < stackSize);
                        stack[stackIdx++] = new MyCellCoord(data.Lod - 1, nodePositionInChild + childPosRelative);
                    }
                    else
                    {
                        var nodeData = node.Data[i];
                        if (lodDiff == 0)
                        {
                            if (nodeData != 0) return ContainmentType.Intersects;
                        }
                        else
                        {
                            BoundingBoxI nodeBox;
                            nodeBox.Min = nodePositionInChild + childPosRelative;
                            nodeBox.Min <<= lodDiff;
                            nodeBox.Max = nodeBox.Min + (1 << lodDiff) - 1;
                            Vector3I.Max(ref nodeBox.Min, ref minInLod, out nodeBox.Min);
                            Vector3I.Min(ref nodeBox.Max, ref maxInLod, out nodeBox.Max);

                            bool res;
                            nodeBox.Intersects(ref nodeBox, out res);

                            if (res) return ContainmentType.Intersects;
                        }
                    }
                }
            }

            return cont;*/
        }
    }
}