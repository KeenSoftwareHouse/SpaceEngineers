using SharpDX;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.LightingStage;
using VRage.Render11.Profiler;
using VRageRender;
using Format = SharpDX.DXGI.Format;

namespace VRage.Render11.Resources
{
    internal class MyGBuffer : MyImmediateRC
    {
        internal const Format LBufferFormat = Format.R11G11B10_Float;

        int m_samplesCount;
        int m_samplesQuality;

        IDepthStencil m_depthStencil, m_resolvedDepthStencil;
        IRtvTexture m_gbuffer0;
        IRtvTexture m_gbuffer1;
        IRtvTexture m_gbuffer2;
        IRtvTexture m_lbuffer;

        public int SamplesCount { get { return m_samplesCount; } }
        public int SamplesQuality { get { return m_samplesQuality; } }

        internal IDepthStencil DepthStencil { get { return m_depthStencil; } }
        internal IDepthStencil ResolvedDepthStencil { get { return MyRender11.MultisamplingEnabled ? m_resolvedDepthStencil : m_depthStencil; } }
        internal IRtvTexture GBuffer0 { get { return m_gbuffer0; } }
        internal IRtvTexture GBuffer1 { get { return m_gbuffer1; } }
        internal IRtvTexture GBuffer2 { get { return m_gbuffer2; } }
        internal IRtvTexture LBuffer { get { return m_lbuffer; } }


        public void ResolveMultisample()
        {
            if (MyRender11.MultisamplingEnabled)
            {
                MyRender11.RC.ClearDsv(m_resolvedDepthStencil, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                MyGpuProfiler.IC_BeginBlock("MarkAAEdges");
                MyAAEdgeMarking.Run();
                MyGpuProfiler.IC_EndBlock();
                MyDepthResolve.Run(m_resolvedDepthStencil, m_depthStencil);
            }
        }

        public IBorrowedRtvTexture GetGbuffer1CopyRtv()
        {
            int width = MyRender11.ResolutionI.X;
            int height = MyRender11.ResolutionI.Y;
            int samples = MyRender11.Settings.User.AntialiasingMode.SamplesCount();

            var gbuffer1Copy = MyManagers.RwTexturesPool.BorrowRtv("MyGlobalResources.Gbuffer1Copy", width, height, Format.R10G10B10A2_UNorm, samples, SamplesQuality);
            RC.CopyResource(GBuffer1, gbuffer1Copy);
            return gbuffer1Copy;
        }

        public IBorrowedDepthStencilTexture GetDepthStencilCopyRtv()
        {
            int width = MyRender11.ResolutionI.X;
            int height = MyRender11.ResolutionI.Y;
            int samples = MyRender11.Settings.User.AntialiasingMode.SamplesCount();

            var depthStencilCopy = MyManagers.RwTexturesPool.BorrowDepthStencil("DepthStencilCopy", width, height, samples, SamplesQuality);
            RC.CopyResource(DepthStencil, depthStencilCopy);
            return depthStencilCopy;
        }

        internal void Resize(int width, int height, int samplesNum, int samplesQuality)
        {
            Release();

            m_samplesCount = samplesNum;
            m_samplesQuality = samplesQuality;

            MyDepthStencilManager dsManager = MyManagers.DepthStencils;
            m_depthStencil = dsManager.CreateDepthStencil("MyGBuffer.DepthStencil", width, height, samplesCount: samplesNum, samplesQuality: samplesQuality);
            if (MyRender11.MultisamplingEnabled)
                m_resolvedDepthStencil = dsManager.CreateDepthStencil("MyGBuffer.ResolvedDepth", width, height);

            MyRwTextureManager rwManager = MyManagers.RwTextures;
            m_gbuffer0 = rwManager.CreateRtv("MyGBuffer.GBuffer0", width, height, Format.R8G8B8A8_UNorm_SRgb,
                samplesNum, samplesQuality);
            m_gbuffer1 = rwManager.CreateRtv("MyGBuffer.GBuffer1", width, height, Format.R10G10B10A2_UNorm,
                samplesNum, samplesQuality);
            m_gbuffer2 = rwManager.CreateRtv("MyGBuffer.GBuffer2", width, height, Format.R8G8B8A8_UNorm,
                samplesNum, samplesQuality);
            m_lbuffer = rwManager.CreateRtv("MyGBuffer.LBuffer", width, height, LBufferFormat,
                samplesNum, samplesQuality);
        }

        internal void Release()
        {
            MyDepthStencilManager dsManager = MyManagers.DepthStencils;
            MyRwTextureManager rwManager = MyManagers.RwTextures;

            dsManager.DisposeTex(ref m_depthStencil);
            dsManager.DisposeTex(ref m_resolvedDepthStencil);
            rwManager.DisposeTex(ref m_gbuffer0);
            rwManager.DisposeTex(ref m_gbuffer1);
            rwManager.DisposeTex(ref m_gbuffer2);
            rwManager.DisposeTex(ref m_lbuffer);
        }

        internal void Clear(VRageMath.Color clearColor)
        {
            RC.ClearDsv(DepthStencil,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, MyRender11.DepthClearValue, 0);

            var v3 = clearColor.ToVector3();
            RC.ClearRtv(m_gbuffer0, new Color4(v3.X, v3.Y, v3.Z, 1));
            RC.ClearRtv(m_gbuffer1, Color4.Black);
            RC.ClearRtv(m_gbuffer2, Color4.Black);
            RC.ClearRtv(m_lbuffer, Color4.Black);
        }

        internal static MyGBuffer Main;
    }
}
