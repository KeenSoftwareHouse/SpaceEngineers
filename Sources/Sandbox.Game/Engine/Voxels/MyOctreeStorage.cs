using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRage;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using Sandbox.Graphics;
using Sandbox.Game.World;
using Sandbox.Game.Entities.Character;

namespace Sandbox.Engine.Voxels
{
    public partial class MyOctreeStorage : MyStorageBase
    {
        const int CURRENT_FILE_VERSION = 1;

        public enum ChunkTypeEnum : ushort
        { // Changing values will break backwards compatibility!
            StorageMetaData      = 1,
            MaterialIndexTable   = 2,
            MacroContentNodes    = 3,
            MacroMaterialNodes   = 4,
            ContentLeafProvider  = 5,
            ContentLeafOctree    = 6,
            MaterialLeafProvider = 7,
            MaterialLeafOctree   = 8,
            DataProvider         = 9,

            EndOfFile = ushort.MaxValue,
        }

        public struct ChunkHeader
        {
            public ChunkTypeEnum ChunkType;
            public int Version;
            public int Size;

            public void WriteTo(Stream stream)
            {
                stream.Write7BitEncodedInt((ushort)ChunkType);
                stream.Write7BitEncodedInt(Version);
                stream.Write7BitEncodedInt(Size);
            }

            public void ReadFrom(Stream stream)
            {
                ChunkType    = (ChunkTypeEnum)stream.Read7BitEncodedInt();
                Version = stream.Read7BitEncodedInt();
                Size    = stream.Read7BitEncodedInt();
            }
        }

        public const int LeafLodCount = 4;
        public const int LeafSizeInVoxels = 1 << LeafLodCount;

        private static readonly Dictionary<byte, byte> m_oldToNewIndexMap = new Dictionary<byte, byte>();
        private static readonly MyStorageDataCache m_temporaryCache = new MyStorageDataCache();
        private static readonly MyStorageDataCache m_temporaryCache2 = new MyStorageDataCache();

        private int m_treeHeight;

        private readonly Dictionary<UInt64, MyOctreeNode> m_contentNodes = new Dictionary<UInt64, MyOctreeNode>();
        private readonly Dictionary<UInt64, IMyOctreeLeafNode> m_contentLeaves = new Dictionary<UInt64, IMyOctreeLeafNode>();

        private readonly Dictionary<UInt64, MyOctreeNode> m_materialNodes = new Dictionary<UInt64, MyOctreeNode>();
        private readonly Dictionary<UInt64, IMyOctreeLeafNode> m_materialLeaves = new Dictionary<UInt64, IMyOctreeLeafNode>();

        private IMyStorageDataProvider m_dataProvider;

        private readonly Dictionary<UInt64, IMyOctreeLeafNode> m_tmpResetLeaves = new Dictionary<UInt64, IMyOctreeLeafNode>();
        private BoundingBoxD m_tmpResetLeavesBoundingBox; 


        public override IMyStorageDataProvider DataProvider
        {
            get { return m_dataProvider; }
            set
            {
                m_dataProvider = value;
                foreach (var leaf in m_contentLeaves.Values)
                    leaf.OnDataProviderChanged(value);
                foreach (var leaf in m_materialLeaves.Values)
                    leaf.OnDataProviderChanged(value);
                OnRangeChanged(Vector3I.Zero, Size - 1, MyStorageDataTypeFlags.All);
            }
        }

        public MyOctreeStorage() { }

        public MyOctreeStorage(IMyStorageDataProvider dataProvider, Vector3I size)
        {
            {
                int tmp = MathHelper.Max(size.X, size.Y, size.Z);
                tmp = MathHelper.GetNearestBiggerPowerOfTwo(tmp);
                if (tmp < LeafSizeInVoxels)
                    tmp = LeafSizeInVoxels;
                Size = new Vector3I(tmp);
            }
            m_dataProvider = dataProvider;

            InitTreeHeight();
            Debug.Assert(m_treeHeight < MyCellCoord.MAX_LOD_COUNT);
            ResetInternal(MyStorageDataTypeFlags.All);

            base.Geometry.Init(this);
        }

        private void InitTreeHeight()
        {
            var sizeLeaves = Size >> LeafLodCount;
            m_treeHeight = -1;
            var lodSize = sizeLeaves;
            while (lodSize != Vector3I.Zero)
            {
                lodSize >>= 1;
                ++m_treeHeight;
            }
        }

        protected override void ResetInternal(MyStorageDataTypeFlags dataToReset)
        {
            bool resetContent = (dataToReset & MyStorageDataTypeFlags.Content) != 0;
            bool resetMaterials = (dataToReset & MyStorageDataTypeFlags.Material) != 0;

            if (resetContent)
            {
                m_contentLeaves.Clear();
                m_contentNodes.Clear();
            }

            if (resetMaterials)
            {
                m_materialLeaves.Clear();
                m_materialNodes.Clear();
            }

            if (m_dataProvider != null)
            {
                var cellCoord = new MyCellCoord(m_treeHeight, ref Vector3I.Zero);
                var leafId = cellCoord.PackId64();
                cellCoord.Lod += LeafLodCount;
                var end = Size - 1;
                if (resetContent)
                {
                    m_contentLeaves.Add(leafId,
                        new MyProviderLeaf(m_dataProvider, MyStorageDataTypeEnum.Content, ref cellCoord));
                }
                if (resetMaterials)
                {
                    m_materialLeaves.Add(leafId,
                        new MyProviderLeaf(m_dataProvider, MyStorageDataTypeEnum.Material, ref cellCoord));
                }
            }
            else
            {
                var nodeId = new MyCellCoord(m_treeHeight - 1, ref Vector3I.Zero).PackId64();
                if (resetContent)
                {
                    m_contentNodes.Add(nodeId, new MyOctreeNode());
                }
                if (resetMaterials)
                {
                    m_materialNodes.Add(nodeId, new MyOctreeNode());
                }
            }
        }

        protected override void OverwriteAllMaterialsInternal(MyVoxelMaterialDefinition material)
        {
            Debug.Fail("Not implemented.");
        }

        protected override void LoadInternal(int fileVersion, Stream stream, ref bool isOldFormat)
        {
            Debug.Assert(fileVersion == CURRENT_FILE_VERSION);

            ChunkHeader header = new ChunkHeader();
            Dictionary<byte, MyVoxelMaterialDefinition> materialTable = null;
            HashSet<UInt64> materialLeaves = new HashSet<UInt64>();
            HashSet<UInt64> contentLeaves = new HashSet<UInt64>();
            while (header.ChunkType != ChunkTypeEnum.EndOfFile)
            {
                MyMicroOctreeLeaf contentLeaf;
                MyMicroOctreeLeaf materialLeaf;
                UInt64 key;

                header.ReadFrom(stream);
                Debug.Assert(Enum.IsDefined(typeof(ChunkTypeEnum), header.ChunkType));

                switch (header.ChunkType)
                {
                    case ChunkTypeEnum.StorageMetaData:
                        ReadStorageMetaData(stream, header, ref isOldFormat);
                        break;

                    case ChunkTypeEnum.MaterialIndexTable:
                        materialTable = ReadMaterialTable(stream, header, ref isOldFormat);
                        break;

                    case ChunkTypeEnum.MacroContentNodes:
                        ReadOctreeNodes(stream, header, ref isOldFormat, m_contentNodes);
                        break;

                    case ChunkTypeEnum.MacroMaterialNodes:
                        ReadOctreeNodes(stream, header, ref isOldFormat, m_materialNodes);
                        break;

                    case ChunkTypeEnum.ContentLeafProvider:
                        ReadProviderLeaf(stream, header, ref isOldFormat, contentLeaves);
                        break;

                    case ChunkTypeEnum.ContentLeafOctree:
                        ReadOctreeLeaf(stream, header, ref isOldFormat, MyStorageDataTypeEnum.Content, out key, out contentLeaf);
                        m_contentLeaves.Add(key, contentLeaf);
                        break;

                    case ChunkTypeEnum.MaterialLeafProvider:
                        ReadProviderLeaf(stream, header, ref isOldFormat, materialLeaves);
                        break;

                    case ChunkTypeEnum.MaterialLeafOctree:
                        ReadOctreeLeaf(stream, header, ref isOldFormat, MyStorageDataTypeEnum.Material, out key, out materialLeaf);
                        m_materialLeaves.Add(key, materialLeaf);
                        break;

                    case ChunkTypeEnum.DataProvider:
                        ReadDataProvider(stream, header, ref isOldFormat, out m_dataProvider);
                        break;

                    case ChunkTypeEnum.EndOfFile:
                        break;

                    default:
                        throw new InvalidBranchException();
                }
            }

            { // At this point data provider should be loaded too, so have him create leaves
                MyCellCoord cell = new MyCellCoord();
                foreach (var key in contentLeaves)
                {
                    cell.SetUnpack(key);
                    cell.Lod += LeafLodCount;
                    m_contentLeaves.Add(key, new MyProviderLeaf(m_dataProvider, MyStorageDataTypeEnum.Content, ref cell));
                }

                foreach (var key in materialLeaves)
                {
                    cell.SetUnpack(key);
                    cell.Lod += LeafLodCount;
                    m_materialLeaves.Add(key, new MyProviderLeaf(m_dataProvider, MyStorageDataTypeEnum.Material, ref cell));
                }
            }

            { // material reindexing when definitions change
                Debug.Assert(materialTable != null);
                bool needsReindexing = false;
                foreach (var entry in materialTable)
                {
                    if (entry.Key != entry.Value.Index)
                        needsReindexing = true;
                    m_oldToNewIndexMap.Add(entry.Key, entry.Value.Index);
                }

                if (needsReindexing)
                {
                    if (m_dataProvider != null)
                    {
                        m_dataProvider.ReindexMaterials(m_oldToNewIndexMap);
                    }

                    foreach (var entry in m_materialLeaves)
                    {
                        entry.Value.ReplaceValues(m_oldToNewIndexMap);
                    }

                    MySparseOctree.ReplaceValues(m_materialNodes, m_oldToNewIndexMap);
                }
                m_oldToNewIndexMap.Clear();
            }

        }

        protected override void SaveInternal(Stream stream)
        {
            WriteStorageMetaData(stream);
            WriteMaterialTable(stream);
            WriteDataProvider(stream, m_dataProvider);
            WriteOctreeNodes(stream, ChunkTypeEnum.MacroContentNodes, m_contentNodes);
            WriteOctreeNodes(stream, ChunkTypeEnum.MacroMaterialNodes, m_materialNodes);
            WriteOctreeLeaves(stream, m_contentLeaves);
            WriteOctreeLeaves(stream, m_materialLeaves);

            new ChunkHeader()
            {
                ChunkType = ChunkTypeEnum.EndOfFile,
            }.WriteTo(stream);
        }

        protected override void ReadRangeInternal(MyStorageDataCache target, ref Vector3I targetWriteOffset,
            MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelCoordStart, ref Vector3I lodVoxelCoordEnd)
        {
            bool hasLod = lodIndex <= (m_treeHeight + LeafLodCount);
            Debug.Assert(dataToRead != MyStorageDataTypeFlags.None);
            if ((dataToRead & MyStorageDataTypeFlags.Content) != 0)
            {
                if (hasLod)
                {
                    ReadRange(target, ref targetWriteOffset, MyStorageDataTypeEnum.Content, m_treeHeight, m_contentNodes, m_contentLeaves, lodIndex, ref lodVoxelCoordStart, ref lodVoxelCoordEnd);
                }
            }

            if ((dataToRead & MyStorageDataTypeFlags.Material) != 0)
            {
                if (hasLod)
                {
                    ReadRange(target, ref targetWriteOffset, MyStorageDataTypeEnum.Material, m_treeHeight, m_materialNodes, m_materialLeaves, lodIndex, ref lodVoxelCoordStart, ref lodVoxelCoordEnd);
                }
            }
        }

        protected override void WriteRangeInternal(MyStorageDataCache source, MyStorageDataTypeFlags dataToWrite, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax)
        {
            ProfilerShort.Begin("MyOctreeStorage.WriteRange");

            Debug.Assert(dataToWrite != MyStorageDataTypeFlags.None);
            if ((dataToWrite & MyStorageDataTypeFlags.Content) != 0)
            {
                var args = new WriteRangeArgs()
                {
                    DataFilter = MyOctreeNode.ContentFilter,
                    DataType = MyStorageDataTypeEnum.Content,
                    Leaves = m_contentLeaves,
                    Nodes = m_contentNodes,
                    Provider = m_dataProvider,
                    Source = source,
                };
                WriteRange(ref args, 0, m_treeHeight + LeafLodCount, Vector3I.Zero, ref voxelRangeMin, ref voxelRangeMax);
            }

            if ((dataToWrite & MyStorageDataTypeFlags.Material) != 0)
            {
                var args = new WriteRangeArgs()
                {
                    DataFilter = MyOctreeNode.MaterialFilter,
                    DataType = MyStorageDataTypeEnum.Material,
                    Leaves = m_materialLeaves,
                    Nodes = m_materialNodes,
                    Provider = m_dataProvider,
                    Source = source,
                };
                WriteRange(ref args, 0, m_treeHeight + LeafLodCount, Vector3I.Zero, ref voxelRangeMin, ref voxelRangeMax);
            }

            ProfilerShort.End();
        }

        public override void DebugDraw(MyVoxelBase referenceVoxelMap, MyVoxelDebugDrawMode mode)
        {
            Matrix worldMatrix = Matrix.CreateTranslation(referenceVoxelMap.PositionLeftBottomCorner);
            Color color;
            color = Color.CornflowerBlue;
            color.A = 25;
            switch (mode)
            {
                case MyVoxelDebugDrawMode.Content_MicroNodes:
                case MyVoxelDebugDrawMode.Content_MicroNodesScaled:
                    DrawSparseOctrees(ref worldMatrix, color, mode, m_contentLeaves);
                    break;

                case MyVoxelDebugDrawMode.Content_MacroNodes:
                    DrawNodes(ref worldMatrix, color, m_contentNodes);
                    break;

                case MyVoxelDebugDrawMode.Content_MacroLeaves:
                    DrawLeaves(ref worldMatrix, color, m_contentLeaves);
                    break;

                case MyVoxelDebugDrawMode.Content_MacroScaled:
                    DrawScaledNodes(ref worldMatrix, color, m_contentNodes);
                    break;

                case MyVoxelDebugDrawMode.Materials_MacroNodes:
                    DrawNodes(ref worldMatrix, color, m_materialNodes);
                    break;

                case MyVoxelDebugDrawMode.Materials_MacroLeaves:
                    DrawLeaves(ref worldMatrix, color, m_materialLeaves);
                    break;

                case MyVoxelDebugDrawMode.Content_DataProvider:
                    if (m_dataProvider != null)
                    {
                        var world = MatrixD.CreateTranslation(referenceVoxelMap.PositionLeftBottomCorner);
                        m_dataProvider.DebugDraw(ref world);
                    }
                    break;
            }

            if (m_tmpResetLeaves.Count > 0)
            {
                Color clr = Color.GreenYellow;
                MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref m_tmpResetLeavesBoundingBox, ref clr, MySimpleObjectRasterizer.Wireframe, 1, 0.04f);
                clr.A = 25;
                DrawLeaves(ref worldMatrix, clr, m_tmpResetLeaves);
            }

            if (MyFakes.ENABLE_DRAW_VOXEL_STORAGE_PLAYER_POSITION)
            {
                MyCharacter character = MySession.Static.CameraController as MyCharacter;
                if (character != null)
                {
                    Vector3D worldPos = character.WorldMatrix.Translation;
                    Vector3D localPos;
                    MyVoxelCoordSystems.WorldPositionToLocalPosition(referenceVoxelMap.PositionLeftBottomCorner, ref worldPos, out localPos);

                    localPos = Vector3I.Floor(localPos / 16) * 16;

                    Vector3D outWorldPosMin;
                    MyVoxelCoordSystems.LocalPositionToWorldPosition(referenceVoxelMap.PositionLeftBottomCorner, ref localPos, out outWorldPosMin);

                    BoundingBoxD box = new BoundingBoxD(outWorldPosMin, outWorldPosMin + new Vector3D(16, 16, 16));

                    Color clr = Color.Orange;
                    MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref box, ref clr, MySimpleObjectRasterizer.Wireframe, 1, 0.04f);
                }
            }
        }

        /// <summary>
        /// For debugging/testing only! This can be very slow for large storage.
        /// </summary>
        public void Voxelize(MyStorageDataTypeFlags data)
        {
            var cache = new MyStorageDataCache();

            cache.Resize(new Vector3I(LeafSizeInVoxels));
            var leafCount = (Size / LeafSizeInVoxels);
            Vector3I leaf = Vector3I.Zero;
            var end = leafCount - 1;
            for (var it = new Vector3I.RangeIterator(ref Vector3I.Zero, ref end);
                it.IsValid();
                it.GetNext(out leaf))
            {
                Debug.WriteLine("Processing {0} / {1}", leaf, end);
                var min = leaf * LeafSizeInVoxels;
                var max = min + (LeafSizeInVoxels - 1);
                ReadRangeInternal(cache, ref Vector3I.Zero, data, 0, ref min, ref max);
                WriteRangeInternal(cache, data, ref min, ref max);
            }

            OnRangeChanged(Vector3I.Zero, Size - 1, data);
        }

        /// <summary>
        /// Returns count of changed voxels amount according to the given base storage. This storage is simply modified data of the base storage
        /// (note that premodified base storage is not supported - base storage must be default unganged one).
        /// </summary>
        public ulong CountChangedVoxelsAmount(MyOctreeStorage baseStorage)
        {
            ulong count = CountChangedVoxelsAmount(baseStorage, m_treeHeight, m_contentNodes, m_contentLeaves, Vector3I.Zero);
            return count;
        }

        private static ulong CountChangedVoxelsAmount(
            MyOctreeStorage baseStorage,
            int lodIdx,
            Dictionary<UInt64, MyOctreeNode> nodes,
            Dictionary<UInt64, IMyOctreeLeafNode> leaves,
            Vector3I lodCoord)
        {
            var currentCell = new MyCellCoord(lodIdx, lodCoord);
            var leafKey = currentCell.PackId64();

            IMyOctreeLeafNode leaf;
            if (leaves.TryGetValue(leafKey, out leaf))
            {
                if (!leaf.ReadOnly && currentCell.Lod == 0)
                {
                    // Read data from leaf
                    var rangeEnd = new Vector3I(LeafSizeInVoxels - 1);
                    m_temporaryCache.Resize(Vector3I.Zero, rangeEnd);
                    leaf.ReadRange(m_temporaryCache, ref Vector3I.Zero, 0, ref Vector3I.Zero, ref rangeEnd);

                    // Read data from base storage
                    var minLeafVoxel = currentCell.CoordInLod * LeafSizeInVoxels;
                    var maxLeafVoxel = minLeafVoxel + (LeafSizeInVoxels - 1);
                    m_temporaryCache2.Resize(minLeafVoxel, maxLeafVoxel);
                    baseStorage.ReadRange(m_temporaryCache2, MyStorageDataTypeFlags.Content, currentCell.Lod, ref minLeafVoxel, ref maxLeafVoxel);

                    byte[] origData = m_temporaryCache2.Data;
                    byte[] currData = m_temporaryCache.Data;

                    Debug.Assert(currData.Length == origData.Length);

                    if (currData.Length != origData.Length)
                        return 0;

                    ulong countChangedVoxels = 0;
                    for (int i = (int)MyStorageDataTypeEnum.Content; i < m_temporaryCache.SizeLinear; i += m_temporaryCache.StepLinear)
                    {
                        countChangedVoxels += (ulong)Math.Abs(currData[i] - origData[i]);
                    }

                    return countChangedVoxels;
                }
            }
            else
            {
                currentCell.Lod -= 1;

                var nodeKey = currentCell.PackId64();
                var node = nodes[nodeKey];

                var childBase = lodCoord << 1;
                Vector3I childOffset;

                if (node.HasChildren)
                {
                    ulong count = 0;

                    for (int i = 0; i < MyOctreeNode.CHILD_COUNT; i++)
                    {
                        if (node.HasChild(i))
                        {
                            ComputeChildCoord(i, out childOffset);
                            currentCell.CoordInLod = childBase + childOffset;

                            count += CountChangedVoxelsAmount(baseStorage, currentCell.Lod, nodes, leaves, currentCell.CoordInLod);
                        }
                    }

                    return count;
                }
                else
                {
                    return (ulong)((MyOctreeNode.CHILD_COUNT << (currentCell.Lod * 3)) * LeafSizeInVoxels * LeafSizeInVoxels * LeafSizeInVoxels * MyVoxelConstants.VOXEL_CONTENT_FULL);
                }
            }

            return 0;
        }


        /// <summary>
        /// Resets aabbb outside area of the given voxel map to default. Area inside aabb (inclusive) stays the same.
        /// mk:TODO Remove MyVoxelBase and bounding box reference and just pass in integer range. Move computation of range to entities.
        /// </summary>
        public override void ResetOutsideBorders(MyVoxelBase voxelMap, BoundingBoxD worldAabb) 
        {
            m_tmpResetLeaves.Clear();
            m_tmpResetLeavesBoundingBox = worldAabb;

            Vector3I minVoxel, maxVoxel;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref worldAabb.Min, out minVoxel);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref worldAabb.Max, out maxVoxel);

            bool unused;
            bool changedContent = ResetOutsideBorders(m_dataProvider, MyStorageDataTypeEnum.Content, m_treeHeight, m_contentNodes, m_contentLeaves, Vector3I.Zero, minVoxel, maxVoxel, out unused,
                outResetLeaves: m_tmpResetLeaves);
            bool changedMaterial = ResetOutsideBorders(m_dataProvider, MyStorageDataTypeEnum.Material, m_treeHeight, m_materialNodes, m_materialLeaves, Vector3I.Zero, minVoxel, maxVoxel, out unused);

            if (changedContent || changedMaterial)
            {
                OnRangeChanged(Vector3I.Zero, Size - 1, MyStorageDataTypeFlags.ContentAndMaterial);
            }
        }

        private static bool ResetOutsideBorders(
            IMyStorageDataProvider provider,
            MyStorageDataTypeEnum dataType,
            int lodIdx,
            Dictionary<UInt64, MyOctreeNode> nodes,
            Dictionary<UInt64, IMyOctreeLeafNode> leaves,
            Vector3I lodCoord,
            Vector3I minVoxel,
            Vector3I maxVoxel,
            out bool canCollapse,
            Dictionary<UInt64, IMyOctreeLeafNode> outResetLeaves = null) 
        {
            canCollapse = false;

            bool changed = false;

            var currentCell = new MyCellCoord(lodIdx, lodCoord);
            var key = currentCell.PackId64();
            var leafCell = currentCell;
            var leafKey = leafCell.PackId64();

            IMyOctreeLeafNode leaf;
            if (leaves.TryGetValue(leafKey, out leaf))
            {
                canCollapse = leaf.ReadOnly;

                if (leafCell.Lod != 0)
                {
                    Debug.Assert(leaf.ReadOnly);
                    return false;
                }
                else if (!leaf.ReadOnly)
                {
                    var minCell = minVoxel >> (LeafLodCount + leafCell.Lod);
                    var maxCell = maxVoxel >> (LeafLodCount + leafCell.Lod);

                    if (!leafCell.CoordInLod.IsInsideInclusive(ref minCell, ref maxCell))
                    {
                        canCollapse = true;

                        leaves.Remove(leafKey);
                        var leafCellCopy = leafCell;
                        leafCellCopy.Lod += LeafLodCount;
                        var leafNew = new MyProviderLeaf(provider, dataType, ref leafCellCopy);
                        leaves.Add(leafKey, leafNew);

                        changed = true;

                        if (outResetLeaves != null)
                            outResetLeaves.Add(leafKey, leafNew);
                    }
                }
            }
            else
            {
                currentCell.Lod -= 1;
                key = currentCell.PackId64();
                var nodeCell = currentCell;

                var nodeKey = currentCell.PackId64();
                var node = nodes[nodeKey];

                var childBase = lodCoord << 1;
                Vector3I childOffset;
                var minInChild = (minVoxel >> (LeafLodCount + currentCell.Lod)) - childBase;
                var maxInChild = (maxVoxel >> (LeafLodCount + currentCell.Lod)) - childBase;
                var leafSize = LeafSizeInVoxels << currentCell.Lod;

                unsafe
                {
                    canCollapse = true;

                    for (int i = 0; i < MyOctreeNode.CHILD_COUNT; i++)
                    {
                        ComputeChildCoord(i, out childOffset);
                        if (childOffset.IsInsideExclusive(ref minInChild, ref maxInChild))
                        {
                            canCollapse = false;
                            continue;
                        }

                        currentCell.CoordInLod = childBase + childOffset;

                        if (node.HasChild(i))
                        {
                            bool localCanCollapse;
                            bool resetChanged = ResetOutsideBorders(provider, dataType, currentCell.Lod, nodes, leaves, currentCell.CoordInLod, minVoxel, maxVoxel, out localCanCollapse, outResetLeaves: outResetLeaves);
                            changed = changed || resetChanged;

                            canCollapse = localCanCollapse && canCollapse;
                        }
                        else
                        {
                            var currentCellCopy = currentCell;
                            currentCellCopy.Lod += LeafLodCount;
                            IMyOctreeLeafNode octreeLeaf = new MyProviderLeaf(provider, dataType, ref currentCellCopy);
                            leaves.Add(currentCell.PackId64(), octreeLeaf);
                            node.SetChild(i, true);
                            node.SetData(i, octreeLeaf.GetFilteredValue());

                            changed = true;
                        }
                    }
                    nodes[nodeKey] = node;

                    if (canCollapse)
                        {
                        // Remove leaves
                        for (int i = 0; i < MyOctreeNode.CHILD_COUNT; i++)
                        {
                            if (node.HasChild(i))
                            {
                                ComputeChildCoord(i, out childOffset);
                                currentCell.CoordInLod = childBase + childOffset;

                                var childKey = currentCell.PackId64();
                                leaves.Remove(childKey);
                                node.SetChild(i, false);
                        }
                    }

                        // Remove node
                        nodes.Remove(nodeKey);

                        // Add leaf
                        var leafCellCopy = leafCell;
                        leafCellCopy.Lod += LeafLodCount;
                        var leafNew = new MyProviderLeaf(provider, dataType, ref leafCellCopy);
                        leaves.Add(leafKey, leafNew);
                }
            }
            }

            return changed;
        }

        private static unsafe void ReadRange(
            MyStorageDataCache target,
            ref Vector3I targetWriteOffset,
            MyStorageDataTypeEnum type,
            int treeHeight,
            Dictionary<UInt64, MyOctreeNode> nodes,
            Dictionary<UInt64, IMyOctreeLeafNode> leaves,
            int lodIndex,
            ref Vector3I minInLod,
            ref Vector3I maxInLod)
        {
            int stackIdx = 0;
            int stackSize = MySparseOctree.EstimateStackSize(treeHeight);
            MyCellCoord* stack = stackalloc MyCellCoord[stackSize];
            MyCellCoord data = new MyCellCoord(treeHeight + LeafLodCount, ref Vector3I.Zero);
            stack[stackIdx++] = data;
            MyCellCoord cell = new MyCellCoord();

            while (stackIdx > 0)
            {
                Debug.Assert(stackIdx <= stackSize);
                data = stack[--stackIdx];

                cell.Lod = data.Lod - LeafLodCount;
                cell.CoordInLod = data.CoordInLod;

                int lodDiff;
                IMyOctreeLeafNode leaf;
                if (leaves.TryGetValue(cell.PackId64(), out leaf))
                {
                    lodDiff = data.Lod - lodIndex;
                    var rangeMinInDataLod = minInLod >> lodDiff;
                    var rangeMaxInDataLod = maxInLod >> lodDiff;
                    if (data.CoordInLod.IsInsideInclusive(ref rangeMinInDataLod, ref rangeMaxInDataLod))
                    {
                        var nodePosInLod = data.CoordInLod << lodDiff;
                        var writeOffset = nodePosInLod - minInLod;
                        Vector3I.Max(ref writeOffset, ref Vector3I.Zero, out writeOffset);
                        writeOffset += targetWriteOffset;
                        var lodSizeMinusOne = new Vector3I((1 << lodDiff) - 1);
                        var minInLeaf = Vector3I.Clamp(minInLod - nodePosInLod, Vector3I.Zero, lodSizeMinusOne);
                        var maxInLeaf = Vector3I.Clamp(maxInLod - nodePosInLod, Vector3I.Zero, lodSizeMinusOne);
                        leaf.ReadRange(target, ref writeOffset, lodIndex, ref minInLeaf, ref maxInLeaf);
                    }
                    continue;
                }

                cell.Lod -= 1;
                lodDiff = data.Lod - 1 - lodIndex;
                var node = nodes[cell.PackId64()];

                var min = minInLod >> lodDiff;
                var max = maxInLod >> lodDiff;
                var nodePositionInChild = data.CoordInLod << 1;
                min -= nodePositionInChild;
                max -= nodePositionInChild;
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    Vector3I childPosRelative;
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
                        var childMin = nodePositionInChild + childPosRelative;
                        childMin <<= lodDiff;
                        var writeOffset = childMin - minInLod;
                        Vector3I.Max(ref writeOffset, ref Vector3I.Zero, out writeOffset);
                        writeOffset += targetWriteOffset;
                        var nodeData = node.GetData(i);
                        if (lodDiff == 0)
                        {
                            target.Set(type, ref writeOffset, nodeData);
                        }
                        else
                        {
                            var childMax = childMin + ((1 << lodDiff) - 1);
                            Vector3I.Max(ref childMin, ref minInLod, out childMin);
                            Vector3I.Min(ref childMax, ref maxInLod, out childMax);
                            for (int z = childMin.Z; z <= childMax.Z; ++z)
                            for (int y = childMin.Y; y <= childMax.Y; ++y)
                            for (int x = childMin.X; x <= childMax.X; ++x)
                            {
                                Vector3I write = writeOffset;
                                write.X += x - childMin.X;
                                write.Y += y - childMin.Y;
                                write.Z += z - childMin.Z;
                                target.Set(type, ref write, nodeData);
                            }
                        }
                    }
                }
            }
        }

        private static void WriteRange(
            ref WriteRangeArgs args,
            byte defaultData,
            int lodIdx,
            Vector3I lodCoord,
            ref Vector3I min,
            ref Vector3I max)
        {
            MyOctreeNode node = new MyOctreeNode();
            {
                MyCellCoord leaf = new MyCellCoord(lodIdx - LeafLodCount, ref lodCoord);
                var leafKey = leaf.PackId64();
                if (args.Leaves.ContainsKey(leafKey))
                {
                    args.Leaves.Remove(leafKey);
                    var childBase = lodCoord << 1;
                    Vector3I childOffset;
                    MyCellCoord child = new MyCellCoord();
                    child.Lod = leaf.Lod - 1;
                    var leafSize = LeafSizeInVoxels << child.Lod;
                    for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                    {
                        ComputeChildCoord(i, out childOffset);
                        child.CoordInLod = childBase + childOffset;
                        var childCopy = child;
                        childCopy.Lod += LeafLodCount;
                        IMyOctreeLeafNode octreeLeaf = new MyProviderLeaf(args.Provider, args.DataType, ref childCopy);
                        args.Leaves.Add(child.PackId64(), octreeLeaf);
                        node.SetChild(i, true);
                        node.SetData(i, octreeLeaf.GetFilteredValue());
                    }
                }
                else
                {
                    leaf.Lod -= 1; // changes to node coord instead of leaf coord
                    var nodeKey = leaf.PackId64();

                    if (!args.Nodes.TryGetValue(nodeKey, out node))
                    {
                        for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                            node.SetData(i, defaultData);
                    }
                }
            }

            if (lodIdx == (LeafLodCount + 1))
            {
                MyCellCoord child = new MyCellCoord();
                Vector3I childBase = lodCoord << 1;
                Vector3I minInLod = min >> LeafLodCount;
                Vector3I maxInLod = max >> LeafLodCount;
                Vector3I leafSizeMinusOne = new Vector3I(LeafSizeInVoxels - 1);
                Vector3I childOffset;
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    ComputeChildCoord(i, out childOffset);
                    child.CoordInLod = childBase + childOffset;
                    if (!child.CoordInLod.IsInsideInclusive(ref minInLod, ref maxInLod))
                        continue;
                    var childMin = child.CoordInLod << LeafLodCount;
                    var childMax = childMin + LeafSizeInVoxels - 1;
                    Vector3I.Max(ref childMin, ref min, out childMin);
                    Vector3I.Min(ref childMax, ref max, out childMax);
                    var readOffset = childMin - min;
                    IMyOctreeLeafNode leaf;
                    var leafKey = child.PackId64();
                    var startInChild = childMin - (child.CoordInLod << LeafLodCount);
                    var endInChild = childMax - (child.CoordInLod << LeafLodCount);

                    args.Leaves.TryGetValue(leafKey, out leaf);

                    byte uniformValue;
                    bool uniformLeaf;
                    {
                        // ensure leaf exists and is writable
                        // the only writable leaf type is MicroOctree at this point

                        byte childDefaultData = node.GetData(i);

                        if (leaf == null)
                        {
                            var octree = new MyMicroOctreeLeaf(args.DataType, LeafLodCount, child.CoordInLod << (child.Lod + LeafLodCount));
                            octree.BuildFrom(childDefaultData);
                            leaf = octree;
                        }

                        if (leaf.ReadOnly)
                        {
                            var rangeEnd = new Vector3I(LeafSizeInVoxels - 1);
                            m_temporaryCache.Resize(Vector3I.Zero, rangeEnd);
                            leaf.ReadRange(m_temporaryCache, ref Vector3I.Zero, 0, ref Vector3I.Zero, ref rangeEnd);
                            var inCell = startInChild;
                            for (var it2 = new Vector3I.RangeIterator(ref startInChild, ref endInChild);
                                it2.IsValid(); it2.GetNext(out inCell))
                            {
                                var read = readOffset + (inCell - startInChild);
                                m_temporaryCache.Set(args.DataType, ref inCell, args.Source.Get(args.DataType, ref read));
                            }

                            var octree = new MyMicroOctreeLeaf(args.DataType, LeafLodCount, child.CoordInLod << (child.Lod + LeafLodCount));
                            octree.BuildFrom(m_temporaryCache);
                            leaf = octree;
                        }
                        else
                        {
                            leaf.WriteRange(args.Source, ref readOffset, ref startInChild, ref endInChild);
                        }

                        uniformLeaf = ((MyMicroOctreeLeaf)leaf).TryGetUniformValue(out uniformValue);
                    }

                    if (!uniformLeaf)
                    {
                        args.Leaves[leafKey] = leaf;
                        node.SetChild(i, true);
                    }
                    else
                    {
                        args.Leaves.Remove(leafKey);
                        node.SetChild(i, false);
                    }

                    node.SetData(i, leaf.GetFilteredValue());
                }
                args.Nodes[new MyCellCoord(lodIdx - 1 - LeafLodCount, ref lodCoord).PackId64()] = node;
            }
            else
            {
                MyCellCoord child = new MyCellCoord();
                child.Lod = lodIdx - 2 - LeafLodCount;
                var childBase = lodCoord << 1;
                Vector3I childOffset;
                var minInChild = (min >> (lodIdx-1)) - childBase;
                var maxInChild = (max >> (lodIdx-1)) - childBase;
                for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                {
                    ComputeChildCoord(i, out childOffset);
                    if (!childOffset.IsInsideInclusive(ref minInChild, ref maxInChild))
                        continue;

                    child.CoordInLod = childBase + childOffset;
                    WriteRange(ref args, node.GetData(i), lodIdx - 1, child.CoordInLod, ref min, ref max);
                    var childKey = child.PackId64();
                    var childNode = args.Nodes[childKey];
                    if (!childNode.HasChildren && childNode.AllDataSame())
                    {
                        node.SetChild(i, false);
                        node.SetData(i, childNode.GetData(0));
                        args.Nodes.Remove(childKey);
                    }
                    else
                    {
                        node.SetChild(i, true);
                        node.SetData(i, childNode.ComputeFilteredValue(args.DataFilter));
                    }
                }

                args.Nodes[new MyCellCoord(lodIdx - 1 - LeafLodCount, ref lodCoord).PackId64()] = node;
            }
        }

        private static void DrawNodes(ref Matrix worldMatrix, Color color, Dictionary<UInt64, MyOctreeNode> octree)
        {
            using (var batch = MyRenderProxy.DebugDrawBatchAABB(worldMatrix, color, true, true))
            {
                MyCellCoord cell = new MyCellCoord();
                foreach (var entry in octree)
                {
                    cell.SetUnpack(entry.Key);
                    cell.Lod += LeafLodCount;
                    var data = entry.Value;
                    for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                    {
                        if (data.HasChild(i))
                            continue;
                        Vector3I childOffset;
                        ComputeChildCoord(i, out childOffset);
                        var voxelPos = (cell.CoordInLod << (cell.Lod + 1)) + (childOffset << cell.Lod);
                        var lodSize = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << cell.Lod);
                        var center = voxelPos * MyVoxelConstants.VOXEL_SIZE_IN_METRES + 0.5f * lodSize;
                        BoundingBoxD bb = new BoundingBoxD(
                            center - 0.5f * lodSize,
                            center + 0.5f * lodSize);
                        batch.Add(ref bb);
                    }
                }
            }
        }

        private static void DrawLeaves(ref Matrix worldMatrix, Color color, Dictionary<UInt64, IMyOctreeLeafNode> octree)
        {
            using (var batch = MyRenderProxy.DebugDrawBatchAABB(worldMatrix, color, true, true))
            {
                MyCellCoord cell = new MyCellCoord();
                foreach (var entry in octree)
                {
                    cell.SetUnpack(entry.Key);
                    cell.Lod += LeafLodCount;
                    var data = entry.Value;
                    var voxelPos = cell.CoordInLod << cell.Lod;
                    var bb = new BoundingBoxD(
                        voxelPos * MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                        (voxelPos + (1 << cell.Lod)) * MyVoxelConstants.VOXEL_SIZE_IN_METRES);
                    batch.Add(ref bb);
                }
            }
        }

        private static void DrawScaledNodes(ref Matrix worldMatrix, Color color, Dictionary<UInt64, MyOctreeNode> octree)
        {
            using (var batch = MyRenderProxy.DebugDrawBatchAABB(worldMatrix, color, true, true))
            {
                MyCellCoord cell = new MyCellCoord();
                foreach (var entry in octree)
                {
                    cell.SetUnpack(entry.Key);
                    cell.Lod += LeafLodCount;
                    var data = entry.Value;
                    for (int i = 0; i < MyOctreeNode.CHILD_COUNT; ++i)
                    {
                        if (data.HasChild(i) && cell.Lod != LeafLodCount)
                            continue;
                        Vector3I childOffset;
                        ComputeChildCoord(i, out childOffset);
                        float ratio = data.GetData(i) / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;
                        if (ratio == 0f)
                            continue;

                        var voxelPos = (cell.CoordInLod << (cell.Lod + 1)) + (childOffset << cell.Lod);
                        var lodSize = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << cell.Lod);
                        var center = voxelPos * MyVoxelConstants.VOXEL_SIZE_IN_METRES + 0.5f * lodSize;
                        ratio = (float)Math.Pow((double)ratio * MyVoxelConstants.VOXEL_VOLUME_IN_METERS, 0.3333);
                        lodSize *= ratio;
                        BoundingBoxD bb = new BoundingBoxD(
                            center - 0.5f * lodSize,
                            center + 0.5f * lodSize);
                        batch.Add(ref bb);
                    }
                }
            }
        }

        private static void DrawSparseOctrees(ref Matrix worldMatrix, Color color, MyVoxelDebugDrawMode mode, Dictionary<UInt64, IMyOctreeLeafNode> octree)
        {
            var camera = Sandbox.Game.World.MySector.MainCamera;
            if (camera == null)
                return;
            var targetPoint = camera.Position + camera.ForwardVector * 10;
            targetPoint = (Vector3)Vector3D.Transform(targetPoint, MatrixD.Invert(worldMatrix));

            using (var batch = VRageRender.MyRenderProxy.DebugDrawBatchAABB(worldMatrix, color, true, true))
            {
                MyCellCoord cell = new MyCellCoord();
                foreach (var entry in octree)
                {
                    var leaf = entry.Value as MyMicroOctreeLeaf;
                    if (leaf != null)
                    {
                        cell.SetUnpack(entry.Key);
                        Vector3D min = (cell.CoordInLod << LeafLodCount) * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                        Vector3D max = min + LeafSizeInVoxels * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                        if (targetPoint.IsInsideInclusive(ref min, ref max))
                            leaf.DebugDraw(batch, min, mode);
                    }
                }
            }
        }

        private static void ComputeChildCoord(int childIdx, out Vector3I relativeCoord)
        {
            Debug.Assert(childIdx < 8);
            relativeCoord.X = (childIdx >> 0) & 1;
            relativeCoord.Y = (childIdx >> 1) & 1;
            relativeCoord.Z = (childIdx >> 2) & 1;
        }

        struct WriteRangeArgs
        {
            public IMyStorageDataProvider Provider;
            public MyStorageDataCache Source;
            public Dictionary<UInt64, MyOctreeNode> Nodes;
            public Dictionary<UInt64, IMyOctreeLeafNode> Leaves;
            public MyOctreeNode.FilterFunction DataFilter;
            public MyStorageDataTypeEnum DataType;
        }

    }
}