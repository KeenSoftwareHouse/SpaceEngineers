using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawLine2D : IMyRenderMessage
    {
        public Vector2 PointFrom;
        public Vector2 PointTo;
        public Color ColorFrom;
        public Color ColorTo;
        public Matrix? Projection;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawLine2D; } }
    }
}
