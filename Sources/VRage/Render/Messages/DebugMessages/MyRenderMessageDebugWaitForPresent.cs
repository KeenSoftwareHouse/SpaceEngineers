using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugWaitForPresent : MyRenderMessageBase
    {
        public EventWaitHandle WaitHandle;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugWaitForPresent; } }
    }
}
