using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRageMath;

namespace VRageRender
{
    class MyShadowsResolve : MyScreenPass
    {
        static ComputeShaderId m_gather;
        static ComputeShaderId m_blur_h;
        static ComputeShaderId m_blur_v;
        const int m_numthreads = 8;

        internal static new void Init()
        {
			m_gather = MyShaders.CreateCs("shadows.hlsl", "write_shadow", MyShaderHelpers.FormatMacros("NUMTHREADS " + m_numthreads) + MyRender11.ShaderCascadesNumberHeader());
			m_blur_h = MyShaders.CreateCs("shadows.hlsl", "blur", MyShaderHelpers.FormatMacros("NUMTHREADS " + m_numthreads, MyRender11.ShaderCascadesNumberDefine()));
			m_blur_v = MyShaders.CreateCs("shadows.hlsl", "blur", MyShaderHelpers.FormatMacros("NUMTHREADS " + m_numthreads, "VERTICAL", MyRender11.ShaderCascadesNumberDefine()));
        }

        internal static void Run()
        {

            RC.SetCS(m_gather);
            //RC.BindDepthRT(dst, DepthStencilAccess.ReadWrite, null);
            RC.Context.ComputeShader.SetUnorderedAccessView(0, MyRender11.m_shadowsHelper.Uav);
            
            RC.Context.ComputeShader.SetShaderResources(0, 
                MyRender11.MultisamplingEnabled ? MyScreenDependants.m_resolvedDepth.m_SRV_depth : MyGBuffer.Main.DepthStencil.m_SRV_depth,
                MyGBuffer.Main.DepthStencil.m_SRV_stencil);
            RC.Context.ComputeShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);
            RC.Context.ComputeShader.SetConstantBuffer(0, MyCommon.FrameConstants);
            RC.Context.ComputeShader.SetConstantBuffer(4, MyShadows.m_csmConstants);
            RC.Context.ComputeShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, MyShadows.m_cascadeShadowmapArray.ShaderView);

            var kernel = ((Vector2)MyRender11.ViewportResolution + m_numthreads - 1) / m_numthreads;
            RC.Context.Dispatch((int)kernel.X, (int)kernel.Y, 1);

            RC.SetCS(m_blur_h);
            RC.Context.ComputeShader.SetUnorderedAccessViews(0, MyRender11.m_shadowsHelper1.Uav);
            RC.Context.ComputeShader.SetShaderResource(0, MyRender11.m_shadowsHelper.ShaderView);
            RC.Context.Dispatch((int)kernel.X, (int)kernel.Y, 1);

            RC.Context.ComputeShader.SetShaderResource(0, null);

            RC.SetCS(m_blur_v);
            RC.Context.ComputeShader.SetUnorderedAccessViews(0, MyRender11.m_shadowsHelper.Uav);
            RC.Context.ComputeShader.SetShaderResource(0, MyRender11.m_shadowsHelper1.ShaderView);
            RC.Context.Dispatch((int)kernel.X, (int)kernel.Y, 1);

            RC.Context.ComputeShader.SetUnorderedAccessViews(0, (UnorderedAccessView)null);
        }
    }
}
