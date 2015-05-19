using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageUpdateSSAOSettings : IMyRenderMessage
    {
        public bool Enabled;
        public bool ShowOnlySSAO;
        public bool UseBlur;

        public float MinRadius;
        public float MaxRadius;
        public float RadiusGrowZScale;
        public float CameraZFar;

        public float Bias;
        public float Falloff;
        public float NormValue;
        public float Contrast;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateSSAOSettings; } }
    }


    public struct MyPostprocessSettings
    {
        public bool EnableTonemapping;
        public bool EnableEyeAdaptation;
        public bool EnableSsao;

        public float Contrast;
        public float Brightness;

        public float LuminanceExposure;
        public float EyeAdaptationTau;
        public float MiddleGrey;
        public float MiddleGreyAt0;
        public float MiddleGreyCurveSharpness;
        public float BlueShiftRapidness;
        public float BlueShiftScale;

        public float BloomExposure;
        public float BloomMult;

        public float Tonemapping_A;
        public float Tonemapping_B;
        public float Tonemapping_C;
        public float Tonemapping_D;
        public float Tonemapping_E;
        public float Tonemapping_F;
        public float LogLumThreshold;

        public static MyPostprocessSettings DefaultGame()
        {
            return new MyPostprocessSettings
            {
                EnableTonemapping = true,
                EnableSsao = true,
                EnableEyeAdaptation = true,

                EyeAdaptationTau = 0.3f,
                LuminanceExposure = 0.51f,
                Contrast = 0.006f,
                Brightness = 0,
                MiddleGrey = 0,
                BloomExposure = 0.5f,
                BloomMult = 0.25f,
                MiddleGreyCurveSharpness = 3.0f,
                MiddleGreyAt0 = 0.005f,
                BlueShiftRapidness = 0.01f,
                BlueShiftScale = 0.5f,

                Tonemapping_A = 0.22f,
                Tonemapping_B = 0.30f,
                Tonemapping_C = 0.10f,
                Tonemapping_D = 0.20f,
                Tonemapping_E = 0.01f,
                Tonemapping_F = 0.30f,
                LogLumThreshold = -16.0f
            };
        }

        public static MyPostprocessSettings DefaultEditor()
        {
            return new MyPostprocessSettings
            {
                EnableTonemapping = false,
                EnableSsao = true,
                EnableEyeAdaptation = false,
            };
        }
    }

    public class MyRenderMessageUpdatePostprocessSettings : IMyRenderMessage
    {
        public MyPostprocessSettings Settings;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdatePostprocessSettings; } }
    }
}
