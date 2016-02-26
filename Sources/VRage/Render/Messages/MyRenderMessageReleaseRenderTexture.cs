using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace VRageRender
{
    public class MyRenderMessageReleaseRenderTexture : MyRenderMessageBase
    {
        public long EntityId;
        public uint RenderObjectID;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ReleaseRenderTexture; } }
    }
}
