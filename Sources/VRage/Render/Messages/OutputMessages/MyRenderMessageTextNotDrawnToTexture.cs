using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageTextNotDrawnToTexture : MyRenderMessageBase
    {
        public long EntityId;
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.TextNotDrawnToTexture; } }
    }
}
