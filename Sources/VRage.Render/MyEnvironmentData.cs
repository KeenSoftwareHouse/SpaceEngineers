using System;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using VRage;
using VRageMath;

namespace VRageRender
{
    public struct MyEnvironmentData
    {
        public MyEnvironmentLightData EnvironmentLight;

        public string Skybox;
        public Quaternion SkyboxOrientation;

        public float DistanceToSun;         // In milions km
        public string SunMaterial;          // CHECK-ME: unused for now
        public bool SunBillboardEnabled;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MyEnvironmentLightData
    {
        [StructDefault]
        public static readonly MyEnvironmentLightData Default;

        [XmlIgnore]
        public Vector3 SunColorRaw;                   // Linear RGB
        public float SunDiffuseFactor;

        [XmlIgnore]
        public Vector3 BackLightColor1Raw;            // Linear RGB
        public float SunGlossFactor;

        [XmlIgnore]
        public Vector3 BackLightColor2Raw;            // Linear RGB
        public float BackLightGlossFactor;

        /// <summary>Direction FROM sun</summary>
        public Vector3 SunLightDirection;
        public float AODirLight;

        public Vector3 BackLightDirection1;
        public float AmbientDiffuseFactor;

        public Vector3 BackLightDirection2;
        public float AmbientSpecularFactor;

        public float AmbientGlobalMinimum;
        public float AmbientGlobalDensity;
        public float AmbientGlobalMultiplier;
        public float AmbientForwardPass;

        public Vector3 SunDiscColor;
        public float SunDiscInnerDot;

        public float SunDiscOuterDot;
        public float AOIndirectLight, AOPointLight, AOSpotLight;

        public float SkyboxBrightness;
        public float EnvSkyboxBrightness;
        public float ShadowFadeoutMultiplier;
        public float EnvShadowFadeoutMultiplier;

        public float EnvAtmosphereBrightness;
        public Vector3 __pad;

        static MyEnvironmentLightData()
        {
            Default = new MyEnvironmentLightData()
            {
                SunColor = Defaults.SunColor,
                BackLightColor1 = Defaults.BackLightColor1,
                BackLightColor2 = Defaults.BackLightColor2,

                SunDiffuseFactor = Defaults.SunDiffuseFactor,
                SunGlossFactor = Defaults.SunGlossFactor,
                BackLightGlossFactor = Defaults.BackLightGlossFactor,

                AmbientDiffuseFactor = Defaults.AmbientDiffuseFactor,
                AmbientSpecularFactor = Defaults.AmbientSpecularFactor,
                AmbientForwardPass = Defaults.AmbientForwardPass,
                AmbientGlobalMinimum = Defaults.AmbientGlobalMinimum,
                AmbientGlobalDensity = Defaults.AmbientGlobalDensity,
                AmbientGlobalMultiplier = Defaults.AmbientGlobalMultiplier,
            
                SunDiscColor = Defaults.SunDiscColor,
                SunDiscInnerDot = Defaults.SunDiscInnerDot,
                SunDiscOuterDot = Defaults.SunDiscOuterDot,
                
                AODirLight = Defaults.AODirLight,
                AOIndirectLight = Defaults.AOIndirectLight,
                AOPointLight = Defaults.AOPointLight,
                AOSpotLight = Defaults.AOSpotLight,

                SkyboxBrightness = Defaults.SkyboxBrightness,
                EnvSkyboxBrightness = Defaults.EnvSkyboxBrightness,
                ShadowFadeoutMultiplier = Defaults.ShadowFadeoutMultiplier,
                EnvShadowFadeoutMultiplier = Defaults.EnvShadowFadeoutMultiplier,
              
                EnvAtmosphereBrightness = Defaults.EnvAtmosphereBrightness,
            };
        }

        public Vector3 SunColor
        {
            get { return SunColorRaw.ToSRGB(); }
            set { SunColorRaw = value.ToLinearRGB(); }
        }
        public Vector3 BackLightColor1
        {
            get { return BackLightColor1Raw.ToSRGB(); }
            set { BackLightColor1Raw = value.ToLinearRGB(); }
        }

        public Vector3 BackLightColor2
        {
            get { return BackLightColor2Raw.ToSRGB(); }
            set { BackLightColor2Raw = value.ToLinearRGB(); }
        }

        public MyEnvironmentLightData InterpolateWith(ref MyEnvironmentLightData other, float interpolator)
        {
            var result = new MyEnvironmentLightData();

            result.SunColorRaw = Vector3.Lerp(this.SunColorRaw, other.SunColorRaw, interpolator);
            result.BackLightColor1Raw = Vector3.Lerp(this.BackLightColor1Raw, other.BackLightColor1Raw, interpolator);
            result.BackLightColor2Raw = Vector3.Lerp(this.BackLightColor2Raw, other.BackLightColor2Raw, interpolator);

            result.SunDiffuseFactor = MathHelper.Lerp(this.SunDiffuseFactor, other.SunDiffuseFactor, interpolator);
            result.SunGlossFactor = MathHelper.Lerp(this.SunGlossFactor, other.SunGlossFactor, interpolator);
            result.BackLightGlossFactor = MathHelper.Lerp(this.BackLightGlossFactor, other.BackLightGlossFactor, interpolator);

            result.AmbientDiffuseFactor = MathHelper.Lerp(this.AmbientDiffuseFactor, other.AmbientDiffuseFactor, interpolator);
            result.AmbientSpecularFactor = MathHelper.Lerp(this.AmbientSpecularFactor, other.AmbientSpecularFactor, interpolator);
            result.AmbientForwardPass = MathHelper.Lerp(this.AmbientForwardPass, other.AmbientForwardPass, interpolator);
            result.AmbientGlobalMinimum = MathHelper.Lerp(this.AmbientGlobalMinimum, other.AmbientGlobalMinimum, interpolator);
            result.AmbientGlobalDensity = MathHelper.Lerp(this.AmbientGlobalDensity, other.AmbientGlobalDensity, interpolator);
            result.AmbientGlobalMultiplier = MathHelper.Lerp(this.AmbientGlobalMultiplier, other.AmbientGlobalMultiplier, interpolator);

            result.SunDiscColor = Vector3.Lerp(this.SunDiscColor, other.SunDiscColor, interpolator);
            result.SunDiscInnerDot = MathHelper.Lerp(this.SunDiscInnerDot, other.SunDiscInnerDot, interpolator);
            result.SunDiscOuterDot = MathHelper.Lerp(this.SunDiscOuterDot, other.SunDiscOuterDot, interpolator);

            result.AODirLight = MathHelper.Lerp(this.AODirLight, other.AODirLight, interpolator);
            result.AOIndirectLight = MathHelper.Lerp(this.AOIndirectLight, other.AOIndirectLight, interpolator);
            result.AOPointLight = MathHelper.Lerp(this.AOPointLight, other.AOPointLight, interpolator);
            result.AOSpotLight = MathHelper.Lerp(this.AOSpotLight, other.AOSpotLight, interpolator);

            result.SkyboxBrightness = MathHelper.Lerp(this.SkyboxBrightness, other.SkyboxBrightness, interpolator);
            result.EnvSkyboxBrightness = MathHelper.Lerp(this.EnvSkyboxBrightness, other.EnvSkyboxBrightness, interpolator);
            result.ShadowFadeoutMultiplier = MathHelper.Lerp(this.ShadowFadeoutMultiplier, other.ShadowFadeoutMultiplier, interpolator);
            result.EnvShadowFadeoutMultiplier = MathHelper.Lerp(this.EnvShadowFadeoutMultiplier, other.EnvShadowFadeoutMultiplier, interpolator);

            result.EnvAtmosphereBrightness = MathHelper.Lerp(this.EnvAtmosphereBrightness, other.EnvAtmosphereBrightness, interpolator);

            return result;
        }

        public static void CalculateBackLightDirections(Vector3 sunDir, Vector3 sunRotationAxis, out Vector3 backLight1Dir, out Vector3 backLight2Dir)
        {
            /*sunDir = -sunDir;
            var backLightAxis = Vector3.Cross(sunRotationAxis, sunDir);
            backLight1Dir = Vector3.Transform(sunDir, Matrix.CreateFromAxisAngle(backLightAxis, MathHelper.Pi * 0.1f));
            backLight2Dir = Vector3.Transform(sunDir, Matrix.CreateFromAxisAngle(backLightAxis, MathHelper.Pi * 0.9f));*/
            backLight1Dir = -sunRotationAxis;
            backLight2Dir = sunRotationAxis;
        }

        static class Defaults
        {
            public static readonly Vector3 SunColor = new Vector3(1.0f, 1.0f, 1.0f);
            public static readonly Vector3 BackLightColor1 = new Vector3(0.59f, 0.73f, 1.0f);
            public static readonly Vector3 BackLightColor2 = new Vector3(0.59f, 0.73f, 1.0f);

            public const float SunDiffuseFactor = 2.9f;
            public const float SunGlossFactor = 1.0f;
            public const float BackLightGlossFactor = 0.5f;

            public const float AmbientDiffuseFactor = 1.0f;
            public const float AmbientSpecularFactor = 1.0f;
            public const float AmbientForwardPass = 0.01f;
            public const float AmbientGlobalMinimum = 0f;
            public const float AmbientGlobalDensity = 0f;
            public const float AmbientGlobalMultiplier = 0f;

            public static readonly Vector3 SunDiscColor = new Vector3(1.5f, 1.35f, 1.0f);
            public const float SunDiscInnerDot = 0.999f;
            public const float SunDiscOuterDot = 0.996f;

            public const float AODirLight = 1.0f;
            public const float AOIndirectLight = 1.5f;
            public const float AOPointLight = 0.5f;
            public const float AOSpotLight = 0.5f;

            public const float SkyboxBrightness = 1.0f;
            public const float EnvSkyboxBrightness = 3.0f;
            public const float ShadowFadeoutMultiplier = 0.02f;
            public const float EnvShadowFadeoutMultiplier = 0f;

            public const float EnvAtmosphereBrightness = 0.2f;
        }
    }
}
