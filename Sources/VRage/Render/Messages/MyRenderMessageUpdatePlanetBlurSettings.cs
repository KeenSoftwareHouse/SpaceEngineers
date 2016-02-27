using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public struct MyPlanetBlurSettings
    {
        public bool BlurEnabled;
        public float BlurAmount;
        public float BlurDistance;
        public float BlurTransitionRatio;

        public static MyPlanetBlurSettings Defaults()
        {
            MyPlanetBlurSettings settings;
            settings.BlurEnabled = false;
            settings.BlurAmount = 2.0f;
            settings.BlurDistance = 10000.0f;
            settings.BlurTransitionRatio = 0.3f;
            return settings;
        }
    }

    public class MyRenderMessageUpdatePlanetBlurSettings : MyRenderMessageBase
    {
        public MyPlanetBlurSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdatePlanetBlurSettings; } }
    }
}
