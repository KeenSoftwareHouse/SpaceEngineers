using Havok;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities.Character.Components
{
    // TODO : This class should be replaced by MyEntityInventorySpawnComponent so it can be used on any entity
    public class MyInventorySpawnComponent : MyCharacterComponent
    {
        private MyInventory m_spawnInventory = null;
        private const string INVENTORY_USE_DUMMY_NAME = "inventory";

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
                                MyContainerDefinition containerDefinition;
                                if (MyDefinitionManager.Static.TryGetContainerDefinition(Character.Definition.InventorySpawnContainerId.Value, out containerDefinition))
                                {
                                    inventoryAggregate.RemoveComponent(myInventory);
                                    if (Sync.IsServer)
                                    {
                                        var bagEntityId = SpawnInventoryContainer(Character.Definition.InventorySpawnContainerId.Value, myInventory);
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Fail("The provided definiton of the container was not found!");
                                }
                            }
                            else
                            {
                                inventoryAggregate.RemoveComponent(inventoryComponent);
                            }

                        }
                    }
                    else if (inventory is MyInventory && Character.Definition.SpawnInventoryOnBodyRemoval)
                    {
                        m_spawnInventory = inventory as MyInventory;
                        Character.OnClosing += Character_OnClosing;
                    }                    
                    else
                    {
                        System.Diagnostics.Debug.Fail("Reached unpredicted branch on spawning inventory, can't spawn inventory if it is not in aggregate, or if it is not going to be spawned on body removal");
                    }
                }                
                CloseComponent();
            }            
        }

        private void Character_OnClosing(MyEntity obj)
        {
            System.Diagnostics.Debug.Assert(obj is MyCharacter, "Entity is not character!");
            System.Diagnostics.Debug.Assert(obj == Character, "Called from another entity!");
            System.Diagnostics.Debug.Assert(Character.IsDead, "Called but character is not dead!");
            System.Diagnostics.Debug.Assert(m_spawnInventory != null, "Inventory was not set!");

            Character.OnClosing -= Character_OnClosing;

            if (m_spawnInventory != null && m_spawnInventory.GetItemsCount() > 0)
            {
                MyContainerDefinition containerDefinition;
                if (!MyComponentContainerExtension.TryGetContainerDefinition(Character.Definition.InventorySpawnContainerId.Value.TypeId, Character.Definition.InventorySpawnContainerId.Value.SubtypeId, out containerDefinition))
                {
                    // Backward compatibility - old definition id is with MyObjectBuilder_EntityBase
                    MyDefinitionId compatIventorySpawnContainerId = new MyDefinitionId(typeof(MyObjectBuilder_InventoryBagEntity), Character.Definition.InventorySpawnContainerId.Value.SubtypeId);
                    MyComponentContainerExtension.TryGetContainerDefinition(compatIventorySpawnContainerId.TypeId, compatIventorySpawnContainerId.SubtypeId, out containerDefinition);
                }

                if (containerDefinition != null)
                {
                    Character.Components.Remove<MyInventoryBase>();
                    if (Sync.IsServer)
                    {
                        var bagEntityId = SpawnInventoryContainer(Character.Definition.InventorySpawnContainerId.Value, m_spawnInventory, false);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Fail("The provided definiton of the container was not found!");
                }
            }
        }

        private long SpawnInventoryContainer(MyDefinitionId bagDefinition, MyInventory inventory, bool spawnAboveCharacter = true)
        {
            //TODO: this should not be here but we have to know if session is being closed if so then no new entity will be created. 
            // Entity closing method and event should have parameter with sessionIsClosing. 
            if (Sandbox.Game.World.MySession.Static == null || !Sandbox.Game.World.MySession.Static.Ready)
                return 0;

            MyEntity builder = Character;
            var worldMatrix = Character.WorldMatrix;
            if (spawnAboveCharacter)
            {
                worldMatrix.Translation += worldMatrix.Up + worldMatrix.Forward;
            }
            else
            {
                Vector3 modelCenter = Character.Render.GetModel().BoundingBox.Center;
                Vector3 translationToCenter = Vector3.Transform(modelCenter, worldMatrix);
                worldMatrix.Translation = translationToCenter;
            }

            MyContainerDefinition containerDefinition;
            if (!MyComponentContainerExtension.TryGetContainerDefinition(bagDefinition.TypeId, bagDefinition.SubtypeId, out containerDefinition))
            {
                System.Diagnostics.Debug.Fail("Entity container definition: " + bagDefinition.ToString() + " was not found!");
                return 0;
            }

            MyEntity entity = MyEntities.CreateFromComponentContainerDefinitionAndAdd(containerDefinition.Id);
            System.Diagnostics.Debug.Assert(entity != null);
            if (entity == null)
                return 0;

            entity.PositionComp.SetWorldMatrix(worldMatrix);

            entity.Physics.LinearVelocity = builder.Physics.LinearVelocity;
            entity.Physics.AngularVelocity = builder.Physics.AngularVelocity;

            //GR: Change color of spawned backpack to much character color
            entity.Render.EnableColorMaskHsv = true;
            entity.Render.ColorMaskHsv = Character.Render.ColorMaskHsv;

            inventory.RemoveEntityOnEmpty = true;
            entity.Components.Add<MyInventoryBase>(inventory);

            return entity.EntityId;
        }
                    
    
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
        }

        private void CloseComponent()
        {
            //TODO: remove the component somehow from the container  
            // This would need to allow different iteration through dictionary of components, so the collection can get changed while iterating through it
        }

        public override bool IsSerialized()
        {
            return false;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            System.Diagnostics.Debug.Assert(Character != null, "Character can't be null when removing this component..");

            if (Character != null)
            {
                Character.OnClosing -= Character_OnClosing;
            }
        }
    }
}
