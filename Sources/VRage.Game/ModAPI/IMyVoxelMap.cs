using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyVoxelMap : IMyVoxelBase
    {     
        void ClampVoxelCoord(ref VRageMath.Vector3I voxelCoord);

        void Init(MyObjectBuilder_EntityBase builder);

        new MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false);

        new void Close();

        new bool DoOverlapSphereTest(float sphereRadius, Vector3D spherePos);
        new bool GetIntersectionWithSphere(ref VRageMath.BoundingSphereD sphere);

        float GetVoxelContentInBoundingBox(BoundingBoxD worldAabb, out float cellCount);
        Vector3I GetVoxelCoordinateFromMeters(Vector3D pos);
    }
}