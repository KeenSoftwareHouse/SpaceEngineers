using Sandbox.Engine.Voxels;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Voxels;

namespace Sandbox.Game.Entities
{
    partial class MyVoxelMap : IMyVoxelMap
    {
        void IMyVoxelMap.Close()
        {
            Close();
        }

        bool IMyVoxelMap.DoOverlapSphereTest(float sphereRadius, VRageMath.Vector3D spherePos)
        {
            return DoOverlapSphereTest(sphereRadius, spherePos);
        }

        void IMyVoxelMap.ClampVoxelCoord(ref VRageMath.Vector3I voxelCoord)
        {
            Storage.ClampVoxelCoord(ref voxelCoord);
        }

        bool IMyVoxelMap.GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere)
        {
            return GetIntersectionWithSphere(ref sphere);
        }

        MyObjectBuilder_EntityBase IMyVoxelMap.GetObjectBuilder(bool copy)
        {
            return GetObjectBuilder(copy);
        }

        float IMyVoxelMap.GetVoxelContentInBoundingBox(VRageMath.BoundingBoxD worldAabb, out float cellCount)
        {
            return GetVoxelContentInBoundingBox_Obsolete(worldAabb, out cellCount);
        }

        VRageMath.Vector3I IMyVoxelMap.GetVoxelCoordinateFromMeters(VRageMath.Vector3D pos)
        {
            VRageMath.Vector3I result;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(this.PositionLeftBottomCorner, ref pos, out result);
            return result;
        }

        void IMyVoxelMap.Init(MyObjectBuilder_EntityBase builder)
        {
            Init(builder);
        }
    }
}
