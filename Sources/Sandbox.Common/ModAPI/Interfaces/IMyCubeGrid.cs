using System;
using System.Collections.Generic;
namespace Sandbox.ModAPI.Ingame
{
    public interface IMyCubeGrid
    {
        List<long> BigOwners { get; }
        List<long> SmallOwners { get; }
        void ConvertToDynamic();
        bool CubeExists(VRageMath.Vector3I pos);
        void FixTargetCube(out VRageMath.Vector3I cube, VRageMath.Vector3 fractionalGridPosition);
        VRageMath.Vector3 GetClosestCorner(VRageMath.Vector3I gridPos, VRageMath.Vector3 position);
        IMySlimBlock GetCubeBlock(VRageMath.Vector3I pos);
        VRageMath.Vector3D GridIntegerToWorld(VRageMath.Vector3I gridCoords);
        float GridSize { get; }
        Sandbox.Common.ObjectBuilders.MyCubeSize GridSizeEnum { get; }
        bool IsStatic { get; }
        VRageMath.Vector3I Max { get; }
        VRageMath.Vector3I Min { get; }
        VRageMath.Vector3I? RayCastBlocks(VRageMath.Vector3D worldStart, VRageMath.Vector3D worldEnd);
        void RayCastCells(VRageMath.Vector3D worldStart, VRageMath.Vector3D worldEnd, List<VRageMath.Vector3I> outHitPositions, VRageMath.Vector3I? gridSizeInflate = null, bool havokWorld = false);
        void UpdateOwnership(long ownerId, bool isFunctional);
        VRageMath.Vector3I WorldToGridInteger(VRageMath.Vector3D coords);

        //Allocations
        void GetBlocks(List<IMySlimBlock> blocks, Func<IMySlimBlock, bool> collect = null);
        List<IMySlimBlock> GetBlocksInsideSphere(ref VRageMath.BoundingSphereD sphere);
    }
}
