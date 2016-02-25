using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateDistantImpostors : MyRenderMessageBase
    {
        public MyImpostorProperties[] ImpostorProperties;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateDistantImpostors; } }
    }
}
