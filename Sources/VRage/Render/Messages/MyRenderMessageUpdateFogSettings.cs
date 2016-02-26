using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public struct MyRenderFogSettings
    {
        // used in dx9
        public bool Enabled;
        public float FogNear;
        public float FogFar;
        public float FogBacklightMultiplier;

        // used in dx9 & dx11
        public float FogMultiplier;
        public Color FogColor;
        public float FogDensity;
    }

    public class MyRenderMessageUpdateFogSettings : MyRenderMessageBase
    {
        public MyRenderFogSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateFogSettings; } }
    }
}
