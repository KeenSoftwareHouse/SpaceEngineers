using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    /// TODO: This component should replace the MyInventorySpawnComponent which is limited to be used by CharacterComponents container
    [MyComponentType(typeof(MyEntityInventorySpawnComponent))]
    [MyComponentBuilder(typeof(MyObjectBuilder_InventorySpawnComponent))]
    public class MyEntityInventorySpawnComponent : MyEntityComponentBase
    {
        private MyDefinitionId m_containerDefinition;

        public override string ComponentTypeDebugString
        {
            get
            {
                return "Inventory Spawn Component";
            }
        }
                
        public bool SpawnInventoryContainer(bool spawnAboveEntity = true)
        {
            //TODO: this should not be here but we have to know if session is being closed if so then no new entity will be created. 
            // Entity closing method and event should have parameter with sessionIsClosing. 
            if (Sandbox.Game.World.MySession.Static == null || !Sandbox.Game.World.MySession.Static.Ready)
                return false;

            var ownerEntity = Entity as MyEntity;
            for (int i = 0; i < ownerEntity.InventoryCount; ++i)
            {
                var inventory = ownerEntity.GetInventory(i);
                if (inventory != null && inventory.GetItemsCount() > 0)
                {
                    MyEntity inventoryOwner = Entity as MyEntity;
                    var worldMatrix = inventoryOwner.WorldMatrix;
                    if (spawnAboveEntity)
                    {
                        Vector3 upDir = -Sandbox.Game.GameSystems.MyGravityProviderSystem.CalculateNaturalGravityInPoint(inventoryOwner.PositionComp.GetPosition());
                        if (upDir == Vector3.Zero)
                            upDir = Vector3.Up;
                        upDir.Normalize();

                        Vector3 forwardDir = Vector3.CalculatePerpendicularVector(upDir);

                        var ownerPosition = worldMatrix.Translation;
                        var ownerAabb = inventoryOwner.PositionComp.WorldAABB;
                        for (int moveIter = 0; moveIter < 20; ++moveIter)
                        {
                            var newPosition = ownerPosition + 0.1f * moveIter * upDir + 0.1f * moveIter * forwardDir;
                            var aabb = new BoundingBoxD(newPosition - 0.25 * Vector3D.One, newPosition + 0.25 * Vector3D.One);
                            if (!aabb.Intersects(ref ownerAabb))
                            {
                                // Move newPosition a little to avoid collision with fractured pieces.
                                worldMatrix.Translation = newPosition + 0.25f * upDir;
                                break;
                            }
                        }

                        if (worldMatrix.Translation == ownerPosition)
                            worldMatrix.Translation += upDir + forwardDir;
                    }
                    else
                    {
                        var model = (inventoryOwner.Render.ModelStorage as MyModel);
                        if (model != null)
                        {
                            Vector3 modelCenter = model.BoundingBox.Center;
                            Vector3 translationToCenter = Vector3.Transform(modelCenter, worldMatrix);
                            worldMatrix.Translation = translationToCenter;
                        }
                    }

                    MyContainerDefinition entityDefinition;
                    if (!MyComponentContainerExtension.TryGetContainerDefinition(m_containerDefinition.TypeId, m_containerDefinition.SubtypeId, out entityDefinition))
                    {
                        System.Diagnostics.Debug.Fail("Container Definition: " + m_containerDefinition.ToString() + " was not found!");
                        return false;
                    }

                    MyEntity entity = MyEntities.CreateFromComponentContainerDefinitionAndAdd(entityDefinition.Id);
                    System.Diagnostics.Debug.Assert(entity != null);
                    if (entity == null)
                        return false;

                    entity.PositionComp.SetWorldMatrix(worldMatrix);

                    System.Diagnostics.Debug.Assert(inventoryOwner != null, "Owner is not set!");

                    if (inventoryOwner.InventoryCount == 1)
                    {
                        inventoryOwner.Components.Remove<MyInventoryBase>();
                    }
                    else
                    {
                        var aggregate = inventoryOwner.GetInventoryBase() as MyInventoryAggregate;
                        if (aggregate != null)
                        {
                            aggregate.RemoveComponent(inventory);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Inventory owners indicates that it owns more inventories, but doesn't have aggregate?");
                            return false;
                        }
                    }

                    // Replaces bag default inventory with existing one.
                    entity.Components.Add<MyInventoryBase>(inventory);
                    inventory.RemoveEntityOnEmpty = true;

                    entity.Physics.LinearVelocity = Vector3.Zero;
                    entity.Physics.AngularVelocity = Vector3.Zero;

                    if (ownerEntity.Physics != null)
                    {
                        entity.Physics.LinearVelocity = ownerEntity.Physics.LinearVelocity;
                        entity.Physics.AngularVelocity = ownerEntity.Physics.AngularVelocity;
                    }
                    else if (ownerEntity is MyCubeBlock)
                    {
                        var grid = (ownerEntity as MyCubeBlock).CubeGrid;
                        if (grid.Physics != null)
                        {
                            entity.Physics.LinearVelocity = grid.Physics.LinearVelocity;
                            entity.Physics.AngularVelocity = grid.Physics.AngularVelocity;
                        }
                    }

                    return true;
                }
            }
            return false;
        }


        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            if (Sync.IsServer)
            {
                Entity.OnClosing += OnEntityClosing;
            }
        }

        private void OnEntityClosing(VRage.ModAPI.IMyEntity obj)
        {
            var entity = obj as MyEntity;
            
            if (entity.HasInventory && entity.InScene)
            {
                SpawnInventoryContainer();
            }
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            System.Diagnostics.Debug.Assert(Entity != null, "Entity can't be null when removing this component..");

            if (Sync.IsServer)
            {
                Entity.OnClosing -= OnEntityClosing;
            }
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            var inventorySpawnDef = definition as MyEntityInventorySpawnComponent_Definition;

            m_containerDefinition = inventorySpawnDef.ContainerDefinition;
        }
    }
}
