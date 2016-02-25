using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageSetParentCullObject : MyRenderMessageBase
    {
        public uint ID;
        public uint CullObjectID;
        public Matrix? ChildToParent;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetParentCullObject; } }
    }
}
