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

        internal static void RunWithStencil(IRtvBindable destinationResource, ISrvBindable sourceResource, IBlendState blendState, IDepthStencilState depthStencilState, int stencilMask)
        {
            RC.SetDepthStencilState(depthStencilState, stencilMask);
            RC.SetBlendState(blendState);
            RC.SetRasterizerState(null);
            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, destinationResource);
            RC.PixelShader.SetSrv(0, sourceResource);
            RC.PixelShader.Set(m_copyPixelShader);

            DrawFullscreenQuad();
            RC.SetBlendState(null);
        }

        internal static void RunWithPixelStencilTest(IRtvBindable dst, ISrvBindable src, IBlendState bs = null, bool inverseTest = false)
        {
            RC.SetDepthStencilState(null);
            RC.SetBlendState(bs);
            RC.SetRasterizerState(null);
            RC.SetRtv(dst);
            RC.PixelShader.SetSrv(0, src);
            RC.PixelShader.SetSrv(1, MyGBuffer.Main.DepthStencil.SrvStencil);
            if (!inverseTest)
                RC.PixelShader.Set(m_stencilTestPixelShader);
            else
                RC.PixelShader.Set(m_stencilInverseTestPixelShader);

            DrawFullscreenQuad();
            RC.SetBlendState(null);
        }
    }
}
