using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.Replication
{
    static class MySupportHelper
    {
        static List<MyEntity> m_entities = new List<MyEntity>();
        static readonly bool DEBUG_DRAW = false;
        const int MinBlockCount = 5; // More than 5 blocks
        const float MinSpeedSq = 10 * 10;

        public static MyEntityPhysicsStateGroup FindSupportForCharacter(MyEntity entity)
        {
            return FindPhysics(FindSupportForCharacterAABB(entity));
        }

        public static MyEntity FindSupportForCharacterAABB(MyEntity entity)
        {
            BoundingBoxD characterBox = entity.PositionComp.WorldAABB;
            characterBox.Inflate(1.0);
            m_entities.Clear();

            MyEntities.GetTopMostEntitiesInBox(ref characterBox, m_entities);

            float maxRadius = 0;
            MyCubeGrid biggestGrid = null;

            foreach(var parent in m_entities)
            {
                MyCubeGrid grid =  parent as MyCubeGrid;

                if(grid != null)
                {
                    if (grid.Physics == null ||  grid.Physics.IsStatic || grid.GridSizeEnum == MyCubeSize.Small)
                    {
                        continue;
                    }
                    grid = MyGridPhysicsStateGroup.GetMasterGrid(grid);
                    var rad = grid.PositionComp.LocalVolume.Radius;
                    if (rad > maxRadius || (rad == maxRadius && (biggestGrid == null || grid.EntityId > biggestGrid.EntityId)))
                    {
                        maxRadius = rad;
                        biggestGrid = grid;
                    }
                }
            }
            if (biggestGrid != null && biggestGrid.CubeBlocks.Count > 10)
            {
                return biggestGrid;
            }
            return null;
        }

        public static MyEntityPhysicsStateGroup FindSupport(MyEntity entity)
        {
            var support = SphereCast(entity, (float)entity.PositionComp.WorldVolume.Radius * 1.1f, 0.1f);
            if (support != null && support.Entity.Physics != null && support.Entity.Physics.IsStatic)
                support = null; // Static == no support

            if (DEBUG_DRAW && support != null)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(support.Entity.PositionComp.WorldAABB, Color.Red, 1, 1, false);
            }

            return support;
        }

        public static MyEntityPhysicsStateGroup FindPhysics(IMyEntity entity)
        {
            // We don't need to send position relative to static, it's useless
            if (entity != null && entity.Physics != null)
            {
                return MyExternalReplicable.FindByObject(entity).FindStateGroup<MyEntityPhysicsStateGroup>();
            }
            return null;
        }

        static bool IsValid(IMyEntity entity, bool shapeCast)
        {
            var grid = entity as MyCubeGrid;
            if (grid != null && (grid.CubeBlocks.Count <= MinBlockCount || grid.GridSizeEnum == MyCubeSize.Small))
                return false; // Ignored small grids

            if (shapeCast)
                return true; // All other entities valid (except small grids discarded above)
            else
                return entity is MyVoxelBase || entity is MyCubeGrid; // Only voxels and grids valid (except small discarded above)
        }

        static MyEntityPhysicsStateGroup SphereCast(MyEntity entity, float radius, float distance)
        {
            if (DEBUG_DRAW)
            {
                VRageRender.MyRenderProxy.DebugDrawCapsule(entity.WorldMatrix.Translation, entity.PositionComp.GetPosition() + entity.WorldMatrix.Down * distance, radius, Color.Red, false, false);
            }

            // Sphere cast under character
            Vector3D target = entity.PositionComp.GetPosition() + entity.WorldMatrix.Down * distance;
            HkSphereShape shape = new HkSphereShape(radius);
            MatrixD transform = entity.WorldMatrix;
            var dist = MyPhysics.CastShapeReturnContactBodyData(target, shape, ref transform, MyPhysics.CollisionLayers.CollisionLayerWithoutCharacter, 0);
            if (dist.HasValue)
            {
                var hitEntity = dist.Value.HkHitInfo.GetHitEntity();
                if (!IsValid(hitEntity, false))
                    return null;

                return FindPhysics(hitEntity);
            }
            return null;
        }

        static MyEntityPhysicsStateGroup SphereBounds(MyCharacter character, float radius)
        {
            if (DEBUG_DRAW)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(character.PositionComp.GetPosition(), radius, Color.Green, 1, false, false);
            }

            // Sphere with radius 5m around character
            BoundingSphereD sphere = new BoundingSphereD(character.PositionComp.GetPosition(), radius);
            var list = MyEntities.GetEntitiesInSphere(ref sphere);
            try
            {
                MyEntityPhysicsStateGroup closest = null;
                float distanceSq = float.MaxValue;

                foreach (var entity in list)
                {
                    if (!IsValid(entity, false))
                        continue;

                    float distSq = (float)entity.PositionComp.WorldAABB.DistanceSquared(sphere.Center);
                    if (distSq < distanceSq && distSq < (radius * radius))
                    {
                        var phys = FindPhysics(entity);
                        if (phys != null)
                        {
                            closest = phys;
                            distanceSq = distSq;
                        }
                    }
                }
                return closest;
            }
            finally
            {
                list.Clear();
            }
        }
    }
}
