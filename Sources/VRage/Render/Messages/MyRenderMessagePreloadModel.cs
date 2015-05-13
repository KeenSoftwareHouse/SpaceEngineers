using System.Collections.Generic;

namespace VRageRender
{    
    public class MyRenderMessagePreloadModel : IMyRenderMessage
    {
        public string Name;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.PreloadModel; } }
    }
}
