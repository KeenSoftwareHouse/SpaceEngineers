using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDrawString : MyRenderMessageBase
    {
        public int FontIndex;
        public Vector2 ScreenCoord;
        public Color ColorMask;
        public readonly StringBuilder Text = new StringBuilder(1024);
        public float ScreenScale;
        public float ScreenMaxWidth;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.Draw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DrawString; } }
    }
}
