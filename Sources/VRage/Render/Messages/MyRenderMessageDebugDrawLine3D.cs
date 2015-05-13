using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawLine3D : IMyRenderMessage
    {
        public Vector3D PointFrom;
        public Vector3D PointTo;
        public Color ColorFrom;
        public Color ColorTo;
        public bool DepthRead;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawLine3D; } }
    }

    public class MyRenderMessageDebugDrawPoint : IMyRenderMessage
    {
        public Vector3D Position;
        public Color Color;
        public bool DepthRead;
        public float? ClipDistance;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawPoint; } }
    }
}
