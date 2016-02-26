using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateGodRaysSettings : MyRenderMessageBase
    {
        public bool Enabled;
        public float Density;
        public float Weight;
        public float Decay;
        public float Exposition;
        public bool ApplyBlur;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGodRaysSettings; } }
    }
}
