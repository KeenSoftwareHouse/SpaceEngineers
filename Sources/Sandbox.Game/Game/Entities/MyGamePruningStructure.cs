using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Entities
{
    using MyDynamicAABBTree = VRageMath.MyDynamicAABBTree;
    using Sandbox.Common;

    // For space queries on all entities (including children, invisible objects and objects without physics)
    public static class MyGamePruningStructure
    {
        // A tree for each query type.
        // If you query for a specific type, consider adding a new QueryFlag and AABBTree (so that you don't have to filter the result afterwards).
        static MyDynamicAABBTreeD m_aabbTree;
        static MyDynamicAABBTreeD m_topMostEntitiesTree;
        static MyDynamicAABBTreeD m_voxelMapsTree;

        static List<Type> TopMostEntitiesTypes = new List<Type>() 
        { 
            typeof(MyPlanet),
            typeof(MyMeteor), 
            typeof(Sandbox.Game.Entities.Character.MyCharacter), 
            typeof(MyCubeGrid),
            typeof(Sandbox.Game.Weapons.MyMissile), 
            typeof(MyVoxelMap),
            typeof(MyFloatingObject),
        };       

        static MyGamePruningStructure()
        {
            Init();
        }

        public static MyDynamicAABBTreeD GetPrunningStructure()
        {
            return m_aabbTree;
        }

      
        static void Init()
        {
            m_aabbTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);
            m_topMostEntitiesTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);
            m_voxelMapsTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);
        }

        static BoundingBoxD GetEntityAABB(MyEntity entity)
        {
            BoundingBoxD bbox = entity.PositionComp.WorldAABB;

            //Include entity velocity to be able to hit fast moving objects
            if (entity.Physics != null)
            {
                bbox = bbox.Include(entity.WorldMatrix.Translation + entity.Physics.LinearVelocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 5);
            }

            return bbox;
        }

        public static void Add(MyEntity entity)
        {
            if (entity.GamePruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED) return;  // already inserted

            BoundingBoxD bbox = GetEntityAABB(entity);
            if (bbox.Size == Vector3D.Zero) return;  // don't add entities with zero bounding boxes

            entity.GamePruningProxyId = m_aabbTree.AddProxy(ref bbox, entity, 0);         
                          
            if(TopMostEntitiesTypes.Contains(entity.GetType()))
            {
                entity.TopMostPruningProxyId = m_topMostEntitiesTree.AddProxy(ref bbox, entity, 0);
            }

            var voxelMap = entity as MyVoxelBase;
            if (voxelMap != null)
            {
                voxelMap.VoxelMapPruningProxyId = m_voxelMapsTree.AddProxy(ref bbox, entity, 0);
            }
        }

        public static void Remove(MyEntity entity)
        {
            if (entity.GamePruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_aabbTree.RemoveProxy(entity.GamePruningProxyId);
                entity.GamePruningProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }                      
       

            var voxelMap = entity as MyVoxelBase;
            if (voxelMap != null && voxelMap.VoxelMapPruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_voxelMapsTree.RemoveProxy(voxelMap.VoxelMapPruningProxyId);
                voxelMap.VoxelMapPruningProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }

            if (entity.TopMostPruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_topMostEntitiesTree.RemoveProxy(entity.TopMostPruningProxyId);
                entity.TopMostPruningProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }
        }

        public static void Clear()
        {
            Init();
            m_aabbTree.Clear();
            m_voxelMapsTree.Clear();
            m_topMostEntitiesTree.Clear();
        }

        public static void Move(MyEntity entity)
        {
            if (entity.GamePruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD bbox = GetEntityAABB(entity);

                if (bbox.Size == Vector3D.Zero)  // remove entities with zero bounding boxes
                {
                    Remove(entity);
                    return;
                }

                m_aabbTree.MoveProxy(entity.GamePruningProxyId, ref bbox, Vector3D.Zero);

                var voxelMap = entity as MyVoxelBase;
                if (voxelMap != null)
                {
                    m_voxelMapsTree.MoveProxy(voxelMap.VoxelMapPruningProxyId, ref bbox, Vector3D.Zero);
                }
    
                if (entity.TopMostPruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
                {
                    m_topMostEntitiesTree.MoveProxy(entity.TopMostPruningProxyId, ref bbox, Vector3D.Zero);
                }
            }
        }

        public static void GetAllEntitiesInBox<T>(ref BoundingBoxD box, List<T> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInBox");
            m_aabbTree.OverlapAllBoundingBox<T>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllTopMostEntitiesInBox<T>(ref BoundingBoxD box, List<T> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllSensableEntitiesInBox");
            m_topMostEntitiesTree.OverlapAllBoundingBox<T>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllVoxelMapsInBox(ref BoundingBoxD box, List<MyVoxelBase> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllVoxelMapsInBox");
            m_voxelMapsTree.OverlapAllBoundingBox<MyVoxelBase>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllEntitiesInSphere<T>(ref BoundingSphereD sphere, List<T> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInSphere");
            m_aabbTree.OverlapAllBoundingSphere<T>(ref sphere, result, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllVoxelMapsInSphere(ref BoundingSphereD sphere, List<MyVoxelBase> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllVoxelMapsInSphere");
            m_voxelMapsTree.OverlapAllBoundingSphere<MyVoxelBase>(ref sphere, result, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }
    
        public static void GetAllTargetsInSphere<T>(ref BoundingSphereD sphere, List<T> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllTargetsInSphere");
            m_topMostEntitiesTree.OverlapAllBoundingSphere<T>(ref sphere, result, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllEntitiesInRay<T>(ref LineD ray, List<MyLineSegmentOverlapResult<T>> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInRay");
            m_aabbTree.OverlapAllLineSegment<T>(ref ray, result);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void DebugDraw()
        {
            //BoundingBox box = new BoundingBox(new Vector3(-10000), new Vector3(10000));
            //var ents = GetAllEntitiesInBox(ref box);
            var result = new List<MyEntity>();
            var resultAABBs = new List<BoundingBoxD>();
            m_aabbTree.GetAll(result, true, resultAABBs);
            for (int i = 0; i < result.Count; i++)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(resultAABBs[i], Vector3.One, 1, 1, false);
            }
        }
    }
}

