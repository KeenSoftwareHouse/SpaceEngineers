using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageSpriteScissorPop : IMyRenderMessage
    {
        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SpriteScissorPop; } }
    }
}
