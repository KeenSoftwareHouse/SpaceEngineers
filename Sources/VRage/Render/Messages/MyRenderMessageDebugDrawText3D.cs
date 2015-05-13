using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawText3D : IMyRenderMessage
    {
        public Vector3D Coord;
        public string Text;
        public Color Color;
        public float Scale;
        public bool DepthRead;
        public float? ClipDistance;
        public MyGuiDrawAlignEnum Align;
        public int CustomViewProjection;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DebugDrawText3D; } }
    }
}
