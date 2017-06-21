using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;

using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender;
using VRage;
using Sandbox.Graphics.GUI;
using System.Text;

using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Plugins;
using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.EntityComponents;
using Sandbox.Engine.Multiplayer;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Models;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    /// Class which handles small block to large one connection. Such connection creates block and grid groups so connected grids can be copied together.
    /// It is done on server and client, but client uses it only for groups (copying of all grids together), dynamic grid testing is processed on server only.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyCubeGridSmallToLargeConnection : MySessionComponentBase
    {
        private static readonly HashSet<MyCubeBlock> m_tmpBlocks = new HashSet<MyCubeBlock>();
        private static readonly HashSet<MySlimBlock> m_tmpSlimBlocks = new HashSet<MySlimBlock>();
        private static readonly HashSet<MySlimBlock> m_tmpSlimBlocks2 = new HashSet<MySlimBlock>();
        private static readonly List<MySlimBlock> m_tmpSlimBlocksList = new List<MySlimBlock>();
        private static readonly HashSet<MyCubeGrid> m_tmpGrids = new HashSet<MyCubeGrid>();
        private static readonly List<MyCubeGrid> m_tmpGridList = new List<MyCubeGrid>();

        private static bool m_smallToLargeCheckEnabled = true;

        private struct MySlimBlockPair : IEquatable<MySlimBlockPair>
        {
            public MySlimBlock Parent;
            public MySlimBlock Child;

            public override int GetHashCode()
            {
                return Parent.GetHashCode() ^ Child.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MySlimBlockPair))
                    return false;
                MySlimBlockPair other = (MySlimBlockPair)obj;
                return Parent == other.Parent && Child == other.Child;
            }

            public bool Equals(MySlimBlockPair other) 
            {
                return Parent == other.Parent && Child == other.Child;
            }
        }
        private static readonly List<MySlimBlockPair> m_tmpBlockConnections = new List<MySlimBlockPair>();


        public static MyCubeGridSmallToLargeConnection Static;

        private readonly Dictionary<MyCubeGrid, HashSet<MySlimBlockPair>> m_mapLargeGridToConnectedBlocks = new Dictionary<MyCubeGrid, HashSet<MySlimBlockPair>>();
        private readonly Dictionary<MyCubeGrid, HashSet<MySlimBlockPair>> m_mapSmallGridToConnectedBlocks = new Dictionary<MyCubeGrid, HashSet<MySlimBlockPair>>();

        public MyCubeGridSmallToLargeConnection()
        {
        }

        public override bool IsRequiredByGame
        {
            get { return base.IsRequiredByGame && MyFakes.ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS; }
        }

        public override void LoadData()
        {
            base.LoadData();
            Static = this;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Static = null;
        }

        /// <summary>
        /// Writes all surrounding blocks around the given one with the given size.
        /// </summary>
        private void GetSurroundingBlocksFromStaticGrids(MySlimBlock block, MyCubeSize cubeSizeEnum, HashSet<MyCubeBlock> outBlocks)
        {
            outBlocks.Clear();

            BoundingBoxD aabb = new BoundingBoxD(block.Min * block.CubeGrid.GridSize - block.CubeGrid.GridSize / 2, block.Max * block.CubeGrid.GridSize + block.CubeGrid.GridSize / 2);
            if (block.FatBlock != null)
            {
                var aabbCenter = aabb.Center;
                aabb = (BoundingBoxD)block.FatBlock.Model.BoundingBox;
                Matrix m;
                block.FatBlock.Orientation.GetMatrix(out m);
                aabb = aabb.TransformFast(m);
                aabb.Translate(aabbCenter);
            }

            aabb = aabb.TransformFast(block.CubeGrid.WorldMatrix);
            aabb.Inflate(0.125);

            List<MyEntity> boxOverlapList = new List<MyEntity>();
            MyEntities.GetElementsInBox(ref aabb, boxOverlapList);

            for (int i = 0; i < boxOverlapList.Count; i++)
            {
                var otherBlock = boxOverlapList[i] as MyCubeBlock;
                if (otherBlock != null && otherBlock.SlimBlock != block 
                    && otherBlock.CubeGrid.IsStatic && otherBlock.CubeGrid.EnableSmallToLargeConnections && otherBlock.CubeGrid.SmallToLargeConnectionsInitialized 
                    && otherBlock.CubeGrid != block.CubeGrid && otherBlock.CubeGrid.GridSizeEnum == cubeSizeEnum && !(otherBlock is MyFracturedBlock)
                    && !(otherBlock.Components.Has<MyFractureComponentBase>()))
                {
                    var compound = otherBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        foreach (var blockInCompound in compound.GetBlocks())
                        {
                            if (blockInCompound != block && !(blockInCompound.FatBlock.Components.Has<MyFractureComponentBase>()))
                                outBlocks.Add(blockInCompound.FatBlock);
                        }
                    }
                    else
                    {
                        outBlocks.Add(otherBlock);
                    }
                }
            }

            boxOverlapList.Clear();
        }

        /// <summary>
        /// Writes all surrounding blocks around the given one with the given size.
        /// </summary>
        private void GetSurroundingBlocksFromStaticGrids(MySlimBlock block, MyCubeSize cubeSizeEnum, HashSet<MySlimBlock> outBlocks)
        {
            outBlocks.Clear();

            BoundingBoxD aabbForNeighbors = new BoundingBoxD(block.Min * block.CubeGrid.GridSize, block.Max * block.CubeGrid.GridSize);
            BoundingBoxD aabb = new BoundingBoxD(block.Min * block.CubeGrid.GridSize - block.CubeGrid.GridSize / 2, block.Max * block.CubeGrid.GridSize + block.CubeGrid.GridSize / 2);
            if (block.FatBlock != null)
            {
                var aabbCenter = aabb.Center;
                aabb = (BoundingBoxD)block.FatBlock.Model.BoundingBox;
                Matrix m;
                block.FatBlock.Orientation.GetMatrix(out m);
                aabb = aabb.TransformFast(m);
                aabb.Translate(aabbCenter);
            }

            aabb.Inflate(0.125);
            var aabbWorld = aabb.TransformFast(block.CubeGrid.WorldMatrix);

            List<MyEntity> boxOverlapList = new List<MyEntity>();
            MyEntities.GetElementsInBox(ref aabbWorld, boxOverlapList);

            for (int i = 0; i < boxOverlapList.Count; i++)
            {
                var cubeGrid = boxOverlapList[i] as MyCubeGrid;
                if (cubeGrid != null)
                {
                    if (cubeGrid.IsStatic && cubeGrid != block.CubeGrid && cubeGrid.EnableSmallToLargeConnections && cubeGrid.SmallToLargeConnectionsInitialized
                        && cubeGrid.GridSizeEnum == cubeSizeEnum)
                    {
                        Debug.Assert(m_tmpSlimBlocksList.Count == 0);
                        m_tmpSlimBlocksList.Clear();
                        cubeGrid.GetBlocksIntersectingOBB(aabb, block.CubeGrid.WorldMatrix, m_tmpSlimBlocksList);

                        CheckNeighborBlocks(block, aabbForNeighbors, cubeGrid, m_tmpSlimBlocksList);

                        foreach (var slimBlock in m_tmpSlimBlocksList)
                        {
                            if (slimBlock.FatBlock != null)
                            {
                                if (slimBlock.FatBlock is MyFracturedBlock)
                                    continue;

                                if (slimBlock.FatBlock.Components.Has<MyFractureComponentBase>())
                                    continue;

                                if (slimBlock.FatBlock is MyCompoundCubeBlock)
                                {
                                    foreach (var blockInCompound in (slimBlock.FatBlock as MyCompoundCubeBlock).GetBlocks())
                                    {
                                        if (!(blockInCompound.FatBlock.Components.Has<MyFractureComponentBase>()))
                                            outBlocks.Add(blockInCompound);
                                    }
                                }
                                else
                                    outBlocks.Add(slimBlock);
                            }
                            else
                                outBlocks.Add(slimBlock);
                        }

                        m_tmpSlimBlocksList.Clear();
                    }
                }
            }

            boxOverlapList.Clear();
        }

        /// <summary>
        /// Checks if blocks are neigbors to block(s) in aabbForNeighbors.
        /// </summary>
        private static void CheckNeighborBlocks(MySlimBlock block, BoundingBoxD aabbForNeighbors, MyCubeGrid cubeGrid, List<MySlimBlock> blocks)
        {
            var compositeTransformToGrid = block.CubeGrid.WorldMatrix * cubeGrid.PositionComp.WorldMatrixNormalizedInv;
            var aabbForNeighborsInGrid = aabbForNeighbors.TransformFast(ref compositeTransformToGrid);
            Vector3I start = Vector3I.Round(cubeGrid.GridSizeR * aabbForNeighborsInGrid.Min);
            Vector3I end = Vector3I.Round(cubeGrid.GridSizeR * aabbForNeighborsInGrid.Max);
            Vector3I startIt = Vector3I.Min(start, end);
            Vector3I endIt = Vector3I.Max(start, end);

            for (int slimBlockIndex = blocks.Count - 1; slimBlockIndex >= 0; --slimBlockIndex)
            {
                var slimBlock = blocks[slimBlockIndex];
                bool found = false;

                Vector3I_RangeIterator itBlockInGridPos = new Vector3I_RangeIterator(ref slimBlock.Min, ref slimBlock.Max);
                var posInGrid = itBlockInGridPos.Current;
                for (; itBlockInGridPos.IsValid(); itBlockInGridPos.GetNext(out posInGrid))
                {
                    Vector3I_RangeIterator itBlockPos = new Vector3I_RangeIterator(ref startIt, ref endIt);
                    var pos = itBlockPos.Current;
                    for (; itBlockPos.IsValid(); itBlockPos.GetNext(out pos))
                    {
                        Vector3I diff = Vector3I.Abs(posInGrid - pos);
                        if (pos == posInGrid || diff.X + diff.Y + diff.Z == 1)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        break;
                }

                if (!found)
                    blocks.RemoveAt(slimBlockIndex);
            }
        }

        /// <summary>
        /// Adds small/large block connection.
        /// </summary>
        private void ConnectSmallToLargeBlock(MySlimBlock smallBlock, MySlimBlock largeBlock)
        {
            Debug.Assert(GetCubeSize(smallBlock) == MyCubeSize.Small);
            Debug.Assert(GetCubeSize(largeBlock) == MyCubeSize.Large);
            Debug.Assert(!(smallBlock.FatBlock is MyCompoundCubeBlock));
            Debug.Assert(!(largeBlock.FatBlock is MyCompoundCubeBlock));

            if (GetCubeSize(smallBlock) != MyCubeSize.Small || GetCubeSize(largeBlock) != MyCubeSize.Large
                || (smallBlock.FatBlock is MyCompoundCubeBlock) || (largeBlock.FatBlock is MyCompoundCubeBlock))
                return;

            // Block link
            long linkId = ((long)largeBlock.UniqueId << 32) + smallBlock.UniqueId;
            if (MyCubeGridGroups.Static.SmallToLargeBlockConnections.LinkExists(linkId, largeBlock, null))
            {
                //Debug.WriteLine("Link exist id: "  + linkId + " " + largeBlock + " " + smallBlock);
                return;
            }
            MyCubeGridGroups.Static.SmallToLargeBlockConnections.CreateLink(linkId, largeBlock, smallBlock);

            // Grid link
            MyCubeGridGroups.Static.Physical.CreateLink(linkId, largeBlock.CubeGrid, smallBlock.CubeGrid);
            MyCubeGridGroups.Static.Logical.CreateLink(linkId, largeBlock.CubeGrid, smallBlock.CubeGrid);

            // Remember link
            MySlimBlockPair pair = new MySlimBlockPair();
            pair.Parent = largeBlock;
            pair.Child = smallBlock;

            {
                HashSet<MySlimBlockPair> slimBlockPairs;
                if (!m_mapLargeGridToConnectedBlocks.TryGetValue(largeBlock.CubeGrid, out slimBlockPairs))
                {
                    slimBlockPairs = new HashSet<MySlimBlockPair>();
                    m_mapLargeGridToConnectedBlocks.Add(largeBlock.CubeGrid, slimBlockPairs);
                    largeBlock.CubeGrid.OnClosing += CubeGrid_OnClosing;
                }
                slimBlockPairs.Add(pair);
            }

            {
                HashSet<MySlimBlockPair> slimBlockPairs;
                if (!m_mapSmallGridToConnectedBlocks.TryGetValue(smallBlock.CubeGrid, out slimBlockPairs))
                {
                    slimBlockPairs = new HashSet<MySlimBlockPair>();
                    m_mapSmallGridToConnectedBlocks.Add(smallBlock.CubeGrid, slimBlockPairs);
                    smallBlock.CubeGrid.OnClosing += CubeGrid_OnClosing;
                }
                slimBlockPairs.Add(pair);
            }

            //Debug.WriteLine("Link created id: " + linkId + " " + largeBlock + " " + smallBlock);
        }

        /// <summary>
        /// Removes small/large block connection. Note that grids in parameters can be different from the ones in slimblock! Used for grid splits, etc.
        /// </summary>
        private void DisconnectSmallToLargeBlock(MySlimBlock smallBlock, MyCubeGrid smallGrid, MySlimBlock largeBlock, MyCubeGrid largeGrid)
        {
            Debug.Assert(GetCubeSize(smallBlock) == MyCubeSize.Small);
            Debug.Assert(GetCubeSize(largeBlock) == MyCubeSize.Large);
            Debug.Assert(smallGrid.GridSizeEnum == MyCubeSize.Small);
            Debug.Assert(largeGrid.GridSizeEnum == MyCubeSize.Large);
            Debug.Assert(!(smallBlock.FatBlock is MyCompoundCubeBlock));
            Debug.Assert(!(largeBlock.FatBlock is MyCompoundCubeBlock));

            if (GetCubeSize(smallBlock) != MyCubeSize.Small || GetCubeSize(largeBlock) != MyCubeSize.Large
                || (smallBlock.FatBlock is MyCompoundCubeBlock) || (largeBlock.FatBlock is MyCompoundCubeBlock))
                return;

            // Block link
            long linkId = ((long)largeBlock.UniqueId << 32) + smallBlock.UniqueId;
            MyCubeGridGroups.Static.SmallToLargeBlockConnections.BreakLink(linkId, largeBlock, null);

            // Grid link
            bool removedPhysical = MyCubeGridGroups.Static.Physical.BreakLink(linkId, largeGrid, null);
            bool removedLogical = MyCubeGridGroups.Static.Logical.BreakLink(linkId, largeGrid, null);

            // Remove link from maps
            MySlimBlockPair pair = new MySlimBlockPair();
            pair.Parent = largeBlock;
            pair.Child = smallBlock;

            {
                HashSet<MySlimBlockPair> slimBlockPairs;
                if (m_mapLargeGridToConnectedBlocks.TryGetValue(largeGrid, out slimBlockPairs))
                {
                    bool removed = slimBlockPairs.Remove(pair);
                    Debug.Assert(removed);
                    if (slimBlockPairs.Count == 0)
                    {
                        m_mapLargeGridToConnectedBlocks.Remove(largeGrid);
                        largeGrid.OnClosing -= CubeGrid_OnClosing;
                    }
                }
                else
                {
                    Debug.Fail("Small to large block connection not found in map");
                }
            }

            {
                HashSet<MySlimBlockPair> slimBlockPairs;
                if (m_mapSmallGridToConnectedBlocks.TryGetValue(smallGrid, out slimBlockPairs))
                {
                    bool removed = slimBlockPairs.Remove(pair);
                    Debug.Assert(removed);
                    if (slimBlockPairs.Count == 0)
                    {
                        m_mapSmallGridToConnectedBlocks.Remove(smallGrid);
                        smallGrid.OnClosing -= CubeGrid_OnClosing;
                    }
                }
                else
                {
                    Debug.Fail("Small to large block connection not found in map");
                }
            }

            //Debug.WriteLine("Link removed id: " + linkId + " " + largeBlock + " " + smallBlock);
        }

        /// <summary>
        /// Removes small/large block connection.
        /// </summary>
        private void DisconnectSmallToLargeBlock(MySlimBlock smallBlock, MySlimBlock largeBlock)
        {
            DisconnectSmallToLargeBlock(smallBlock, smallBlock.CubeGrid, largeBlock, largeBlock.CubeGrid);
        }

        /// <summary>
        /// Adds possible connections of grid blocks.  
        /// </summary>
        /// <returns>Returns true when small/large block connection has been added otherwise false.</returns>
        internal bool AddGridSmallToLargeConnection(MyCubeGrid grid)
        {
            if (!grid.IsStatic)
                return false;

            if (!grid.EnableSmallToLargeConnections || !grid.SmallToLargeConnectionsInitialized)
                return false;

            bool retval = false;
            foreach (var block in grid.GetBlocks())
            {
                if (!(block.FatBlock is MyFracturedBlock))
                {
                    bool localRetVal = AddBlockSmallToLargeConnection(block);
                    retval = retval || localRetVal;
                }
            }

            return retval;
        }

        /// <summary>
        /// Adds small/large block static connections and creates links. Returns true if the block connects to any other block.
        /// </summary>
        public bool AddBlockSmallToLargeConnection(MySlimBlock block)
        {
            if (!m_smallToLargeCheckEnabled)
                return true;

            if (!block.CubeGrid.IsStatic || !block.CubeGrid.EnableSmallToLargeConnections || !block.CubeGrid.SmallToLargeConnectionsInitialized 
                || (block.FatBlock != null && block.FatBlock.Components.Has<MyFractureComponentBase>()))
                return false;

            bool retval = false;

            if (block.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                foreach (var blockInCompound in compoundBlock.GetBlocks())
                {
                    bool localRetVal = AddBlockSmallToLargeConnection(blockInCompound);
                    retval = retval || localRetVal;
                }
                return retval;
            }

            MyCubeSize searchCubeSize = GetCubeSize(block) == MyCubeSize.Large ? MyCubeSize.Small : MyCubeSize.Large;
            GetSurroundingBlocksFromStaticGrids(block, searchCubeSize, m_tmpSlimBlocks2);

            if (m_tmpSlimBlocks2.Count == 0)
                return false;

            float smallGridSize = MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Small);

            BoundingBoxD blockAabb;
            block.GetWorldBoundingBox(out blockAabb);
            blockAabb.Inflate(0.05);

            if (GetCubeSize(block) == MyCubeSize.Large)
            {
                foreach (var smallBlock in m_tmpSlimBlocks2)
                {
                    Debug.Assert(GetCubeSize(smallBlock) == MyCubeSize.Small);

                    BoundingBoxD smallAabb;
                    smallBlock.GetWorldBoundingBox(out smallAabb);

                    if (!smallAabb.Intersects(blockAabb))
                        continue;

                    if (SmallBlockConnectsToLarge(smallBlock, ref smallAabb, block, ref blockAabb))
                    {
                        ConnectSmallToLargeBlock(smallBlock, block);
                        retval = true;
                    }
                }
            }
            else
            {
                Debug.Assert(GetCubeSize(block) == MyCubeSize.Small);

                foreach (var largeBlock in m_tmpSlimBlocks2)
                {
                    Debug.Assert(GetCubeSize(largeBlock) == MyCubeSize.Large);

                    BoundingBoxD largeAabb;
                    largeBlock.GetWorldBoundingBox(out largeAabb);

                    if (!largeAabb.Intersects(blockAabb))
                        continue;

                    if (SmallBlockConnectsToLarge(block, ref blockAabb, largeBlock, ref largeAabb))
                    {
                        ConnectSmallToLargeBlock(block, largeBlock);
                        retval = true;
                    }
                }
            }

            return retval;
        }

        /// <summary>
        /// Block has been removed and all small/large static connections must be removed.
        /// </summary>
        internal void RemoveBlockSmallToLargeConnection(MySlimBlock block)
        {
            if (!m_smallToLargeCheckEnabled)
                return;

            if (!block.CubeGrid.IsStatic)
                return;

            MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
            if (compoundBlock != null)
            {
                foreach (var blockInCompound in compoundBlock.GetBlocks())
                    RemoveBlockSmallToLargeConnection(blockInCompound);
                return;
            }

            Debug.Assert(m_tmpGrids.Count == 0);
            m_tmpGrids.Clear();

            if (GetCubeSize(block) == MyCubeSize.Large)
            {
                RemoveChangedLargeBlockConnectionToSmallBlocks(block, m_tmpGrids);

                if (Sync.IsServer)
                {
                    // Convert free small grids to dynamic
                    foreach (var smallGrid in m_tmpGrids)
                    {
                        if (smallGrid.TestDynamic == MyCubeGrid.MyTestDynamicReason.NoReason && !SmallGridIsStatic(smallGrid))
                            smallGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                    }
                }

                m_tmpGrids.Clear();
            }
            else
            {
                Debug.Assert(GetCubeSize(block) == MyCubeSize.Small);

                var group = MyCubeGridGroups.Static.SmallToLargeBlockConnections.GetGroup(block);
                if (group == null)
                {
                    if (Sync.IsServer && block.CubeGrid.GetBlocks().Count > 0)
                    {
                        // Convert free small grid to dynamic
                        if (block.CubeGrid.TestDynamic == MyCubeGrid.MyTestDynamicReason.NoReason && !SmallGridIsStatic(block.CubeGrid))
                            block.CubeGrid.TestDynamic =  MyCubeGrid.MyTestDynamicReason.GridSplit;
                    }
                    return;
                }

                Debug.Assert(m_tmpSlimBlocks.Count == 0);
                m_tmpSlimBlocks.Clear();

                // Get connections
                foreach (var node in group.Nodes)
                {
                    foreach (var child in node.Children)
                    {
                        if (child.NodeData == block)
                        {
                            m_tmpSlimBlocks.Add(node.NodeData);
                            break;
                        }
                    }
                }

                // Remove connections
                foreach (var largeBlock in m_tmpSlimBlocks)
                {
                    DisconnectSmallToLargeBlock(block, largeBlock);
                }

                m_tmpSlimBlocks.Clear();

                HashSet<MySlimBlockPair> connections;
                if (Sync.IsServer && !m_mapSmallGridToConnectedBlocks.TryGetValue(block.CubeGrid, out connections) && block.CubeGrid.GetBlocks().Count > 0)
                {
                    // Convert free small grid to dynamic
                    if (block.CubeGrid.TestDynamic == MyCubeGrid.MyTestDynamicReason.NoReason && !SmallGridIsStatic(block.CubeGrid))
                        block.CubeGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                }
            }

        }

        /// <summary>
        /// Grid has been converted to dynamic, all small to large connections must be removed.
        /// </summary>
        internal void GridConvertedToDynamic(MyCubeGrid grid)
        {
            if (grid.GridSizeEnum == MyCubeSize.Small) 
            {
                RemoveSmallGridConnections(grid);
                if (Sync.IsServer) 
                    MyMultiplayer.RaiseEvent(grid, x => x.RequestConvertToDynamic);
            }
            else
            {
                RemoveLargeGridConnections(grid);
            }
        }

        private void RemoveLargeGridConnections(MyCubeGrid grid)
        {
            Debug.Assert(grid.GridSizeEnum == MyCubeSize.Large);
            Debug.Assert(m_tmpGrids.Count == 0);
            m_tmpGrids.Clear();

            HashSet<MySlimBlockPair> connections;
            if (!m_mapLargeGridToConnectedBlocks.TryGetValue(grid, out connections))
                return;

            m_tmpBlockConnections.Clear();
            m_tmpBlockConnections.AddRange(connections);

            foreach (var connection in m_tmpBlockConnections)
            {
                DisconnectSmallToLargeBlock(connection.Child, connection.Parent);
                m_tmpGrids.Add(connection.Child.CubeGrid);
            }

            m_tmpBlockConnections.Clear();

            if (Sync.IsServer)
            {
                Debug.Assert(m_tmpGridList.Count == 0);
                m_tmpGridList.Clear();

                // Remove small grids with some connections
                foreach (var smallGrid in m_tmpGrids)
                {
                    Debug.Assert(smallGrid.GridSizeEnum == MyCubeSize.Small);
                    if (m_mapSmallGridToConnectedBlocks.ContainsKey(smallGrid))
                        m_tmpGridList.Add(smallGrid);
                }

                foreach (var smallgGrid in m_tmpGridList)
                    m_tmpGrids.Remove(smallgGrid);

                m_tmpGridList.Clear();

                // Convert free small grids to dynamic
                foreach (var smallGrid in m_tmpGrids)
                {
                    if (smallGrid.IsStatic && smallGrid.TestDynamic == MyCubeGrid.MyTestDynamicReason.NoReason && !SmallGridIsStatic(smallGrid))
                        smallGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                }
            }

            m_tmpGrids.Clear();
        }

        private void RemoveSmallGridConnections(MyCubeGrid grid)
        {
            Debug.Assert(grid.GridSizeEnum == MyCubeSize.Small);

            HashSet<MySlimBlockPair> connections;
            if (!m_mapSmallGridToConnectedBlocks.TryGetValue(grid, out connections))
                return;

            m_tmpBlockConnections.Clear();
            m_tmpBlockConnections.AddRange(connections);

            foreach (var connection in m_tmpBlockConnections)
            {
                DisconnectSmallToLargeBlock(connection.Child, connection.Parent);
            }

            m_tmpBlockConnections.Clear();
        }


        /// <summary>
        /// Tests whether the given small grid connects to any large static block.
        /// </summary>
        /// <returns>true if small grid connects to a latge grid otherwise false</returns>
        public bool TestGridSmallToLargeConnection(MyCubeGrid smallGrid)
        {
            Debug.Assert(smallGrid.GridSizeEnum == MyCubeSize.Small);

            if (!smallGrid.IsStatic)
                return false;

            // On clients all static grids are connected. Only server calculates connections.
            if (!Sync.IsServer)
                return true;

            // Any connection in grid
            HashSet<MySlimBlockPair> connections;
            if (m_mapSmallGridToConnectedBlocks.TryGetValue(smallGrid, out connections) && connections.Count > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Returns small block add direction (returns large block add normal). 
        /// Assumes that smallBlockWorldAabb intersects largeBlockWorldAabb and smallBlockWorldAabbReduced does not intersects largeBlockWorldAabb.
        /// </summary>
        private Vector3I GetSmallBlockAddDirection(ref BoundingBoxD smallBlockWorldAabb, ref BoundingBoxD smallBlockWorldAabbReduced, ref BoundingBoxD largeBlockWorldAabb)
        {
            Debug.Assert(largeBlockWorldAabb.Intersects(smallBlockWorldAabb));
            Debug.Assert(!largeBlockWorldAabb.Intersects(smallBlockWorldAabbReduced));

            if (smallBlockWorldAabbReduced.Min.X > largeBlockWorldAabb.Max.X && smallBlockWorldAabb.Min.X <= largeBlockWorldAabb.Max.X)
                return Vector3I.UnitX;
            else if (smallBlockWorldAabbReduced.Max.X < largeBlockWorldAabb.Min.X && smallBlockWorldAabb.Max.X >= largeBlockWorldAabb.Min.X)
                return -Vector3I.UnitX;

            if (smallBlockWorldAabbReduced.Min.Y > largeBlockWorldAabb.Max.Y && smallBlockWorldAabb.Min.Y <= largeBlockWorldAabb.Max.Y)
                return Vector3I.UnitY;
            else if (smallBlockWorldAabbReduced.Max.Y < largeBlockWorldAabb.Min.Y && smallBlockWorldAabb.Max.Y >= largeBlockWorldAabb.Min.Y)
                return -Vector3I.UnitY;

            if (smallBlockWorldAabbReduced.Min.Z > largeBlockWorldAabb.Max.Z && smallBlockWorldAabb.Min.Z <= largeBlockWorldAabb.Max.Z)
                return Vector3I.UnitZ;
            else
            {
                Debug.Assert(smallBlockWorldAabbReduced.Max.Z < largeBlockWorldAabb.Min.Z && smallBlockWorldAabb.Max.Z >= largeBlockWorldAabb.Min.Z);
                return -Vector3I.UnitZ;
            }
        }

        /// <summary>
        /// Returns true if the given small block connects to large one. One of the given AABB's is inflated with 0.05 to reduce inaccuracies.
        /// </summary>
        /// <param name="smallBlock">small block</param>
        /// <param name="smallBlockWorldAabb">small block world AABB</param>
        /// <param name="largeBlock">large block</param>
        /// <param name="largeBlockWorldAabb">large block wotld AABB</param>
        /// <returns>true when connected</returns>
        private bool SmallBlockConnectsToLarge(MySlimBlock smallBlock, ref BoundingBoxD smallBlockWorldAabb, MySlimBlock largeBlock, ref BoundingBoxD largeBlockWorldAabb)
        {
            Debug.Assert(GetCubeSize(smallBlock) == MyCubeSize.Small);
            Debug.Assert(GetCubeSize(largeBlock) == MyCubeSize.Large);
            Debug.Assert(!(smallBlock.FatBlock is MyCompoundCubeBlock));
            Debug.Assert(!(largeBlock.FatBlock is MyCompoundCubeBlock));

            BoundingBoxD smallBlockWorldAabbReduced = smallBlockWorldAabb;
            smallBlockWorldAabbReduced.Inflate(-smallBlock.CubeGrid.GridSize / 4);

            // Small block aabb penetrates large block aabb (large timbers).
            bool penetratesAabbs = largeBlockWorldAabb.Intersects(smallBlockWorldAabbReduced);
            if (!penetratesAabbs)
            {
                Vector3I addDir = GetSmallBlockAddDirection(ref smallBlockWorldAabb, ref smallBlockWorldAabbReduced, ref largeBlockWorldAabb);
                // Check small grid mount points
                Quaternion smallBlockRotation;
                smallBlock.Orientation.GetQuaternion(out smallBlockRotation);
                smallBlockRotation = Quaternion.CreateFromRotationMatrix(smallBlock.CubeGrid.WorldMatrix) * smallBlockRotation;
                if (!MyCubeGrid.CheckConnectivitySmallBlockToLargeGrid(largeBlock.CubeGrid, smallBlock.BlockDefinition, ref smallBlockRotation, ref addDir))
                    return false;
            }

            BoundingBoxD smallBlockWorldAabbInflated = smallBlockWorldAabb;
            smallBlockWorldAabbInflated.Inflate(2 * smallBlock.CubeGrid.GridSize / 3);

            // Trim small block aabb with large block aabb.
            BoundingBoxD intersectedBox = smallBlockWorldAabbInflated.Intersect(largeBlockWorldAabb);
            Vector3D intersectedBoxCenter = intersectedBox.Center;
            HkShape shape = new HkBoxShape((Vector3)intersectedBox.HalfExtents);

            Quaternion largeRotation;
            largeBlock.Orientation.GetQuaternion(out largeRotation);
            largeRotation = Quaternion.CreateFromRotationMatrix(largeBlock.CubeGrid.WorldMatrix) * largeRotation;
            Vector3D largeTranslation;
            largeBlock.ComputeWorldCenter(out largeTranslation);

            bool result = false;

            try
            {
                if (largeBlock.FatBlock != null)
                {
                    MyModel model = largeBlock.FatBlock.Model;
                    if (model != null && model.HavokCollisionShapes != null)
                    {
                        HkShape[] shapes = model.HavokCollisionShapes;
                        for (int i = 0; i < shapes.Length; ++i)
                        {
                            result = MyPhysics.IsPenetratingShapeShape(shape, ref intersectedBoxCenter, ref Quaternion.Identity, shapes[i], ref largeTranslation, ref largeRotation);
                            if (result)
                                break;
                        }
                    }
                    else
                    {
                        HkShape shapeLarge = new HkBoxShape(largeBlock.BlockDefinition.Size * largeBlock.CubeGrid.GridSize / 2);

                        result = MyPhysics.IsPenetratingShapeShape(shape, ref intersectedBoxCenter, ref Quaternion.Identity, shapeLarge, ref largeTranslation, ref largeRotation);

                        shapeLarge.RemoveReference();
                    }
                }
                else
                {
                    HkShape shapeLarge = new HkBoxShape(largeBlock.BlockDefinition.Size * largeBlock.CubeGrid.GridSize / 2);

                    result = MyPhysics.IsPenetratingShapeShape(shape, ref intersectedBoxCenter, ref Quaternion.Identity, shapeLarge, ref largeTranslation, ref largeRotation);

                    shapeLarge.RemoveReference();
                }
            }
            finally
            {
                shape.RemoveReference();
            }

            return result;
        }

        /// <summary>
        /// Remove all large block connections to small blocks (large block has been removed or its grid has been changed to dynamic).
        /// </summary>
        private void RemoveChangedLargeBlockConnectionToSmallBlocks(MySlimBlock block, HashSet<MyCubeGrid> outSmallGrids)
        {
            Debug.Assert(GetCubeSize(block) == MyCubeSize.Large);

            var group = MyCubeGridGroups.Static.SmallToLargeBlockConnections.GetGroup(block);
            if (group == null)
                return;

            Debug.Assert(m_tmpSlimBlocks.Count == 0);
            m_tmpSlimBlocks.Clear();

            // Get connections
            foreach (var node in group.Nodes)
            {
                if (node.NodeData == block)
                {
                    foreach (var child in node.Children)
                        m_tmpSlimBlocks.Add(child.NodeData);

                    break;
                }
            }

            // Remove connections
            foreach (var smallBlock in m_tmpSlimBlocks)
            {
                DisconnectSmallToLargeBlock(smallBlock, block);
                outSmallGrids.Add(smallBlock.CubeGrid);
            }

            m_tmpSlimBlocks.Clear();

            Debug.Assert(m_tmpGridList.Count == 0);
            m_tmpGridList.Clear();

            // Remove small grids with some connections
            HashSet<MySlimBlockPair> connections;
            foreach (var smallGrid in outSmallGrids)
            {
                Debug.Assert(smallGrid.GridSizeEnum == MyCubeSize.Small);
                if (m_mapSmallGridToConnectedBlocks.TryGetValue(smallGrid, out connections))
                    m_tmpGridList.Add(smallGrid);
            }

            foreach (var smallgGrid in m_tmpGridList)
                outSmallGrids.Remove(smallgGrid);

            m_tmpGridList.Clear();

        }

        /// <summary>
        /// Returns true if the given small grid connects to a large static grid, otherwise false.
        /// </summary>
        private bool SmallGridIsStatic(MyCubeGrid smallGrid)
        {
            if (TestGridSmallToLargeConnection(smallGrid))
                return true;

            return false;
        }

        /// <summary>
        /// Grid will be split. Called before split. All connections with the grid will be removed.
        /// </summary>
        internal void BeforeGridSplit_SmallToLargeGridConnectivity(MyCubeGrid originalGrid)
        {
            m_smallToLargeCheckEnabled = false;
        }

        /// <summary>
        /// Grid has been split. All connections will be recreated for original grid and also for all splits.
        /// </summary>
        internal void AfterGridSplit_SmallToLargeGridConnectivity(MyCubeGrid originalGrid, List<MyCubeGrid> gridSplits)
        {
            m_smallToLargeCheckEnabled = true;

            if (originalGrid.GridSizeEnum == MyCubeSize.Small)
                AfterGridSplit_Small(originalGrid, gridSplits);
            else
                AfterGridSplit_Large(originalGrid, gridSplits);
        }

        private void AfterGridSplit_Small(MyCubeGrid originalGrid, List<MyCubeGrid> gridSplits)
        {
            Debug.Assert(originalGrid.GridSizeEnum == MyCubeSize.Small);

            if (!originalGrid.IsStatic)
                return;

            // Process all old connections from original and set new ones.
            HashSet<MySlimBlockPair> connections;
            if (m_mapSmallGridToConnectedBlocks.TryGetValue(originalGrid, out connections))
            {
                m_tmpBlockConnections.Clear();

                // Mark connections for reconnect
                foreach (var connection in connections)
                {
                    if (connection.Child.CubeGrid != originalGrid)
                        m_tmpBlockConnections.Add(connection);
                }

                foreach (var connection in m_tmpBlockConnections)
                {
                    // Disconnect using original grid
                    DisconnectSmallToLargeBlock(connection.Child, originalGrid, connection.Parent, connection.Parent.CubeGrid);
                    // Reconnect splits
                    ConnectSmallToLargeBlock(connection.Child, connection.Parent);
                }

                m_tmpBlockConnections.Clear();

            }

            if (Sync.IsServer)
            {
                // Test dynamic
                if (!m_mapSmallGridToConnectedBlocks.TryGetValue(originalGrid, out connections) || connections.Count == 0)
                    originalGrid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;

                foreach (var split in gridSplits)
                {
                    if (!m_mapSmallGridToConnectedBlocks.TryGetValue(split, out connections) || connections.Count == 0)
                        split.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                }
            }
        }

        private void AfterGridSplit_Large(MyCubeGrid originalGrid, List<MyCubeGrid> gridSplits)
        {
            Debug.Assert(originalGrid.GridSizeEnum == MyCubeSize.Large);

            if (!originalGrid.IsStatic)
                return;

            // Process all old connections from original and set new ones.
            HashSet<MySlimBlockPair> connections;
            if (!m_mapLargeGridToConnectedBlocks.TryGetValue(originalGrid, out connections))
                return;

            m_tmpBlockConnections.Clear();

            // Mark connections for reconnect
            foreach (var connection in connections)
            {
                if (connection.Parent.CubeGrid != originalGrid)
                    m_tmpBlockConnections.Add(connection);
            }

            //Debug.WriteLine("AfterGridSplit:Restored connections: " + m_tmpBlockConnections.Count);

            foreach (var connection in m_tmpBlockConnections)
            {
                // Disconnect using original grid
                DisconnectSmallToLargeBlock(connection.Child, connection.Child.CubeGrid, connection.Parent, originalGrid);
                // Reconnect splits
                ConnectSmallToLargeBlock(connection.Child, connection.Parent);
            }

            m_tmpBlockConnections.Clear();

        }

        /// <summary>
        /// Grids will be merged. Called before merge. All grids connections will be removed.
        /// </summary>
        internal void BeforeGridMerge_SmallToLargeGridConnectivity(MyCubeGrid originalGrid, MyCubeGrid mergedGrid)
        {
            Debug.Assert(m_tmpGrids.Count == 0);
            m_tmpGrids.Clear();

            if (originalGrid.IsStatic && mergedGrid.IsStatic)
            {
                m_tmpGrids.Add(mergedGrid);
            }

            m_smallToLargeCheckEnabled = false;
        }

        /// <summary>
        /// Grid has been merged. All connections will be recreated.
        /// </summary>
        internal void AfterGridMerge_SmallToLargeGridConnectivity(MyCubeGrid originalGrid)
        {
            m_smallToLargeCheckEnabled = true;

            if (m_tmpGrids.Count == 0)
                return;

            if (!originalGrid.IsStatic)
                return;

            // Process all old connections from merged grid.
            if (originalGrid.GridSizeEnum == MyCubeSize.Large) 
            {
                foreach (var mergedGrid in m_tmpGrids)
                {
                    HashSet<MySlimBlockPair> connections;
                    if (!m_mapLargeGridToConnectedBlocks.TryGetValue(mergedGrid, out connections))
                        continue;

                    m_tmpBlockConnections.Clear();
                    m_tmpBlockConnections.AddRange(connections);

                    //Debug.WriteLine("AfterGridMerge:Restored connections: " + m_tmpBlockConnections.Count);

                    foreach (var connection in m_tmpBlockConnections)
                    {
                        // Disconnect using merged grid
                        DisconnectSmallToLargeBlock(connection.Child, connection.Child.CubeGrid, connection.Parent, mergedGrid);
                        // Reconnect in original
                        Debug.Assert(connection.Parent.CubeGrid == originalGrid);
                        ConnectSmallToLargeBlock(connection.Child, connection.Parent);
                    }

                    Debug.Assert(connections.Count == 0);
                }
            }
            else
            {
                foreach (var mergedGrid in m_tmpGrids)
                {
                    HashSet<MySlimBlockPair> connections;
                    if (!m_mapSmallGridToConnectedBlocks.TryGetValue(mergedGrid, out connections))
                        continue;

                    m_tmpBlockConnections.Clear();
                    m_tmpBlockConnections.AddRange(connections);

                    foreach (var connection in m_tmpBlockConnections)
                    {
                        // Disconnect using merged grid
                        DisconnectSmallToLargeBlock(connection.Child, mergedGrid, connection.Parent, connection.Parent.CubeGrid);
                        // Reconnect in original
                        Debug.Assert(connection.Child.CubeGrid == originalGrid);
                        ConnectSmallToLargeBlock(connection.Child, connection.Parent);
                    }

                    Debug.Assert(connections.Count == 0);
                }
            }

            m_tmpGrids.Clear();
            m_tmpBlockConnections.Clear();
        }

        private void CubeGrid_OnClosing(MyEntity entity)
        {
            MyCubeGrid grid = (MyCubeGrid)entity;
            if (grid.GridSizeEnum == MyCubeSize.Small)
                RemoveSmallGridConnections(grid);
            else
                RemoveLargeGridConnections(grid);
        }

        private static MyCubeSize GetCubeSize(MySlimBlock block)
        {
            if (block.CubeGrid != null)
                return block.CubeGrid.GridSizeEnum;

            // Fractured small blocks are in large fracture block!
            MyFracturedBlock fractureBlock = block.FatBlock as MyFracturedBlock;
            if (fractureBlock != null && fractureBlock.OriginalBlocks.Count > 0)
            {
                MyCubeBlockDefinition def;
                if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(fractureBlock.OriginalBlocks[0], out def))
                    return def.CubeSize;
            }

            return block.BlockDefinition.CubeSize;
        }

    }
}
