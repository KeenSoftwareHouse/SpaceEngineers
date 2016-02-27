using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawTriangle : MyRenderMessageBase
    {
        public Vector3D Vertex0;
        public Vector3D Vertex1;
        public Vector3D Vertex2;
        public Color Color;
        public bool DepthRead;
        public bool Smooth;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawTriangle; } }
    }
}
