using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderObjectVisibility : IMyRenderMessage
    {
        public uint ID;
        public bool Visible; //Note that invisible objects still can cast shadows
        public bool NearFlag;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderObjectVisibility; } }
    }
}
