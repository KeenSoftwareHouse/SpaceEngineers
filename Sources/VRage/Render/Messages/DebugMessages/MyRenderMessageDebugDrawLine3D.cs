using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawLine3D : MyRenderMessageBase
    {
        public Vector3D PointFrom;
        public Vector3D PointTo;
        public Color ColorFrom;
        public Color ColorTo;
        public bool DepthRead;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawLine3D; } }
    }

    public class MyRenderMessageDebugDrawPoint : MyRenderMessageBase
    {
        public Vector3D Position;
        public Color Color;
        public bool DepthRead;
        public float? ClipDistance;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawPoint; } }
    }
}
