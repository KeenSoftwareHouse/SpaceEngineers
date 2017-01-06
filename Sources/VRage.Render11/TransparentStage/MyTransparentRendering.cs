using System.Collections.Generic;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Rendering;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;

namespace VRageRender
{
    internal class MyTransparentRendering : MyImmediateRC
    {
        private const float PROXIMITY_DECALS_SQ_TH = 2.25f;         // 1.5m

        private static HashSet<uint> m_glassWithDecals;
        private static PixelShaderId m_psResolve;
        static PixelShaderId m_psOverlappingHeatMap;
        static PixelShaderId m_psOverlappingHeatMapInGrayscale;
        private static float[] m_distances;

        internal static void Init()
        {
            MyGPUParticleRenderer.Init();

            m_glassWithDecals = new HashSet<uint>();
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

            RC.SetRtvs(MyGBuffer.Main.ResolvedDepthStencil, MyDepthStencilAccess.ReadOnly, accumTarget, coverageTarget);
        }

        private static void SetupStandard()
        {
            RC.SetScreenViewport();
            RC.SetBlendState(MyBlendStateManager.BlendAlphaPremult);

            RC.SetRtvs(MyGBuffer.Main.ResolvedDepthStencil, MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);
        }

        private static void SetupTargets(IUavTexture accumTarget, IUavTexture coverageTarget, bool clear)
        {
            if (MyRender11.DebugOverrides.OIT)
                SetupOIT(accumTarget, coverageTarget, clear);
            else
                SetupStandard();
        }

        private static void ResolveOIT(ISrvBindable accumTarget, ISrvBindable coverageTarget)
        {
            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            RC.SetBlendState(MyBlendStateManager.BlendWeightedTransparencyResolve);
            RC.PixelShader.Set(m_psResolve);
            RC.SetRtv(MyGBuffer.Main.LBuffer);
            RC.PixelShader.SetSrv(0, accumTarget);
            RC.PixelShader.SetSrv(1, coverageTarget);
            MyScreenPass.DrawFullscreenQuad(null);
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

        internal static void Render(IRtvTexture gbuffer1Copy)
        {
            IBorrowedUavTexture accumTarget = MyManagers.RwTexturesPool.BorrowUav("MyTransparentRendering.AccumTarget", Format.R16G16B16A16_Float);
            IBorrowedUavTexture coverageTarget = MyManagers.RwTexturesPool.BorrowUav("MyTransparentRendering.CoverageTarget", Format.R16_UNorm);

            ProfilerShort.Begin("Atmosphere");
            MyGpuProfiler.IC_BeginBlock("Atmosphere");
            if (MyRender11.DebugOverrides.Atmosphere)
                MyAtmosphereRenderer.RenderGBuffer();

            ProfilerShort.BeginNextBlock("Clouds");
            MyGpuProfiler.IC_BeginNextBlock("Clouds");
            if (MyRender11.DebugOverrides.Clouds)
                MyCloudRenderer.Render();

            var depthResource = MyGBuffer.Main.ResolvedDepthStencil;

            // setup weighted blended OIT targets + blend states
            if (MyRender11.Settings.DrawBillboards)
            {
                ProfilerShort.BeginNextBlock("Billboards");
                MyGpuProfiler.IC_BeginNextBlock("Billboards");
                MyBillboardRenderer.Gather();

                MyBillboardRenderer.RenderAdditveBottom(depthResource.SrvDepth);

                SetupTargets(accumTarget, coverageTarget, true);

                MyBillboardRenderer.RenderStandard(depthResource.SrvDepth);
            }
            else SetupTargets(accumTarget, coverageTarget, true);

            ProfilerShort.BeginNextBlock("GPU Particles");
            MyGpuProfiler.IC_BeginNextBlock("GPU Particles");
            if (MyRender11.DebugOverrides.GPUParticles)
                MyGPUParticleRenderer.Run(depthResource.SrvDepth, MyGBuffer.Main.GBuffer1);

            // Render decals on transparent surfaces in 2 steps: first far, second proximity
            if (MyRender11.Settings.DrawGlass)
            {
                ProfilerShort.BeginNextBlock("Static glass");
                MyGpuProfiler.IC_BeginNextBlock("Static glass");
                m_glassWithDecals.Clear();
                MyStaticGlassRenderer.Render(HandleGlass);

                float intervalMax = MyScreenDecals.VISIBLE_DECALS_SQ_TH;
                for (int it = 0; it < m_distances.Length; it++)
                {
                    float intervalMin = m_distances[it];

                    ProfilerShort.BeginNextBlock("Glass - Depth Only");
                    MyGpuProfiler.IC_BeginNextBlock("Glass - Depth Only");
                    //TODO: This code should properly render glass decals, that they are visible when looking through window on another window
                    // Anyway, it is italian code and it doesnt work. Solve after Beta
                    bool glassFound = MyStaticGlassRenderer.RenderGlassDepthOnly(depthResource, gbuffer1Copy, intervalMin, intervalMax);

                    // if (glassFound)
                    {
                        SetupTargets(accumTarget, coverageTarget, false);

                        ProfilerShort.BeginNextBlock("Render decals - Transparent");
                        MyGpuProfiler.IC_BeginNextBlock("Render decals - Transparent");
                        MyScreenDecals.Draw(gbuffer1Copy, true/*, m_glassWithDecals*/);
                    }

                    intervalMax = intervalMin;
                }

                ProfilerShort.BeginNextBlock("New static glass");
                //MyManagers.GeometryRenderer.RenderGlass();

            }

            if (IsUsedOverlappingHeatMap())
                DisplayOverlappingHeatMap(accumTarget, coverageTarget, MyRender11.Settings.DisplayTransparencyHeatMapInGrayscale);

            MyGpuProfiler.IC_BeginNextBlock("OIT Resolve");
            // resolve weighted blended OIT in  accum / coverage to LBuffer
            if (MyRender11.DebugOverrides.OIT)
                ResolveOIT(accumTarget, coverageTarget);

            ProfilerShort.BeginNextBlock("Billboards");
            MyGpuProfiler.IC_BeginNextBlock("Billboards");
            if (MyRender11.Settings.DrawBillboards)
                MyBillboardRenderer.RenderAdditveTop(depthResource.SrvDepth);

            RC.SetRtv(null);

            MyGpuProfiler.IC_EndBlock();

            coverageTarget.Release();
            accumTarget.Release();

            ProfilerShort.End();
        }

        /// <returns>True if window glass decals and is not too far</returns>
        static bool HandleGlass(MyRenderCullResultFlat result, double viewDistanceSquared)
        {
            uint id = result.RenderProxy.Parent.Owner.ID;
            if (MyScreenDecals.HasEntityDecals(id) && viewDistanceSquared < MyScreenDecals.VISIBLE_DECALS_SQ_TH)
            {
                m_glassWithDecals.Add(id);
                return true;
            }

            return false;
        }
    }
}
