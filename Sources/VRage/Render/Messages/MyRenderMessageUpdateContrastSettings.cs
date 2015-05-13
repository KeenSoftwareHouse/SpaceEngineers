using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateContrastSettings : IMyRenderMessage
    {
        public bool Enabled;
        public float Contrast;
        public float Hue;
        public float Saturation;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateContrastSettings; } }
    }
}
