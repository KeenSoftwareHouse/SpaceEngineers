using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateGodRaysSettings : IMyRenderMessage
    {
        public bool Enabled;
        public float Density;
        public float Weight;
        public float Decay;
        public float Exposition;
        public bool ApplyBlur;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateGodRaysSettings; } }
    }
}
