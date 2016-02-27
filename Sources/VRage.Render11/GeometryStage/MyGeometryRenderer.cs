using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRageMath;
using Vector3 = VRageMath.Vector3;

namespace VRageRender
{
    struct MyCullingSmallObjects
    {
        internal Vector3 ProjectionDir;
        internal float ProjectionFactor;
        internal float SkipThreshold;
    }

    class MyGeometryRenderer
    {
        internal const string DEFAULT_OPAQUE_PASS = "gbuffer";
        internal const string DEFAULT_DEPTH_PASS = "depth";
        internal const string DEFAULT_FORWARD_PASS = "forward";
        internal const string DEFAULT_HIGHLIGHT_PASS = "highlight";

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

        // Adds to commandLists the command lists containing the rendering commands for the renderables given in renderablesDBVH
        internal void Render(Queue<CommandList> commandLists, bool updateEnvironmentMap = false)
        {
            ProfilerShort.Begin("MyGeometryRenderer.Render");
            MyGpuProfiler.IC_BeginBlock("MyGeometryRenderer.Render");

            ProfilerShort.Begin("Culling");
            PrepareFrame();

            ProfilerShort.Begin("Prepare culling");
            var shadowmapQueries = m_shadowHandler.PrepareQueries();
            MyVisibilityCuller.PrepareCullQuery(m_cullQuery, shadowmapQueries, updateEnvironmentMap);

            ProfilerShort.BeginNextBlock("Perform culling");
            m_visibilityCuller.PerformCulling(m_cullQuery, m_renderablesDBVH);
            ProfilerShort.End();
            ProfilerShort.End();

            ProfilerShort.BeginNextBlock("Record command lists");
            m_renderingDispatcher.RecordCommandLists(m_cullQuery, commandLists);

            ProfilerShort.BeginNextBlock("Send output messages");
            SendOutputMessages(m_cullQuery);

            ProfilerShort.BeginNextBlock("Frame cleanup");
            EndFrame();

            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();    // End function block
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
