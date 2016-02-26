using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawText3D : MyRenderMessageBase
    {
        public Vector3D Coord;
        public string Text;
        public Color Color;
        public float Scale;
        public bool DepthRead;
        public float? ClipDistance;
        public MyGuiDrawAlignEnum Align;
        public int CustomViewProjection;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawText3D; } }
    }
}
