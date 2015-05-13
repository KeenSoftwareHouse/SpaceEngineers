using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDrawString : IMyRenderMessage
    {
        public int FontIndex;
        public Vector2 ScreenCoord;
        public Color ColorMask;
        public readonly StringBuilder Text = new StringBuilder(1024);
        public float ScreenScale;
        public float ScreenMaxWidth;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DrawString; } }
    }
}
