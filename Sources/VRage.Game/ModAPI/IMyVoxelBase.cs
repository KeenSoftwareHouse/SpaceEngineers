using VRageMath;

namespace VRage.ModAPI
{
    public interface IMyVoxelBase : IMyEntity
    {
        IMyStorage Storage { get; }

        VRageMath.Vector3D PositionLeftBottomCorner { get; }

        bool IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref BoundingBoxD boundingBox);

        string StorageName
        {
            get;
        }
    }
}
