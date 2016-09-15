
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
using VRage.Game;
using VRage.Profiler;

namespace Sandbox.Game.Entities.Cube
{
    using ModelId = System.Int32;
    using VRage.Utils;
    using VRage.Library.Utils;
    using VRage;
    using Sandbox.Game.EntityComponents;

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

        // Right directions for Forward vectors
        protected static Vector3I[] Rights = new Vector3I[] 
        {
            Vector3I.Right,
            Vector3I.Backward,
            Vector3I.Left,
            Vector3I.Forward,
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

            public override bool Equals(object ob)
            {
                if (ob is MyGeneratedBlockLocation)
                {
                    MyGeneratedBlockLocation other = (MyGeneratedBlockLocation)ob;
                    return IsSameGeneratedBlockLocation(this, other);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return BlockDefinition.Id.GetHashCode() + 17 * Position.GetHashCode() + 137 * Orientation.GetHashCode();
            }
        }

        protected static readonly MyStringId BUILD_TYPE_WALL = MyStringId.GetOrCompute("wall");
        private static readonly List<Tuple<MyCubeGrid.MyBlockLocation, MySlimBlock>> m_tmpLocationsAndRefBlocks = new List<Tuple<MyCubeGrid.MyBlockLocation, MySlimBlock>>();
        private static readonly List<Vector3I> m_tmpLocations = new List<Vector3I>();
        private static readonly List<Tuple<Vector3I, ushort>> m_tmpLocationsAndIds = new List<Tuple<Vector3I, ushort>>();

        protected static readonly List<Vector3I> m_tmpPositionsAdd = new List<Vector3I>(32);
        protected static readonly List<Vector3I> m_tmpPositionsRemove = new List<Vector3I>(32);
        protected static readonly List<MyGeneratedBlockLocation> m_tmpLocationsRemove = new List<MyGeneratedBlockLocation>(32);

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

        public virtual void GenerateBlocks(MySlimBlock generatingBlock)
        {
            Grid_OnBlockAdded(generatingBlock);
        }

        public virtual void UpdateBeforeSimulation()
        {
            UpdateInternal();
        }

        public virtual void UpdateAfterSimulation()
        {
            UpdateInternal();
        }

        private void UpdateInternal()
        {
            Debug.Assert(MyFakes.ENABLE_GENERATED_BLOCKS);

            if (m_addLocations.Count > 0)
            {
                // Remove other grid blocks.
                m_addLocations.RemoveWhere(delegate(MyGeneratedBlockLocation loc)
                {
                    return loc.RefBlock != null && loc.RefBlock.CubeGrid != m_grid;
                });

                // Check if the block can be placed to grid - must be before check the same remove/add
                m_addLocations.RemoveWhere(delegate(MyGeneratedBlockLocation loc)
                {
                    return !m_grid.CanAddCube(loc.Position, loc.Orientation, loc.BlockDefinition, ignoreSame: true);
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
        /// Return true if any not generated cube exists in the given positions.
        /// </summary>
        protected bool CubeExistsOnPositions(List<Vector3I> positions)
        {
            foreach (var pos in positions)
            {
                if (CubeExistsOnPosition(pos))
                    return true;
            }

            return false;
        }

        protected bool CubeExistsOnPosition(Vector3I pos)
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

            return block1.BlockDefinition.BuildMaterial == block2.BlockDefinition.BuildMaterial;
        }

        protected bool CanGenerateFromBlock(MySlimBlock cube)
        {
            if (cube == null)
                return false;

            MyCompoundCubeBlock compoundBlock = cube.FatBlock as MyCompoundCubeBlock;

            if (!m_enabled || !cube.CubeGrid.InScene || cube.BlockDefinition.IsGeneratedBlock
                || (compoundBlock != null && compoundBlock.GetBlocksCount() == 0)
                || (compoundBlock == null && MySession.Static.SurvivalMode && cube.ComponentStack.BuildRatio < cube.BlockDefinition.BuildProgressToPlaceGeneratedBlocks)
                || (MyFakes.ENABLE_FRACTURE_COMPONENT && cube.FatBlock != null && cube.FatBlock.Components.Has<MyFractureComponentBase>())
                || (cube.FatBlock is MyFracturedBlock))
                return false;

            return true;
        }

        private void Grid_OnBlockAdded(MySlimBlock cube)
        {
            Debug.Assert(MyFakes.ENABLE_GENERATED_BLOCKS);
            ProfilerShort.Begin("ModelGenerator.OnBlockAdded");

            if (!CanGenerateFromBlock(cube))
            {
                ProfilerShort.End();
                return;
            }

            Debug.Assert(cube.CubeGrid == m_grid);

            if (cube.FatBlock is MyCompoundCubeBlock)
            {
                foreach (var blockInCompound in (cube.FatBlock as MyCompoundCubeBlock).GetBlocks())
                {
                    if (CanGenerateFromBlock(blockInCompound))
                    {
                        OnAddedCube(blockInCompound);
                    }
                }
            }
            else
            {
                OnAddedCube(cube);
            }

            ProfilerShort.End();
        }

        private void Grid_OnBlockRemoved(MySlimBlock cube)
        {
            Debug.Assert(MyFakes.ENABLE_GENERATED_BLOCKS);
            ProfilerShort.Begin("ModelGenerator.OnBlockRemoved");

            if (!m_enabled || !cube.CubeGrid.InScene || cube.BlockDefinition.IsGeneratedBlock || ((cube.FatBlock is MyCompoundCubeBlock) && ((MyCompoundCubeBlock)cube.FatBlock).GetBlocksCount() == 0))
            {
                ProfilerShort.End();
                return;
            }

            Debug.Assert(cube.CubeGrid == m_grid);

            OnRemovedCube(cube);

            ProfilerShort.End();
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
        /// Adds generated block to add block locations.
        /// </summary>
        protected void AddGeneratedBlock(MySlimBlock refBlock, MyCubeBlockDefinition generatedBlockDefinition, Vector3I position, Vector3I forward, Vector3I up)
        {
            Debug.Assert(refBlock != null && !refBlock.BlockDefinition.IsGeneratedBlock);

            var orientation = new MyBlockOrientation(Base6Directions.GetDirection(ref forward), Base6Directions.GetDirection(ref up));
            Debug.Assert(generatedBlockDefinition.Size == Vector3I.One);
            if (generatedBlockDefinition.Size == Vector3I.One)
                m_addLocations.Add(new MyGeneratedBlockLocation(refBlock, generatedBlockDefinition, position, orientation));
        }

        /// <summary>
        /// Removes generated blocks in the given locations.
        /// </summary>
        protected void RemoveGeneratedBlock(MyStringId generatedBlockType, List<MyGeneratedBlockLocation> locations)
        {
            if (locations == null || locations.Count == 0)
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
            Debug.Assert(m_tmpLocationsAndRefBlocks.Count == 0);

            Quaternion rotation;
            foreach (var loc in m_addLocations)
            {
                loc.Orientation.GetQuaternion(out rotation);
                MyCubeGrid.MyBlockLocation blockLocation = new MyCubeGrid.MyBlockLocation(
                    loc.BlockDefinition.Id, loc.Position, loc.Position, loc.Position, rotation, MyEntityIdentifier.AllocateId(), MySession.Static.LocalPlayerId);

                m_tmpLocationsAndRefBlocks.Add(new Tuple<MyCubeGrid.MyBlockLocation, MySlimBlock>(blockLocation, loc.RefBlock));
            }

            foreach (var location in m_tmpLocationsAndRefBlocks)
            {
                var block = m_grid.BuildGeneratedBlock(location.Item1, Vector3I.Zero);
                if (block != null)
                {
                    var compound = block.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        foreach (var blockInCompound in compound.GetBlocks())
                        {
                            Quaternion q;
                            location.Item1.Orientation.GetQuaternion(out q);
                            MyBlockOrientation r = new MyBlockOrientation(ref q);
                            if (blockInCompound.Orientation == r && blockInCompound.BlockDefinition.Id == location.Item1.BlockDefinition)
                            {
                                block = blockInCompound;
                                break;
                            }
                        }
                    }

                    var refBlock = location.Item2;
                    if (block != null && block.BlockDefinition.IsGeneratedBlock && refBlock != null)
                        block.SetGeneratedBlockIntegrity(refBlock);
                }
            }

            m_tmpLocationsAndRefBlocks.Clear();
        }

        private void RemoveBlocks(bool removeLocalBlocks = true)
        {
            Debug.Assert(m_tmpLocations.Count == 0);
            Debug.Assert(m_tmpLocationsAndIds.Count == 0);

            if (removeLocalBlocks)
            {
                foreach (var loc in m_removeLocations)
                {
                    if (loc.GridInfo != null)
                        continue;

                    if (loc.BlockIdInCompound == null)
                        m_tmpLocations.Add(loc.Position);
                    else
                        m_tmpLocationsAndIds.Add(new Tuple<Vector3I, ushort>(loc.Position, loc.BlockIdInCompound.Value));
                }

                if (m_tmpLocations.Count > 0)
                    m_grid.RazeGeneratedBlocks(m_tmpLocations);

                if (m_tmpLocationsAndIds.Count > 0)
                    m_grid.RazeGeneratedBlocksInCompoundBlock(m_tmpLocationsAndIds);
            }

            // Process split grids
            foreach (var gridInfo in m_splitGridInfos)
            {
                m_tmpLocations.Clear();
                m_tmpLocationsAndIds.Clear();

                foreach (var loc in m_removeLocations)
                {
                    if (loc.GridInfo != gridInfo)
                        continue;

                    if (loc.BlockIdInCompound == null)
                        m_tmpLocations.Add(loc.Position);
                    else
                        m_tmpLocationsAndIds.Add(new Tuple<Vector3I, ushort>(loc.Position, loc.BlockIdInCompound.Value));
                }

                if (m_tmpLocations.Count > 0)
                    gridInfo.Grid.RazeGeneratedBlocks(m_tmpLocations);

                if (m_tmpLocationsAndIds.Count > 0)
                    gridInfo.Grid.RazeGeneratedBlocksInCompoundBlock(m_tmpLocationsAndIds);
            }

            m_tmpLocations.Clear();
            m_tmpLocationsAndIds.Clear();
        }

        protected bool GeneratedBlockExists(Vector3I pos, MyBlockOrientation orientation, MyCubeBlockDefinition definition)
        {
            MySlimBlock block = m_grid.GetCubeBlock(pos);
            if (block == null)
                return false;

            var cmpBlock = block.FatBlock as MyCompoundCubeBlock;
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && cmpBlock != null)
            {
                foreach (var blockInCompound in cmpBlock.GetBlocks())
                {
                    // The same block with the same orientation
                    if (blockInCompound.BlockDefinition.Id.SubtypeId == definition.Id.SubtypeId && blockInCompound.Orientation == orientation)
                        return true;
                }

                return false;
            }
            else
            {
                return block.BlockDefinition.Id.SubtypeId == definition.Id.SubtypeId && block.Orientation == orientation;
            }
        }
    }
}
