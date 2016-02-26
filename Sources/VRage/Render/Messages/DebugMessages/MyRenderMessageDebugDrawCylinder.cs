using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawCylinder : MyRenderMessageBase
    {
        public MatrixD Matrix;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawCylinder; } }
    }

    public class MyRenderMessageDebugDrawCone : MyRenderMessageBase
    {
        public Vector3D Translation;
        public Vector3D DirectionVector;
        public Vector3D BaseVector;
        public Color Color;
        public bool DepthRead;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawCone; } }
    }
}
