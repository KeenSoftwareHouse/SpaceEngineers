using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRageMath;

namespace VRageRender
{
    class MyImmediateRC
    {
        internal static MyRenderContext RC { get { return MyRenderContext.Immediate; } }
    }

    internal delegate void OnSettingsChangedDelegate();

    class MyScreenPass : MyImmediateRC
    {
        internal static VertexShaderId m_fullscreenQuadVS;

        internal static void Init()
        {
            m_fullscreenQuadVS = MyShaders.CreateVs("postprocess_copy.hlsl");
        }

        internal static void DrawFullscreenQuad(MyViewport ? customViewport = null)
        {
            if(customViewport.HasValue)
            {
                RC.DeviceContext.Rasterizer.SetViewport(customViewport.Value.OffsetX, customViewport.Value.OffsetY, customViewport.Value.Width, customViewport.Value.Height);
            }
            else
            {
                RC.DeviceContext.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            }

            RC.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            RC.SetIL(null);
            RC.SetVS(m_fullscreenQuadVS);
            RC.DeviceContext.Draw(3, 0);
        }

        internal static void RunFullscreenPixelFreq(MyBindableResource RT)
        {
            if(MyRender11.MultisamplingEnabled)
            {
                RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0);
            }
            RC.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadOnly, RT);
            DrawFullscreenQuad();
            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetDS(MyDepthStencilState.DefaultDepthState);
            }
        }

        internal static void RunFullscreenSampleFreq(MyBindableResource RT)
        {
            Debug.Assert(MyRender11.MultisamplingEnabled);
            RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0x80);
            RC.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadOnly, RT);
            DrawFullscreenQuad();
            RC.SetDS(MyDepthStencilState.DefaultDepthState);
        }
    }
}
