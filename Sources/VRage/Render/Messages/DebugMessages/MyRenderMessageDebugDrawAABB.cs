using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawAABB : MyRenderMessageBase
    {
        public BoundingBoxD AABB;
        public Color Color;
        public float Alpha;
        public float Scale;
        public bool DepthRead;
        public bool Shaded;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawAABB; } }
    }
}
