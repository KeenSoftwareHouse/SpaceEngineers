using System.Diagnostics;
using ParallelTasks;
using VRage;
using VRage.Profiler;
using VRageMath;

namespace VRageRender
{
    [PooledObject]
#if XB1
    internal class MyFrustumCullingWork : IPrioritizedWork, IMyPooledObjectCleaner
#else // !XB1
    internal class MyFrustumCullingWork : IPrioritizedWork
#endif // !XB1
    {
        private MyFrustumCullQuery m_query;
        private MyDynamicAABBTreeD m_renderables;
        internal long Elapsed { get; private set; }

        internal void Init(MyFrustumCullQuery query, MyDynamicAABBTreeD renderables)
        {
            Debug.Assert(query.List.Count == 0, "List not cleared before use");
            Debug.Assert(query.IsInsideList.Count == 0, "IsInsideList not cleared before use");
            m_query = query;
            m_renderables = renderables;
        }

#if XB1
        public void ObjectCleaner()
        {
            Cleanup();
        }
#else // !XB1
        [PooledObjectCleaner]
        public static void Cleanup(MyFrustumCullingWork frustumCullingWork)
        {
            frustumCullingWork.Cleanup();
        }
#endif // !XB1

        internal void Cleanup()
        {
            m_query = null;
            m_renderables = null;
        }

        public WorkPriority Priority
        {
            get { return WorkPriority.Normal; }
        }

        public void DoWork(WorkData workData = null)
        {
            long Started = Stopwatch.GetTimestamp();
            ProfilerShort.Begin("DoCullWork");

            var frustum = m_query.Frustum;

            if (m_query.SmallObjects.HasValue)
            {
                if (MyRender11.Settings.DrawNonMergeInstanced)
                {
                    m_renderables.OverlapAllFrustum<MyCullProxy>(ref frustum, m_query.List, m_query.IsInsideList,
                        m_query.SmallObjects.Value.ProjectionFactor, m_query.SmallObjects.Value.SkipThreshold,
                        0, false);
                }

                if (MyRender11.Settings.DrawMergeInstanced)
                {
                    MyScene.GroupsDBVH.OverlapAllFrustum<MyCullProxy_2>(ref frustum, m_query.List2, m_query.IsInsideList2,
                        m_query.SmallObjects.Value.ProjectionFactor, m_query.SmallObjects.Value.SkipThreshold,
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
            Elapsed = Stopwatch.GetTimestamp() - Started;
        }

        public WorkOptions Options
        {
            get { return Parallel.DefaultOptions; }
        }
    }
}
