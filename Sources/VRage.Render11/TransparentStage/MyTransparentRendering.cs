using System;
using System.Collections.Generic;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using VRage;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;

namespace VRageRender
{
    internal class MyTransparentRendering : MyImmediateRC
    {
        public const float PROXIMITY_DECALS_SQ_TH = 2.25f;         // 1.5m

        private static HashSet<uint> m_windowsWithDecals;
        private static PixelShaderId m_psResolve;
        static PixelShaderId m_psOverlappingHeatMap;
        static PixelShaderId m_psOverlappingHeatMapInGrayscale;
        private static float[] m_distances;

        internal static void Init()
        {
            MyGPUParticleRenderer.Init();

            m_windowsWithDecals = new HashSet<uint>();
            if (m_distances == null)
                m_distances = new float[] { PROXIMITY_DECALS_SQ_TH, 0 };

            m_psResolve = MyShaders.CreatePs("Transparent/OIT/Resolve.hlsl");
            m_psOverlappingHeatMap = MyShaders.CreatePs("Transparent/ResolveAccumIntoHeatMap.hlsl");
            m_psOverlappingHeatMapInGrayscale = MyShaders.CreatePs("Transparent/ResolveAccumIntoHeatMap.hlsl", new ShaderMacro[] { new ShaderMacro("USE_GRAYSCALE", null), });
        }
        internal static void OnDeviceReset()
        {
            MyGPUParticleRenderer.OnDeviceReset();
        }
        internal static void OnDeviceEnd()
        {
            MyGPUParticleRenderer.OnDeviceEnd();
        }
        internal static void OnSessionEnd()
        {
            MyGPUParticleRenderer.OnSessionEnd();
        }

        static void SetupOIT(IUavTexture accumTarget, IUavTexture coverageTarget, bool clear)
        {
            RC.SetScreenViewport();
            RC.SetBlendState(MyBlendStateManager.BlendWeightedTransparency);
            if (clear)
            {
                RC.ClearRtv(accumTarget, new SharpDX.Color4(0, 0, 0, 0));
                RC.ClearRtv(coverageTarget, new SharpDX.Color4(1, 1, 1, 1));//0,0,0,0));
            }

            if (MyRender11.MultisamplingEnabled)
                RC.SetRtvs(MyScreenDependants.m_resolvedDepth, MyDepthStencilAccess.ReadOnly, accumTarget, coverageTarget);
            else
                RC.SetRtvs(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, accumTarget, coverageTarget);
        }
        internal static void SetupStandard()
        {
            RC.SetScreenViewport();
            RC.SetBlendState(MyBlendStateManager.BlendAlphaPremult);

            if (MyRender11.MultisamplingEnabled)
                RC.SetRtvs(MyScreenDependants.m_resolvedDepth, MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);
            else
                RC.SetRtvs(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);
        }

        internal static void SetupTargets(IUavTexture accumTarget, IUavTexture coverageTarget, bool clear)
        {
            if (MyRender11.DebugOverrides.OIT)
                SetupOIT(accumTarget, coverageTarget, clear);
            else
                SetupStandard();
        }

        internal static void ResolveOIT(ISrvBindable accumTarget, ISrvBindable coverageTarget)
        {
            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            RC.SetBlendState(MyBlendStateManager.BlendWeightedTransparencyResolve);
            RC.PixelShader.Set(m_psResolve);
            RC.SetRtv(MyGBuffer.Main.LBuffer);
            RC.PixelShader.SetSrv(0, accumTarget);
            RC.PixelShader.SetSrv(1, coverageTarget);
            MyScreenPass.DrawFullscreenQuad(null);
            RC.SetRtv(null);
        }

        static bool IsUsedOverlappingHeatMap()
        {
            return MyRender11.Settings.DisplayTransparencyHeatMap;
        }

        static void DisplayOverlappingHeatMap(IUavTexture accumTarget, IUavTexture coverageTarget, bool useGrayscale)
        {
            IBorrowedRtvTexture heatMap = MyManagers.RwTexturesPool.BorrowRtv("MyTransparentRendering.HeatMap",
                Format.R8G8B8A8_UNorm);
            RC.ClearRtv(heatMap, default(RawColor4));

            RC.SetRtv(heatMap);
            RC.PixelShader.SetSrv(0, accumTarget);
            RC.PixelShader.Set(useGrayscale ? m_psOverlappingHeatMapInGrayscale : m_psOverlappingHeatMap);
            RC.SetBlendState(MyBlendStateManager.BlendAdditive);
            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            MyScreenPass.DrawFullscreenQuad();

            RC.PixelShader.Set(null);
            RC.PixelShader.SetSrv(0, null);
            RC.SetRtv(null);
            SetupOIT(accumTarget, coverageTarget, false);

            MyDebugTextureDisplay.Select(heatMap);
            heatMap.Release();
        }

        internal static void Render()
        {
            IBorrowedUavTexture accumTarget = MyManagers.RwTexturesPool.BorrowUav("MyTransparentRendering.AccumTarget", Format.R16G16B16A16_Float);
            IBorrowedUavTexture coverageTarget = MyManagers.RwTexturesPool.BorrowUav("MyTransparentRendering.CoverageTarget",Format.R16_UNorm);
 
            ProfilerShort.Begin("Atmosphere");
            MyGpuProfiler.IC_BeginBlock("Atmosphere");
            if (MyRender11.DebugOverrides.Atmosphere)
                MyAtmosphereRenderer.Render();

            ProfilerShort.BeginNextBlock("Clouds");
            MyGpuProfiler.IC_BeginNextBlock("Clouds");
            if (MyRender11.DebugOverrides.Clouds)
                MyCloudRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            // setup weighted blended OIT targets + blend states
            SetupTargets(accumTarget, coverageTarget, true);

            IDepthStencil depthResource;
            if (MyRender11.MultisamplingEnabled)
                depthResource = MyScreenDependants.m_resolvedDepth;
            else
                depthResource = MyGBuffer.Main.DepthStencil;

            m_windowsWithDecals.Clear();
            ProfilerShort.BeginNextBlock("Billboards");
            MyGpuProfiler.IC_BeginBlock("Billboards");
            bool resetBindings = MyBillboardRenderer.Gather(HandleWindow);
            if (resetBindings)
                SetupTargets(accumTarget, coverageTarget, false);

            MyBillboardRenderer.Render(depthResource.SrvDepth);
            
            ProfilerShort.BeginNextBlock("GPU Particles");
            MyGpuProfiler.IC_BeginNextBlock("GPU Particles");
            if (MyRender11.DebugOverrides.GPUParticles)
                MyGPUParticleRenderer.Run(depthResource.SrvDepth);
            MyGpuProfiler.IC_EndBlock();

            // Render decals on transparent surfaces in 2 steps: first far, second proximity
            float intervalMax = MyScreenDecals.VISIBLE_DECALS_SQ_TH;
            for (int it = 0; it < m_distances.Length; it++)
            {
                float intervalMin = m_distances[it];

                ProfilerShort.BeginNextBlock("Billboards - Depth Only");
                MyGpuProfiler.IC_BeginBlock("Billboards - Depth Only");
                bool windowsFound = MyBillboardRenderer.RenderWindowsDepthOnly(depthResource, MyGlobalResources.Gbuffer1Copy, intervalMin, intervalMax);
                MyGpuProfiler.IC_EndBlock();

                if (windowsFound)
                {
                    SetupTargets(accumTarget, coverageTarget, false);

                    ProfilerShort.BeginNextBlock("Render decals - Transparent");
                    MyGpuProfiler.IC_BeginBlock("Render decals - Transparent");
                    MyScreenDecals.Draw(true, m_windowsWithDecals);
                    MyGpuProfiler.IC_EndBlock();
                }

                intervalMax = intervalMin;
            }

            if (IsUsedOverlappingHeatMap())
                DisplayOverlappingHeatMap(accumTarget, coverageTarget, MyRender11.Settings.DisplayTransparencyHeatMapInGrayscale);

            MyGpuProfiler.IC_BeginBlock("OIT Resolve");
            // resolve weighted blended OIT in  accum / coverage to LBuffer
            if (MyRender11.DebugOverrides.OIT)
                ResolveOIT(accumTarget, coverageTarget);
            else RC.SetRtv(null);
            MyGpuProfiler.IC_EndBlock();

             coverageTarget.Release();
            accumTarget.Release();

            ProfilerShort.End();
        }

        /// <returns>True if window has decals and is not too far</returns>
        static bool HandleWindow(MyBillboard billboard)
        {
            uint parentID = (uint)billboard.ParentID;
            if (MyScreenDecals.HasEntityDecals(parentID) && billboard.DistanceSquared < MyScreenDecals.VISIBLE_DECALS_SQ_TH)
            {
                m_windowsWithDecals.Add(parentID);
                return true;
            }

            return false;
        }
    }
}
