using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public enum MyRenderInstanceBufferType
    {
        Cube,
        Generic
    }

    public class MyRenderMessageCreateRenderInstanceBuffer: IMyRenderMessage
    {
        public uint ID;
        public string DebugName;
        public MyRenderInstanceBufferType Type;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateRenderInstanceBuffer; } }
    }

    public class MyRenderMessageUpdateRenderCubeInstanceBuffer : IMyRenderMessage
    {
        public uint ID;
        public List<MyCubeInstanceData> InstanceData = new List<MyCubeInstanceData>();
        public int Capacity;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderCubeInstanceBuffer; } }
    }

    public class MyRenderMessageUpdateRenderInstanceBuffer : IMyRenderMessage
    {
        public uint ID;
        public List<MyInstanceData> InstanceData = new List<MyInstanceData>();
        public int Capacity;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderInstanceBuffer; } }
    }
}
