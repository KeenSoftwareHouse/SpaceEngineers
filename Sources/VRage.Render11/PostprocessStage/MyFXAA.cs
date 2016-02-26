using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Resources;

namespace VRageRender
{
    class MyFXAA : MyImmediateRC
    {
        static PixelShaderId m_ps;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("fxaa.hlsl");
        }

        internal static void Run(MyBindableResource destination, MyBindableResource source)
        {
            var context = MyRender11.DeviceContext;

            context.OutputMerger.BlendState = null;

            context.InputAssembler.InputLayout = null;
            context.PixelShader.Set(m_ps);

            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);
            RC.BindSRV(0, source);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(destination.GetSize().X, destination.GetSize().Y));
        }
    }
}