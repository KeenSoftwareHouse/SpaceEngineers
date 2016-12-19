using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageRender;

namespace VRage.Render11.LightingStage
{
    class MyDepthResolve : MyScreenPass
    {
        static PixelShaderId m_ps;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("Postprocess/DepthResolve.hlsl");
        }

        internal static void Run(IDepthStencil dst, IDepthStencil src)
        {
            RC.PixelShader.Set(m_ps);
            RC.SetRtv(dst, MyDepthStencilAccess.ReadWrite);
            RC.PixelShader.SetSrv(0, src.SrvDepth);
            DrawFullscreenQuad();
        }
    }
}
