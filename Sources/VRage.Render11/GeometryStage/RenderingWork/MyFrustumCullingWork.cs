﻿using System.Diagnostics;
using ParallelTasks;
using VRage;
using VRageMath;

namespace VRageRender
{
    [PooledObject]
    internal class MyFrustumCullingWork : IPrioritizedWork
    {
        private MyFrustumCullQuery m_query;
        private MyDynamicAABBTreeD m_renderables;

        internal void Init(MyFrustumCullQuery query, MyDynamicAABBTreeD renderables)
        {
            Debug.Assert(query.List.Count == 0, "List not cleared before use");
            Debug.Assert(query.IsInsideList.Count == 0, "IsInsideList not cleared before use");
            m_query = query;
            m_renderables = renderables;
        }

        [PooledObjectCleaner]
        public static void Cleanup(MyFrustumCullingWork frustumCullingWork)
        {
            frustumCullingWork.Cleanup();
        }

        internal void Cleanup()
        {
            m_query = null;
            m_renderables = null;
        }

        public WorkPriority Priority
        {
            get { return WorkPriority.Normal; }
        }

        public void DoWork()
        {
            ProfilerShort.Begin("DoCullWork");

            var frustum = m_query.Frustum;

            if (m_query.SmallObjects.HasValue)
            {
                if (MyRender11.Settings.DrawNonMergeInstanced)
                {
                    m_renderables.OverlapAllFrustum<MyCullProxy>(ref frustum, m_query.List, m_query.IsInsideList,
                        m_query.SmallObjects.Value.ProjectionDir, m_query.SmallObjects.Value.ProjectionFactor, m_query.SmallObjects.Value.SkipThreshold,
                        0, false);
                }

                if (MyRender11.Settings.DrawMergeInstanced)
                {
                    MyScene.GroupsDBVH.OverlapAllFrustum<MyCullProxy_2>(ref frustum, m_query.List2, m_query.IsInsideList2,
                        m_query.SmallObjects.Value.ProjectionDir, m_query.SmallObjects.Value.ProjectionFactor, m_query.SmallObjects.Value.SkipThreshold,
                        0, false);
                }
            }
            else
            {
                if (MyRender11.Settings.DrawNonMergeInstanced)
                    m_renderables.OverlapAllFrustum<MyCullProxy>(ref frustum, m_query.List, m_query.IsInsideList, 0, false);

                if(MyRender11.Settings.DrawMergeInstanced)
                    MyScene.GroupsDBVH.OverlapAllFrustum<MyCullProxy_2>(ref frustum, m_query.List2, m_query.IsInsideList2, 0, false);
            }

            ProfilerShort.End();
        }

        public WorkOptions Options
        {
            get { return Parallel.DefaultOptions; }
        }
    }
}
