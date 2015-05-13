using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    class MyLuminanceDebugTools : MyImmediateRC
    {
        static PixelShaderId m_drawHistogram;
        static ComputeShaderId m_buildHistogram;
        static RwTexId m_histogram;

        const int m_numthreads = 8;

        internal static int NumThreads { get { return m_numthreads; } }

        internal static void Init()
        {
            m_buildHistogram = MyShaders.CreateCs("histogram.hlsl", "build_histogram", MyShaderHelpers.FormatMacros("NUMTHREADS 8"));
            m_drawHistogram = MyShaders.CreatePs("data_visualization.hlsl", "display_histogram");

            m_histogram = MyRwTextures.CreateUav1D(512, SharpDX.DXGI.Format.R32_UInt, "histogram");
        }

        internal static void CreateHistogram(ShaderResourceView texture, Vector2I resolution, int samples)
        {
            RC.Context.ClearUnorderedAccessView(m_histogram.Uav, Int4.Zero);
            RC.Context.ComputeShader.Set(m_buildHistogram);
            RC.Context.ComputeShader.SetShaderResource(0, texture);
            RC.Context.ComputeShader.SetUnorderedAccessView(0, m_histogram.Uav);

            var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(16));
            mapping.stream.Write((uint)resolution.X);
            mapping.stream.Write((uint)resolution.Y);
            mapping.Unmap();
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            RC.Context.Dispatch((resolution.X + m_numthreads - 1) / m_numthreads, (resolution.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.Context.ComputeShader.Set(null);
        }

        internal static void DisplayHistogram(RenderTargetView rtv)
        {
            RC.Context.PixelShader.SetShaderResource(0, m_histogram.ShaderView);
            RC.Context.PixelShader.Set(m_drawHistogram);
            RC.Context.OutputMerger.SetRenderTargets(rtv);
            MyScreenPass.DrawFullscreenQuad(new MyViewport(64, 64, 512, 64));
        }
    }
}
