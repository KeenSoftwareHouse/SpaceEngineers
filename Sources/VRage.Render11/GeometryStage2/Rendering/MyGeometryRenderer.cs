using System.Collections.Generic;
using ParallelTasks;
using VRage.Generics;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Culling;
using VRage.Render11.GeometryStage2.Instancing;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageRender;
using Matrix = VRageMath.Matrix;

namespace VRage.Render11.GeometryStage2.Rendering
{
    class MyGeometryRenderer: IManager, IManagerUnloadData
    {
        MyGBufferPass m_gbufferPass;
        MyObjectsPool<MyDepthPass> m_depthPassesPool;

        List<Task> m_tmpParallelTasks = new List<Task>();
        List<MyCpuFrustumCullingWork> m_tmpCullingWork; 
        MyCpuFrustumCullPasses m_tmpCullPasses;
        List<MyRenderPass> m_tmpRenderPasses;
        List<MyInstanceComponent>[] m_tmpVisibleInstances;
        public bool IsLodUpdateEnabled = true;

        void AllocateInternal()
        {
            m_gbufferPass = new MyGBufferPass();
            m_depthPassesPool = new MyObjectsPool<MyDepthPass>(1);

            m_tmpCullingWork = new List<MyCpuFrustumCullingWork>();
            m_tmpCullPasses = new MyCpuFrustumCullPasses();
            m_tmpRenderPasses = new List<MyRenderPass>();

            m_tmpVisibleInstances = new List<MyInstanceComponent>[MyPassIdResolver.AllPassesCount];
            for (int i = 0; i < m_tmpVisibleInstances.Length; i++)
                m_tmpVisibleInstances[i] = new List<MyInstanceComponent>();
        }

        public MyGeometryRenderer()
        {
            AllocateInternal();
        }

        void IManagerUnloadData.OnUnloadData()
        {
            // Is it really needed to clear all the data? They will be reused in the next session.
            // If it should be, the buffers in passes need to be disposed (so far no mechanism for this)
            //AllocateInternal();
        }
   
        int GetPassId(VRageRender.MyGBufferPass gbufferPass)
        {
            return 0;
        }

        int GetPassId(VRageRender.MyDepthPass oldDepthPass)
        {
            if (oldDepthPass.IsCascade)
            {
                return MyPassIdResolver.GetCascadeDepthPassId(oldDepthPass.FrustumIndex);
            }
            else
            {
                return MyPassIdResolver.GetSingleDepthPassId(oldDepthPass.FrustumIndex);
            }
        }

        void InitFrustumCullPasses(MyCullQuery cullQuery, MyCpuFrustumCullPasses cullPasses)
        {
            cullPasses.Clear();
            for (int i = 0; i < cullQuery.Size; i++)
            {
                MyRenderingPass renderingPass = cullQuery.RenderingPasses[i];
                MyFrustumCullQuery frustumCullQuery = cullQuery.FrustumCullQueries[i];

                if (renderingPass is VRageRender.MyGBufferPass)
                {
                    MyCpuFrustumCullPass cpuFrustumCullPass = cullPasses.Allocate();
                    cpuFrustumCullPass.Frustum = frustumCullQuery.Frustum;
                    cpuFrustumCullPass.IsInsideList.Clear();
                    cpuFrustumCullPass.List.Clear();
                    cpuFrustumCullPass.PassId = GetPassId((VRageRender.MyGBufferPass)renderingPass);
                }

                if (renderingPass is VRageRender.MyDepthPass)
                {
                    MyCpuFrustumCullPass cpuFrustumCullPass = cullPasses.Allocate();
                    cpuFrustumCullPass.Frustum = frustumCullQuery.Frustum;
                    cpuFrustumCullPass.IsInsideList.Clear();
                    cpuFrustumCullPass.List.Clear();
                    cpuFrustumCullPass.PassId = GetPassId((VRageRender.MyDepthPass)renderingPass);
                }
            }
        }

        public void DispatchCullPasses(MyCpuFrustumCullPasses cpuFrustumCullPasses, List<MyInstanceComponent>[] out_visibleInstances)
        {
            for (int frustumQueryIndex = 1; frustumQueryIndex < cpuFrustumCullPasses.Count; ++frustumQueryIndex)
            {
                var cullWork = MyObjectPoolManager.Allocate<MyCpuFrustumCullingWork>();
                m_tmpCullingWork.Add(cullWork);
                int passId = cpuFrustumCullPasses.At(frustumQueryIndex).PassId;
                cullWork.Init(cpuFrustumCullPasses.At(frustumQueryIndex), MyManagers.HierarchicalCulledEntities.AabbTree,
                    out_visibleInstances[passId]);
                m_tmpParallelTasks.Add(Parallel.Start(cullWork));
            }

            if (cpuFrustumCullPasses.Count != 0)
            {
                ProfilerShort.Begin("Culling in the render thread");
                var cullWork = MyObjectPoolManager.Allocate<MyCpuFrustumCullingWork>();
                m_tmpCullingWork.Add(cullWork);
                int passId = cpuFrustumCullPasses.At(0).PassId;
                cullWork.Init(cpuFrustumCullPasses.At(0), MyManagers.HierarchicalCulledEntities.AabbTree,
                    out_visibleInstances[passId]);
                cullWork.DoWork();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("WaitingForOtherTasks");
            foreach (Task cullingTask in m_tmpParallelTasks)
            {
                cullingTask.Wait();
            }
            m_tmpParallelTasks.Clear();
            ProfilerShort.End();

            foreach (MyCpuFrustumCullingWork cullWork in m_tmpCullingWork)
            {
                MyObjectPoolManager.Deallocate(cullWork);
            }
            m_tmpCullingWork.Clear();
        }

        void SendOutputMessages(List<MyInstanceComponent>[] visibleInstances)
        {
            List<MyInstanceComponent> visibleInstancesPass0 = visibleInstances[0];
            foreach (var instance in visibleInstancesPass0)
                MyRenderProxy.VisibleObjectsWrite.Add(instance.Owner.ID);
        }

        void InitRenderPasses(MyCullQuery cullQuery, List<MyRenderPass> renderPasses)
        {
            renderPasses.Clear();
            foreach (var query in cullQuery.RenderingPasses)
            {
                if (query == null)
                    continue;

                Matrix matrix = query.ViewProjection;
                MyViewport viewport = query.Viewport;

                if (query is VRageRender.MyGBufferPass)
                {
                    VRageRender.MyGBufferPass oldGBufferPass = (VRageRender.MyGBufferPass)query;
                    MyGBuffer gbuffer = oldGBufferPass.GBuffer;
                    MyGBufferPass gbufferPass;
                    int passId = GetPassId(oldGBufferPass);
                    m_gbufferPass.Init(passId, matrix, viewport, gbuffer);
                    renderPasses.Add(m_gbufferPass);
                }

                if (query is VRageRender.MyDepthPass)
                {
                    VRageRender.MyDepthPass oldDepthPass = (VRageRender.MyDepthPass)query;
                    IDsvBindable dsv = oldDepthPass.Dsv;
                    MyDepthPass depthPass;
                    bool isCascade = oldDepthPass.IsCascade;
                    int passId = GetPassId(oldDepthPass);
                    m_depthPassesPool.AllocateOrCreate(out depthPass);
                    depthPass.Init(passId, matrix, viewport, dsv, isCascade, oldDepthPass.DebugName);
                    renderPasses.Add(depthPass);
                }
            }
        }

        void Draw(List<MyRenderPass> renderPasses, List<MyInstanceComponent>[] visibleInstances, IGeometrySrvStrategy srvStrategy)
        {
            ProfilerShort.Begin("Preparing");
            foreach (var pass in renderPasses)
                pass.InitWork(visibleInstances[pass.PassId], srvStrategy);

            if (MyRender11.MultithreadedRenderingEnabled && MyDebugGeometryStage2.EnableParallelRendering)
            {
                for (int i = 1; i < renderPasses.Count; i++)
                {
                    var pass = renderPasses[i];
                    m_tmpParallelTasks.Add(Parallel.Start(pass));
                }

                ProfilerShort.BeginNextBlock("Recording commands");
                if (renderPasses.Count != 0)
                {
                    MyRenderPass renderPasss0 = renderPasses[0];
                    renderPasss0.DoWork();
                }

                ProfilerShort.BeginNextBlock("Waiting for tasks");
                foreach (Task cullingTask in m_tmpParallelTasks)
                    cullingTask.Wait();
                m_tmpParallelTasks.Clear();
            }
            else
            {
                ProfilerShort.BeginNextBlock("Recording commands");
                foreach (var pass in renderPasses)
                    pass.DoWork();
            }

            ProfilerShort.BeginNextBlock("Executing command lists");
            foreach (var pass in renderPasses)
                pass.PostprocessWork();
            ProfilerShort.End();
        }

        void ClearRenderPasses(List<MyRenderPass> renderPasses)
        {
            m_depthPassesPool.DeallocateAll();
            renderPasses.Clear();
        }
        
        public void Render(MyCullQuery cullQuery, IGeometrySrvStrategy srvStrategy)
        {
            ProfilerShort.Begin("Preculling");
            InitFrustumCullPasses(cullQuery, m_tmpCullPasses);

            ProfilerShort.BeginNextBlock("Culling");
            DispatchCullPasses(m_tmpCullPasses, m_tmpVisibleInstances);

            ProfilerShort.BeginNextBlock("Send output messages");
            SendOutputMessages(m_tmpVisibleInstances);

            ProfilerShort.BeginNextBlock("Prerender");
            InitRenderPasses(cullQuery, m_tmpRenderPasses);

            ProfilerShort.BeginNextBlock("Update LOD selection");
            if (IsLodUpdateEnabled)
                MyManagers.Instances.UpdateLods(m_tmpRenderPasses, m_tmpVisibleInstances);
   
            ProfilerShort.BeginNextBlock("Rendering");
            Draw(m_tmpRenderPasses, m_tmpVisibleInstances, srvStrategy);

            ProfilerShort.BeginNextBlock("Clearing");
            ClearRenderPasses(m_tmpRenderPasses);
            ProfilerShort.End();
        }

        public void RenderGlass()
        {
            m_gbufferPass.DrawGlass(MyRender11.RC);
        }
    }
}
