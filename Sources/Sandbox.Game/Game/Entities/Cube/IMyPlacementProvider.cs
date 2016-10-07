using System.Collections.Generic;
using Sandbox.Engine.Physics;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    //TODO: the BlockBuilder still has placement related methods directly implemented => move them to provider?
    public interface IMyPlacementProvider
    {
        Vector3D RayStart { get; }
        Vector3D RayDirection { get; }
        MyPhysics.HitInfo? HitInfo { get; }
        MyCubeGrid ClosestGrid { get; }
        MyVoxelBase ClosestVoxelMap { get; }
        bool CanChangePlacementObjectSize { get; }
        float IntersectionDistance { get; set; }

        void RayCastGridCells(MyCubeGrid grid, List<Vector3I> outHitPositions, Vector3I gridSizeInflate, float maxDist);

        //this call should update closest grid/voxel and hitInfo
        //the three are usualy but not always requested at same time
        void UpdatePlacement();
    }
}