using SharpDX.Direct3D11;

namespace VRageRender
{
    class MyBlendTargets : MyScreenPass
    {
        static PixelShaderId m_copyPixelShader = PixelShaderId.NULL;
        static PixelShaderId m_stencilTestPixelShader = PixelShaderId.NULL;
        static PixelShaderId m_stencilInverseTestPixelShader = PixelShaderId.NULL;

        internal static void Init()
        {
            m_copyPixelShader = MyShaders.CreatePs("postprocess_copy.hlsl");
            m_stencilTestPixelShader = MyShaders.CreatePs("postprocess_copy_stencil.hlsl");
            m_stencilInverseTestPixelShader = MyShaders.CreatePs("postprocess_copy_inversestencil.hlsl");
        }

        internal static void Run(MyBindableResource dst, MyBindableResource src, BlendState bs = null)
        {
            RC.SetBS(bs);
            RC.SetRS(null);
            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, dst);
            RC.BindSRV(0, src);
            RC.SetPS(m_copyPixelShader);

            DrawFullscreenQuad();
            RC.SetBS(null);
        }

        internal static void RunWithStencil(MyBindableResource destinationResource, MyBindableResource sourceResource, BlendState blendState, DepthStencilState depthStencilState, int stencilMask)
        {
            RC.SetDS(depthStencilState, stencilMask);
            RC.SetBS(blendState);
            RC.SetRS(null);
            RC.BindDepthRT(MyGBuffer.Main.DepthStencil, DepthStencilAccess.ReadOnly, destinationResource);
            RC.BindSRV(0, sourceResource);
            RC.SetPS(m_copyPixelShader);

            DrawFullscreenQuad();
            RC.SetBS(null);
        }

        internal static void RunWithPixelStencilTest(MyBindableResource dst, MyBindableResource src, BlendState bs = null, bool inverseTest = false)
        {
            RC.SetDS(null);
            RC.SetBS(bs);
            RC.SetRS(null);
            RC.BindDepthRT(null, DepthStencilAccess.ReadOnly, dst);
            RC.BindSRV(0, src);
            RC.BindSRV(1, MyGBuffer.Main.DepthStencil.Stencil);
            if (!inverseTest)
                RC.SetPS(m_stencilTestPixelShader);
            else
                RC.SetPS(m_stencilInverseTestPixelShader);

            DrawFullscreenQuad();
            RC.SetBS(null);
        }
    }
}
