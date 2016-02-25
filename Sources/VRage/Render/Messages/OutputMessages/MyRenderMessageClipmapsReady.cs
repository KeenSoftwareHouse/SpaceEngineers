using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    class MyRenderMessageClipmapsReady : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ClipmapsReady; } }
    }
}
