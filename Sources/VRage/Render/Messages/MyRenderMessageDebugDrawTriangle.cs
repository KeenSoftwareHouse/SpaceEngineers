using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawTriangle : IMyRenderMessage
    {
        public Vector3D Vertex0;
        public Vector3D Vertex1;
        public Vector3D Vertex2;
        public Color Color;
        public bool DepthRead;
        public bool Smooth;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawTriangle; } }
    }
}
