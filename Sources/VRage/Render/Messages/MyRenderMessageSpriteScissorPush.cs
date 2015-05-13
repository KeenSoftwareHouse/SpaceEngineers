using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageSpriteScissorPush : IMyRenderMessage
    {
        public Rectangle ScreenRectangle; // Defined in screen coordinates.

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SpriteScissorPush; } }
    }
}
