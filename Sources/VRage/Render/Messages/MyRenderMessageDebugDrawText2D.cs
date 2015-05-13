using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawText2D : IMyRenderMessage
    {
        public Vector2 Coord;
        public string Text;
        public Color Color;
        public float Scale;
        public MyGuiDrawAlignEnum Align;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawText2D; } }
    }
}
