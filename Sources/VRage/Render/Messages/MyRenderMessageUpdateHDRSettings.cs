using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateHDRSettings : MyRenderMessageBase
    {
        public bool Enabled;
        public float Exposure;
        public float Threshold;
        public float BloomIntensity;
        public float BloomIntensityBackground;
        public float VerticalBlurAmount;
        public float HorizontalBlurAmount;
        public int NumberOfBlurPasses;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateHDRSettings; } }
    }
}
