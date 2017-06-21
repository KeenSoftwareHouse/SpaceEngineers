using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Engine.Utils;
using VRage;
using VRage.ModAPI;
using VRage.Profiler;

namespace Sandbox.Game.Entities
{
    using MyDynamicAABBTree = VRageMath.MyDynamicAABBTree;
    using Sandbox.Common;
    using System.Diagnostics;
    using VRage.Collections;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Game.Entity;

    [Flags]
    public enum MyEntityQueryType : byte
    {
        Static  = 0x1,
        Dynamic = 0x2,
        Both    = Static | Dynamic
    }

    public static class MyEntityQueryTypeExtensions
    {
        public static bool HasDynamic(this MyEntityQueryType qtype)
        {
            return (qtype & MyEntityQueryType.Dynamic) != 0;
        }

        public static bool HasStatic(this MyEntityQueryType qtype)
        {
            return (qtype & MyEntityQueryType.Static) != 0;
        }
    }

    // For space queries on all entities (including children, invisible objects and objects without physics)
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    public class MyGamePruningStructure : MySessionComponentBase
    {
        // A tree for each query type.
        // If you query for a specific type, consider adding a new QueryFlag and AABBTree (so that you don't have to filter the result afterwards).
        private static MyDynamicAABBTreeD m_dynamicObjectsTree;
        private static MyDynamicAABBTreeD m_staticObjectsTree;
        private static MyDynamicAABBTreeD m_voxelMapsTree;

        // List of voxel maps scanned when looking for closest voxel.
        [ThreadStatic]
        static List<MyVoxelBase> m_cachedVoxelList;

        static VRage.FastResourceLock m_movedLock = new VRage.FastResourceLock();

        static MyGamePruningStructure()
        {
            Init();
        }

        static void Init()
        {
            m_dynamicObjectsTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);
            m_voxelMapsTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);
            m_staticObjectsTree = new MyDynamicAABBTreeD(Vector3D.Zero);
        }

        static BoundingBoxD GetEntityAABB(MyEntity entity)
        {
            BoundingBoxD bbox = entity.PositionComp.WorldAABB;

            //Include entity velocity to be able to hit fast moving objects
            if (entity.Physics != null)
            {
                bbox = bbox.Include(entity.WorldMatrix.Translation + entity.Physics.LinearVelocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 5);
            }

            return bbox;
        }

        private static bool IsEntityStatic(MyEntity entity)
        {
            return entity.Physics == null || entity.Physics.IsStatic;
        }

        public static void Add(MyEntity entity)
        {
            Debug.Assert(entity.Parent == null || (entity.Flags & EntityFlags.IsGamePrunningStructureObject) != 0, "Only topmost entities");

            if (entity.TopMostPruningProxyId != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED) return;  // already inserted

            BoundingBoxD bbox = GetEntityAABB(entity);
            if (bbox.Size == Vector3D.Zero) return;  // don't add entities with zero bounding boxes


            if (IsEntityStatic(entity))
            {
                entity.TopMostPruningProxyId = m_staticObjectsTree.AddProxy(ref bbox, entity, 0);
                entity.StaticForPruningStructure = true;
            }
            else
            {
                entity.TopMostPruningProxyId = m_dynamicObjectsTree.AddProxy(ref bbox, entity, 0);
                entity.StaticForPruningStructure = false;
            }

            var voxelMap = entity as MyVoxelBase;
            if (voxelMap != null)
            {
                voxelMap.VoxelMapPruningProxyId = m_voxelMapsTree.AddProxy(ref bbox, entity, 0);
            }
        }

        public static void Remove(MyEntity entity)
        {
            var voxelMap = entity as MyVoxelBase;
            if (voxelMap != null && voxelMap.VoxelMapPruningProxyId != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_voxelMapsTree.RemoveProxy(voxelMap.VoxelMapPruningProxyId);
                voxelMap.VoxelMapPruningProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }

            if (entity.TopMostPruningProxyId != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                if (entity.StaticForPruningStructure)
                    m_staticObjectsTree.RemoveProxy(entity.TopMostPruningProxyId);
                else
                    m_dynamicObjectsTree.RemoveProxy(entity.TopMostPruningProxyId);

                entity.TopMostPruningProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }
        }

        public static void Clear()
        {
            Debug.Assert(m_dynamicObjectsTree != null && m_voxelMapsTree != null);
            m_voxelMapsTree.Clear();
            m_dynamicObjectsTree.Clear();
            m_staticObjectsTree.Clear();
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
            if (entity.Parent != null && (entity.Flags & EntityFlags.IsGamePrunningStructureObject) == 0)
                return;
            ProfilerShort.Begin(string.Format("Move:{0}", (entity.GetTopMostParent() == entity ? "Topmost" : "Child")));
            if (entity.TopMostPruningProxyId != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD bbox = GetEntityAABB(entity);

                if (bbox.Size == Vector3D.Zero)  // remove entities with zero bounding boxes
                {
                    Remove(entity);
                    ProfilerShort.End();
                    return;
                }

                var voxelMap = entity as MyVoxelBase;
                if (voxelMap != null)
                {
                    m_voxelMapsTree.MoveProxy(voxelMap.VoxelMapPruningProxyId, ref bbox, Vector3D.Zero);
                }

                if (entity.TopMostPruningProxyId != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
                {
                    bool stat = IsEntityStatic(entity);

                    // Swap trees if necessary.
                    if (stat != entity.StaticForPruningStructure)
                    {
                        if (entity.StaticForPruningStructure)
                        {
                            m_staticObjectsTree.RemoveProxy(entity.TopMostPruningProxyId);
                            entity.TopMostPruningProxyId = m_dynamicObjectsTree.AddProxy(ref bbox, entity, 0);
                        }
                        else
                        {
                            m_dynamicObjectsTree.RemoveProxy(entity.TopMostPruningProxyId);
                            entity.TopMostPruningProxyId = m_staticObjectsTree.AddProxy(ref bbox, entity, 0);
                        }
                        entity.StaticForPruningStructure = stat;
                    }
                    else
                    {
                        if (entity.StaticForPruningStructure)
                            m_staticObjectsTree.MoveProxy(entity.TopMostPruningProxyId, ref bbox, Vector3D.Zero);
                        else
                            m_dynamicObjectsTree.MoveProxy(entity.TopMostPruningProxyId, ref bbox, Vector3D.Zero);
                    }
                }
            }
            ProfilerShort.End();
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

        public static void GetAllEntitiesInBox(ref BoundingBoxD box, List<MyEntity> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInBox");

            if(qtype.HasDynamic())
            m_dynamicObjectsTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);
            if (qtype.HasStatic())
            m_staticObjectsTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);

            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Hierarchy != null)
                    result[i].Hierarchy.QueryAABB(ref box, result);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetTopMostEntitiesInBox(ref BoundingBoxD box, List<MyEntity> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetTopmostEntitiesInBox");
            if (qtype.HasDynamic())
            m_dynamicObjectsTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);
            if (qtype.HasStatic())
            m_staticObjectsTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllTopMostStaticEntitiesInBox(ref BoundingBoxD box, List<MyEntity> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetTopmostEntitiesInBox");
            if (qtype.HasDynamic())
            m_dynamicObjectsTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);
            if (qtype.HasStatic())
            m_staticObjectsTree.OverlapAllBoundingBox<MyEntity>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllEntitiesInSphere(ref BoundingSphereD sphere, List<MyEntity> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInSphere");

            if (qtype.HasDynamic())
            m_dynamicObjectsTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            if (qtype.HasStatic())
            m_staticObjectsTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Hierarchy != null)
                    result[i].Hierarchy.QuerySphere(ref sphere, result);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllTopMostEntitiesInSphere(ref BoundingSphereD sphere, List<MyEntity> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllTopMostEntitiesInSphere");
            if (qtype.HasDynamic())
            m_dynamicObjectsTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            if (qtype.HasStatic())
            m_staticObjectsTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllTargetsInSphere(ref BoundingSphereD sphere, List<MyEntity> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllTargetsInSphere");

            if (qtype.HasDynamic())
            m_dynamicObjectsTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            if (qtype.HasStatic())
            m_staticObjectsTree.OverlapAllBoundingSphere<MyEntity>(ref sphere, result, false);
            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Hierarchy != null)
                    result[i].Hierarchy.QuerySphere(ref sphere, result);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetAllEntitiesInRay(ref LineD ray, List<MyLineSegmentOverlapResult<MyEntity>> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllEntitiesInRay");
            if (qtype.HasDynamic())
            m_dynamicObjectsTree.OverlapAllLineSegment<MyEntity>(ref ray, result);
            if (qtype.HasStatic())
            m_staticObjectsTree.OverlapAllLineSegment<MyEntity>(ref ray, result, false);
            int topmostCount = result.Count;
            for (int i = 0; i < topmostCount; i++)
            {
                if (result[i].Element.Hierarchy != null)
                    result[i].Element.Hierarchy.QueryLine(ref ray, result);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void GetTopmostEntitiesOverlappingRay(ref LineD ray, List<MyLineSegmentOverlapResult<MyEntity>> result, MyEntityQueryType qtype = MyEntityQueryType.Both)
        {
            ProfilerShort.Begin("MyGamePruningStructure::GetAllEntitiesInRay");
            if (qtype.HasDynamic())
                m_dynamicObjectsTree.OverlapAllLineSegment<MyEntity>(ref ray, result);
            if (qtype.HasStatic())
                m_staticObjectsTree.OverlapAllLineSegment<MyEntity>(ref ray, result, false);
            ProfilerShort.End();
        }

        public static void GetVoxelMapsOverlappingRay(ref LineD ray, List<MyLineSegmentOverlapResult<MyVoxelBase>> result)
        {
            ProfilerShort.Begin("MyGamePruningStructure::GetVoxelMapsOverlappingRay");
            m_voxelMapsTree.OverlapAllLineSegment<MyVoxelBase>(ref ray, result);
            ProfilerShort.End();
        }

        public static void GetAproximateDynamicClustersForSize(ref BoundingBoxD container, double clusterSize, List<BoundingBoxD> clusters)
        {
            m_dynamicObjectsTree.GetAproximateClustersForAabb(ref container, clusterSize, clusters);
        }

        #region Voxel Map Tree

        public static void GetAllVoxelMapsInBox(ref BoundingBoxD box, List<MyVoxelBase> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllVoxelMapsInBox");
            m_voxelMapsTree.OverlapAllBoundingBox<MyVoxelBase>(ref box, result, 0, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        /**
         * Get the closest planet overlapping a position.
         * 
         * This will not return anything if the position is not within the bounding box of the planet.
         */

        public static MyPlanet GetClosestPlanet(Vector3D position)
        {
            var bb = new BoundingBoxD(position, position);

            return GetClosestPlanet(ref bb);
        }

        public static MyPlanet GetClosestPlanet(ref BoundingBoxD box)
        {
            if (m_cachedVoxelList == null) m_cachedVoxelList = new List<MyVoxelBase>();

            try
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetClosestPlanet");

                m_voxelMapsTree.OverlapAllBoundingBox<MyVoxelBase>(ref box, m_cachedVoxelList, 0, false);

                MyPlanet planet = null;

                Vector3D center = box.Center;

                double dist = double.PositiveInfinity;
                foreach (var voxel in m_cachedVoxelList)
                {
                    if (voxel is MyPlanet)
                    {
                        var dd = (center - voxel.WorldMatrix.Translation).LengthSquared();
                        if (dd < dist)
                        {
                            dist = dd;
                            planet = (MyPlanet)voxel;
                        }
                    }
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                return planet;
            }
            finally
            {
                m_cachedVoxelList.Clear();
            }
        }

        public static void GetAllVoxelMapsInSphere(ref BoundingSphereD sphere, List<MyVoxelBase> result)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGamePruningStructure::GetAllVoxelMapsInSphere");
            m_voxelMapsTree.OverlapAllBoundingSphere<MyVoxelBase>(ref sphere, result, false);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        #endregion

        public static void DebugDraw()
        {
            //BoundingBox box = new BoundingBox(new Vector3(-10000), new Vector3(10000));
            //var ents = GetAllEntitiesInBox(ref box);
            
            var result = new List<BoundingBoxD>();
            m_dynamicObjectsTree.GetAllNodeBounds(result);
            using (var batch = VRageRender.MyRenderProxy.DebugDrawBatchAABB(MatrixD.Identity, new Color(Color.SkyBlue, 0.05f), false, false))
            {
                foreach (var box in result)
                {
                    var aabb = box;
                    batch.Add(ref aabb);
                }
            }

            result.Clear();
            m_staticObjectsTree.GetAllNodeBounds(result);
            using (var batch = VRageRender.MyRenderProxy.DebugDrawBatchAABB(MatrixD.Identity, new Color(Color.Aquamarine, 0.05f), false, false))
            {
                foreach (var box in result)
                {
                    var aabb = box;
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

