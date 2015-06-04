﻿namespace VRageRender
{
    class MyFXAA : MyImmediateRC
    {
        static PixelShaderId m_ps;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("fxaa.hlsl", "fxaa");
        }

        internal static void Run(MyBindableResource destination, MyBindableResource source)
        {
            var context = MyRender11.Context;

            context.OutputMerger.BlendState = null;

            context.InputAssembler.InputLayout = null;
            context.PixelShader.Set(m_ps);

            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, destination);
            RC.BindSRV(0, source);

            MyScreenPass.DrawFullscreenQuad(new MyViewport(destination.GetSize().X, destination.GetSize().Y));
        }
    }
}