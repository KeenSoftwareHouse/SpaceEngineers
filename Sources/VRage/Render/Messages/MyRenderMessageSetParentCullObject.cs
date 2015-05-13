using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageSetParentCullObject : IMyRenderMessage
    {
        public uint ID;
        public uint CullObjectID;
        public Matrix? ChildToParent;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SetParentCullObject; } }
    }
}
