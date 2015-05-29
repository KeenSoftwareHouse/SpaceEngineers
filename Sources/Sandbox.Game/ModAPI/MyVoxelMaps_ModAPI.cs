using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRage.Voxels;
using VRageMath;
using IMyStorage = Sandbox.ModAPI.Interfaces.IMyStorage;

namespace Sandbox.Game.Entities
{
    partial class MyVoxelMaps : ModAPI.IMyVoxelMaps
    {
        static MyShapeBox m_boxVoxelShape = new MyShapeBox();
        static MyShapeCapsule m_capsuleShape = new MyShapeCapsule();
        static MyShapeSphere m_sphereShape = new MyShapeSphere();
        static MyShapeRamp m_rampShape = new MyShapeRamp();


        void IMyVoxelMaps.Clear()
        {
            Clear();
        }

        bool IMyVoxelMaps.Exist(IMyVoxelBase voxelMap)
        {
            return Exist(voxelMap as MyVoxelBase);
        }

        IMyVoxelBase IMyVoxelMaps.GetOverlappingWithSphere(ref BoundingSphereD sphere)
        {
            return GetOverlappingWithSphere(ref sphere);
        }

        IMyVoxelBase IMyVoxelMaps.GetVoxelMapWhoseBoundingBoxIntersectsBox(ref VRageMath.BoundingBoxD boundingBox, IMyVoxelBase ignoreVoxelMap)
        {
            return GetVoxelMapWhoseBoundingBoxIntersectsBox(ref boundingBox, ignoreVoxelMap as Game.Entities.MyVoxelBase);
        }

        void IMyVoxelMaps.GetInstances(List<IMyVoxelBase> voxelMaps, Func<IMyVoxelBase, bool> collect)
        {
            foreach (var map in Instances)
            {
                if (collect == null || collect(map))
                {
                    voxelMaps.Add(map);
                }
            }
        }

        IMyStorage IMyVoxelMaps.CreateStorage(Vector3I size)
        {
            return new MyOctreeStorage(null, size);
        }


        IMyVoxelMap IMyVoxelMaps.CreateVoxelMap(string storageName, IMyStorage storage, Vector3D position,long voxelMapId)
        {
            var voxelMap = new MyVoxelMap();
            voxelMap.EntityId = voxelMapId;
            voxelMap.Init(storageName, storage as Sandbox.Engine.Voxels.IMyStorage, position);
            MyEntities.Add(voxelMap);
            return voxelMap;
        }

        IMyVoxelMap IMyVoxelMaps.CreateVoxelMapFromStorageName(string storageName, string prefabVoxelMapName, Vector3D position)
        {
            var filePath = MyWorldGenerator.GetVoxelPrefabPath(prefabVoxelMapName);
            var storage = MyStorageBase.LoadFromFile(filePath);
            if (storage == null) return null;
            storage.DataProvider = MyCompositeShapeProvider.CreateAsteroidShape(0,
                storage.Size.AbsMax() * MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                MySession.Static.Settings.VoxelGeneratorVersion);
            return MyWorldGenerator.AddVoxelMap(storageName, storage, position);
        }

        IMyStorage IMyVoxelMaps.CreateStorage(byte[] data)
        {
            return MyStorageBase.Load(data);
        }

        IMyVoxelShapeBox IMyVoxelMaps.GetBoxVoxelHand()
        {
            return m_boxVoxelShape;
        }

        IMyVoxelShapeCapsule IMyVoxelMaps.GetCapsuleVoxelHand()
        {
            return m_capsuleShape;
        }

        IMyVoxelShapeSphere IMyVoxelMaps.GetSphereVoxelHand()
        {
            return m_sphereShape;
        }

        IMyVoxelShapeRamp IMyVoxelMaps.GetRampVoxelHand()
        {
            return m_rampShape;
        }

        void IMyVoxelMaps.PaintInShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape, byte materialIdx)
        {
            MyVoxelGenerator.RequestPaintInShape(voxelMap, voxelShape, materialIdx);
        }

        void IMyVoxelMaps.CutOutShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape)
        {
            MyVoxelGenerator.RequestCutOutShape(voxelMap, voxelShape);
        }

        void IMyVoxelMaps.FillInShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape, byte materialIdx)
        {
            MyVoxelGenerator.RequestFillInShape(voxelMap, voxelShape, materialIdx);
        }

        int IMyVoxelMaps.VoxelMaterialCount
        {
            get
            {
                return MyDefinitionManager.Static.VoxelMaterialCount;
            }
        }


    }
}
