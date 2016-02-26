using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawSphere : MyRenderMessageBase
    {
        public Vector3D Position;
        public float Radius;
        public Color Color;
        public float Alpha;
        public float ? ClipDistance;
        public bool DepthRead;
        public bool Smooth;
        public bool Cull;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawSphere; } }
    }
}
