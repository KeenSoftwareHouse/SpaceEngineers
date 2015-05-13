using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    class MyLBufferResolve : MyImmediateRC
    {
        static ComputeShaderId m_cs;

        const int m_numthreads = 8;

        //internal static void RecreateShadersForSettings()
        //{
        //    m_cs = MyShaderFactory.CreateCS("custom_resolve.hlsl", "resolve_lbuffer", MyShaderHelpers.FormatMacros(MyRender11.ShaderMultisamplingDefine(), "NUMTHREADS 8"));
        //}

        internal static void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
            m_cs = MyShaders.CreateCs("custom_resolve.hlsl", "resolve_lbuffer", MyShaderHelpers.FormatMacros("NUMTHREADS 8"));
        }

        internal static void Run(MyBindableResource dst, MyBindableResource src, MyBindableResource stencil)
        {
            RC.BindUAV(0, dst);
            RC.BindSRV(0, src, stencil);

            RC.SetCS(m_cs);

            var size = dst.GetSize();
            RC.Context.Dispatch((size.X + m_numthreads - 1) / m_numthreads, (size.Y + m_numthreads - 1) / m_numthreads, 1);
            RC.SetCS(null);
        }
    }
}
