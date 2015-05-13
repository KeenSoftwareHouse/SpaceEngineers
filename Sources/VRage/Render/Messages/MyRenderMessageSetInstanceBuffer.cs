using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageSetInstanceBuffer : IMyRenderMessage
    {
        public uint ID;
        public uint InstanceBufferId;
        public int InstanceStart;
        public int InstanceCount;
        public BoundingBox LocalAabb;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SetInstanceBuffer; } }
    }
}
