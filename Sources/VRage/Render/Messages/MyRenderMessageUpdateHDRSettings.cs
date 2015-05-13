using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateHDRSettings : IMyRenderMessage
    {
        public bool Enabled;
        public float Exposure;
        public float Threshold;
        public float BloomIntensity;
        public float BloomIntensityBackground;
        public float VerticalBlurAmount;
        public float HorizontalBlurAmount;
        public int NumberOfBlurPasses;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateHDRSettings; } }
    }
}
