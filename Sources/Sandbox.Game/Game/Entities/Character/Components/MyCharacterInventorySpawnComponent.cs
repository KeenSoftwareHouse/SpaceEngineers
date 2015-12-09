using Havok;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Components;
using VRage.Game.ObjectBuilders;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities.Character.Components
{
    public class MyInventorySpawnComponent : MyCharacterComponent
    {
        public override string ComponentTypeDebugString
        {
            get
            {
                return "Inventory Spawn Component";
            }
        }

        public override void OnCharacterDead()
        {
            System.Diagnostics.Debug.Assert(!Character.Definition.EnableSpawnInventoryAsContainer ||  (Character.Definition.EnableSpawnInventoryAsContainer && Character.Definition.InventorySpawnContainerId.HasValue), "Container id is not defined, but is enabled to spawn the inventory into container");
            if (!Sync.IsServer)
            {
                //System.Diagnostics.Debug.Fail("Should not get here, don't allow this compomonent to be loaded on clients!");
                return;
            }
            if (Character.IsDead && Character.Definition.EnableSpawnInventoryAsContainer && Character.Definition.InventorySpawnContainerId.HasValue)
            {
                if (Character.Components.Has<MyInventoryBase>())
                {
                    var inventory = Character.Components.Get<MyInventoryBase>();
                    if (inventory is MyInventoryAggregate)
                    {
                        var inventoryAggregate = inventory as MyInventoryAggregate;
                        var components = new List<MyComponentBase>();
                        inventoryAggregate.GetComponentsFlattened(components);
                        foreach (var inventoryComponent in components)
                        {
                            //TODO: This spawn all MyInventory components, which are currently used with Characters
                            var myInventory = inventoryComponent as MyInventory;
                            if (myInventory != null && myInventory.GetItemsCount() > 0)
                            {                                
                                MyPhysicalItemDefinition bagDefinition;
                                if (MyDefinitionManager.Static.TryGetDefinition(Character.Definition.InventorySpawnContainerId.Value, out bagDefinition))
                                {
                                    var bagEntityId = SpawnInventoryContainer(Character.Definition.InventorySpawnContainerId.Value);
                                    if (bagEntityId != null)
                                    {
                                        myInventory.RemoveEntityOnEmpty = true;
                                        MySyncInventory.SendTransferInventoryMsg(Character.EntityId, bagEntityId, myInventory, true);
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Fail("The provided definiton of the container was not found!");
                                }
                            }
                        }
                    }
                    else
                    {
                        //if (inventory != null)
                        //{
                        //    MyPhysicalItemDefinition bagDefinition;
                        //    if (MyDefinitionManager.Static.TryGetDefinition(Character.Definition.InventorySpawnContainerId.Value, out bagDefinition))
                        //    {
                        //        var container = SpawnInventoryContainer(bagDefinition);
                        //        System.Diagnostics.Debug.Assert(container != null, "The block in null! Not spawned?");
                        //        if (container != null)
                        //        {
                        //            MySyncInventory.SendTransferInventoryMsg(Character, container, myInventory);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        System.Diagnostics.Debug.Fail("The provided definiton of the container was not found!");
                        //    }
                        //}

                    }
                    
                }                
                CloseComponent();
            }            
        }

        private long SpawnInventoryContainer(MyDefinitionId bagDefinition)
        {
            MyEntity builder = Character;
            var worldMatrix = Character.WorldMatrix;
            worldMatrix.Translation += worldMatrix.Up + worldMatrix.Forward;

            MyObjectBuilder_EntityBase bagBuilder = new MyObjectBuilder_EntityBase();
            
            var position =  new MyPositionAndOrientation(worldMatrix);
            bagBuilder.PositionAndOrientation = position;
            bagBuilder.EntityId = MyEntityIdentifier.AllocateId();
            var entity = MyEntities.CreateAndAddFromDefinition(bagBuilder, bagDefinition);

            entity.Physics.ClearSpeed();
            entity.Physics.ForceActivate();
            entity.NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;           
           
            MyTimerComponent timerComponent = new MyTimerComponent();
            timerComponent.SetRemoveEntityTimer(1440);
            entity.GameLogic = timerComponent;
           
            
            MySyncCreate.SendEntityCreated(entity.GetObjectBuilder(), bagDefinition);
            
            return entity.EntityId;
        }
                    
    
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            if (!Sync.IsServer)
            {
                CloseComponent();
            }
        }

        private void CloseComponent()
        {
            //TODO: remove the component somehow from the container
        }

        public override bool IsSerialized()
        {
            return false;
        }
    }
}
