﻿using System.Diagnostics;
using SharpDX.Direct3D;

namespace VRageRender
{
    class MyImmediateRC
    {
        internal static MyRenderContext RC => MyRenderContextPool.Immediate;
    }

    internal delegate void OnSettingsChangedDelegate();

    class MyScreenPass : MyImmediateRC
    {
        internal static VertexShaderId m_fullscreenQuadVS;

        internal static void Init()
        {
            m_fullscreenQuadVS = MyShaders.CreateVs("postprocess.hlsl", "fullscreen");
        }

        internal static void DrawFullscreenQuad(MyViewport ? customViewport = null)
        {
            if(customViewport.HasValue)
            {
                RC.Context.Rasterizer.SetViewport(customViewport.Value.OffsetX, customViewport.Value.OffsetY, customViewport.Value.Width, customViewport.Value.Height);
            }
            else
            {
                RC.Context.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            }

            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetIL(null);
            RC.SetVS(m_fullscreenQuadVS);
            RC.Context.Draw(3, 0);
        }

        internal static void RunFullscreenPixelFreq(params MyBindableResource [] RTs)
        {
            if(MyRender11.MultisamplingEnabled)
            {
                RC.SetDS(MyDepthStencilState.TestAAEdge, 0);
            }
            RC.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadOnly, RTs);
            DrawFullscreenQuad();
            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetDS(MyDepthStencilState.DefaultDepthState);
            }
        }

        internal static void RunFullscreenSampleFreq(params MyBindableResource[] RTs)
        {
            Debug.Assert(MyRender11.MultisamplingEnabled);
            RC.SetDS(MyDepthStencilState.TestAAEdge, 0x80);
            RC.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadOnly, RTs);
            DrawFullscreenQuad();
            RC.SetDS(MyDepthStencilState.DefaultDepthState);
        }
    }
}
