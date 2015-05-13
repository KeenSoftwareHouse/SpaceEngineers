using System.Collections.Generic;

namespace VRageRender
{    
    public class MyRenderMessagePreloadMaterials : IMyRenderMessage
    {
        public string Name;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.PreloadMaterials; } }
    }
}
