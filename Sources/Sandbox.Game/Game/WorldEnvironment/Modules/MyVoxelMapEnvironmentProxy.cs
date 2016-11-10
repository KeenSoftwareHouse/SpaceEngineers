using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.WorldEnvironment.Modules
{
    public class MyVoxelMapEnvironmentProxy : IMyEnvironmentModuleProxy
    {
        protected struct VoxelMapInfo
        {
            public string Name;
            public MyDefinitionId Storage;
            public Matrix Matrix;
            public int Item;
            public MyStringHash Modifier;
            public long EntityId;
        }

        public MyVoxelMapEnvironmentProxy()
        {
            m_voxelMap_RangeChangedDelegate = VoxelMap_RangeChanged;
        }

        public void Init(MyEnvironmentSector sector, List<int> items)
        {
            m_sector = sector;
            MyEntityComponentBase comp = (MyEntityComponentBase)m_sector.Owner;
            m_planet = comp.Entity as MyPlanet;
            m_items = items;

            LoadVoxelMapsInfo();
        }

        public void Close()
        {
            RemoveVoxelMaps();
        }

        public void CommitLodChange(int lodBefore, int lodAfter)
        {
            if (lodAfter >= 0)
                AddVoxelMaps();
            else if (!m_sector.HasPhysics)
                RemoveVoxelMaps();
        }

        public void CommitPhysicsChange(bool enabled)
        {
            if (enabled)
                AddVoxelMaps();
            else if (m_sector.LodLevel == -1)
                RemoveVoxelMaps();
        }

        public void OnItemChange(int index, short newModel)
        {

        }

        public void OnItemChangeBatch(List<int> items, int offset, short newModel)
        {

        }

        public void HandleSyncEvent(int item, object data, bool fromClient)
        {
        }

        public void DebugDraw()
        {}

        #region Private

        protected MyEnvironmentSector m_sector;
        protected MyPlanet m_planet;

        protected readonly MyRandom m_random = new MyRandom();
        protected readonly MyVoxelBase.StorageChanged m_voxelMap_RangeChangedDelegate;

        protected List<int> m_items;
        protected List<VoxelMapInfo> m_voxelMapsToAdd = new List<VoxelMapInfo>();
        protected Dictionary<MyVoxelMap, int> m_voxelMaps = new Dictionary<MyVoxelMap, int>();

        #endregion

        private void LoadVoxelMapsInfo()
        {
            m_voxelMapsToAdd.Clear();

            foreach (var item in m_items)
            {
                var envItem = m_sector.DataView.Items[item];
                MyRuntimeEnvironmentItemInfo it;
                m_sector.Owner.GetDefinition((ushort)envItem.DefinitionIndex, out it);
                var definitionId = new MyDefinitionId(typeof(MyObjectBuilder_VoxelMapCollectionDefinition), it.Subtype);
                var definition = MyDefinitionManager.Static.GetDefinition<MyVoxelMapCollectionDefinition>(definitionId);
                Debug.Assert(definition != null, "Definition not found!");

                if (definition == null) continue;

                // Get the logical counterparts to calculate EntityId.
                int logicalId;
                MyLogicalEnvironmentSectorBase logicalSector;
                m_sector.DataView.GetLogicalSector(item, out logicalId, out logicalSector);

                string name = String.Format("P({0})S({1})A({2}__{3})", m_sector.Owner.Entity.Name, logicalSector.Id, it.Subtype, logicalId);
                Matrix matrix = MatrixD.CreateFromQuaternion(envItem.Rotation);
                matrix.Translation = m_sector.SectorCenter + envItem.Position;
                long id = MyEntityIdentifier.ConstructIdFromString(MyEntityIdentifier.ID_OBJECT_TYPE.PLANET_VOXEL_DETAIL, name);

                using (m_random.PushSeed(envItem.Rotation.GetHashCode()))
                {
                    m_voxelMapsToAdd.Add(new VoxelMapInfo
                    {
                        Name = name,
                        Storage = definition.StorageFiles.Sample(m_random),
                        Matrix = matrix,
                        Item = item,
                        Modifier = definition.Modifier,
                        EntityId = id
                    });
                }
            }
        }

        private void AddVoxelMaps()
        {
            if (m_voxelMaps.Count > 0) return;

            foreach (var map in m_voxelMapsToAdd)
            {
                MyVoxelMap existingStone;

                // Handle when the stone was already modified and is sent by the server.
                if (MyEntities.TryGetEntityById(map.EntityId, out existingStone))
                {
                    if (!existingStone.Save)
                    {
                        // It's just still on the different lod sector.
                        RegisterVoxelMap(map.Item, existingStone);
                    }
                    else
                    {
                        // it was saved and we don't know about it, baad
                        if (existingStone != null && existingStone.StorageName == map.Name)
                            Debug.Assert(false, "Storage was already sent but sector did not know about the modified item.");
                        else
                            Debug.Assert(false, "Voxel stone entity Id collisioncollision.");
                    }
                    continue;
                }

                var mod = MyDefinitionManager.Static.GetDefinition<MyVoxelMaterialModifierDefinition>(map.Modifier);
                Dictionary<byte, byte> ops = null;

                //if there is modifier pick one based on chance
                if (mod != null)
                    ops = mod.Options.Sample(MyHashRandomUtils.UniformFloatFromSeed(map.Item + map.Matrix.GetHashCode())).Changes;

                AddVoxelMap(map.Item, map.Storage.SubtypeName, map.Matrix, map.Name, map.EntityId, ops);
            }
        }

        private void AddVoxelMap(int item, string prefabName, MatrixD matrix, string name, long entityId, Dictionary<byte, byte> modifiers = null)
        {
            var fileName = MyWorldGenerator.GetVoxelPrefabPath(prefabName);
            var storage = MyStorageBase.LoadFromFile(fileName, modifiers);

            if (storage == null) return;

            var voxelMap = MyWorldGenerator.AddVoxelMap(name, storage, matrix, entityId, true);
            RegisterVoxelMap(item, voxelMap);
        }

        private void RegisterVoxelMap(int item, MyVoxelMap voxelMap)
        {
            voxelMap.Save = false;
            voxelMap.RangeChanged += m_voxelMap_RangeChangedDelegate;
            m_voxelMaps[voxelMap] = item;

            MyEntityReferenceComponent component;
            if (!voxelMap.Components.TryGet(out component))
            {
                voxelMap.Components.Add(component = new MyEntityReferenceComponent());
            }

            DisableOtherItemsInVMap(voxelMap);

            component.Ref();
        }

        private static List<MyEntity> m_entities = new List<MyEntity>();

        private unsafe void DisableOtherItemsInVMap(MyVoxelBase voxelMap)
        {
            MyOrientedBoundingBoxD obb = MyOrientedBoundingBoxD.Create((BoundingBoxD)voxelMap.PositionComp.LocalAABB, voxelMap.PositionComp.WorldMatrix);
            var center = obb.Center;

            var box = voxelMap.PositionComp.WorldAABB;

            m_entities.Clear();
            MyGamePruningStructure.GetAllEntitiesInBox(ref box, m_entities, MyEntityQueryType.Static);

            for (int eIndex = 0; eIndex < m_entities.Count; ++eIndex)
            {
                var sector = m_entities[eIndex] as MyEnvironmentSector;

                if (sector == null || sector.DataView == null)
                    continue;

                obb.Center = center - sector.SectorCenter;

                for (int sectorInd = 0; sectorInd < sector.DataView.LogicalSectors.Count; sectorInd++)
                {
                    var logicalSector = sector.DataView.LogicalSectors[sectorInd];
                    var logicalItems = logicalSector.Items;
                    var cnt = logicalItems.Count;

                    fixed (ItemInfo* items = logicalItems.GetInternalArray())
                        for (int i = 0; i < cnt; ++i)
                        {
                            var point = items[i].Position + sector.SectorCenter;
                            if (items[i].DefinitionIndex >= 0 && obb.Contains(ref items[i].Position)
                                && voxelMap.CountPointsInside(&point, 1) > 0 && !IsVoxelItem(sector, items[i].DefinitionIndex))
                                logicalSector.EnableItem(i, false);
                        }
                }
            }
        }

        private static bool IsVoxelItem(MyEnvironmentSector sector, short definitionIndex)
        {
            var modules = sector.Owner.EnvironmentDefinition.Items[definitionIndex].Type.ProxyModules;

            if (modules == null)
                return false;

            for (int i = 0; i < modules.Length; ++i)
            {
                if (modules[i].Type.IsSubclassOf(typeof(MyVoxelMapEnvironmentProxy)) || modules[i].Type == typeof(MyVoxelMapEnvironmentProxy))
                    return true;
            }

            return false;
        }

        private void RemoveVoxelMaps()
        {
            foreach (var map in m_voxelMaps)
            {
                var vox = map.Key;
                if (!vox.Closed)
                {
                    vox.Components.Get<MyEntityReferenceComponent>().Unref();
                    vox.RangeChanged -= m_voxelMap_RangeChangedDelegate;
                }
            }

            m_voxelMaps.Clear();
            m_voxelMapsToAdd.Clear();
        }

        private void RemoveVoxelMap(MyVoxelMap map)
        {
            map.Save = true;
            map.RangeChanged -= m_voxelMap_RangeChangedDelegate;

            if (m_voxelMaps.ContainsKey(map))
            {
                var item = m_voxelMaps[map];
                m_sector.EnableItem(item, false);
                m_voxelMaps.Remove(map);
            }
            else
            {
                Debug.Fail("Voxel map was no longer tracked");
            }
        }

        private void VoxelMap_RangeChanged(MyVoxelBase voxel, Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
        {
            RemoveVoxelMap((MyVoxelMap)voxel);
        }
    }
}
