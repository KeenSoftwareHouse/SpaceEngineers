using ParallelTasks;
using System.Collections.Generic;
using VRage.Render11.GeometryStage2.Instancing;
using VRageMath;
using VRageRender;

namespace VRage.Render11.GeometryStage2.Culling
{
    class MyCpuFrustumCullPass
    {
        internal BoundingFrustumD Frustum { get; set; }
        internal readonly List<MyCpuCulledEntity> List = new List<MyCpuCulledEntity>();
        internal readonly List<bool> IsInsideList = new List<bool>();
        internal int PassId = 0;
    }

    class MyCpuFrustumCullPasses
    {
        List<MyCpuFrustumCullPass> m_passes = new List<MyCpuFrustumCullPass>();
        public int Count { get; private set; }

        public void Clear()
        {
            for (int i = 0; i < Count; i++)
                m_passes[i].List.Clear();
            Count = 0;
        }

        public MyCpuFrustumCullPass Allocate()
        {
            if (Count == m_passes.Count)
            {
                m_passes.Add(new MyCpuFrustumCullPass());
            }
            Count++;
            return m_passes[Count - 1];
        }

        public MyCpuFrustumCullPass At(int index)
        {
            MyRenderProxy.Assert(index < Count);
            return m_passes[index];
        }
    }

    [PooledObject]
    class MyCpuFrustumCullingWork : IPrioritizedWork
    {
        MyCpuFrustumCullPass m_cpuFrustumCullPass;
        MyDynamicAABBTreeD m_renderables;
        List<MyInstanceComponent> m_visibleInstances;

        internal void Init(MyCpuFrustumCullPass pass, MyDynamicAABBTreeD renderables, List<MyInstanceComponent> out_visibleInstances)
        {
            m_cpuFrustumCullPass = pass;
            m_renderables = renderables;
            m_visibleInstances = out_visibleInstances;
        }

        [PooledObjectCleaner]
        public static void Cleanup(MyCpuFrustumCullingWork frustumCullingWork)
        {
            frustumCullingWork.Cleanup();
        }

        internal void Cleanup()
        {
            m_cpuFrustumCullPass = null;
            m_renderables = null;
        }

        public WorkPriority Priority
        {
            get { return WorkPriority.VeryHigh; }
        }

        public void DoWork(WorkData workData = null)
        {
            var frustum = m_cpuFrustumCullPass.Frustum;
            if (MyRender11.Settings.DrawNonMergeInstanced)
                m_renderables.OverlapAllFrustum<MyCpuCulledEntity>(ref frustum, m_cpuFrustumCullPass.List, m_cpuFrustumCullPass.IsInsideList, 0, false);

            m_visibleInstances.Clear();
            foreach (var cullEntity in m_cpuFrustumCullPass.List)
            {
                MyInstanceComponent instance = cullEntity.Owner;
                if (instance.IsVisible(m_cpuFrustumCullPass.PassId))
                    m_visibleInstances.Add(cullEntity.Owner);
            }
        }

        public WorkOptions Options
        {
            get { return Parallel.DefaultOptions; }
        }
    }
}
