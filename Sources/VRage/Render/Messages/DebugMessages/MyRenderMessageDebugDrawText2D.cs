using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawText2D : MyRenderMessageBase
    {
        public Vector2 Coord;
        public string Text;
        public Color Color;
        public float Scale;
        public MyGuiDrawAlignEnum Align;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawText2D; } }
    }
}
