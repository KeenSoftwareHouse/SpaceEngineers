using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace VRageRender
{
    internal class MyTransparentRendering : MyImmediateRC
    {
        private static PixelShaderId m_psResolve;

        internal static void Init()
        {
            MyGPUParticleRenderer.Init();

            m_psResolve = MyShaders.CreatePs("Transparency/Resolve.hlsl");
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

        internal static void SetupOIT(MyUnorderedAccessTexture accumTarget, MyUnorderedAccessTexture coverageTarget, bool clear)
        {
            RC.SetupScreenViewport();
            RC.SetBS(MyRender11.BlendWeightedTransparency);
            if (clear)
            {
                MyRender11.DeviceContext.ClearRenderTargetView(accumTarget.m_RTV, new SharpDX.Color4(0,0,0,0));
                MyRender11.DeviceContext.ClearRenderTargetView(coverageTarget.m_RTV, new SharpDX.Color4(1,1,1,1));//0,0,0,0));
            }

            if (MyRender11.MultisamplingEnabled)
                RC.BindDepthRT(MyScreenDependants.m_resolvedDepth, DepthStencilAccess.ReadOnly, accumTarget, coverageTarget);
            else RC.BindDepthRT(MyGBuffer.Main.DepthStencil, DepthStencilAccess.ReadOnly, accumTarget, coverageTarget);
        }
        internal static void SetupStandard()
        {
            RC.SetupScreenViewport();
            RC.SetBS(MyRender11.BlendAlphaPremult);

            if (MyRender11.MultisamplingEnabled)
                RC.BindDepthRT(MyScreenDependants.m_resolvedDepth, DepthStencilAccess.ReadOnly, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            else RC.BindDepthRT(MyGBuffer.Main.DepthStencil, DepthStencilAccess.ReadOnly, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
        }

        internal static void ResolveOIT(MyUnorderedAccessTexture accumTarget, MyUnorderedAccessTexture coverageTarget)
        {
            RC.SetDS(MyDepthStencilState.IgnoreDepthStencil);
            RC.SetBS(MyRender11.BlendWeightedTransparencyResolve);
            RC.SetPS(m_psResolve);
            RC.BindDepthRT(null, DepthStencilAccess.ReadOnly, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            RC.BindSRV(0, accumTarget);
            RC.BindSRV(1, coverageTarget);
            MyScreenPass.DrawFullscreenQuad(null);
        }

        internal static void Render(MyUnorderedAccessTexture accumTarget, MyUnorderedAccessTexture coverageTarget)
        {
            ProfilerShort.Begin("Atmosphere");
            MyGpuProfiler.IC_BeginBlock("Atmosphere");
            if (MyRender11.DebugOverrides.Atmosphere)
                MyAtmosphereRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Clouds");
            MyGpuProfiler.IC_BeginBlock("Clouds");
            if (MyRender11.DebugOverrides.Clouds)
                MyCloudRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            // setup weighted blended OIT targets + blend states
            if (MyRender11.DebugOverrides.OIT)
                SetupOIT(accumTarget, coverageTarget, true);
            else SetupStandard();

            MyDepthStencil depthResource;
            if (MyRender11.MultisamplingEnabled)
                depthResource = MyScreenDependants.m_resolvedDepth;
            else
                depthResource = MyGBuffer.Main.DepthStencil;

            ProfilerShort.BeginNextBlock("Billboards");
            MyGpuProfiler.IC_BeginBlock("Billboards");
            MyBillboardRenderer.Render(depthResource.Depth);
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("GPU Particles");
            MyGpuProfiler.IC_BeginBlock("GPU Particles");
            if (MyRender11.DebugOverrides.GPUParticles)
                MyGPUParticleRenderer.Run(depthResource.Depth);
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Billboards - Depth Only");
            MyGpuProfiler.IC_BeginBlock("Billboards - Depth Only");
            bool windowsFound = MyBillboardRenderer.RenderWindowsDepthOnly(depthResource, MyRender11.Gbuffer1Copy);
            MyGpuProfiler.IC_EndBlock();

            if (MyRender11.DebugOverrides.OIT && windowsFound)
                SetupOIT(accumTarget, coverageTarget, false);

            ProfilerShort.BeginNextBlock("Render decals - Transparent");
            MyGpuProfiler.IC_BeginBlock("Render decals - Transparent");
            MyScreenDecals.Draw(true);
            MyGpuProfiler.IC_EndBlock();

            MyGpuProfiler.IC_BeginBlock("OIT Resolve");
            // resolve weighted blended OIT in  accum / coverage to LBuffer
            if (MyRender11.DebugOverrides.OIT)
                ResolveOIT(accumTarget, coverageTarget);
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.End();
        }
    }
}
