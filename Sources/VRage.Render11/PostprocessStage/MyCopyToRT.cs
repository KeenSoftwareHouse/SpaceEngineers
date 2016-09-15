using VRage.Render11.Common;
using VRage.Render11.Resources;

namespace VRageRender
{
    class MyCopyToRT : MyImmediateRC
    {
        static PixelShaderId m_copyPs;
        static PixelShaderId m_clearAlphaPs;

        internal static PixelShaderId CopyPs { get { return m_copyPs; } }

        internal static void Init()
        {
            m_copyPs = MyShaders.CreatePs("Postprocess/PostprocessCopy.hlsl");
            m_clearAlphaPs = MyShaders.CreatePs("Postprocess/PostprocessClearAlpha.hlsl");
        }

        internal static void Run(IRtvBindable destination, ISrvBindable source, bool alphaBlended = false, MyViewport? customViewport = null)
        {
            if (alphaBlended)
                RC.SetBlendState(MyBlendStateManager.BlendAlphaPremult);
            else
                RC.SetBlendState(null);
            //context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            RC.SetInputLayout(null);
            RC.PixelShader.Set(m_copyPs);
        
            //context.OutputMerger.SetTargets(null as DepthStencilView, target);
            //context.PixelShader.SetShaderResource(0, resource);
        
            RC.SetRtv(destination);
            RC.PixelShader.SetSrv(0, source);

            MyScreenPass.DrawFullscreenQuad(customViewport ?? new MyViewport(destination.Size.X, destination.Size.Y));
        }

        internal static void ClearAlpha(IRtvBindable destination)
        {
            RC.SetBlendState(MyBlendStateManager.BlendAdditive);

            RC.SetInputLayout(null);
            RC.PixelShader.Set(m_clearAlphaPs);

            RC.SetRtv(destination);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(destination.Size.X, destination.Size.Y));

            RC.SetBlendState(null);
        }
    }
}
