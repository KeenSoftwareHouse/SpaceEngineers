using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender.Messages
{
    public class MyRenderMessageCollectGarbage : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CollectGarbage; } }
    }
}
