using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System.Diagnostics;
using VRage;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;
using VRage.Game.Models;
using System;
using Sandbox.Game.GameSystems;
using Sandbox.Engine.Physics;
using Havok;
using System.Collections.Generic;
using Sandbox.Game.Entities.Character;
using VRage.Game.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using VRage.Game;

namespace Sandbox.Game.Entities.Inventory
{
    public static class MyPhysicalInventoryItemExtensions
    {
        private const float ITEM_SPAWN_RADIUS = 1.0f;

        private static List<HkBodyCollision> m_tmpCollisions = new List<HkBodyCollision>();

        public static MyEntity Spawn(this MyPhysicalInventoryItem thisItem, MyFixedPoint amount, BoundingBoxD box, MyEntity owner = null)
        {
            if(amount < 0)
            {
                return null;
            }

            MatrixD spawnMatrix = MatrixD.Identity;
            spawnMatrix.Translation = box.Center;
            var entity = Spawn(thisItem, amount, spawnMatrix, owner);
            if (entity == null)
                return null;
            var size = entity.PositionComp.LocalVolume.Radius;
            var halfSize = box.Size / 2 - new Vector3(size);
            halfSize = Vector3.Max(halfSize, Vector3.Zero);
            box = new BoundingBoxD(box.Center - halfSize, box.Center + halfSize);
            var pos = MyUtils.GetRandomPosition(ref box);

            Vector3 forward = MyUtils.GetRandomVector3Normalized();
            Vector3 up = MyUtils.GetRandomVector3Normalized();
            while (forward == up)
                up = MyUtils.GetRandomVector3Normalized();

            Vector3 right = Vector3.Cross(forward, up);
            up = Vector3.Cross(right, forward);
            entity.WorldMatrix = MatrixD.CreateWorld(pos, forward, up);
            return entity;
        }

        public static MyEntity Spawn(this MyPhysicalInventoryItem thisItem, MyFixedPoint amount, MatrixD worldMatrix, MyEntity owner = null)
        {
            if(amount < 0)
            {
                return null;
            }
            if (thisItem.Content == null)
            {
                Debug.Fail("Can not spawn item with null content!");
                return null;
            }

            if (thisItem.Content is MyObjectBuilder_BlockItem)
            {
                Debug.Assert(MyFixedPoint.IsIntegral(amount), "Spawning fractional number of grids!");
                bool isBlock = typeof(MyObjectBuilder_CubeBlock).IsAssignableFrom(thisItem.Content.GetObjectId().TypeId);
                Debug.Assert(isBlock, "Block item does not contain block!?!?@&*#%!");
                if (!isBlock) return null;

                var blockItem = thisItem.Content as MyObjectBuilder_BlockItem;
                MyCubeBlockDefinition blockDefinition;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(blockItem.BlockDefId, out blockDefinition);
                Debug.Assert(blockDefinition != null, "Block definition not found");
                if (blockDefinition == null) return null;

                var builder = MyObjectBuilderSerializer.CreateNewObject(typeof(MyObjectBuilder_CubeGrid)) as MyObjectBuilder_CubeGrid;
                builder.GridSizeEnum = blockDefinition.CubeSize;
                builder.IsStatic = false;
                builder.PersistentFlags |= MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled;
                builder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix);

                var block = MyObjectBuilderSerializer.CreateNewObject(blockItem.BlockDefId) as MyObjectBuilder_CubeBlock;
                System.Diagnostics.Debug.Assert(block != null, "Block couldn't been created, maybe wrong definition id? DefID: " + blockItem.BlockDefId);

                if (block != null)
                {
                    block.Min = blockDefinition.Size / 2 - blockDefinition.Size + Vector3I.One;
                    builder.CubeBlocks.Add(block);

                    MyCubeGrid firstGrid = null;
                    for (int i = 0; i < amount; ++i)
                    {
                        builder.EntityId = MyEntityIdentifier.AllocateId();
                        block.EntityId = MyEntityIdentifier.AllocateId();
                        MyCubeGrid newGrid = MyEntities.CreateFromObjectBuilder(builder) as MyCubeGrid;
                        firstGrid = firstGrid ?? newGrid;
                        MyEntities.Add(newGrid);
                    }
                    return firstGrid;
                }
                return null;
            }
            else 
            {
                MyPhysicalItemDefinition itemDefinition = null;
                if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(thisItem.Content.GetObjectId(), out itemDefinition))
                {
                    return MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(amount, thisItem.Content), worldMatrix, owner != null ? owner.Physics : null);
                }
                return null;
            }
        }

        public static MyDefinitionBase GetItemDefinition(this MyPhysicalInventoryItem thisItem)
        {
            if (thisItem.Content == null)
                return null;

            // Block
            MyDefinitionBase itemDefinition = null;
            if (thisItem.Content is MyObjectBuilder_BlockItem)
            {
                var id = (thisItem.Content as MyObjectBuilder_BlockItem).BlockDefId;
                MyCubeBlockDefinition blockDef = null;
                if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(id, out blockDef))
                    itemDefinition = blockDef;
            }
            else
            {
                itemDefinition = MyDefinitionManager.Static.TryGetComponentBlockDefinition(thisItem.Content.GetId());
            }

            // Floating object
            if (itemDefinition == null)
            {
                MyPhysicalItemDefinition floatingObjectDefinition;
                if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(thisItem.Content.GetId(), out floatingObjectDefinition))
                    itemDefinition = floatingObjectDefinition;
            }

            return itemDefinition;
        }

        public static MyEntity SpawnInWorldOrLootBag(this MyPhysicalInventoryItem thisItem, MyEntity owner, ref MyEntity lootBagEntity)
        {
            Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer);

            MyDefinitionBase itemDefinition = thisItem.GetItemDefinition();
            Debug.Assert(itemDefinition != null);
            if (itemDefinition == null)
                return null;

            MyEntity spawnedItem = null;

            Vector3 upDir = -MyGravityProviderSystem.CalculateNaturalGravityInPoint(owner.PositionComp.WorldMatrix.Translation);
            if (upDir == Vector3.Zero)
                upDir = Vector3.Up;
            else
                upDir.Normalize();

            if (itemDefinition is MyCubeBlockDefinition)
            {
                MyCubeBlockDefinition blockDef = itemDefinition as MyCubeBlockDefinition;

                if (MyDefinitionManager.Static.GetLootBagDefinition() != null)
                {
                    // New code which tries to spawn item with "MyEntities.FindFreePlace" and if there is no such position then loot bag is spawn and item is moved into it.
                    MyModel blockModel = MyModels.GetModelOnlyData(blockDef.Model);
                    BoundingBox box = blockModel.BoundingBox;
                    box.Inflate(0.15f); // Inflate with value that is higher than half size of small grid so it will eliminate problems with block center offsets.
                    float radius = box.HalfExtents.Max();
                    var baseSpawnPosition = owner.PositionComp.WorldMatrix.Translation;
                    if (owner is MyCharacter)
                        baseSpawnPosition += owner.PositionComp.WorldMatrix.Up + owner.PositionComp.WorldMatrix.Forward;
                    else
                        baseSpawnPosition += upDir;

                    for (int gridIndex = 0; gridIndex < thisItem.Amount; ++gridIndex)
                    {
                        Vector3D? spawnPos = null;
                        if (lootBagEntity == null && (spawnPos = MyEntities.FindFreePlace(baseSpawnPosition, radius, maxTestCount: 50, testsPerDistance: 5, stepSize: 0.25f)) != null)
                        {
                            MatrixD transform = owner.PositionComp.WorldMatrix;
                            transform.Translation = spawnPos.Value;

                            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(blockDef.Id) as MyObjectBuilder_CubeBlock;
                            blockBuilder.Min = blockDef.Size / 2 - blockDef.Size + Vector3I.One;
                            blockBuilder.EntityId = MyEntityIdentifier.AllocateId();

                            var newGrid = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
                            newGrid.PositionAndOrientation = new MyPositionAndOrientation(transform);
                            newGrid.GridSizeEnum = blockDef.CubeSize;
                            newGrid.PersistentFlags |= MyPersistentEntityFlags2.InScene;
                            newGrid.EntityId = MyEntityIdentifier.AllocateId();
                            newGrid.CubeBlocks.Add(blockBuilder);

                            var entity = MyEntities.CreateFromObjectBuilderAndAdd(newGrid);
                            spawnedItem = spawnedItem ?? entity;
                        }
                        else
                        {
                            AddItemToLootBag(owner, new MyPhysicalInventoryItem(1, thisItem.Content), ref lootBagEntity);
                        }
                    }
                }
                else
                {
                    // Old code used in SE (when no loot bag definition is defined).
                    float spawnRadius = MyUtils.GetRandomFloat(Math.Max(0.25f, ITEM_SPAWN_RADIUS), ITEM_SPAWN_RADIUS);
                    Vector3D randomizer = MyUtils.GetRandomVector3CircleNormalized() * spawnRadius + Vector3D.Up * 0.25f;

                    int yOffset = 0;
                    MyModel m = MyModels.GetModelOnlyData(blockDef.Model);
                    float sizeY = m.BoundingBoxSize.Y + 0.05f;
                    BoundingBox box = m.BoundingBox;

                    for (int gridIndex = 0; gridIndex < thisItem.Amount; ++gridIndex)
                    {
                        var newGrid = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();

                        MatrixD transform = owner.PositionComp.WorldMatrix * MatrixD.CreateTranslation(randomizer + new Vector3D(0, yOffset * sizeY, 0));
                        if (!GetNonPenetratingTransformPosition(ref box, ref transform))
                        {
                            randomizer = MyUtils.GetRandomVector3CircleNormalized() * 0.25f + Vector3D.Up * 0.25f;
                            transform = owner.PositionComp.WorldMatrix * MatrixD.CreateTranslation(randomizer + new Vector3D(0, yOffset * sizeY, 0));
                        }

                        newGrid.PositionAndOrientation = new MyPositionAndOrientation(transform);
                        newGrid.GridSizeEnum = blockDef.CubeSize;
                        newGrid.PersistentFlags |= MyPersistentEntityFlags2.InScene;
                        newGrid.EntityId = MyEntityIdentifier.AllocateId();

                        var newBlock = MyObjectBuilderSerializer.CreateNewObject(blockDef.Id) as MyObjectBuilder_CubeBlock;
                        newBlock.EntityId = MyEntityIdentifier.AllocateId();
                        newGrid.CubeBlocks.Add(newBlock);

                        var entity = MyEntities.CreateFromObjectBuilderAndAdd(newGrid);
                        spawnedItem = spawnedItem ?? entity;

                        if ((gridIndex + 1) % 10 == 0)
                        {
                            spawnRadius = MyUtils.GetRandomFloat(Math.Max(0.25f, ITEM_SPAWN_RADIUS), ITEM_SPAWN_RADIUS);
                            randomizer = MyUtils.GetRandomVector3CircleNormalized() * spawnRadius + Vector3D.Up * 0.25f;
                            yOffset = 0;
                        }
                        else
                        {
                            yOffset++;
                        }
                    }
                }
            }
            else if (itemDefinition is MyPhysicalItemDefinition)
            {
                MyPhysicalItemDefinition floatingObjectDefinition = itemDefinition as MyPhysicalItemDefinition;

                MyFixedPoint amount = thisItem.Amount;
                bool canStack = thisItem.Content.CanStack(thisItem.Content);
                MyFixedPoint stackSize = canStack ? amount : 1;
                MyFixedPoint maxStackAmount = MyFixedPoint.MaxValue;
                MyComponentDefinition compDef = null;
                if (MyDefinitionManager.Static.TryGetComponentDefinition(thisItem.Content.GetId(), out compDef))
                {
                    maxStackAmount = compDef.MaxStackAmount;
                    stackSize = MyFixedPoint.Min(stackSize, maxStackAmount);
                }

                if (MyDefinitionManager.Static.GetLootBagDefinition() != null)
                {
                    // New code which tries to spawn item with "MyEntities.FindFreePlace" and if there is no such position then loot bag is spawn and item is moved into it.
                    MyModel model = MyModels.GetModelOnlyData(floatingObjectDefinition.Model);
                    BoundingBox box = model.BoundingBox;
                    float radius = box.HalfExtents.Max();
                    var baseSpawnPosition = owner.PositionComp.WorldMatrix.Translation;
                    if (owner is MyCharacter)
                        baseSpawnPosition += owner.PositionComp.WorldMatrix.Up + owner.PositionComp.WorldMatrix.Forward;
                    else
                        baseSpawnPosition += upDir;

                    while (amount > 0)
                    {
                        MyFixedPoint spawnAmount = stackSize;
                        amount -= stackSize;
                        if (amount < 0)
                            spawnAmount = amount + stackSize;

                        Vector3D? spawnPos = null;
                        if (lootBagEntity == null && (spawnPos = MyEntities.FindFreePlace(baseSpawnPosition, radius, maxTestCount: 50, testsPerDistance: 5, stepSize: 0.25f)) != null)
                        {
                            MatrixD worldMat = owner.PositionComp.WorldMatrix;
                            worldMat.Translation = spawnPos.Value;
                            var entity = MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(spawnAmount, thisItem.Content), worldMat);
                            spawnedItem = spawnedItem ?? entity;
                        }
                        else
                        {
                            AddItemToLootBag(owner, new MyPhysicalInventoryItem(spawnAmount, thisItem.Content), ref lootBagEntity);
                        }
                    }
                }
                else
                {
                    // Old code used in SE (when no loot bag definition is defined).
                    while (amount > 0)
                    {
                        MyFixedPoint spawnAmount = stackSize;
                        amount -= stackSize;
                        if (amount < 0)
                            spawnAmount = amount + stackSize;

                        float spawnRadius = MyUtils.GetRandomFloat(Math.Max(0.25f, ITEM_SPAWN_RADIUS), ITEM_SPAWN_RADIUS);
                        Vector3D randomizer = MyUtils.GetRandomVector3CircleNormalized() * spawnRadius + Vector3D.Up * 0.25f;
                        var worldMat = owner.PositionComp.WorldMatrix * MatrixD.CreateTranslation(randomizer);

                        var entity = MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(spawnAmount, thisItem.Content), worldMat);
                        spawnedItem = spawnedItem ?? entity;
                    }
                }
            }

            return spawnedItem;
        }

        private static bool GetNonPenetratingTransformPosition(ref BoundingBox box, ref MatrixD transform)
        {
            Quaternion q = Quaternion.CreateFromRotationMatrix(transform);
            Vector3 halfExtents = box.HalfExtents;

            try
            {
                for (int i = 0; i < 11; ++i)
                {
                    float offset = 0.3f * i;
                    Vector3D translation = transform.Translation + Vector3D.UnitY * offset;
                    m_tmpCollisions.Clear();
                    MyPhysics.GetPenetrationsBox(ref halfExtents, ref translation, ref q, m_tmpCollisions, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                    if (m_tmpCollisions.Count == 0)
                    {
                        transform.Translation = translation;
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                m_tmpCollisions.Clear();
            }
        }

        /// <summary>
        /// Add the given inventory item to loot bag. If loot bag does not exist then it will be created.
        /// </summary>
        private static void AddItemToLootBag(MyEntity itemOwner, MyPhysicalInventoryItem item, ref MyEntity lootBagEntity)
        {
            Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer);

            var lootBagDefinition = MyDefinitionManager.Static.GetLootBagDefinition();
            Debug.Assert(lootBagDefinition != null, "Loot bag not definined");
            if (lootBagDefinition == null)
                return;

            // Block
            MyDefinitionBase itemDefinition = item.GetItemDefinition();
            Debug.Assert(itemDefinition != null, "Unknown inventory item");
            if (itemDefinition == null)
                return;

            // Find lootbag nearby.
            if (lootBagEntity == null && lootBagDefinition.SearchRadius > 0)
            {
                Vector3D itemOwnerPosition = itemOwner.PositionComp.GetPosition();
                BoundingSphereD sphere = new BoundingSphereD(itemOwnerPosition, lootBagDefinition.SearchRadius);
                var entitiesInSphere = MyEntities.GetEntitiesInSphere(ref sphere);
                double minDistanceSq = double.MaxValue;
                foreach (var entity in entitiesInSphere)
                {
                    if (!entity.MarkedForClose && (entity.GetType() == typeof(MyEntity)))
                    {
                        if (entity.DefinitionId != null && entity.DefinitionId.Value == lootBagDefinition.ContainerDefinition)
                        {
                            var distanceSq = (entity.PositionComp.GetPosition() - itemOwnerPosition).LengthSquared();
                            if (distanceSq < minDistanceSq)
                            {
                                lootBagEntity = entity;
                                minDistanceSq = distanceSq;
                            }
                        }
                    }
                }
                entitiesInSphere.Clear();
            }

            // Create lootbag
            if (lootBagEntity == null
                || (lootBagEntity.Components.Has<MyInventoryBase>() && !(lootBagEntity.Components.Get<MyInventoryBase>() as MyInventory).CanItemsBeAdded(item.Amount, itemDefinition.Id)))
            {
                lootBagEntity = null;
                MyContainerDefinition lootBagDef;
                if (MyComponentContainerExtension.TryGetContainerDefinition(lootBagDefinition.ContainerDefinition.TypeId, lootBagDefinition.ContainerDefinition.SubtypeId, out lootBagDef))
                {
                    lootBagEntity = SpawnBagAround(itemOwner, lootBagDef);
                }
            }

            Debug.Assert(lootBagEntity != null, "Loot bag not created");

            // Fill lootbag inventory
            if (lootBagEntity != null)
            {
                MyInventory inventory = lootBagEntity.Components.Get<MyInventoryBase>() as MyInventory;
                Debug.Assert(inventory != null);
                if (inventory != null)
                {
                    if (itemDefinition is MyCubeBlockDefinition)
                        inventory.AddBlocks(itemDefinition as MyCubeBlockDefinition, item.Amount);
                    else
                        inventory.AddItems(item.Amount, item.Content);
                }
            }
        }

        /// <summary>
        /// Spawns bag around position given by "baseTransform", checks all 4 directions around - forwards (forward, right, backward, left) and on each such direction moves test sphere 
        /// in 3 directions forward (frontChecks), sides (perpendicular to forward direction - rights) and up. If spawn position is not found then position above "worldAabbTopPosition"
        /// is selected.
        /// </summary>
        private static MyEntity SpawnBagAround(MyEntity itemOwner, MyContainerDefinition bagDefinition,
            int sideCheckCount = 3, int frontCheckCount = 2, int upCheckCount = 5, float stepSize = 1f)
        {
            Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer);

            Vector3D? finalPos = null;

            // Model sphere
            MyModel bagModel = null;
            foreach (var componentDef in bagDefinition.DefaultComponents)
            {
                if (typeof(MyObjectBuilder_ModelComponent).IsAssignableFrom(componentDef.BuilderType))
                {
                    MyComponentDefinitionBase componentDefinition = null;
                    var componentSubtype = bagDefinition.Id.SubtypeId;
                    if (componentDef.SubtypeId.HasValue)
                        componentSubtype = componentDef.SubtypeId.Value;

                    if (MyComponentContainerExtension.TryGetComponentDefinition(componentDef.BuilderType, componentSubtype, out componentDefinition))
                    {
                        var modelComponentDef = componentDefinition as MyModelComponentDefinition;
                        Debug.Assert(modelComponentDef != null);
                        if (modelComponentDef != null)
                            bagModel = MyModels.GetModelOnlyData(modelComponentDef.Model);
                    }

                    break;
                }
            }

            Debug.Assert(bagModel != null);
            if (bagModel == null)
                return null;

            float bagBoxRadius = bagModel.BoundingBox.HalfExtents.Max();
            HkShape sphere = new HkSphereShape(bagBoxRadius);

            try
            {
                Vector3D basePos = itemOwner.PositionComp.WorldMatrix.Translation;
                float step = bagBoxRadius * stepSize;

                // Calculate right, up and forward vectors from gravity
                Vector3 upDir = -MyGravityProviderSystem.CalculateNaturalGravityInPoint(itemOwner.PositionComp.WorldMatrix.Translation);
                if (upDir == Vector3.Zero)
                    upDir = Vector3.Up;
                else
                    upDir.Normalize();

                Vector3 forwardDir;
                upDir.CalculatePerpendicularVector(out forwardDir);

                Vector3 rightDir = Vector3.Cross(forwardDir, upDir);
                rightDir.Normalize();

                Vector3D currentPos;
                Quaternion rot = Quaternion.Identity;

                Vector3[] forwards = new Vector3[] 
                {
                    forwardDir,
                    rightDir,
                    -forwardDir,
                    -rightDir
                };

                Vector3[] rights = new Vector3[] 
                {
                    rightDir,
                    -forwardDir,
                    -rightDir,
                    forwardDir
                };

                // All sides
                for (int i = 0; i < forwards.Length && finalPos == null; ++i)
                {
                    var forward = forwards[i];
                    var right = rights[i];

                    // Move forward
                    for (int frontMove = 0; frontMove < frontCheckCount && finalPos == null; ++frontMove)
                    {
                        Vector3D sidePosBase = basePos + 0.25f * forward + bagBoxRadius * forward + frontMove * step * forward - 0.5f * (sideCheckCount - 1) * step * right;

                        // Move perp to forward
                        for (int sideMove = 0; sideMove < sideCheckCount && finalPos == null; ++sideMove)
                        {
                            // Move up
                            for (int upMove = 0; upMove < upCheckCount && finalPos == null; ++upMove)
                            {
                                currentPos = sidePosBase + sideMove * step * right + upMove * step * upDir;

                                if (MyEntities.IsInsideWorld(currentPos) && !MyEntities.IsShapePenetrating(sphere, ref currentPos, ref rot))
                                {
                                    BoundingSphereD boundingSphere = new BoundingSphereD(currentPos, bagBoxRadius);
                                    MyVoxelBase overlappedVoxelmap = MySession.Static.VoxelMaps.GetOverlappingWithSphere(ref boundingSphere);

                                    if (overlappedVoxelmap == null)
                                    {
                                        finalPos = currentPos;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // If not found position then select position above aabb's top.
                if (finalPos == null)
                {
                    MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD((BoundingBoxD)itemOwner.PositionComp.LocalAABB, itemOwner.PositionComp.WorldMatrix);
                    Vector3D[] corners = new Vector3D[8];
                    obb.GetCorners(corners, 0);
                    float dotUp = float.MinValue;
                    foreach (var corner in corners)
                    {
                        var localDot = Vector3.Dot(corner - obb.Center, upDir);
                        dotUp = Math.Max(dotUp, localDot);
                    }

                    finalPos = itemOwner.PositionComp.WorldMatrix.Translation;
                    Debug.Assert(dotUp > 0);
                    if (dotUp > 0)
                        finalPos = obb.Center + dotUp * upDir;
                }
            }
            finally
            {
                sphere.RemoveReference();
            }

            Debug.Assert(finalPos != null);

            MatrixD transform = itemOwner.PositionComp.WorldMatrix;
            transform.Translation = finalPos.Value;

            MyEntity bagEntity = MyEntities.CreateFromComponentContainerDefinitionAndAdd(bagDefinition.Id);
            if (bagEntity == null)
                return null;

            bagEntity.PositionComp.SetWorldMatrix(transform);

            bagEntity.Physics.LinearVelocity = Vector3.Zero;
            bagEntity.Physics.AngularVelocity = Vector3.Zero;

            return bagEntity;
        }


    }
}
