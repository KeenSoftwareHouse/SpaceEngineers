using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageChangeMaterialTexture : IMyRenderMessage
    {
        public uint RenderObjectID;
        public string MaterialName;
        public string TextureName;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ChangeMaterialTexture; } }
    }

}
