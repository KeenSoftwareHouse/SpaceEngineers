using System.Collections.Generic;
using VRage;

namespace VRageRender
{    
    public class MyRenderMessageAddRuntimeModel : IMyRenderMessage
    {
        public string Name;
        public string ReplacedModel;
        public MyModelData ModelData = new MyModelData();

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.AddRuntimeModel; } }
    }
}
