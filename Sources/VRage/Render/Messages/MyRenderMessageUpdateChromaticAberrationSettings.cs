using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateChromaticAberrationSettings : MyRenderMessageBase
    {
        public bool Enabled;
        public float DistortionLens;
        public float DistortionCubic;
        public Vector3 DistortionWeights;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateChromaticAberrationSettings; } }
    }
}
