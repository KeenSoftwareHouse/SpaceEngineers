using Sandbox.Game.Entities.Cube;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public partial class MyCubeGrid : IMyCubeGrid
    {
        IMySlimBlock IMyCubeGrid.AddBlock(MyObjectBuilder_CubeBlock objectBuilder, bool testMerge)
        {
            return AddBlock(objectBuilder, testMerge);
        }

        void IMyCubeGrid.ApplyDestructionDeformation(IMySlimBlock block)
        {
            if (block is MySlimBlock)
                ApplyDestructionDeformation(block as MySlimBlock);
        }

        VRageMath.MatrixI IMyCubeGrid.CalculateMergeTransform(IMyCubeGrid gridToMerge, VRageMath.Vector3I gridOffset)
        {
            if (gridToMerge is MyCubeGrid)
                return CalculateMergeTransform(gridToMerge as MyCubeGrid, gridOffset);
            return new MatrixI();
        }

        bool IMyCubeGrid.CanMergeCubes(IMyCubeGrid gridToMerge, VRageMath.Vector3I gridOffset)
        {
            if (gridToMerge is MyCubeGrid)
                return CanMergeCubes(gridToMerge as MyCubeGrid, gridOffset);
            return false;
        }

        void IMyCubeGrid.GetBlocks(List<IMySlimBlock> blocks, Func<IMySlimBlock, bool> collect)
        {
            var lst = GetBlocks();
            foreach (var block in lst)
                if (collect == null || collect(block))
                    blocks.Add(block);
        }

        List<IMySlimBlock> IMyCubeGrid.GetBlocksInsideSphere(ref VRageMath.BoundingSphereD sphere)
        {
            HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>();
            GetBlocksInsideSphere(ref sphere, blocks);
            var result = new List<IMySlimBlock>(blocks.Count);
            foreach (var block in blocks)
                result.Add(block);
            return result;
        }

        IMySlimBlock IMyCubeGrid.GetCubeBlock(VRageMath.Vector3I pos)
        {
            return GetCubeBlock(pos);
        }

        VRageMath.Vector3D? IMyCubeGrid.GetLineIntersectionExactAll(ref VRageMath.LineD line, out double distance, out IMySlimBlock intersectedBlock)
        {
            MySlimBlock block;
            var retVal = GetLineIntersectionExactAll(ref line, out distance, out block);
            intersectedBlock = block;
            return retVal;
        }

        IMyCubeGrid IMyCubeGrid.MergeGrid_MergeBlock(IMyCubeGrid gridToMerge, VRageMath.Vector3I gridOffset)
        {
            if (gridToMerge is MyCubeGrid)
                return MergeGrid_MergeBlock(gridToMerge as MyCubeGrid, gridOffset);
            return null;
        }

        void IMyCubeGrid.RemoveBlock(IMySlimBlock block, bool updatePhysics)
        {
            if (block is MySlimBlock)
                RemoveBlock(block as MySlimBlock, updatePhysics);
        }

        void IMyCubeGrid.RemoveDestroyedBlock(IMySlimBlock block)
        {
            if (block is MySlimBlock)
                RemoveDestroyedBlock(block as MySlimBlock);
        }

        void IMyCubeGrid.UpdateBlockNeighbours(IMySlimBlock block)
        {
            if (block is MySlimBlock)
                UpdateBlockNeighbours(block as MySlimBlock);
        }

        List<long> IMyCubeGrid.BigOwners
        {
            get { return BigOwners; }
        }

        List<long> IMyCubeGrid.SmallOwners
        {
            get { return SmallOwners; }
        }

        void IMyCubeGrid.ChangeGridOwnership(long playerId, MyOwnershipShareModeEnum shareMode)
        {
            ChangeGridOwnership(playerId, shareMode);
        }

        void IMyCubeGrid.ClearSymmetries()
        {
            ClearSymmetries();
        }

        void IMyCubeGrid.ColorBlocks(Vector3I min, Vector3I max, Vector3 newHSV)
        {
            ColorBlocks(min, max, newHSV, false);
        }

        void IMyCubeGrid.ConvertToDynamic()
        {
            ConvertToDynamic();
        }

        bool IMyCubeGrid.CubeExists(Vector3I pos)
        {
            return CubeExists(pos);
        }

        void IMyCubeGrid.FixTargetCube(out Vector3I cube, Vector3 fractionalGridPosition)
        {
            FixTargetCube(out cube, fractionalGridPosition);
        }

        Vector3 IMyCubeGrid.GetClosestCorner(Vector3I gridPos, Vector3 position)
        {
            return GetClosestCorner(gridPos, position);
        }

        bool IMyCubeGrid.GetLineIntersectionExactGrid(ref LineD line, ref Vector3I position, ref double distanceSquared)
        {
            return GetLineIntersectionExactGrid(ref line, ref position, ref distanceSquared);
        }

        Vector3D IMyCubeGrid.GridIntegerToWorld(Vector3I gridCoords)
        {
            return GridIntegerToWorld(gridCoords);
        }

        float IMyCubeGrid.GridSize
        {
            get { return GridSize; }
        }

        MyCubeSize IMyCubeGrid.GridSizeEnum
        {
            get
            {
                return GridSizeEnum;
            }
            set
            {
                GridSizeEnum = value;
            }
        }

        bool IMyCubeGrid.IsStatic
        {
            get { return IsStatic; }
        }

        bool IMyCubeGrid.IsTouchingAnyNeighbor(Vector3I min, Vector3I max)
        {
            return IsTouchingAnyNeighbor(min, max);
        }

        bool IMyCubeGrid.IsTrash()
        {
            return IsTrash();
        }

        Vector3I IMyCubeGrid.Max
        {
            get { return Max; }
        }

        Vector3I IMyCubeGrid.Min
        {
            get { return Min; }
        }

        Vector3I? IMyCubeGrid.RayCastBlocks(Vector3D worldStart, Vector3D worldEnd)
        {
            return RayCastBlocks(worldStart, worldEnd);
        }

        void IMyCubeGrid.RayCastCells(Vector3D worldStart, Vector3D worldEnd, List<Vector3I> outHitPositions, Vector3I? gridSizeInflate, bool havokWorld)
        {
            RayCastCells(worldStart, worldEnd, outHitPositions, gridSizeInflate, havokWorld);
        }

        void IMyCubeGrid.RazeBlock(Vector3I position)
        {
            RazeBlock(position);
        }

        void IMyCubeGrid.RazeBlocks(ref Vector3I pos, ref Vector3UByte size)
        {
            RazeBlocks(ref pos, ref size);
        }

        void IMyCubeGrid.RazeBlocks(List<Vector3I> locations)
        {
            RazeBlocks(locations);
        }

        Vector3I IMyCubeGrid.WorldToGridInteger(Vector3 coords)
        {
            return WorldToGridInteger(coords);
        }

        bool IMyCubeGrid.WillRemoveBlockSplitGrid( IMySlimBlock testBlock )
        {
            return WillRemoveBlockSplitGrid( (MySlimBlock)testBlock );
        }

        Action<MySlimBlock> GetDelegate(Action<IMySlimBlock> value)
        {
            return (Action<MySlimBlock>)Delegate.CreateDelegate(typeof(Action<MySlimBlock>), value.Target, value.Method);
        }

        Action<MyCubeGrid> GetDelegate(Action<IMyCubeGrid> value)
        {
            return (Action<MyCubeGrid>)Delegate.CreateDelegate(typeof(Action<MyCubeGrid>), value.Target, value.Method);
        }

        event Action<IMySlimBlock> IMyCubeGrid.OnBlockAdded
        {
            add { OnBlockAdded += GetDelegate(value); }
            remove { OnBlockAdded -= GetDelegate(value); }
        }

        event Action<IMySlimBlock> IMyCubeGrid.OnBlockRemoved
        {
            add { OnBlockRemoved += GetDelegate(value); }
            remove { OnBlockRemoved -= GetDelegate(value); }
        }

        event Action<IMyCubeGrid> IMyCubeGrid.OnBlockOwnershipChanged
        {
            add { OnBlockOwnershipChanged += GetDelegate(value); }
            remove { OnBlockOwnershipChanged -= GetDelegate(value); }
        }

        event Action<IMyCubeGrid> IMyCubeGrid.OnGridChanged
        {
            add { OnGridChanged += GetDelegate(value); }
            remove { OnGridChanged -= GetDelegate(value); }
        }

        VRage.Game.ModAPI.Ingame.IMySlimBlock VRage.Game.ModAPI.Ingame.IMyCubeGrid.GetCubeBlock(Vector3I position)
        {
            VRage.Game.ModAPI.Ingame.IMySlimBlock block = GetCubeBlock(position);
            if (block != null && block.FatBlock != null)
            {
                if ((block.FatBlock is MyTerminalBlock) && (block.FatBlock as MyTerminalBlock).IsAccessibleForProgrammableBlock)
                {
                    return block;
                }
            }
            return null;
        }
    }
}
