using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.AI;
using VRage.Game.Entity;
using Sandbox.Engine.Utils;
using VRage.Game;
using Sandbox.Engine.Physics;

namespace Sandbox.Game.Entities
{
    public static class MyItemsCollector
    {
        public struct ItemInfo
        {
            public Vector3D Target;
            public long ItemsEntityId;
            public int ItemId;
        }

        public struct EntityInfo
        {
            public Vector3D Target;
            public long EntityId;
        }

        public struct ComponentInfo
        {
            public long EntityId;
            public Vector3I BlockPosition;
            public MyDefinitionId ComponentDefinitionId;
            public int ComponentCount;
            public bool IsBlock;           
        }

        public struct CollectibleInfo
        {
            public long EntityId;
            public MyDefinitionId DefinitionId;
            public MyFixedPoint Amount;
        }

        private static List<MyFracturedPiece> m_tmpFracturePieceList = new List<MyFracturedPiece>();
        private static List<MyEnvironmentItems.ItemInfo> m_tmpEnvItemList = new List<MyEnvironmentItems.ItemInfo>();
        private static List<ItemInfo> m_tmpItemInfoList = new List<ItemInfo>();

        private static List<ComponentInfo> m_retvalBlockInfos = new List<ComponentInfo>();
        private static List<CollectibleInfo> m_retvalCollectibleInfos = new List<CollectibleInfo>();

        public static bool FindClosestTreeInRadius(Vector3D fromPosition, float radius, out ItemInfo result)
        {
            result = default(ItemInfo);

            BoundingSphereD sphere = new BoundingSphereD(fromPosition, (double)radius);
            var entities = MyEntities.GetEntitiesInSphere(ref sphere);

            double closestDistanceSq = double.MaxValue;

            foreach (MyEntity entity in entities)
            {
                MyTrees trees = entity as MyTrees;
                if (trees == null) continue;

                trees.GetPhysicalItemsInRadius(fromPosition, radius, m_tmpEnvItemList);

                foreach (var tree in m_tmpEnvItemList)
                {
                    double distanceSq = Vector3D.DistanceSquared(fromPosition, tree.Transform.Position);
                    if (distanceSq < closestDistanceSq)
                    {
                        result.ItemsEntityId = entity.EntityId;
                        result.ItemId = tree.LocalId;
                        result.Target = tree.Transform.Position;
                        closestDistanceSq = distanceSq;
                    }
                }
            }

			entities.Clear();

            return closestDistanceSq != double.MaxValue;
        }

        public static bool FindClosestTreeInPlaceArea(Vector3D fromPosition, long entityId, MyHumanoidBot bot, out ItemInfo result)
		{
            result = default(ItemInfo);
            MyPlaceArea area = MyPlaceArea.FromEntity(entityId);
            if (area == null)
                return false;

            var areaBoundingBox = area.WorldAABB;
            var entities = MyEntities.GetEntitiesInAABB(ref areaBoundingBox, true);

            double closestDistanceSq = double.MaxValue;

            foreach (MyEntity entity in entities)
            {
                MyTrees trees = entity as MyTrees;
                if (trees == null) continue;

				m_tmpEnvItemList.Clear();
                trees.GetPhysicalItemsInRadius(areaBoundingBox.Center, (float)areaBoundingBox.HalfExtents.Length(), m_tmpEnvItemList);

                foreach (var tree in m_tmpEnvItemList)
                {
                    if (!area.TestPoint(tree.Transform.Position))
                        continue;

                    if (!bot.AgentLogic.AiTarget.IsTreeReachable(entity, tree.LocalId))
                        continue;

                    double distanceSq = Vector3D.DistanceSquared(fromPosition, tree.Transform.Position);
                    if (distanceSq < closestDistanceSq)
                    {
                        result.ItemsEntityId = entity.EntityId;
                        result.ItemId = tree.LocalId;
                        result.Target = tree.Transform.Position;
                        closestDistanceSq = distanceSq;
                    }
                }
                m_tmpEnvItemList.Clear();
            }

            entities.Clear();

            return (closestDistanceSq != double.MaxValue);
		}

        public static bool FindFallingTreeInRadius(Vector3D position, float radius, out EntityInfo result)
        {
            result = new EntityInfo();
            BoundingSphereD testSphere = new BoundingSphereD(position, radius);

            m_tmpFracturePieceList.Clear();
            MyFracturedPiecesManager.Static.GetFracturesInSphere(ref testSphere, ref m_tmpFracturePieceList);
            foreach (var fracture in m_tmpFracturePieceList)
            {
                if (fracture.Physics.RigidBody != null && fracture.Physics.RigidBody.IsActive && !Vector3.IsZero(fracture.Physics.AngularVelocity) && !Vector3.IsZero(fracture.Physics.LinearVelocity))
                {
                    result.Target = Vector3D.Transform(fracture.Shape.CoM, fracture.PositionComp.WorldMatrix);
                    result.EntityId = fracture.EntityId;
                    m_tmpFracturePieceList.Clear();
                    return true;
                }
            }
            m_tmpFracturePieceList.Clear();

            return false;
        }

        public static bool FindClosestFracturedTreeInRadius(Vector3D fromPosition, double radius, MyHumanoidBot bot, out EntityInfo result)
        {
            return FindClosestFracturedTreeInternal(fromPosition, fromPosition, radius, null, bot, out result);
        }

        public static bool FindClosestFracturedTreeInArea(Vector3D fromPosition, long areaEntityId, MyHumanoidBot bot, out EntityInfo result)
        {
            result = default(EntityInfo);

            MyPlaceArea area = MyPlaceArea.FromEntity(areaEntityId);
            if (area == null) return false;
    
            BoundingBoxD areaBB = area.WorldAABB;
            double radius = (double)areaBB.HalfExtents.Length();


			return FindClosestFracturedTreeInternal(fromPosition, areaBB.Center, radius, area, bot, out result); ;
        }

        private static bool FindClosestFracturedTreeInternal(Vector3D fromPosition, Vector3D searchCenter, double searchRadius, MyPlaceArea area, MyHumanoidBot bot, out EntityInfo result)
        {
            result = default(EntityInfo);

            double closestDistanceSq = double.MaxValue;
            MyFracturedPiece closestTarget = null;
            Vector3D closestPoint = default(Vector3D);

            BoundingSphereD searchSphere = new BoundingSphereD(searchCenter, searchRadius);
            
            m_tmpFracturePieceList.Clear();
            MyFracturedPiecesManager.Static.GetFracturesInSphere(ref searchSphere, ref m_tmpFracturePieceList);
            for (int i = 0; i < m_tmpFracturePieceList.Count; ++i)
            {
                var fracture = m_tmpFracturePieceList[i];

                // Skip non-tree fractures
                if (!MyTrees.IsEntityFracturedTree(fracture))
                {
                    continue;
                }

                if (IsFracturedTreeStump(fracture))
                    continue;

                if (!bot.AgentLogic.AiTarget.IsEntityReachable(fracture))
                    continue;

                Vector3D positionInTrunkLocal = Vector3D.Transform(fromPosition, fracture.PositionComp.WorldMatrixNormalizedInv);
                Vector3D closestPointOnTrunk;

                if (!FindClosestPointOnFracturedTree(positionInTrunkLocal, fracture, out closestPointOnTrunk))
                    continue;

                if (area == null || area.TestPoint(closestPointOnTrunk))
                {
                    double distanceSq = Vector3D.DistanceSquared(closestPointOnTrunk, fromPosition);

                    if (distanceSq < closestDistanceSq)
                    {
                        closestDistanceSq = distanceSq;
                        closestTarget = fracture;
                        closestPoint = closestPointOnTrunk;
                    }
                }
            }
            m_tmpFracturePieceList.Clear();

            if (closestTarget == null)
                return false;

            result.EntityId = closestTarget.EntityId;
            result.Target = closestPoint;
            return true;
        }

        public static bool FindCollectableItemInRadius(Vector3D position, float radius, HashSet<MyDefinitionId> itemDefs, Vector3D initialPosition, float ignoreRadius, out ComponentInfo result)
        {
            BoundingSphereD sphere = new BoundingSphereD(position, radius);
            var entities = MyEntities.GetEntitiesInSphere(ref sphere);

            result = default(ComponentInfo);

            double closestCubeDistanceSq = double.MaxValue;

            foreach (var entity in entities)
            {
                if (MyManipulationTool.IsEntityManipulated(entity))
                    continue;

                if (entity is MyCubeGrid) // TODO: Add logs and timbers
                {
                    var cubeGrid = entity as MyCubeGrid;
                    if (cubeGrid.BlocksCount == 1)
                    {
                        var first = cubeGrid.CubeBlocks.First();
                        if (itemDefs.Contains(first.BlockDefinition.Id))
                        {
                            var worldPosition = cubeGrid.GridIntegerToWorld(first.Position);
                            var cubeDistanceFromSpawnSq = Vector3D.DistanceSquared(worldPosition, initialPosition);
                            if (cubeDistanceFromSpawnSq <= ignoreRadius * ignoreRadius)
                                continue;
                            var cubeDistanceFromCharacterSq = Vector3D.DistanceSquared(worldPosition, position);
                            if (cubeDistanceFromCharacterSq < closestCubeDistanceSq)
                            {
                                closestCubeDistanceSq = cubeDistanceFromCharacterSq;
                                result.EntityId = cubeGrid.EntityId;
                                result.BlockPosition = first.Position;
                                result.ComponentDefinitionId = GetComponentId(first);
                                result.IsBlock = true;
                            }
                        }
                    }
                }

                if (entity is MyFloatingObject)
                {
                    var fo = entity as MyFloatingObject;
                    var id = fo.Item.Content.GetId();
                    if (itemDefs.Contains(id))
                    {
                        var foToPlayerDistSq = Vector3D.DistanceSquared(fo.PositionComp.WorldMatrix.Translation, position);
                        if (foToPlayerDistSq < closestCubeDistanceSq)
                        {
                            closestCubeDistanceSq = foToPlayerDistSq;
                            result.EntityId = fo.EntityId;
                            result.IsBlock = false;
                        }
                    }
                }
            }
            entities.Clear();

            return closestCubeDistanceSq != double.MaxValue;
        }

        public static bool FindClosestCollectableItemInPlaceArea(Vector3D fromPosition, long entityId, HashSet<MyDefinitionId> itemDefinitions, out ComponentInfo result)
        {
            List<MyEntity> entities = null;
            result = default(ComponentInfo);

            try
            {
                MyEntity containingEntity = null;
                MyPlaceArea area = null;

                if (!MyEntities.TryGetEntityById(entityId, out containingEntity))
                    return false;
                if (!containingEntity.Components.TryGet<MyPlaceArea>(out area))
                    return false;

                var areaBoundingBox = area.WorldAABB;
                entities = MyEntities.GetEntitiesInAABB(ref areaBoundingBox, true);

                MyEntity closestObject = null;
                MySlimBlock first = null;
                double closestObjectDistanceSq = double.MaxValue;
                bool closestIsBlock = false;

                foreach (var entity in entities)
                {
                    if (MyManipulationTool.IsEntityManipulated(entity))
                        continue;

                    if (entity is MyCubeGrid)
                    {
                        var cubeGrid = entity as MyCubeGrid;
                        if (cubeGrid.BlocksCount == 1)
                        {
                            first = cubeGrid.CubeBlocks.First();
                            if (itemDefinitions.Contains(first.BlockDefinition.Id))
                            {
                                var worldPosition = cubeGrid.GridIntegerToWorld(first.Position);
                                var cubeDistanceFromCharacterSq = Vector3D.DistanceSquared(worldPosition, fromPosition);

                                if (cubeDistanceFromCharacterSq < closestObjectDistanceSq)
                                {
                                    closestObjectDistanceSq = cubeDistanceFromCharacterSq;
                                    closestObject = cubeGrid;
                                    closestIsBlock = true;
                                }
                            }
                        }
                    }

                    if (entity is MyFloatingObject)
                    {
                        var fo = entity as MyFloatingObject;
                        var id = fo.Item.Content.GetId();
                        if (itemDefinitions.Contains(id))
                        {
                            var foToPlayerDistSq = Vector3D.DistanceSquared(fo.PositionComp.WorldMatrix.Translation, fromPosition);
                            if (foToPlayerDistSq < closestObjectDistanceSq)
                            {
                                closestObjectDistanceSq = foToPlayerDistSq;
                                closestObject = fo;
                                closestIsBlock = false;
                            }
                        }
                    }
                }

                if (closestObject == null)
                    return false;

                result.IsBlock = closestIsBlock;
                result.EntityId = closestObject.EntityId;
                if (closestIsBlock)
                {
                    result.BlockPosition = first.Position;
                    result.ComponentDefinitionId = GetComponentId(first);
                }

                return true;
            }
            finally
            {
                if (entities != null)
                {
                    entities.Clear();
                }
            }
        }

        public static List<ComponentInfo> FindComponentsInRadius(Vector3D fromPosition, double radius)
        {
            Debug.Assert(m_retvalBlockInfos.Count == 0, "The result of the last call of FindComponentsInRadius was not cleared!");

            BoundingSphereD sphere = new BoundingSphereD(fromPosition, radius);

            var entities = MyEntities.GetEntitiesInSphere(ref sphere);
            foreach (var entity in entities)
            {
                if (entity is MyFloatingObject)
                {
                    var floatingObject = entity as MyFloatingObject;
                    if (floatingObject.Item.Content is MyObjectBuilder_Component)
                    {
                        ComponentInfo info = new ComponentInfo();
                        info.EntityId = floatingObject.EntityId;
                        info.BlockPosition = Vector3I.Zero;
                        info.ComponentDefinitionId = floatingObject.Item.Content.GetObjectId();
                        info.IsBlock = false;
                        info.ComponentCount = (int)floatingObject.Item.Amount;
                        m_retvalBlockInfos.Add(info);
                    }
                }
                else
                {
                    MyCubeBlock block = null;
                    MyCubeGrid grid = TryGetAsComponent(entity, out block);
                    if (grid == null) continue;

                    ComponentInfo info = new ComponentInfo();
                    info.IsBlock = true;
                    info.EntityId = grid.EntityId;
                    info.BlockPosition = block.Position;
                    info.ComponentDefinitionId = GetComponentId(block.SlimBlock);

                    if (block.BlockDefinition.Components != null)
                        info.ComponentCount = block.BlockDefinition.Components[0].Count;
                    else
                    {
                        Debug.Assert(false, "Block definition does not have any components!");
                        info.ComponentCount = 0;
                    }
                    m_retvalBlockInfos.Add(info);
                }               
            }

			entities.Clear();

            return m_retvalBlockInfos;
        }

        public static List<CollectibleInfo> FindCollectiblesInRadius(Vector3D fromPosition, double radius, bool doRaycast = false)
        {
            Debug.Assert(m_retvalCollectibleInfos.Count == 0, "The result of the last call of FindComponentsInRadius was not cleared!");

            List<MyPhysics.HitInfo> hits = new List<MyPhysics.HitInfo>();

            BoundingSphereD sphere = new BoundingSphereD(fromPosition, radius);
            var entities = MyEntities.GetEntitiesInSphere(ref sphere);
            foreach (var entity in entities)
            {
                bool addCollectibleInfo = false;

                CollectibleInfo info = new CollectibleInfo();
                MyCubeBlock block = null;
                MyCubeGrid grid = TryGetAsComponent(entity, out block);
                if (grid != null)
                {
                    info.EntityId = grid.EntityId;
                    info.DefinitionId = GetComponentId(block.SlimBlock);
                    if (block.BlockDefinition.Components != null)
                        info.Amount = block.BlockDefinition.Components[0].Count;
                    else
                    {
                        Debug.Assert(false, "Block definition does not have any components!");
                        info.Amount = 0;
                    }
                    addCollectibleInfo = true;
                }
                else if (entity is MyFloatingObject)
                {
                    var floatingObj = entity as MyFloatingObject;
                    var defId = floatingObj.Item.Content.GetObjectId();
                    if (MyDefinitionManager.Static.GetPhysicalItemDefinition(defId).Public)
                    {
                        info.EntityId = floatingObj.EntityId;
                        info.DefinitionId = defId;
                        info.Amount = floatingObj.Item.Amount;
                        addCollectibleInfo = true;
                    }
                }

                if (addCollectibleInfo)
                {
                    bool hitSomething = false;
                    MyPhysics.CastRay(fromPosition, entity.WorldMatrix.Translation, hits, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                    foreach (var hit in hits)
                    {
                        var hitEntity = hit.HkHitInfo.GetHitEntity();
                        if (hitEntity == entity) continue;
                        if (hitEntity is MyCharacter) continue;
                        if (hitEntity is MyFracturedPiece) continue;
                        if (hitEntity is MyFloatingObject) continue;
                        MyCubeBlock dummy = null;
                        if (TryGetAsComponent(hitEntity as MyEntity, out dummy) != null) continue;
                        hitSomething = true;
                        break;
                    }

                    if (!hitSomething)
                    {
                        m_retvalCollectibleInfos.Add(info);
                    }
                }
            }
            entities.Clear();

            return m_retvalCollectibleInfos;
        }

        public static MyCubeGrid TryGetAsComponent(MyEntity entity, out MyCubeBlock block, bool blockManipulatedEntity = true, Vector3D? hitPosition = null)
        {
            block = null;

            if (MyManipulationTool.IsEntityManipulated(entity) && blockManipulatedEntity)
                return null;

            if (entity.MarkedForClose)
                return null;

            var grid = entity as MyCubeGrid;
            if (grid == null) return null;
            if (grid.GridSizeEnum != MyCubeSize.Small) return null;
            MyCubeGrid returnedGrid = null;

            if (MyFakes.ENABLE_GATHERING_SMALL_BLOCK_FROM_GRID && hitPosition != null)
            {
                var gridLocalPos = Vector3D.Transform(hitPosition.Value, grid.PositionComp.WorldMatrixNormalizedInv);
                Vector3I cubePosition;
                grid.FixTargetCube(out cubePosition, gridLocalPos / grid.GridSize);
                MySlimBlock slimBlock = grid.GetCubeBlock(cubePosition);
                if (slimBlock != null && slimBlock.IsFullIntegrity)
                    block = slimBlock.FatBlock;
            }
            else
            {
                if (grid.CubeBlocks.Count != 1) return null;
                if (grid.IsStatic) return null;
                if (!MyCubeGrid.IsGridInCompleteState(grid)) return null;
                if (MyCubeGridSmallToLargeConnection.Static.TestGridSmallToLargeConnection(grid)) return null;

                var enumerator = grid.CubeBlocks.GetEnumerator();
                enumerator.MoveNext();
                block = enumerator.Current.FatBlock;
                enumerator.Dispose();

                returnedGrid = grid;
            }

            if (block == null) return null;

            if (!MyDefinitionManager.Static.IsComponentBlock(block.BlockDefinition.Id)) return null;
            if (block.IsSubBlock) return null;
            var subBlocks = block.GetSubBlocks();
            if (subBlocks.HasValue && subBlocks.Count() > 0) return null;

            return returnedGrid;
        }

        private static void FindFracturedTreesInternal(Vector3D fromPosition, MyPlaceArea area, BoundingSphereD sphere)
        {
            Debug.Assert(m_tmpFracturePieceList.Count == 0, "m_tmpFracturePieceList was not cleared after last use!");

            MyFracturedPiecesManager.Static.GetFracturesInSphere(ref sphere, ref m_tmpFracturePieceList);

            for (int i = m_tmpFracturePieceList.Count - 1; i >= 0; i--)
            {
                MyFracturedPiece fracture = m_tmpFracturePieceList[i];

                if (!MyTrees.IsEntityFracturedTree(fracture))
                {
                    m_tmpFracturePieceList.RemoveAtFast(i);
                    continue;
                }

                if (IsFracturedTreeStump(fracture))
                {
                    m_tmpFracturePieceList.RemoveAtFast(i);
                    continue;
                }

                Vector3D positionInTrunkLocal = Vector3D.Transform(fromPosition, fracture.PositionComp.WorldMatrixNormalizedInv);
                Vector3D closestPointOnTrunk;

                if (!FindClosestPointOnFracturedTree(positionInTrunkLocal, fracture, out closestPointOnTrunk))
                {
                    m_tmpFracturePieceList.RemoveAtFast(i);
                    continue;
                }

                if (!area.TestPoint(closestPointOnTrunk))
                {
                    m_tmpFracturePieceList.RemoveAtFast(i);
                    continue;
                }
            }
        }

        public static bool FindRandomCollectableItemInPlaceArea(long entityId, HashSet<MyDefinitionId> itemDefinitions, out ComponentInfo result)
        {
            result = default(ComponentInfo);
            result.IsBlock = true;

            MyPlaceArea area = MyPlaceArea.FromEntity(entityId);
            if (area == null) return false;

            var areaBoundingBox = area.WorldAABB;
            List<MyEntity> entities = null;
            try
            {
                entities = MyEntities.GetEntitiesInAABB(ref areaBoundingBox, true);

                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    var entity = entities[i];

                    if (MyManipulationTool.IsEntityManipulated(entity))
                    {
                        entities.RemoveAtFast(i);
                        continue;
                    }

                    var cubeGrid = entity as MyCubeGrid;
                    var fo = entity as MyFloatingObject;
                    if (fo == null && (cubeGrid == null || cubeGrid.BlocksCount != 1))
                    {
                        entities.RemoveAtFast(i);
                        continue;
                    }

                    if (cubeGrid != null)
                    {
                        MySlimBlock first = cubeGrid.CubeBlocks.First();
                        if (!itemDefinitions.Contains(first.BlockDefinition.Id))
                        {
                            entities.RemoveAtFast(i);
                            continue;
                        }
                    }
                    else if (fo != null)
                    {
                        var id = fo.Item.Content.GetId();
                        if (!itemDefinitions.Contains(id))
                        {
                            entities.RemoveAtFast(i);
                            continue;
                        }
                    }
                }

                if (entities.Count == 0)
                    return false;

                int randIdx = (int)Math.Round(MyRandom.Instance.NextFloat() * (entities.Count - 1));
                var selectedEntity = entities[randIdx];
                result.EntityId = selectedEntity.EntityId;

                if (selectedEntity is MyCubeGrid)
                {
                    var selectedCube = selectedEntity as MyCubeGrid;
                    var first = selectedCube.GetBlocks().First();

                    result.EntityId = selectedCube.EntityId;
                    result.BlockPosition = first.Position;
                    result.ComponentDefinitionId = GetComponentId(first);
                    result.IsBlock = true;
                }
                else
                {
                    result.IsBlock = false;
                }

                return true;
            }
            finally
            {
                entities.Clear();
            }
        }

        public static bool FindRandomTreeInPlaceArea(long entityId, out ItemInfo result)
        {
            result = default(ItemInfo);

            MyPlaceArea area = MyPlaceArea.FromEntity(entityId);
            if (area == null) return false;

            var areaBoundingBox = area.WorldAABB;
            List<MyEntity> entities = null;
            try
            {
                entities = MyEntities.GetEntitiesInAABB(ref areaBoundingBox, true);

                m_tmpItemInfoList.Clear();
                foreach (var entity in entities)
                {
                    MyTrees trees = entity as MyTrees;
                    if (trees == null)
                        continue;

                    m_tmpEnvItemList.Clear();
                    trees.GetPhysicalItemsInRadius(areaBoundingBox.Center, (float)areaBoundingBox.HalfExtents.Length(), m_tmpEnvItemList);
                    foreach (var tree in m_tmpEnvItemList)
                    {
                        if (area.TestPoint(tree.Transform.Position))
                        {
                            var itemInfo = new ItemInfo();
                            itemInfo.ItemsEntityId = trees.EntityId;
                            itemInfo.ItemId = tree.LocalId;
                            itemInfo.Target = tree.Transform.Position;
                            m_tmpItemInfoList.Add(itemInfo);
                        }
                    }
                    m_tmpEnvItemList.Clear();
                }

                if (m_tmpItemInfoList.Count == 0)
                {
                    m_tmpItemInfoList.Clear();
                    return false;
                }

                int treeIndex = (int)Math.Round(MyRandom.Instance.NextFloat() * (m_tmpItemInfoList.Count - 1));
                result = m_tmpItemInfoList[treeIndex];
                m_tmpItemInfoList.Clear();

                return true;
            }
            finally
            {
                entities.Clear();
            }
        }

        public static bool FindRandomFracturedTreeInPlaceArea(Vector3D fromPosition, long entityId, out EntityInfo result)
        {
            result = default(EntityInfo);

            MyPlaceArea area = MyPlaceArea.FromEntity(entityId);
            if (area == null) return false;

            var areaBB = area.WorldAABB;
            BoundingSphereD searchSphere = new BoundingSphereD(areaBB.Center, (double)areaBB.HalfExtents.Length());

            m_tmpFracturePieceList.Clear();
            FindFracturedTreesInternal(fromPosition, area, searchSphere);

            if (m_tmpFracturePieceList.Count == 0)
            {
                m_tmpFracturePieceList.Clear();
                return false;
            }
            
            int fractureIndex = (int)Math.Round(MyRandom.Instance.NextFloat() * (m_tmpFracturePieceList.Count - 1));
            MyFracturedPiece selectedTarget = m_tmpFracturePieceList[fractureIndex];
            m_tmpFracturePieceList.Clear();

            result.EntityId = selectedTarget.EntityId;
            result.Target = selectedTarget.PositionComp.GetPosition();
            return true;
        }

        public static bool FindClosestPlaceAreaInSphere(BoundingSphereD sphere, string typeName, ref MyBBMemoryTarget foundTarget)
        {
            var foundAreas = new List<MyPlaceArea>();
            MyPlaceAreas.Static.GetAllAreasInSphere(sphere, foundAreas);

            var areaType = MyStringHash.GetOrCompute(typeName);

            double closestDistanceSq = sphere.Radius * sphere.Radius;
            MyPlaceArea closestArea = null;
            foreach (var area in foundAreas)
            {
                if (area.Container.Entity == null || area.AreaType != areaType)
                    continue;

                double distanceSq = area.DistanceSqToPoint(sphere.Center);
                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestArea = area;
                }
            }

            if (closestArea == null) return false;
            MyBBMemoryTarget.SetTargetEntity(ref foundTarget, MyAiTargetEnum.ENTITY, closestArea.Container.Entity.EntityId);
            return true;
        }

        private static MyDefinitionId GetComponentId(MySlimBlock block)
        {
            var components = block.BlockDefinition.Components;

            if (components == null || components.Length == 0)
                return new MyDefinitionId();

            return components[0].Definition.Id;
        }

        private static bool IsFracturedTreeStump(MyFracturedPiece fracture)
        {
            Vector4 minFour, maxFour;
            fracture.Shape.GetShape().GetLocalAABB(0, out minFour, out maxFour);
            if (maxFour.Y - minFour.Y < 3.5 * (maxFour.X - minFour.X)) // HACK: find stumps
                return true;

            return false;
        }

        private static bool FindClosestPointOnFracturedTree(Vector3D fromPositionFractureLocal, MyFracturedPiece fracture, out Vector3D closestPoint)
        {
            // Marko: HACK: skip stumps     
            closestPoint = default(Vector3D);
            if (fracture == null)
                return false;

            Vector4 minFour, maxFour;
            fracture.Shape.GetShape().GetLocalAABB(0, out minFour, out maxFour);
            var min = new Vector3D(minFour);
            var max = new Vector3D(maxFour);

            closestPoint = Vector3D.Clamp(fromPositionFractureLocal, min, max);

            closestPoint.X = (closestPoint.X + 2 * (max.X + min.X) / 2) / 3;
            closestPoint.Y = MathHelper.Clamp(closestPoint.Y + 0.25f * (closestPoint.Y - min.Y < max.Y - closestPoint.Y ? 1 : -1), min.Y, max.Y);
            closestPoint.Z = (closestPoint.Z + 2 * (max.Z + min.Z) / 2) / 3;

            closestPoint = Vector3D.Transform(closestPoint, fracture.PositionComp.WorldMatrix);

            return true;
        }
    }
}
