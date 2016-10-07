using VRage.Game.ModAPI.Interfaces;
using VRage.Voxels;
using VRageMath;

namespace VRage.ModAPI
{
    public interface IMyVoxelBase : IMyEntity, IMyDecalProxy
    {
        IMyStorage Storage { get; }

        /// <summary>
        /// Position of left/bottom corner of this voxel map, in world space (not relative to sector)
        /// </summary>
        VRageMath.Vector3D PositionLeftBottomCorner { get; }

        bool IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref BoundingBoxD boundingBox);

        /// <summary>
        /// Gets root voxel, for asteroids and planets itself.
        /// For MyVoxelPhysics, should return owning planet.
        /// </summary>
        IMyVoxelBase RootVoxel { get; }

        Matrix Orientation { get; }

        string StorageName
        {
            get;
        }

        /// <summary>
        /// Size of voxel map (in voxels)
        /// </summary>
        Vector3I Size { get; }

        /// <summary>
        /// Size of voxel map (in metres)
        /// </summary>
        Vector3 SizeInMetres { get; }

        Vector3I StorageMin { get; }
        Vector3I StorageMax { get; }

        /// <summary>
        /// Returns true if all corners of a boundingbox are inside a voxel.
        /// </summary>
        /// <param name="aabbWorldTransform">VoxelMap transform</param>
        /// <param name="aabb">Area to check</param>
        /// <returns></returns>
        bool AreAllAabbCornersInside(MatrixD aabbWorldTransform, BoundingBoxD aabb);

        /// <summary>
        /// Returns true if all corners of a boundingbox are inside a voxel.
        /// </summary>
        /// <param name="aabbWorldTransform">VoxelMap transform</param>
        /// <param name="aabb">Area to check</param>
        /// <returns></returns>
        bool IsAnyAabbCornerInside(MatrixD aabbWorldTransform, BoundingBoxD aabb);

        /// <summary>
        /// Returns the count of how many corners of a boundingbox are inside a voxel.
        /// </summary>
        /// <param name="aabbWorldTransform">VoxelMap transform</param>
        /// <param name="aabb">Area to check</param>
        /// <returns></returns>
        int CountCornersInside(MatrixD aabbWorldTransform, BoundingBoxD aabb);

        /// <summary>
        /// Returns <b>true</b> if the bounding box contains at least the specified amount (as a percent) of voxel material.
        /// </summary>
        /// <param name="worldAabb"></param>
        /// <param name="thresholdPercentage"></param>
        /// <returns></returns>
        bool IsOverlapOverThreshold(BoundingBoxD worldAabb, float thresholdPercentage = 0.9f);

        /// <summary>
        /// Cuts out the request volume from the voxel.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="createDebris"></param>
        /// <param name="damage"></param>
        void VoxelCutoutSphere(Vector3D center, float radius, bool createDebris, bool damage);
        /// <summary>
        /// Perform voxel operation using a capsule shape.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="radius"></param>
        /// <param name="Transformation"></param>
        /// <param name="material"></param>
        /// <param name="operation"></param>
        void VoxelOperationCapsule(Vector3D A, Vector3D B, float radius, MatrixD Transformation, byte material, OperationType operation);
        /// <summary>
        /// Perform voxel operation using a box shape.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="Transformation"></param>
        /// <param name="material"></param>
        /// <param name="operation"></param>
        void VoxelOperationBox(BoundingBoxD box, MatrixD Transformation, byte material, OperationType operation);
        /// <summary>
        /// Perform voxel operation using a ellipsoid shape.
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="Transformation"></param>
        /// <param name="material"></param>
        /// <param name="operation"></param>
        void VoxelOperationElipsoid(Vector3 radius, MatrixD Transformation, byte material, OperationType operation);
        /// <summary>
        /// Perform voxel operation using a ramp shape.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="rampNormal"></param>
        /// <param name="rampNormalW"></param>
        /// <param name="Transformation"></param>
        /// <param name="material"></param>
        /// <param name="operation"></param>
        void VoxelOperationRamp(BoundingBoxD box, Vector3D rampNormal, double rampNormalW, MatrixD Transformation, byte material, OperationType operation);
        /// <summary>
        /// Perform voxel operation using a sphere shape.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="material"></param>
        /// <param name="operation"></param>
        void VoxelOperationSphere(Vector3D center, float radius, byte material, OperationType operation);

        /// <summary>
        /// Creates a meteor crater on a voxel, depositing the specified material. Call on server.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="normal">Direction crater opens (eg. towards meteor source)</param>
        /// <param name="materialIdx">Material to deposit.</param>
        void CreateMeteorCrater(Vector3D center, float radius, Vector3 normal, byte materialIdx);

    }
}
