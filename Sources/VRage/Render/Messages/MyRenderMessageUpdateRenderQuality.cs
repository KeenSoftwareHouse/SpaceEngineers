using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderQuality : MyRenderMessageBase
    {
        public MyRenderQualityEnum RenderQuality;
        public bool EnableCascadeBlending;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderQuality; } }
    }
}
