using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyVoxelShape
    {
        /// <summary>
        /// World matrix of voxel shape
        /// </summary>
        MatrixD Transform
        {
            get;
            set;
        }

        /// <summary>
        /// Gets current world boundaries
        /// </summary>
        /// <returns></returns>
        BoundingBoxD GetWorldBoundary();

        /// <summary>
        /// Peeks world boundaries at given position
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        BoundingBoxD PeekWorldBoundary(ref Vector3D targetPosition);

        /// <summary>
        /// Gets volume of intersection of shape and voxel
        /// </summary>
        /// <param name="voxelPosition">Left bottom point of voxel</param>
        /// <returns>Normalized volume of intersection</returns>
        float GetIntersectionVolume(ref Vector3D voxelPosition);
    }
}
