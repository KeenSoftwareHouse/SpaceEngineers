using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawCylinder : IMyRenderMessage
    {
        public MatrixD Matrix;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawCylinder; } }
    }

    public class MyRenderMessageDebugDrawCone : IMyRenderMessage
    {
        public Vector3D Translation;
        public Vector3D DirectionVector;
        public Vector3D BaseVector;
        public Color Color;
        public bool DepthRead;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawCone; } }
    }
}
