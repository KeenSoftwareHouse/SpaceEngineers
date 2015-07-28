using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRageMath;

namespace VRageRender
{
    class MyAAEdgeMarking : MyScreenPass
    {
        static PixelShaderId m_ps;

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("edge_detection.hlsl", "edge_marking");
        }

        internal static void Run()
        {
            RC.SetDS(MyDepthStencilState.MarkEdgeInStencil, 0xFF);
            RC.SetPS(m_ps);
            RC.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.DepthReadOnly, null);
            RC.BindGBufferForReadSkipStencil(0, MyGBuffer.Main);
            DrawFullscreenQuad();
            RC.SetDS(null);
        }
    }
}
