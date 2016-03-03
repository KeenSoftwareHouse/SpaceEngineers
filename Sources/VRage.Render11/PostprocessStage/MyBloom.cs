using System.Diagnostics;
using SharpDX.Direct3D;

namespace VRageRender
{
    class MyBloom : MyImmediateRC
    {
        static ComputeShaderId m_bloomShader;
        static ComputeShaderId m_downscaleShader;
        static ComputeShaderId m_blurH;
        static ComputeShaderId m_blurV;

        const int m_numthreads = 8;

        internal static void Init()
        {
            var threadMacro = new[] { new ShaderMacro("NUMTHREADS", 8) };
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
            m_bloomShader = MyShaders.CreateCs("bloom_init.hlsl",threadMacro);
            m_downscaleShader = MyShaders.CreateCs("bloom_downscale.hlsl", threadMacro);
            m_blurH = MyShaders.CreateCs("bloom_blur_h.hlsl", threadMacro);
            m_blurV = MyShaders.CreateCs("bloom_blur_v.hlsl", threadMacro);
        }

        internal static MyBindableResource Run(MyBindableResource src, MyBindableResource avgLum)
        {
            var buffer = MyCommon.GetObjectCB(16);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref MyRender11.Settings.MiddleGrey);
            mapping.WriteAndPosition(ref MyRender11.Settings.LuminanceExposure);
            mapping.WriteAndPosition(ref MyRender11.Settings.BloomExposure);
            mapping.WriteAndPosition(ref MyRender11.Settings.BloomMult);
            mapping.Unmap();

            RC.CSSetCB(0, MyCommon.FrameConstants);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            RC.BindUAV(0, MyRender11.HalfScreenUavHDR);
            RC.BindSRVs(0, src, avgLum);

            RC.SetCS(m_bloomShader);

            var size = MyRender11.HalfScreenUavHDR.GetSize();
            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(m_downscaleShader);

            size = MyRender11.QuarterScreenUavHDR.GetSize();
            RC.BindUAV(0, MyRender11.QuarterScreenUavHDR);
            RC.BindSRV(0, MyRender11.HalfScreenUavHDR);
            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            size = MyRender11.EighthScreenUavHDR.GetSize();
            RC.BindUAV(0, MyRender11.EighthScreenUavHDR);
            RC.BindSRV(0, MyRender11.QuarterScreenUavHDR);
            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(m_blurH);
            RC.BindUAV(0, MyRender11.EighthScreenUavHDRHelper);
            RC.BindSRV(0, MyRender11.EighthScreenUavHDR);
            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(m_blurV);
            RC.BindUAV(0, MyRender11.EighthScreenUavHDR);
            RC.BindSRV(0, MyRender11.EighthScreenUavHDRHelper);
            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            return MyRender11.EighthScreenUavHDR;
        }
    }
}
