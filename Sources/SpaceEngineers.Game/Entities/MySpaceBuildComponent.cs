using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;
using Sandbox.Game.Multiplayer;

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

        public override MyInventoryBase GetBuilderInventory(long entityId)
        {
            if (MySession.Static.CreativeMode)
                return null;

            MyEntity entity;
            MyEntities.TryGetEntityById(entityId, out entity);

            Debug.Assert(entity != null, "Entity that wasnt on server is trying to build.");
            if (entity == null) return null;

            return GetBuilderInventory(entity);
        }

        public override MyInventoryBase GetBuilderInventory(MyEntity entity)
        {
            if (MySession.Static.CreativeMode)
                return null;

            var character = entity as MyCharacter;
            if (character != null)
            {
                return character.GetInventory(0) as MyInventoryBase;
            }
            var shipWelder = entity as MyShipWelder;
            if (shipWelder != null)
            {
                return shipWelder.GetInventory(0) as MyInventoryBase;
            }

            Debug.Fail("Only characters and ship welders can build blocks!");
            return null;
        }

        public override bool HasBuildingMaterials(MyEntity builder, bool testTotal)
        {
            if (MySession.Static.CreativeMode || (MySession.Static.CreativeToolsEnabled(Sync.MyId) && builder == MySession.Static.LocalCharacter))
                return true;

            if (builder == null) return false;
            var inventory = GetBuilderInventory(builder);
            if (inventory == null) return false;
            MyInventory shipInventory = null;
            MyCockpit cockpit = null;
            long identityId = MySession.Static.LocalPlayerId;
            if (builder is MyCharacter)
            {//construction cockpit?
                cockpit = (builder as MyCharacter).IsUsing as MyCockpit;
                if (cockpit != null)
                {
                    shipInventory = cockpit.GetInventory();
                    identityId = cockpit.ControllerInfo.ControllingIdentityId;
                }
                else
                    if ((builder as MyCharacter).ControllerInfo != null)
                        identityId = (builder as MyCharacter).ControllerInfo.ControllingIdentityId;
                    else
                        Debug.Fail("failed to get identityId");
            }
            bool result = true;
            if (!testTotal)
            {
                foreach (var entry in m_materialList.RequiredMaterials)
                {
                    result &= inventory.GetItemAmount(entry.Key) >= entry.Value;
                    if (!result && shipInventory != null)
                    {
                        result = shipInventory.GetItemAmount(entry.Key) >= entry.Value;
                        if (!result)
                        {
                            //MyGridConveyorSystem.ItemPullRequest((MySession.Static.ControlledEntity as MyCockpit), shipInventory, MySession.Static.LocalPlayerId, entry.Key, entry.Value);
                            result = MyGridConveyorSystem.ConveyorSystemItemAmount(cockpit, shipInventory, identityId, entry.Key) >= entry.Value;
                        }
                    }
                    if (!result) break;
                }
            }
            else
            {
                foreach (var entry in m_materialList.TotalMaterials)
                {
                    result &= inventory.GetItemAmount(entry.Key) >= entry.Value;
                    if (!result && shipInventory != null)
                    {
                        result = shipInventory.GetItemAmount(entry.Key) >= entry.Value;
                        if (!result)
                        {
                            //MyGridConveyorSystem.ItemPullRequest((MySession.Static.ControlledEntity as MyCockpit), shipInventory, MySession.Static.LocalPlayerId, entry.Key, entry.Value);
                            result = MyGridConveyorSystem.ConveyorSystemItemAmount(cockpit, shipInventory, identityId, entry.Key) >= entry.Value;
                        }
                    }
                    if (!result) break;
                }
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

        public override void GetBlockAmountPlacementMaterials(MyCubeBlockDefinition definition, int amount)
        {
            ClearRequiredMaterials();
            GetMaterialsSimple(definition, m_materialList, amount);
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
                        if (item.PhysicalContent != null)
                        {
                            var itemId = item.PhysicalContent.GetId();
                            m_materialList.AddMaterial(itemId, item.Amount, item.Amount, addToDisplayList: false);
                        }
                    }
                }
            }
        }

        public override void GetMultiBlockPlacementMaterials(MyMultiBlockDefinition multiBlockDefinition)
        {
        }

        public override void BeforeCreateBlock(MyCubeBlockDefinition definition, MyEntity builder, MyObjectBuilder_CubeBlock ob, bool buildAsAdmin)
        {
            base.BeforeCreateBlock(definition, builder, ob, buildAsAdmin);

            if (builder != null && MySession.Static.SurvivalMode && !buildAsAdmin)
            {
                ob.IntegrityPercent = MyComponentStack.MOUNT_THRESHOLD;
                ob.BuildPercent = MyComponentStack.MOUNT_THRESHOLD;
            }
        }

        public override void AfterSuccessfulBuild(MyEntity builder, bool instantBuild)
        {
            if (builder == null || instantBuild) return;

            if (MySession.Static.SurvivalMode)
            {
                TakeMaterialsFromBuilder(builder);
            }
        }

        private void ClearRequiredMaterials()
        {
            m_materialList.Clear();
        }

        private static void GetMaterialsSimple(MyCubeBlockDefinition definition, MyComponentList output, int amount = 1)
        {
            for (int i = 0; i < definition.Components.Length; ++i)
            {
                var component = definition.Components[i];
                output.AddMaterial(component.Definition.Id, component.Count * amount, i == 0 ? 1 : 0);
            }
        }

        private void TakeMaterialsFromBuilder(MyEntity builder)
        {
            // CH: TODO: Please refactor this to not be so ugly. Especially, calling the Solve function multiple times on the component combiner is bad...
            if (builder == null) return;
            var inventory = GetBuilderInventory(builder);
            if (inventory == null) return;
            MyInventory shipInventory = null;
            MyCockpit cockpit = null;
            long identityId=long.MaxValue;
            if (builder is MyCharacter)
            {//construction cockpit?
                cockpit = (builder as MyCharacter).IsUsing as MyCockpit;
                if (cockpit != null)
                {
                    shipInventory = cockpit.GetInventory();
                    identityId = cockpit.ControllerInfo.ControllingIdentityId;
                }
                else
                    if ((builder as MyCharacter).ControllerInfo != null)
                        identityId = (builder as MyCharacter).ControllerInfo.ControllingIdentityId;
                    else
                        Debug.Fail("failed to get identityId");
            }

            VRage.MyFixedPoint hasAmount,hasAmountCockpit;

            foreach (var entry in m_materialList.RequiredMaterials)
            {
                VRage.MyFixedPoint toRemove = entry.Value;
                hasAmount = inventory.GetItemAmount(entry.Key);
                if (hasAmount > entry.Value)
                {
                    inventory.RemoveItemsOfType(toRemove, entry.Key);
                    continue;
                }
                if (hasAmount>0)
                {
                    inventory.RemoveItemsOfType(hasAmount, entry.Key);
                    toRemove -= hasAmount;
                }
                if (shipInventory != null)
                {
                    hasAmountCockpit = shipInventory.GetItemAmount(entry.Key);
                    if (hasAmountCockpit >= toRemove)
                    {
                        shipInventory.RemoveItemsOfType(toRemove, entry.Key);
                        continue;
                    }
                    if (hasAmountCockpit > 0)
                    {
                        shipInventory.RemoveItemsOfType(hasAmountCockpit, entry.Key);
                        toRemove -= hasAmountCockpit;
                    }
                    var transferred = MyGridConveyorSystem.ItemPullRequest(cockpit, shipInventory, identityId, entry.Key, toRemove, true);
                    Debug.Assert(transferred == toRemove, "Cannot pull enough materials to build, "+transferred+"!="+toRemove);
                }
                else
                    Debug.Assert(toRemove==0, "Needs more materials and ship inventory is null");
            }
        }


    }
}