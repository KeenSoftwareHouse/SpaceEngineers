
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRageMath;
using System;
using System.IO;
using System.Linq;
using VRage.Utils;
using Sandbox.Game.Entities;
using Sandbox.Engine.Models;
using VRageRender;
using System.Collections.Generic;
using Havok;
using Sandbox.Engine.Physics;
using System.Diagnostics;
using Sandbox.Definitions;
using VRage.Import;
using Sandbox.Common;
using Sandbox.Game.World;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Entities.Cube
{
    using ModelId = System.Int32;
    using VRage.Utils;
    using VRage.Library.Utils;
    using VRage;

    /// <summary>
    /// Base class for additional model geometry with common implementation.
    /// </summary>
    public abstract class MyAdditionalModelGeneratorBase : IMyBlockAdditionalModelGenerator
    {
        protected static Vector3I[] Forwards = new Vector3I[] 
        {
            Vector3I.Forward,
            Vector3I.Right,
            Vector3I.Backward,
            Vector3I.Left
        };

        protected class MyGridInfo
        {
            public MyCubeGrid Grid;
            // Transform from m_grid to Grid.
            public MatrixI Transform;
        }

        protected struct MyGeneratedBlockLocation
        {
            public MySlimBlock RefBlock; 
            public MyCubeBlockDefinition BlockDefinition;
            public Vector3I Position;
            public MyBlockOrientation Orientation;
            public ushort? BlockIdInCompound;
            public MyGridInfo GridInfo;
            public MyStringId GeneratedBlockType;


            public MyGeneratedBlockLocation(MySlimBlock refBlock, MyCubeBlockDefinition blockDefinition, Vector3I position, MyBlockOrientation orientation, ushort? blockIdInCompound = null, MyGridInfo gridInfo = null)
            {
                RefBlock = refBlock;
                BlockDefinition = blockDefinition;
                Position = position;
                Orientation = orientation;
                BlockIdInCompound = blockIdInCompound;
                GridInfo = gridInfo;
                GeneratedBlockType = MyStringId.NullOrEmpty;
            }

            public MyGeneratedBlockLocation(MySlimBlock refBlock, MyCubeBlockDefinition blockDefinition, Vector3I position, Vector3I forward, Vector3I up, ushort? blockIdInCompound = null, MyGridInfo gridInfo = null)
            {
                RefBlock = refBlock;
                BlockDefinition = blockDefinition;
                Position = position;
                Orientation = new MyBlockOrientation(Base6Directions.GetDirection(ref forward), Base6Directions.GetDirection(ref up));
                BlockIdInCompound = blockIdInCompound;
                GridInfo = gridInfo;
                GeneratedBlockType = MyStringId.NullOrEmpty;
            }

            public static bool IsSameGeneratedBlockLocation(MyGeneratedBlockLocation blockLocAdded, MyGeneratedBlockLocation blockLocRemoved)
            {
                bool result = blockLocAdded.BlockDefinition == blockLocRemoved.BlockDefinition;
                result = result && blockLocAdded.Position == blockLocRemoved.Position;
                result = result && blockLocAdded.Orientation == blockLocRemoved.Orientation;
                return result;
            }

            public static bool IsSameGeneratedBlockLocation(MyGeneratedBlockLocation blockLocAdded, MyGeneratedBlockLocation blockLocRemoved, MyStringId generatedBlockType)
            {
                bool result = blockLocAdded.BlockDefinition.GeneratedBlockType == generatedBlockType;
                result = result && blockLocAdded.Position == blockLocRemoved.Position;
                result = result && blockLocAdded.Orientation == blockLocRemoved.Orientation;
                return result;
            }

        }

        protected static readonly string BUILD_TYPE_WALL = "wall";

        protected MyCubeGrid m_grid;
        private bool m_enabled;

        private readonly HashSet<MyGeneratedBlockLocation> m_addLocations = new HashSet<MyGeneratedBlockLocation>();
        // List of locations in grids to remove.
        private readonly HashSet<MyGeneratedBlockLocation> m_removeLocations = new HashSet<MyGeneratedBlockLocation>();

        // Helper list of block locations which is processed when grids split.
        private readonly List<MyGeneratedBlockLocation> m_removeLocationsForGridSplits = new List<MyGeneratedBlockLocation>();

        // Split grids with transforms
        private readonly HashSet<MyGridInfo> m_splitGridInfos = new HashSet<MyGridInfo>();


        public virtual bool Initialize(MyCubeGrid grid, MyCubeSize gridSizeEnum)
        {
            m_grid = grid;
            m_enabled = true;

            if (IsValid(gridSizeEnum))
            {
                m_grid.OnBlockAdded += Grid_OnBlockAdded;
                m_grid.OnBlockRemoved += Grid_OnBlockRemoved;
                m_grid.OnGridSplit += Grid_OnGridSplit;

                return true;
            }

            return false;
        }

        public virtual void Close()
        {
            m_grid.OnBlockAdded -= Grid_OnBlockAdded;
            m_grid.OnBlockRemoved -= Grid_OnBlockRemoved;
            m_grid.OnGridSplit -= Grid_OnGridSplit;
        }

        protected abstract bool IsValid(MyCubeSize gridSizeEnum);

        public virtual void EnableGenerator(bool enable)
        {
            m_enabled = enable;
        }


        public virtual void UpdateAfterGridSpawn(MySlimBlock block)
        {
            Grid_OnBlockAdded(block);
        }

        public virtual void BlockAddedToMergedGrid(MySlimBlock block)
        {
            Grid_OnBlockAdded(block);
        }

        public virtual void UpdateAfterSimulation()
        {
            Debug.Assert(MyFakes.ENABLE_GENERATED_BLOCKS);

            if (m_addLocations.Count > 0)
            {
                // Remove ather grid blocks.
                m_addLocations.RemoveWhere(delegate(MyGeneratedBlockLocation loc)
                {
                    return loc.RefBlock != null && loc.RefBlock.CubeGrid != m_grid;
                });

                // Check the same remove/add blocks
                m_addLocations.RemoveWhere(delegate(MyGeneratedBlockLocation loc)
                {
                    MyGeneratedBlockLocation? remLocSame = null;
                    foreach (var remLoc in m_removeLocations)
                    {
                        if (MyGeneratedBlockLocation.IsSameGeneratedBlockLocation(loc, remLoc))
                        {
                            remLocSame = remLoc;
                            break;
                        }
                    }

                    if (remLocSame.HasValue)
                    {
                        m_removeLocations.Remove(remLocSame.Value);
                        return true;
                    }

                    return false;
                });

                // Check if there is already placed the same block in the grid
                m_addLocations.RemoveWhere(delegate(MyGeneratedBlockLocation loc)
                {
                    MySlimBlock existingBlock = m_grid.GetCubeBlock(loc.Position);
                    if (existingBlock != null)
                    {
                        if (existingBlock.FatBlock is MyCompoundCubeBlock)
                        {
                            MyCompoundCubeBlock compoundBlock = existingBlock.FatBlock as MyCompoundCubeBlock;
                            foreach (var blockInCompound in compoundBlock.GetBlocks())
                            {
                                if (blockInCompound.BlockDefinition == loc.BlockDefinition && blockInCompound.Orientation == loc.Orientation)
                                    return true;
                            }
                        }
                        else
                        {
                            if (existingBlock.BlockDefinition == loc.BlockDefinition && existingBlock.Orientation == loc.Orientation)
                                return true;
                        }
                    }

                    return false;
                });
            }

            if (m_removeLocations.Count > 0)
                RemoveBlocks();

            if (m_addLocations.Count > 0)
                AddBlocks();

            m_addLocations.Clear();
            m_removeLocations.Clear();
            m_removeLocationsForGridSplits.Clear();

            m_splitGridInfos.Clear();
        }

        public abstract MySlimBlock GetGeneratingBlock(MySlimBlock generatedBlock);

        public abstract void OnAddedCube(MySlimBlock cube);
        public abstract void OnRemovedCube(MySlimBlock cube);

        /// <summary>
        /// Return true if any not genrated cube exists in the given positions.
        /// </summary>
        protected bool CubeExistsOnPositions(Vector3I[] positions)
        {
            foreach (var pos in positions)
            {
                MySlimBlock block = m_grid.GetCubeBlock(pos);
                if (block != null)
                {
                    if (block.FatBlock is MyCompoundCubeBlock)
                    {
                        MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                        foreach (var blockInCompound in compoundBlock.GetBlocks())
                        {
                            if (!blockInCompound.BlockDefinition.IsGeneratedBlock)
                                return true;
                        }
                    }
                    else
                    {
                        if (!block.BlockDefinition.IsGeneratedBlock)
                            return true;
                    }
                }
            }

            return false;
        }

        protected bool CanPlaceBlock(Vector3I position, MyCubeBlockDefinition definition, Vector3I forward, Vector3I up)
        {
            Debug.Assert(forward != up);
            Debug.Assert(forward != Vector3I.Zero);
            Debug.Assert(up != Vector3I.Zero);
            MyBlockOrientation blockOrientation = new MyBlockOrientation(Base6Directions.GetDirection(forward), Base6Directions.GetDirection(up));
            return m_grid.CanPlaceBlock(position, position, blockOrientation, definition);
        }

        protected static bool IsSameMaterial(MySlimBlock block1, MySlimBlock block2)
        {
            Debug.Assert(!(block1.FatBlock is MyCompoundCubeBlock));
            Debug.Assert(!(block2.FatBlock is MyCompoundCubeBlock));
            Debug.Assert(block1 != block2);

            foreach (var component1 in block1.BlockDefinition.Components)
            {
                if (block2.BlockDefinition.Components.Contains(component1))
                    return true;
            }

            return false;
        }

        private void Grid_OnBlockAdded(MySlimBlock cube)
        {
            Debug.Assert(MyFakes.ENABLE_GENERATED_BLOCKS);

            if (!m_enabled || !cube.CubeGrid.InScene || cube.BlockDefinition.IsGeneratedBlock || ((cube.FatBlock is MyCompoundCubeBlock) && ((MyCompoundCubeBlock)cube.FatBlock).GetBlocksCount() == 0))
                return;

            Debug.Assert(cube.CubeGrid == m_grid);

            OnAddedCube(cube);
        }

        private void Grid_OnBlockRemoved(MySlimBlock cube)
        {
            Debug.Assert(MyFakes.ENABLE_GENERATED_BLOCKS);

            if (!m_enabled || !cube.CubeGrid.InScene || cube.BlockDefinition.IsGeneratedBlock || ((cube.FatBlock is MyCompoundCubeBlock) && ((MyCompoundCubeBlock)cube.FatBlock).GetBlocksCount() == 0))
                return;

            Debug.Assert(cube.CubeGrid == m_grid);

            OnRemovedCube(cube);
        }

        private void Grid_OnGridSplit(MyCubeGrid originalGrid, MyCubeGrid newGrid)
        {
            Debug.Assert(originalGrid == m_grid);

            ProcessChangedGrid(newGrid);
        }

        private void ProcessChangedGrid(MyCubeGrid newGrid)
        {
            Vector3I gridOffset = Vector3I.Round((m_grid.PositionComp.GetPosition() - newGrid.PositionComp.GetPosition()) / m_grid.GridSize);
            Vector3 fw = (Vector3)Vector3D.TransformNormal(m_grid.WorldMatrix.Forward, newGrid.PositionComp.WorldMatrixNormalizedInv);
            Vector3 up = (Vector3)Vector3D.TransformNormal(m_grid.WorldMatrix.Up, newGrid.PositionComp.WorldMatrixNormalizedInv);
            Base6Directions.Direction fwDir = Base6Directions.GetClosestDirection(fw);
            Base6Directions.Direction upDir = Base6Directions.GetClosestDirection(up);
            if (upDir == fwDir) upDir = Base6Directions.GetPerpendicular(fwDir);
            MatrixI transform = new MatrixI(ref gridOffset, fwDir, upDir);

            MyGridInfo gridInfo = new MyGridInfo();
            gridInfo.Grid = newGrid;
            gridInfo.Transform = transform;

            m_splitGridInfos.Add(gridInfo);

            // Remove from split grid
            if (m_removeLocationsForGridSplits.Count > 0)
            {
                List<int> indexesToRemove = new List<int>();

                for (int i = 0; i < m_removeLocationsForGridSplits.Count; ++i)
                {
                    MyGeneratedBlockLocation location = m_removeLocationsForGridSplits[i];
                    Debug.Assert(location.GeneratedBlockType != MyStringId.NullOrEmpty);
                    RemoveBlock(location, gridInfo, location.GeneratedBlockType);
                }
            }

            // Add to split grid
            List<MySlimBlock> newGridBlocks = new List<MySlimBlock>();
            m_addLocations.RemoveWhere(delegate(MyGeneratedBlockLocation loc)
            {
                if (loc.RefBlock != null && loc.RefBlock.CubeGrid == newGrid)
                {
                    newGridBlocks.Add(loc.RefBlock);
                    return true;
                }
                return false;
            });

            foreach (var newGridBlock in newGridBlocks) 
            {
                Debug.Assert(newGrid == newGridBlock.CubeGrid);
                newGridBlock.CubeGrid.AdditionalModelGenerators.ForEach(g => g.UpdateAfterGridSpawn(newGridBlock));
            }
        }

        /// <summary>
        /// Adds generated block to block locations. Block is not added if already exists in grid.
        /// </summary>
        protected void AddGeneratedBlock(MySlimBlock refBlock, MyCubeBlockDefinition generatedBlockDefinition, Vector3I position, Vector3I forward, Vector3I up)
        {
            Debug.Assert(refBlock != null && !refBlock.BlockDefinition.IsGeneratedBlock);
            Debug.Assert(generatedBlockDefinition.Size == Vector3I.One);
            if (generatedBlockDefinition.Size == Vector3I.One)
                m_addLocations.Add(new MyGeneratedBlockLocation(refBlock, generatedBlockDefinition, position, forward, up));
        }

        /// <summary>
        /// Removes generated blocks in the given locations.
        /// </summary>
        protected void RemoveGeneratedBlock(MyStringId generatedBlockType, MyGeneratedBlockLocation[] locations)
        {
            if (locations == null)
                return;

            foreach (var location in locations)
            {
                RemoveBlock(location, null, generatedBlockType);
                {
                    // Also try to postpone block remove to possible splits
                    MyGeneratedBlockLocation locToRemove = location;
                    locToRemove.GeneratedBlockType = generatedBlockType;
                    m_removeLocationsForGridSplits.Add(locToRemove);
                }

                // Also process added locations
                if (m_addLocations.Count > 0)
                {
                    // Check the same remove/add blocks
                    m_addLocations.RemoveWhere(delegate(MyGeneratedBlockLocation loc)
                    {
                        return MyGeneratedBlockLocation.IsSameGeneratedBlockLocation(loc, location, generatedBlockType);
                    });
                }
            }
        }

        private bool RemoveBlock(MyGeneratedBlockLocation location, MyGridInfo gridInfo, MyStringId generatedBlockType)
        {
            MySlimBlock slimBlock = null;
            if (gridInfo != null) 
            {
                Vector3I position = Vector3I.Transform(location.Position, gridInfo.Transform);
                slimBlock = gridInfo.Grid.GetCubeBlock(position);
            }
            else 
            {
                slimBlock = m_grid.GetCubeBlock(location.Position);
            }

            if (slimBlock != null)
            {
                if (slimBlock.FatBlock is MyCompoundCubeBlock)
                {
                    MyCompoundCubeBlock compoundBlock = slimBlock.FatBlock as MyCompoundCubeBlock;
                    var compoundBlocks = compoundBlock.GetBlocks();
                    for (int i = 0; i < compoundBlocks.Count; ++i)
                    {
                        MySlimBlock block = compoundBlocks[i];
                        Debug.Assert(block != null);
                        if (block.BlockDefinition.IsGeneratedBlock && block.BlockDefinition.GeneratedBlockType == generatedBlockType
                            && block.Orientation == location.Orientation)
                        {
                            ushort? id = compoundBlock.GetBlockId(block);
                            m_removeLocations.Add(new MyGeneratedBlockLocation(null, block.BlockDefinition, block.Position, block.Orientation, blockIdInCompound: id, gridInfo: gridInfo));
                            return true;
                        }
                    }
                }
                else if (slimBlock.BlockDefinition.IsGeneratedBlock && slimBlock.BlockDefinition.GeneratedBlockType == generatedBlockType
                    && slimBlock.Orientation == location.Orientation)
                {
                    m_removeLocations.Add(new MyGeneratedBlockLocation(null, slimBlock.BlockDefinition, slimBlock.Position, slimBlock.Orientation, gridInfo: gridInfo));
                    return true;
                }
            }

            return false;
        }

        private void AddBlocks()
        {
            HashSet<MyCubeGrid.MyBlockLocation> locations = new HashSet<MyCubeGrid.MyBlockLocation>();

            Quaternion rotation;
            foreach (var loc in m_addLocations)
            {
                loc.Orientation.GetQuaternion(out rotation);
                MyCubeGrid.MyBlockLocation blockLocation = new MyCubeGrid.MyBlockLocation(
                    loc.BlockDefinition.Id, loc.Position, loc.Position, loc.Position, rotation, MyEntityIdentifier.AllocateId(), MySession.LocalPlayerId);

                locations.Add(blockLocation);
            }

            foreach (var location in locations)
                m_grid.BuildGeneratedBlock(location, Vector3I.Zero);
        }

        private void RemoveBlocks(bool removeLocalBlocks = true)
        {
            List<Vector3I> locations = new List<Vector3I>();
            List<Tuple<Vector3I, ushort>> locationsAndIds = new List<Tuple<Vector3I, ushort>>();

            if (removeLocalBlocks)
            {
                foreach (var loc in m_removeLocations)
                {
                    if (loc.GridInfo != null)
                        continue;

                    if (loc.BlockIdInCompound == null)
                        locations.Add(loc.Position);
                    else
                        locationsAndIds.Add(new Tuple<Vector3I, ushort>(loc.Position, loc.BlockIdInCompound.Value));
                }

                if (locations.Count > 0)
                    m_grid.RazeGeneratedBlocks(locations);

                if (locationsAndIds.Count > 0)
                    m_grid.RazeGeneratedBlocksInCompoundBlock(locationsAndIds);
            }

            // Process split grids
            foreach (var gridInfo in m_splitGridInfos)
            {
                locations.Clear();
                locationsAndIds.Clear();

                foreach (var loc in m_removeLocations)
                {
                    if (loc.GridInfo != gridInfo)
                        continue;

                    if (loc.BlockIdInCompound == null)
                        locations.Add(loc.Position);
                    else
                        locationsAndIds.Add(new Tuple<Vector3I, ushort>(loc.Position, loc.BlockIdInCompound.Value));
                }

                if (locations.Count > 0)
                    gridInfo.Grid.RazeGeneratedBlocks(locations);

                if (locationsAndIds.Count > 0)
                    gridInfo.Grid.RazeGeneratedBlocksInCompoundBlock(locationsAndIds);
            }

        }
    }
}
