using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawAABB : MyDebugRenderMessage
    {
        public BoundingBoxD AABB;
        public Color Color;
        public float Alpha;
        public float Scale;
        public bool DepthRead;
        public bool Shaded;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawAABB; } }
    }
}
