using System.Collections.Generic;

namespace VRageRender
{    
    public class MyRenderMessagePreloadMaterials : MyRenderMessageBase
    {
        public string Name;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.PreloadMaterials; } }
    }
}
