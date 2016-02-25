using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageCreateFont : MyRenderMessageBase
    {
        public int FontId;
        public string FontPath;
        public bool IsDebugFont;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateFont; } }
    }
}
