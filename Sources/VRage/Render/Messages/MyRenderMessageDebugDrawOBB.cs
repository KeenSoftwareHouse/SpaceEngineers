using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawOBB : IMyRenderMessage
    {
        public MatrixD Matrix;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;
        public bool Cull;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawOBB; } }
    }
}
