using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Voxels;
using VRageMath;
using IMyStorage = VRage.ModAPI.IMyStorage;

namespace Sandbox.Game.Entities
{
    partial class MyVoxelMaps : IMyVoxelMaps
    {
        static MyShapeBox m_boxVoxelShape = new MyShapeBox();
        static MyShapeCapsule m_capsuleShape = new MyShapeCapsule();
        static MyShapeSphere m_sphereShape = new MyShapeSphere();
        static MyShapeRamp m_rampShape = new MyShapeRamp();
        static readonly List<MyVoxelBase> m_voxelsTmpStorage = new List<MyVoxelBase>(); 

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
            m_voxelsTmpStorage.Clear();
            GetAllOverlappingWithSphere(ref sphere, m_voxelsTmpStorage);
            if(m_voxelsTmpStorage.Count == 0) return null;

            return m_voxelsTmpStorage[0];
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

        void IMyVoxelMaps.MakeCrater(IMyVoxelBase voxelMap, BoundingSphereD sphere, Vector3 normal, byte materialIdx)
        {
            var material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIdx);
            MyVoxelGenerator.MakeCrater((MyVoxelBase)voxelMap, sphere, normal, material);
        }

        List<MyVoxelBase> m_voxelCache = new List<MyVoxelBase>();
        void IMyVoxelMaps.GetAllOverlappingWithSphere(ref BoundingSphereD sphere, List<IMyVoxelBase> voxels)
        {
            Debug.Assert(m_voxelCache.Count == 0, "Voxel cache list not cleared after last use");
            GetAllOverlappingWithSphere(ref sphere, m_voxelCache);

            foreach (var item in m_voxelCache)
                voxels.Add(item);

            m_voxelCache.Clear();
        }

        // Allocates
        List<IMyVoxelBase> IMyVoxelMaps.GetAllOverlappingWithSphere(ref BoundingSphereD sphere)
        {
            Debug.Assert(m_voxelCache.Count == 0, "Voxel cache list not cleared after last use");
            GetAllOverlappingWithSphere(ref sphere, m_voxelCache);

            var list = m_voxelCache.ConvertAll<IMyVoxelBase>((x) => (IMyVoxelBase)x);
            m_voxelCache.Clear();
            return list;
        }
    }
}
