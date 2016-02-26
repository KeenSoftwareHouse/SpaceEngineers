using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawCapsule : MyRenderMessageBase
    {
        public Vector3D P0;
        public Vector3D P1;
        public float Radius;
        public Color Color;
        public bool DepthRead;
        public bool Shaded;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawCapsule; } }
    }
}
