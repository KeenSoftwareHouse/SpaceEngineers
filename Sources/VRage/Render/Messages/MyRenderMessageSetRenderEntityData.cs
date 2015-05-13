using System.Collections.Generic;
using VRage;

namespace VRageRender
{    
    public class MyRenderMessageSetRenderEntityData : IMyRenderMessage
    {
        public uint ID;
        public MyModelData ModelData = new MyModelData();

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SetRenderEntityData; } }
    }
}
