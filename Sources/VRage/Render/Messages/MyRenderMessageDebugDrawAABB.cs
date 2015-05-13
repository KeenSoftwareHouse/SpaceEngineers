using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawAABB : IMyRenderMessage
    {
        public BoundingBoxD AABB;
        public Color Color;
        public float Alpha;
        public float Scale;
        public bool DepthRead;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawAABB; } }
    }
}
