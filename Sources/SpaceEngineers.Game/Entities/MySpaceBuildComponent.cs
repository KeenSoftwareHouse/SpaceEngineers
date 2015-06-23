using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace SpaceEngineers.Game.Entities
{
    [PreloadRequired]
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class MySpaceBuildComponent : MyBuildComponentBase
    {
        public override void LoadData()
        {
            base.LoadData();
            MyCubeBuilder.BuildComponent = this;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyCubeBuilder.BuildComponent = null;
        }

        public override IMyComponentInventory GetBuilderInventory(long entityId)
        {
            if (MySession.Static.CreativeMode || MySession.Static.SimpleSurvival)
                return null;

            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);

            Debug.Assert(entity != null, "Entity that wasnt on server is trying to build.");
            if (entity == null) return null;

            return GetBuilderInventory(entity);
        }

        public override IMyComponentInventory GetBuilderInventory(MyEntity entity)
        {
            if (MySession.Static.CreativeMode || MySession.Static.SimpleSurvival)
                return null;

            var character = entity as MyCharacter;
            if (character != null)
            {
                return character.GetInventory(0);
            }
            var shipWelder = entity as MyShipWelder;
            if (shipWelder != null)
            {
                return shipWelder.GetInventory(0);
            }

            Debug.Fail("Only characters and ship welders can build blocks!");
            return null;
        }

        public override bool HasBuildingMaterials(MyEntity builder)
        {
            if (MySession.Static.CreativeMode || MySession.Static.SimpleSurvival)
                return true;

            if (builder == null) return false;
            var inventory = GetBuilderInventory(builder);
            if (inventory == null) return false;

            bool result = true;
            foreach (var entry in m_materialList.RequiredMaterials)
            {
                result &= inventory.GetItemAmount(entry.Key) >= entry.Value;
                if (!result) break;
            }
            return result;
        }

        public override void GetGridSpawnMaterials(MyCubeBlockDefinition definition, MatrixD worldMatrix, bool isStatic)
        {
            ClearRequiredMaterials();
            GetMaterialsSimple(definition, m_materialList);
        }

        public override void GetBlockPlacementMaterials(MyCubeBlockDefinition definition, Vector3I position, MyBlockOrientation orientation, MyCubeGrid grid)
        {
            ClearRequiredMaterials();
            GetMaterialsSimple(definition, m_materialList);
        }

        public override void GetBlocksPlacementMaterials(HashSet<MyCubeGrid.MyBlockLocation> hashSet, MyCubeGrid grid)
        {
            ClearRequiredMaterials();
            foreach (var location in hashSet)
            {
                MyCubeBlockDefinition definition = null;
                if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(location.BlockDefinition, out definition))
                {
                    continue;
                }
                GetMaterialsSimple(definition, m_materialList);
            }
        }

        public override void GetGridSpawnMaterials(MyObjectBuilder_CubeGrid grid)
        {
            ClearRequiredMaterials();

            foreach (var block in grid.CubeBlocks)
            {
                MyComponentStack.GetMountedComponents(m_materialList, block);
                if (block.ConstructionStockpile != null)
                {
                    foreach (var item in block.ConstructionStockpile.Items)
                    {
                        var itemId = item.PhysicalContent.GetId();
                        m_materialList.AddMaterial(itemId, item.Amount, addToDisplayList: false);
                    }
                }
            }
        }

        public override void BeforeCreateBlock(MyCubeBlockDefinition definition, MyEntity builder, MyObjectBuilder_CubeBlock ob)
        {
            Debug.Assert(MySession.Static.SimpleSurvival == false, "In SE, there should not be simple survival!");

            if (builder != null && MySession.Static.SurvivalMode)
            {
                ob.IntegrityPercent = MyComponentStack.MOUNT_THRESHOLD;
                ob.BuildPercent = MyComponentStack.MOUNT_THRESHOLD;
            }
        }

        public override void AfterGridCreated(MyCubeGrid grid, MyEntity builder)
        {
            if (MySession.Static.SurvivalMode && !MySession.Static.SimpleSurvival)
            {
                TakeMaterialsFromBuilder(builder);
            }
        }

        public override void AfterGridsSpawn(Dictionary<MyDefinitionId, int> buildItems, MyEntity builder)
        {
        }

        public override void AfterBlockBuild(MySlimBlock block, MyEntity builder)
        {
            if (builder == null) return;

            if (MySession.Static.SurvivalMode && !MySession.Static.SimpleSurvival)
            {
                TakeMaterialsFromBuilder(builder);
            }
        }

        public override void AfterBlocksBuild(HashSet<MyCubeGrid.MyBlockLocation> builtBlocks, MyEntity builder)
        {
        }

        private void ClearRequiredMaterials()
        {
            m_materialList.Clear();
        }

        private static void GetMaterialsSimple(MyCubeBlockDefinition definition, MyComponentList output)
        {
            for (int i = 0; i < definition.Components.Length; ++i)
            {
                var component = definition.Components[i];
                output.AddMaterial(component.Definition.Id, component.Count, i == 0 ? 1 : 0);
            }
            return;
        }

        private void TakeMaterialsFromBuilder(MyEntity builder)
        {
            if (builder == null) return;
            var inventory = GetBuilderInventory(builder);
            if (inventory == null) return;

            foreach (var entry in m_materialList.RequiredMaterials)
            {
                inventory.RemoveItemsOfType(entry.Value, entry.Key);
            }
        }
    }
}
