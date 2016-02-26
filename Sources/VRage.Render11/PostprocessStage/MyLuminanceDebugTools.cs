using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    class MyHdrDebugTools : MyImmediateRC
    {
        static PixelShaderId m_drawHistogram;
        static PixelShaderId m_drawTonemapping;
        static ComputeShaderId m_buildHistogram;
        static RwTexId m_histogram;

        const int m_numthreads = 8;

        internal static int NumThreads { get { return m_numthreads; } }

        internal static void Init()
        {
            m_buildHistogram = MyShaders.CreateCs("histogram.hlsl", new[] { new ShaderMacro("NUMTHREADS", 8) });
            m_drawHistogram = MyShaders.CreatePs("data_visualization_histogram.hlsl");
            m_drawTonemapping = MyShaders.CreatePs("data_visualization_tonemapping.hlsl");

            m_histogram = MyRwTextures.CreateUav1D(513, SharpDX.DXGI.Format.R32_UInt, "histogram");
        }

        internal static void CreateHistogram(ShaderResourceView texture, Vector2I resolution, int samples)
        {
            RC.DeviceContext.ClearUnorderedAccessView(m_histogram.Uav, Int4.Zero);
            RC.DeviceContext.ComputeShader.Set(m_buildHistogram);
            RC.DeviceContext.ComputeShader.SetShaderResource(0, texture);
            RC.DeviceContext.ComputeShader.SetUnorderedAccessView(0, m_histogram.Uav);

            var buffer = MyCommon.GetObjectCB(16);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref resolution.X);
            mapping.WriteAndPosition(ref resolution.Y);
            mapping.Unmap();
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            RC.DeviceContext.Dispatch((resolution.X + m_numthreads - 1) / m_numthreads, (resolution.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.DeviceContext.ComputeShader.Set(null);
        }

        internal static void DisplayHistogram(RenderTargetView rtv, ShaderResourceView avgLumSrv)
        {
            RC.DeviceContext.PixelShader.SetShaderResources(0, m_histogram.ShaderView, avgLumSrv);
            RC.DeviceContext.PixelShader.Set(m_drawHistogram);
            RC.DeviceContext.OutputMerger.SetRenderTargets(rtv);
            MyScreenPass.DrawFullscreenQuad(new MyViewport(64, 64, 512, 64));

            RC.DeviceContext.PixelShader.Set(m_drawTonemapping);
            MyScreenPass.DrawFullscreenQuad(new MyViewport(64, 128, 512, 64));
        }
    }
}
