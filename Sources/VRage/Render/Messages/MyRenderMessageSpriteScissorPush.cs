using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageSpriteScissorPush : MyRenderMessageBase
    {
        public Rectangle ScreenRectangle; // Defined in screen coordinates.

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.Draw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SpriteScissorPush; } }
    }
}
