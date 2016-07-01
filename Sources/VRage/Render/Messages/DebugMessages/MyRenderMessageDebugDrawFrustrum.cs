using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawFrustrum : MyDebugRenderMessage
    {
        public BoundingFrustum Frustrum;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;
        public bool Cull;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawFrustrum; } }
    }
}
