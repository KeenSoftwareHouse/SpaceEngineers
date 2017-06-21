using System;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using VRage;
using VRageMath;

namespace VRageRender
{
    public static class MyPostprocessSettingsWrapper
    {
        public static MyPostprocessSettings Settings = MyPostprocessSettings.Default;
    }

    [Serializable]
    public struct MyPostprocessSettings
    {
        [StructDefault]
        public static readonly MyPostprocessSettings Default;

        public bool EnableTonemapping;
        public bool EnableEyeAdaptation;
        public int BloomSize;
        public float Temperature;

        [XmlElement(Type = typeof(MyStructXmlSerializer<Layout>))]
        public Layout Data;

        static MyPostprocessSettings()
        {
            Default = new MyPostprocessSettings
            {
                EnableTonemapping = true,
                EnableEyeAdaptation = false,
                BloomSize = 6,
                Temperature = 6500,
                Data = Layout.Default,
            };
        }

        public static MyPostprocessSettings LerpExposure(ref MyPostprocessSettings A, ref MyPostprocessSettings B, float t)
        {
            MyPostprocessSettings C = A;
            C.Data.LuminanceExposure = VRageMath.MathHelper.Lerp(A.Data.LuminanceExposure, B.Data.LuminanceExposure, t);
            return C;
        }

        public Layout GetProcessedData()
        {
            var ret = Data;
            if (EnableEyeAdaptation)
                ret.ConstantLuminance = -1;
            else
                ret.EyeAdaptationTau = 0;

            ret.TemperatureColor = ColorExtensions.TemperatureToRGB(Temperature);

            return ret;
        }

        [XmlType("MyPostprocessSettings.Layout")]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Layout
        {
            public float Contrast;
            public float Brightness;
            public float ConstantLuminance;
            public float LuminanceExposure;

            public float Saturation;
            public float BrightnessFactorR;
            public float BrightnessFactorG;
            public float BrightnessFactorB;

            public Vector3 TemperatureColor;
            public float TemperatureStrength;

            public float Vibrance;
            public float EyeAdaptationTau;
            public float BloomExposure;
            public float BloomLumaThreshold;

            public float BloomMult;
            public float BloomEmissiveness;
            public float BloomDepthStrength;
            public float BloomDepthSlope;

            public Vector3 LightColor;
            public float LogLumThreshold;

            public Vector3 DarkColor;
            public float SepiaStrength;

            [StructDefault]
            public static readonly Layout Default;

            static Layout()
            {
                Default = new Layout()
                {
                    Contrast = 1,
                    Brightness = 1,
                    ConstantLuminance = 0.1f,
                    LuminanceExposure = 1,
                    Saturation = 1,
                    BrightnessFactorR = 1,
                    BrightnessFactorG = 1,
                    BrightnessFactorB = 1,
                    EyeAdaptationTau = 0.3f,
                    Vibrance = 0.25f,
                    TemperatureStrength = 0,
                    
                    BloomEmissiveness = 1,
                    BloomExposure = 5.8f,
                    BloomLumaThreshold = 0.16f,
                    BloomMult = 0.28f,
                    BloomDepthStrength = 2.0f,
                    BloomDepthSlope = 0.3f,

                    LogLumThreshold = -16,
                    LightColor = new Vector3(1, 0.9f, 0.5f),
                    DarkColor = new Vector3(0.2f, 0.05f, 0),
                    SepiaStrength = 0,
                };
            }
        }
    }
}
