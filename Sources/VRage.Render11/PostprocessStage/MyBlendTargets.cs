using SharpDX.Direct3D11;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;

namespace VRageRender
{
    class MyBlendTargets : MyScreenPass
    {
        static PixelShaderId m_copyPixelShader = PixelShaderId.NULL;
        static PixelShaderId m_stencilTestPixelShader = PixelShaderId.NULL;
        static PixelShaderId m_stencilInverseTestPixelShader = PixelShaderId.NULL;

        internal static void Init()
        {
            m_copyPixelShader = MyShaders.CreatePs("Postprocess/PostprocessCopy.hlsl");
            m_stencilTestPixelShader = MyShaders.CreatePs("Postprocess/PostprocessCopyStencil.hlsl");
            m_stencilInverseTestPixelShader = MyShaders.CreatePs("Postprocess/PostprocessCopyInverseStencil.hlsl");
        }

        internal static void Run(IRtvBindable dst, ISrvBindable src, IBlendState bs = null)
        {
            RC.SetBlendState(bs);
            RC.SetRasterizerState(null);
            RC.SetRtv(dst);
            RC.PixelShader.SetSrv(0, src);
            RC.PixelShader.Set(m_copyPixelShader);

            DrawFullscreenQuad();
            RC.SetBlendState(null);
        }

        internal static void RunWithStencil(IRtvBindable destinationResource, ISrvBindable sourceResource, IBlendState blendState,
            IDepthStencilState depthStencilState = null, int stencilMask = 0x0, IDepthStencil depthStencil = null)
        {
            RC.SetBlendState(blendState);
            RC.SetRasterizerState(null);
            if (depthStencilState == null)
            {
                RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
                RC.SetRtv(null, MyDepthStencilAccess.ReadOnly, destinationResource);
            }
            else
            {
                RC.SetDepthStencilState(depthStencilState, stencilMask);
                RC.SetRtv(depthStencil ?? MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, destinationResource);
            }

            RC.PixelShader.SetSrv(0, sourceResource);
            RC.PixelShader.Set(m_copyPixelShader);

            DrawFullscreenQuad();
            RC.SetBlendState(null);
        }

        internal static void RunWithPixelStencilTest(IRtvBindable dst, ISrvBindable src, IBlendState bs = null,
            bool inverseTest = false, IDepthStencil depthStencil = null)
        {
            RC.SetDepthStencilState(null);
            RC.SetBlendState(bs);
            RC.SetRasterizerState(null);
            RC.SetRtv(dst);
            RC.PixelShader.SetSrv(0, src);
            RC.PixelShader.SetSrv(1, depthStencil == null ? MyGBuffer.Main.DepthStencil.SrvStencil : depthStencil.SrvStencil);
            if (!inverseTest)
                RC.PixelShader.Set(m_stencilTestPixelShader);
            else
                RC.PixelShader.Set(m_stencilInverseTestPixelShader);

            DrawFullscreenQuad();
            RC.SetBlendState(null);
        }
    }
}
