using System.Collections.Generic;

namespace VRageRender
{    
    public class MyRenderMessagePreloadModel : MyRenderMessageBase
    {
        public string Name;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.PreloadModel; } }
    }
}
