using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateContrastSettings : MyRenderMessageBase
    {
        public bool Enabled;
        public float Contrast;
        public float Hue;
        public float Saturation;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateContrastSettings; } }
    }
}
