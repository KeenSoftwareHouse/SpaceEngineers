using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;

using VRage.Collections;
using VRage.Profiler;
using VRage.Render11.GeometryStage2;
using VRage.Render11.LightingStage.Shadows;using VRageMath;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{
    struct MyCullingSmallObjects
    {
        internal float ProjectionFactor;
        internal float SkipThreshold;
    }

    class MyGeometryRenderer
    {
        private readonly MyDynamicAABBTreeD m_renderablesDBVH;
        private readonly MyShadows m_shadowHandler;

        private readonly MyCullQuery m_cullQuery;
        private readonly MyVisibilityCuller m_visibilityCuller;

        private readonly MyRenderingDispatcher m_renderingDispatcher;

        internal MyGeometryRenderer(MyDynamicAABBTreeD renderablesDBVH, MyShadows shadowHandler)
        {
            m_renderablesDBVH = renderablesDBVH;
            m_shadowHandler = shadowHandler;
            m_cullQuery = new MyCullQuery();
            m_visibilityCuller = new MyFrustumCuller();
            m_renderingDispatcher = new MyRenderingDispatcher();
        }

        private void PrepareFrame()
        {
        }

        private void EndFrame()
        {
            m_cullQuery.Reset();
        }

        internal MyCullQuery PrepareCullQuery(bool updateEnvironmentMap = false)
        {
            PrepareFrame();

            var shadowmapQueries = m_shadowHandler.PrepareQueries();
            MyVisibilityCuller.PrepareCullQuery(m_cullQuery, shadowmapQueries, updateEnvironmentMap);
            
            return m_cullQuery;
        }

        // Adds to commandLists the command lists containing the rendering commands for the renderables given in renderablesDBVH
        internal void Render(Queue<CommandList> commandLists)
        {
            ProfilerShort.Begin("Perform culling");
            m_visibilityCuller.PerformCulling(m_cullQuery, m_renderablesDBVH);

            ProfilerShort.BeginNextBlock("Record command lists");
            m_renderingDispatcher.RecordCommandLists(m_cullQuery, commandLists);

            ProfilerShort.BeginNextBlock("Send output messages");
            SendOutputMessages(m_cullQuery);

            ProfilerShort.BeginNextBlock("Frame cleanup");
            EndFrame();
            ProfilerShort.End();
        }

        // Sends information about the visible objects in cullQuery to MyRenderProxy
        void SendOutputMessages(MyCullQuery cullQuery)
        {
            ProfilerShort.Begin("SendOutputMessages");

            ProfilerShort.Begin("FrustumEntities");
            for (int cullQueryIndex = 0; cullQueryIndex < cullQuery.Size; ++cullQueryIndex )
            {
                var frustumQuery = cullQuery.FrustumCullQueries[cullQueryIndex];
                foreach (MyCullProxy cullProxy in frustumQuery.List)
                {
                    MyRenderProxy.VisibleObjectsWrite.Add(cullProxy.OwnerID);
                }
            }
            ProfilerShort.End();

            ProfilerShort.End();
        }
    }
}
