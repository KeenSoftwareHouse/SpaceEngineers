using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    class MyAAEdgeMarking : MyScreenPass
    {
        static PixelShaderId m_ps;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("Postprocess/EdgeDetection.hlsl");
        }

        internal static void Run()
        {
            RC.SetDepthStencilState(MyDepthStencilStateManager.MarkEdgeInStencil, 0xFF);
            RC.PixelShader.Set(m_ps);
            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.DepthReadOnly);
            RC.PixelShader.SetSrvs(0, MyGBuffer.Main, MyGBufferSrvFilter.NO_STENCIL);
            DrawFullscreenQuad();
            RC.SetDepthStencilState(null);
        }
    }
}
