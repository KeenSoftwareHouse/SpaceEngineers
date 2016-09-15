using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugCrashRenderThread : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugCrashRenderThread; } }
    }
}
