using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyVoxelMaps
    {
        void Clear();

        bool Exist(IMyVoxelBase voxelMap);

        IMyVoxelBase GetOverlappingWithSphere(ref BoundingSphereD sphere);
        IMyVoxelBase GetVoxelMapWhoseBoundingBoxIntersectsBox(ref BoundingBoxD boundingBox, IMyVoxelBase ignoreVoxelMap);

        void GetInstances(List<IMyVoxelBase> outInstances, Func<IMyVoxelBase, bool> collect = null);

        IMyStorage CreateStorage(Vector3I size);
        IMyStorage CreateStorage(byte[] data);

        IMyVoxelMap CreateVoxelMap(string storageName, IMyStorage storage, Vector3D position, long voxelMapId);

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

    }
}