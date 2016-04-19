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

        internal static void SetupOIT(MyUnorderedAccessTexture accumTarget, MyUnorderedAccessTexture coverageTarget)
        {
            RC.SetupScreenViewport();
            RC.SetBS(MyRender11.BlendWeightedTransparency);
            MyRender11.DeviceContext.ClearRenderTargetView(accumTarget.m_RTV, SharpDX.Color4.Black);
            MyRender11.DeviceContext.ClearRenderTargetView(coverageTarget.m_RTV, SharpDX.Color4.White);

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
            RC.SetBS(MyRender11.BlendInvTransparent);
            RC.SetPS(m_psResolve);
            RC.BindDepthRT(null, DepthStencilAccess.ReadOnly, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));
            RC.BindSRV(0, accumTarget);
            RC.BindSRV(1, coverageTarget);
            MyScreenPass.DrawFullscreenQuad(null);
        }

        internal static void Render(MyUnorderedAccessTexture accumTarget, MyUnorderedAccessTexture coverageTarget)
        {
            ProfilerShort.Begin("Billboards");
            MyGpuProfiler.IC_BeginBlock("Billboards");
            if (MyRender11.MultisamplingEnabled)
            {
                MyBillboardRenderer.Render(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), MyScreenDependants.m_resolvedDepth, MyScreenDependants.m_resolvedDepth.Depth);
            }
            else
            {
                MyBillboardRenderer.Render(MyGBuffer.Main.Get(MyGbufferSlot.LBuffer), MyGBuffer.Main.DepthStencil, MyGBuffer.Main.DepthStencil.Depth);
            }
            MyGpuProfiler.IC_EndBlock();

            // setup weighted blended OIT targets + blend states
            if (MyRender11.DebugOverrides.OIT)
                SetupOIT(accumTarget, coverageTarget);
            else SetupStandard();

            ProfilerShort.BeginNextBlock("GPU Particles");
            MyGpuProfiler.IC_BeginBlock("GPU Particles");
            if (MyRender11.DebugOverrides.GPUParticles)
            {
                if (MyRender11.MultisamplingEnabled)
                {
                    MyGPUParticleRenderer.Run(MyScreenDependants.m_resolvedDepth.Depth);
                }
                else
                {
                    MyGPUParticleRenderer.Run(MyGBuffer.Main.DepthStencil.Depth);
                }
            }
            MyGpuProfiler.IC_EndBlock();

            // resolve weighted blended OIT in  accum / coverage to LBuffer
            if (MyRender11.DebugOverrides.OIT)
                ResolveOIT(accumTarget, coverageTarget);

            ProfilerShort.BeginNextBlock("Atmosphere");
            MyGpuProfiler.IC_BeginBlock("Atmosphere");
            if (MyRender11.DebugOverrides.Atmosphere)
                MyAtmosphereRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.BeginNextBlock("Clouds");
            MyGpuProfiler.IC_BeginBlock("Clouds");
            if (MyRender11.DebugOverrides.Clouds)
                MyCloudRenderer.Render();
            MyGpuProfiler.IC_EndBlock();

            ProfilerShort.End();
        }
    }
}
