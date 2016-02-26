using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpDX.Direct3D;
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
            m_cs = MyShaders.CreateCs("tonemapping.hlsl", new[] { new ShaderMacro("NUMTHREADS", 8) });
            m_csSkip = MyShaders.CreateCs("tonemapping.hlsl", new[] { new ShaderMacro("NUMTHREADS", 8), new ShaderMacro("DISABLE_TONEMAPPING", null) });
        }

        internal static void Run(MyBindableResource dst, MyBindableResource src, MyBindableResource avgLum, MyBindableResource bloom, bool enableTonemapping = true)
        {
            //Debug.Assert(src.GetSize() == dst.GetSize());

            var buffer = MyCommon.GetObjectCB(16);
            var mapping = MyMapping.MapDiscard(buffer);
            mapping.WriteAndPosition(ref MyRender11.Settings.MiddleGrey);
            mapping.WriteAndPosition(ref MyRender11.Settings.LuminanceExposure);
            mapping.WriteAndPosition(ref MyRender11.Settings.BloomExposure);
            mapping.WriteAndPosition(ref MyRender11.Settings.BloomMult);
            mapping.Unmap();

            RC.CSSetCB(0, MyCommon.FrameConstants);
            RC.CSSetCB(1, MyCommon.GetObjectCB(16));

            RC.BindUAV(0, dst);
            RC.BindSRVs(0, src, avgLum, bloom);

            RC.DeviceContext.ComputeShader.SetSampler(0, MyRender11.m_defaultSamplerState);

            if (enableTonemapping)
            {
                RC.SetCS(m_cs);
            }
            else
            {
                RC.SetCS(m_csSkip);
            }

            var size = dst.GetSize();
            RC.DeviceContext.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);

            ComputeShaderId.TmpUav[0] = null;
            RC.DeviceContext.ComputeShader.SetUnorderedAccessViews(0, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
            RC.SetCS(null);
        }
    }
}
