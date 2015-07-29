using Havok;
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
            if (!MyFakes.ENABLE_INVENTORY_SPAWN)
            {
                return;
            }

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
                            if (myInventory != null)
                            {                                
                                MyPhysicalItemDefinition bagDefinition;
                                if (MyDefinitionManager.Static.TryGetDefinition(Character.Definition.InventorySpawnContainerId.Value, out bagDefinition))
                                {
                                    var bagEntityId = SpawnInventoryContainer(bagDefinition);
                                    if (bagEntityId != null)
                                    {
                                        myInventory.RemoveEntityOnEmpty = true;
                                        MySyncInventory.SendTransferInventoryMsg(Character.EntityId, bagEntityId, myInventory);
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

        private long SpawnInventoryContainer(MyPhysicalItemDefinition bagDefinition)
        {
            MyEntity builder = Character;
            var worldMatrix = Character.WorldMatrix;
            worldMatrix.Translation += worldMatrix.Up + worldMatrix.Forward;

            MyObjectBuilder_EntityBase bagBuilder = new MyObjectBuilder_EntityBase();
            bagBuilder.Name = bagDefinition.DisplayNameText;
            var position =  new MyPositionAndOrientation(worldMatrix);
            bagBuilder.PositionAndOrientation = position;
            bagBuilder.EntityId = MyEntityIdentifier.AllocateId();
            bagBuilder.SubtypeName = bagDefinition.Id.SubtypeName;

            var entity = MyEntities.CreateAndAddFromDefinition(bagBuilder, bagDefinition);

            entity.Physics.ForceActivate();
            entity.Physics.ApplyImpulse(builder.Physics.LinearVelocity, Vector3.Zero);
            
            MySyncCreate.SendEntityCreated(entity.GetObjectBuilder(), bagDefinition.Id);
            
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
