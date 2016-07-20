using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawPlane : MyDebugRenderMessage
    {
        public Vector3D Position;
        public Vector3 Normal;
        public Color Color;
        public bool DepthRead;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawPlane; } }
    }
}
