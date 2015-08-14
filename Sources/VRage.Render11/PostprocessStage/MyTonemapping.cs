using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    class MyToneMapping : MyImmediateRC
    {
        static ComputeShaderId m_cs;
        static ComputeShaderId m_csSkip;

        const int m_numthreads = 8;

        internal static void Init()
        {
            m_cs = MyShaders.CreateCs("tonemapping.hlsl", "tonemapping", MyShaderHelpers.FormatMacros("NUMTHREADS 8"));
            m_csSkip = MyShaders.CreateCs("tonemapping.hlsl", "tonemapping", MyShaderHelpers.FormatMacros("NUMTHREADS 8", "DISABLE_TONEMAPPING"));
        }

        internal static void Run(MyBindableResource dst, MyBindableResource src, MyBindableResource avgLum, MyBindableResource bloom, bool enableTonemapping = true)
        {
            //Debug.Assert(src.GetSize() == dst.GetSize());

            var mapping = MyMapping.MapDiscard(MyCommon.GetObjectCB(16));
            mapping.stream.Write(MyRender11.Settings.MiddleGrey);
            mapping.stream.Write(MyRender11.Settings.LuminanceExposure);
            mapping.stream.Write(MyRender11.Settings.BloomExposure);
            mapping.stream.Write(MyRender11.Settings.BloomMult);
            mapping.Unmap();

            RC.CSSetCB(0, MyCommon.FrameConstants);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            RC.BindUAV(0, dst);
            RC.BindSRV(0, src, avgLum, bloom);

            RC.Context.ComputeShader.SetSampler(0, MyRender11.m_defaultSamplerState);

            if (enableTonemapping)
            {
                RC.SetCS(m_cs);
            }
            else
            {
                RC.SetCS(m_csSkip);
            }

            var size = dst.GetSize();
            RC.Context.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);
            RC.Context.ComputeShader.SetUnorderedAccessViews(0, null as UnorderedAccessView);
            RC.SetCS(null);
        }
    }
}
