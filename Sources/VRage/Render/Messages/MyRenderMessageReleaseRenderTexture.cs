using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace VRageRender
{
    public class MyRenderMessageReleaseRenderTexture : IMyRenderMessage
    {
        public long EntityId;
        public uint RenderObjectID;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ReleaseRenderTexture; } }
    }
}
