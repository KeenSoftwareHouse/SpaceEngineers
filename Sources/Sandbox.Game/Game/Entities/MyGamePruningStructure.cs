using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Entities
{
    using MyDynamicAABBTree = VRageMath.MyDynamicAABBTree;
    using Sandbox.Common;
    using System.Diagnostics;
    using VRage.Collections;

    // For space queries on all entities (including children, invisible objects and objects without physics)
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class MyGamePruningStructure : MySessionComponentBase
    {
        // A tree for each query type.
        // If you query for a specific type, consider adding a new QueryFlag and AABBTree (so that you don't have to filter the result afterwards).
        static MyDynamicAABBTreeD m_topMostEntitiesTree;
        static MyDynamicAABBTreeD m_voxelMapsTree;

        static VRage.FastResourceLock m_movedLock = new VRage.FastResourceLock();

        static MyGamePruningStructure()
        {
            Init();
        }

        static void Init()
        {
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
            Debug.Assert(entity.Parent == null, "Only topmost entities");

            if (entity.TopMostPruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED) return;  // already inserted

            BoundingBoxD bbox = GetEntityAABB(entity);
            if (bbox.Size == Vector3D.Zero) return;  // don't add entities with zero bounding boxes

            entity.TopMostPruningProxyId = m_topMostEntitiesTree.AddProxy(ref bbox, entity, 0);

            var voxelMap = entity as MyVoxelBase;
            if (voxelMap != null)
            {
                voxelMap.VoxelMapPruningProxyId = m_voxelMapsTree.AddProxy(ref bbox, entity, 0);
            }
        }

        public static void Remove(MyEntity entity)
        {
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
            Debug.Assert(m_topMostEntitiesTree != null && m_voxelMapsTree != null);
            m_voxelMapsTree.Clear();
            m_topMostEntitiesTree.Clear();
        }


        private static HashSet<MyEntity> m_moved = new HashSet<MyEntity>();
        private static HashSet<MyEntity> m_movedUpdate = new HashSet<MyEntity>();
        //private static MyConcurrentHashSet<MyEntity> m_moved = new MyConcurrentHashSet<MyEntity>();
        public static void Move(MyEntity entity)
        {
            Debug.Assert(entity.InScene, "Moving entity in prunning structure, but entity not in scene");
            m_movedLock.AcquireExclusive();
            m_moved.Add(entity);
            m_movedLock.ReleaseExclusive();
        }

        private static void MoveInternal(MyEntity entity)
        {
            if (entity.Parent != null)
                return;
            VRage.ProfilerShort.Begin(string.Format("Move:{0}", (entity.GetTopMostParent() == entity ? "Topmost" : "Child")));
            if (entity.TopMostPruningProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD bbox = GetEntityAABB(entity);

                if (bbox.Size == Vector3D.Zero)  // remove entities with zero bounding boxes
                {
                    Remove(entity);
                    VRage.ProfilerShort.End();
                    return;
                }

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
            VRage.ProfilerShort.End();
        }

        private static void Update()
        {
            MySandboxGame.AssertUpdateThread();
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::UpdateInternal");
            m_movedLock.AcquireExclusive();
            var x = m_moved;
            m_moved = m_movedUpdate;
            m_movedLock.ReleaseExclusive();
            m_movedUpdate = x;
            foreach (var moved in m_movedUpdate)
                MoveInternal(moved);
            m_movedUpdate.Clear();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        public static void GetAllEntitiesInBox(ref BoundingBoxD box, List<MyEntity> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInBox");

            m_topMostEntitiesTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);
            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Hierarchy != null)
                    result[i].Hierarchy.QueryAABB(ref box, result);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllTopMostEntitiesInBox(ref BoundingBoxD box, List<MyEntity> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllSensableEntitiesInBox");
            m_topMostEntitiesTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllVoxelMapsInBox(ref BoundingBoxD box, List<MyVoxelBase> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllVoxelMapsInBox");
            m_voxelMapsTree.OverlapAllBoundingBox<MyVoxelBase>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllEntitiesInSphere(ref BoundingSphereD sphere, List<MyEntity> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInSphere");

            m_topMostEntitiesTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Hierarchy != null)
                    result[i].Hierarchy.QuerySphere(ref sphere, result);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllTopMostEntitiesInSphere(ref BoundingSphereD sphere, List<MyEntity> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllTopMostEntitiesInSphere");
            m_topMostEntitiesTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllVoxelMapsInSphere(ref BoundingSphereD sphere, List<MyVoxelBase> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllVoxelMapsInSphere");
            m_voxelMapsTree.OverlapAllBoundingSphere<MyVoxelBase>(ref sphere, result, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllTargetsInSphere(ref BoundingSphereD sphere, List<MyEntity> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllTargetsInSphere");

            m_topMostEntitiesTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Hierarchy != null)
                    result[i].Hierarchy.QuerySphere(ref sphere, result);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllEntitiesInRay(ref LineD ray, List<MyLineSegmentOverlapResult<MyEntity>> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInRay");
            m_topMostEntitiesTree.OverlapAllLineSegment<MyEntity>(ref ray, result);
            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Element.Hierarchy != null)
                    result[i].Element.Hierarchy.QueryLine(ref ray, result);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void DebugDraw()
        {
            //BoundingBox box = new BoundingBox(new Vector3(-10000), new Vector3(10000));
            //var ents = GetAllEntitiesInBox(ref box);
            var result = new List<MyEntity>();
            var resultAABBs = new List<BoundingBoxD>();
            m_topMostEntitiesTree.GetAll(result, true, resultAABBs);
            using (var batch = VRageRender.MyRenderProxy.DebugDrawBatchAABB(MatrixD.Identity, Color.White, false, false))
            {
                for (int i = 0; i < result.Count; i++)
                {
                    var aabb = resultAABBs[i];
                    batch.Add(ref aabb);
                }
            }
        }

        public override void Simulate()
        {
            base.Simulate();
            Update();
        }
    }
}

