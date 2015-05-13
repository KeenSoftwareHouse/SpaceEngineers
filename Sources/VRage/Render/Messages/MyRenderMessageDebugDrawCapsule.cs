using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawCapsule : IMyRenderMessage
    {
        public Vector3D P0;
        public Vector3D P1;
        public float Radius;
        public Color Color;
        public bool DepthRead;
        public bool Shaded;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawCapsule; } }
    }
}
