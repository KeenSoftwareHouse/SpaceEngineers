using VRage.Game.Components;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Entities
{
    static class MyVoxelMapExtensions
    {
        public static Vector3D GetPositionOnVoxel(this MyVoxelMap map, Vector3D position, float maxVertDistance)
        {
            Vector3D result = position;
            Vector3I cell;
            BoundingBox cellAabb;
            MyVoxelCoordSystems.WorldPositionToGeometryCellCoord(map.PositionLeftBottomCorner, ref position, out cell);
            MyVoxelCoordSystems.GeometryCellCoordToLocalAABB(ref cell, out cellAabb);
            var localCellCenter = cellAabb.Center;
            Line l = new Line(
                localCellCenter + Vector3D.Up * maxVertDistance,
                localCellCenter + Vector3D.Down * maxVertDistance);
            VRage.Game.Models.MyIntersectionResultLineTriangle intersection;
            if (map.Storage.Geometry.Intersect(ref l, out intersection, IntersectionFlags.ALL_TRIANGLES))
            {
                Vector3D isect = intersection.InputTriangle.Vertex0;
                MyVoxelCoordSystems.LocalPositionToWorldPosition(map.PositionLeftBottomCorner- (Vector3D)map.StorageMin, ref isect, out result);
            }
            return result;
        }

    }
}
