using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDraw6FaceConvex : MyRenderMessageBase
    {
        /* Vertices of the 6-faced convex shape. */
        /* vertext order is: tlb trb tlf trf bfb brb blf brf */
        public Vector3D[] Vertices;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Fill;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDraw6FaceConvex; } }
    }
}
