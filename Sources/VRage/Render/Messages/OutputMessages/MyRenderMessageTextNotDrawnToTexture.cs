using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageTextNotDrawnToTexture : IMyRenderMessage
    {
        public long EntityId;
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.TextNotDrawnToTexture; } }
    }
}
