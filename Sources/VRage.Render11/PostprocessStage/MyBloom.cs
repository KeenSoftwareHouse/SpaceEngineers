using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRageMath;

namespace VRageRender
{
    class MyBloom : MyImmediateRC
    {
        static ComputeShaderId m_bloomShader;
        static ComputeShaderId m_downscaleShader;
        static ComputeShaderId m_blurH;
        static ComputeShaderId m_blurV;

        const int m_numthreads = 8;

        //internal static void RecreateShadersForSettings()
        //{
        //    m_bloomShader = MyShaderFactory.CreateCS("tonemapping.hlsl", "bloom_initial", MyShaderHelpers.FormatMacros(MyRender11.ShaderMultisamplingDefine(), "NUMTHREADS 8"));
        //}

        internal static void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
            m_bloomShader = MyShaders.CreateCs("tonemapping.hlsl", "bloom_initial", MyShaderHelpers.FormatMacros("NUMTHREADS 8"));
            m_downscaleShader = MyShaders.CreateCs("tonemapping.hlsl", "downscale", MyShaderHelpers.FormatMacros("NUMTHREADS 8"));
            m_blurH = MyShaders.CreateCs("tonemapping.hlsl", "blur_h", MyShaderHelpers.FormatMacros("NUMTHREADS 8"));
            m_blurV = MyShaders.CreateCs("tonemapping.hlsl", "blur_v", MyShaderHelpers.FormatMacros("NUMTHREADS 8"));
        }

        internal static MyBindableResource Run(MyBindableResource src, MyBindableResource avgLum)
        {
            var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(16));
            mapping.stream.Write(MyRender11.Settings.MiddleGrey);
            mapping.stream.Write(MyRender11.Settings.LuminanceExposure);
            mapping.stream.Write(MyRender11.Settings.BloomExposure);
            mapping.stream.Write(MyRender11.Settings.BloomMult);
            mapping.Unmap();

            RC.CSSetCB(0, MyCommon.FrameConstants);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            RC.BindUAV(0, MyRender11.m_div2);
            RC.BindSRV(0, src, avgLum);

            RC.SetCS(m_bloomShader);

            var size = MyRender11.m_div2.GetSize();
            RC.Context.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(m_downscaleShader);

            size = MyRender11.m_div4.GetSize();
            RC.BindUAV(0, MyRender11.m_div4);
            RC.BindSRV(0, MyRender11.m_div2);
            RC.Context.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            size = MyRender11.m_div8.GetSize();
            RC.BindUAV(0, MyRender11.m_div8);
            RC.BindSRV(0, MyRender11.m_div4);
            RC.Context.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(m_blurH);
            RC.BindUAV(0, MyRender11.m_div8_1);
            RC.BindSRV(0, MyRender11.m_div8);
            RC.Context.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            RC.SetCS(m_blurV);
            RC.BindUAV(0, MyRender11.m_div8);
            RC.BindSRV(0, MyRender11.m_div8_1);
            RC.Context.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            return MyRender11.m_div8;
        }
    }
}
