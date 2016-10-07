using System;
using System.Collections.Generic;
using VRage.ModAPI;
using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMyVoxelMaps
    {
        void Clear();

        bool Exist(IMyVoxelBase voxelMap);

        IMyVoxelBase GetOverlappingWithSphere(ref BoundingSphereD sphere);
        IMyVoxelBase GetVoxelMapWhoseBoundingBoxIntersectsBox(ref BoundingBoxD boundingBox, IMyVoxelBase ignoreVoxelMap);

        /// <summary>
        /// Gets a list of all voxelmaps that overlap the specified sphere.
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns></returns>
        void GetAllOverlappingWithSphere(ref BoundingSphereD sphere, List<IMyVoxelBase> voxels);

        /// <summary>
        /// Gets a list of all voxelmaps that overlap the specified sphere.
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns></returns>
        /// <remarks>Not thread safe.</remarks>
        List<IMyVoxelBase> GetAllOverlappingWithSphere(ref BoundingSphereD sphere);

        void GetInstances(List<IMyVoxelBase> outInstances, Func<IMyVoxelBase, bool> collect = null);

        IMyStorage CreateStorage(Vector3I size);
        IMyStorage CreateStorage(byte[] data);

        IMyVoxelMap CreateVoxelMap(string storageName, IMyStorage storage, Vector3D position, long voxelMapId);

        /// <summary>
        /// Adds a prefab voxel to the game world.
        /// </summary>
        /// <param name="storageName">The name of which the voxel storage will be called within the world.</param>
        /// <param name="prefabVoxelMapName">The prefab voxel to add.</param>
        /// <param name="position">The Min corner position of the voxel within the world.</param>
        /// <returns>The newly added voxel map. Returns null if the prefabVoxelMapName does not exist.</returns>
        IMyVoxelMap CreateVoxelMapFromStorageName(string storageName, string prefabVoxelMapName, Vector3D position);

        IMyVoxelShapeBox GetBoxVoxelHand();

        IMyVoxelShapeCapsule GetCapsuleVoxelHand();

        IMyVoxelShapeSphere GetSphereVoxelHand();

        IMyVoxelShapeRamp GetRampVoxelHand();

        /// <summary>
        /// Will paint given material with given shape
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="voxelShape"></param>
        /// <param name="materialIdx"></param>
        void PaintInShape(
            IMyVoxelBase voxelMap,
          IMyVoxelShape voxelShape,
          byte materialIdx);

        /// <summary>
        /// Will cut out given shape
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="voxelShape"></param>

        void CutOutShape(
           IMyVoxelBase voxelMap,
           IMyVoxelShape voxelShape);


        /// <summary>
        /// Will fill given material with given shape
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="voxelShape"></param>
        /// <param name="materialIdx"></param>

        void FillInShape(
           IMyVoxelBase voxelMap,
           IMyVoxelShape voxelShape,
           byte materialIdx);


        int VoxelMaterialCount
        {
            get;
        }

        /// <summary>
        /// Creates a crater in a voxel, and deposits the specifid material.
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="sphere"></param>
        /// <param name="normal"></param>
        /// <param name="materialIdx"></param>
        /// <remarks>Not synced.<br/>
        /// Call <see cref="VRage.ModAPI.IMyVoxelBase.CreateMeteorCrater">IMyVoxelBase.CreateMeteorCrater</see> on the server instead for a synced operation.
        /// </remarks>
        void MakeCrater(IMyVoxelBase voxelMap, BoundingSphereD sphere, Vector3 normal, byte materialIdx);

    }
}