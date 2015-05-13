using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateChromaticAberrationSettings : IMyRenderMessage
    {
        public bool Enabled;
        public float DistortionLens;
        public float DistortionCubic;
        public Vector3 DistortionWeights;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateChromaticAberrationSettings; } }
    }
}
