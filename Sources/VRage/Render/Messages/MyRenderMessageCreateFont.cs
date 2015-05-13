using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageCreateFont : IMyRenderMessage
    {
        public int FontId;
        public string FontPath;
        public bool IsDebugFont;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateFont; } }
    }
}
