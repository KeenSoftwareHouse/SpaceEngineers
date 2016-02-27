using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawLine2D : MyRenderMessageBase
    {
        public Vector2 PointFrom;
        public Vector2 PointTo;
        public Color ColorFrom;
        public Color ColorTo;
        public Matrix? Projection;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawLine2D; } }
    }
}
