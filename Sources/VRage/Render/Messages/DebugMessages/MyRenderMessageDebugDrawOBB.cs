using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawOBB : MyRenderMessageBase
    {
        public MatrixD Matrix;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;
        public bool Cull;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawOBB; } }
    }
}
