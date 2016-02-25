using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Resources;

namespace VRageRender
{
    class MyCopyToRT : MyImmediateRC
    {
        static PixelShaderId m_copyPs;
        static PixelShaderId m_clearAlphaPs;

        internal static PixelShaderId CopyPs { get { return m_copyPs; } }

        internal static void Init()
        {
            m_copyPs = MyShaders.CreatePs("postprocess_copy.hlsl");
            m_clearAlphaPs = MyShaders.CreatePs("postprocess_clear_alpha.hlsl");
        }

        internal static void Run(MyBindableResource destination, MyBindableResource source, MyViewport? customViewport = null)
        {
            var context = MyRender11.DeviceContext;
        
            context.OutputMerger.BlendState = null;
            //context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            context.InputAssembler.InputLayout = null;
            context.PixelShader.Set(m_copyPs);
        
            //context.OutputMerger.SetTargets(null as DepthStencilView, target);
            //context.PixelShader.SetShaderResource(0, resource);
        
            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);
            RC.BindSRV(0, source);

            MyScreenPass.DrawFullscreenQuad(customViewport ?? new MyViewport(destination.GetSize().X, destination.GetSize().Y));
        }

        internal static void ClearAlpha(MyBindableResource destination)
        {
            var context = MyRender11.DeviceContext;

            context.OutputMerger.BlendState = MyRender11.BlendAdditive;

            context.InputAssembler.InputLayout = null;
            context.PixelShader.Set(m_clearAlphaPs);

            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(destination.GetSize().X, destination.GetSize().Y));

            context.OutputMerger.BlendState = null;
        }
    }
}
