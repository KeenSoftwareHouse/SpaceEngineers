using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageSpriteScissorPop : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.Draw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SpriteScissorPop; } }
    }
}
