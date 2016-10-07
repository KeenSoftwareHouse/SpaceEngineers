using SharpDX;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageMath;

namespace VRageRender
{
    class MyHdrDebugTools : MyImmediateRC
    {
        static PixelShaderId m_drawHistogram;
        static ComputeShaderId m_buildHistogram;

        static PixelShaderId m_psDisplayHdrIntensity;

        const int m_numthreads = 8;

        public static int NumThreads { get { return m_numthreads; } }

        public static void Init()
        {
            m_buildHistogram = MyShaders.CreateCs("Debug/Histogram.hlsl", new[] { new ShaderMacro("NUMTHREADS", 8) });
            m_drawHistogram = MyShaders.CreatePs("Debug/DataVisualizationHistogram.hlsl");

            m_psDisplayHdrIntensity = MyShaders.CreatePs("Debug/DisplayHdrIntensity.hlsl");
        }

        public static IBorrowedUavTexture CreateHistogram(ISrvBindable texture, int samples)
        {
            Vector2I resolution = texture.Size;
            IBorrowedUavTexture histogram = MyManagers.RwTexturesPool.BorrowUav("MyHdrDebugTools.Histogram", 513, 1, SharpDX.DXGI.Format.R32_UInt);

            RC.ClearUav(histogram, Int4.Zero);
            RC.ComputeShader.Set(m_buildHistogram);
            RC.ComputeShader.SetSrv(0, texture);
            RC.ComputeShader.SetUav(0, histogram);

            var buffer = MyCommon.GetObjectCB(16);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref resolution.X);
            mapping.WriteAndPosition(ref resolution.Y);
            mapping.Unmap();
            RC.ComputeShader.SetConstantBuffer(1, MyCommon.GetObjectCB(16));

            RC.Dispatch((resolution.X + m_numthreads - 1) / m_numthreads, (resolution.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.ComputeShader.Set(null);
            return histogram;
        }

        public static void DisplayHistogram(IRtvBindable output, ISrvBindable avgLumSrv, ISrvTexture histogram)
        {
            RC.PixelShader.SetSrvs(0, histogram, avgLumSrv);
            RC.PixelShader.Set(m_drawHistogram);
            RC.SetRtv(output);
            MyScreenPass.DrawFullscreenQuad(new MyViewport(64, 64, 512, 64));
            //m_histogram.Release();
            //m_histogram = null;
        }

        public static void DisplayHdrIntensity(ISrvBindable srv)
        {
            RC.PixelShader.Set(m_psDisplayHdrIntensity);
            RC.PixelShader.SetSrv(5, srv);
            RC.SetBlendState(null);
            IBorrowedRtvTexture outTex = MyManagers.RwTexturesPool.BorrowRtv("MyHdrDebugTools.DisplayHdrIntensity.OutTex", srv.Size.X, srv.Size.Y, Format.B8G8R8X8_UNorm);
            MyScreenPass.RunFullscreenPixelFreq(outTex);
            MyDebugTextureDisplay.Select(outTex);

            RC.PixelShader.SetSrv(5, null);
            RC.SetRtvs(MyGBuffer.Main, MyDepthStencilAccess.ReadOnly);
            outTex.Release();
        }
    }
}
