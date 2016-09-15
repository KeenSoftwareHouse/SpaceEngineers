using System;
using System.Xml.Serialization;
using VRage.Data;
using VRageMath;
using VRageRender;

namespace VRage.Game
{
    // Environmental light and ambient
    public struct MySunProperties
    {
        [StructDefault]
        public static readonly MySunProperties Default;

        public float SunIntensity;
        public float BackLightIntensity1;
        public float BackLightIntensity2;

        [XmlElement(Type = typeof(MyStructXmlSerializer<MyEnvironmentLightData>))]
        public MyEnvironmentLightData EnvironmentLight;

        /// <summary>Direction TO sun</summary>
        public Vector3 BaseSunDirectionNormalized;
        /// <summary>Direction TO sun</summary>
        public Vector3 SunDirectionNormalized;

        public string SunMaterial;                  // CHECK-ME: Unused
        public float DistanceToSun;

        static MySunProperties()
        {
            Default = new MySunProperties()
            {
                SunIntensity = Defaults.SunIntensity,
                BackLightIntensity1 = Defaults.BackLightIntensity1,
                BackLightIntensity2 = Defaults.BackLightIntensity2,
                EnvironmentLight = MyEnvironmentLightData.Default,
                SunDirectionNormalized = Defaults.SunDirectionNormalized,
                BaseSunDirectionNormalized = Defaults.BaseSunDirectionNormalized,
                SunMaterial = Defaults.SunMaterial,
                DistanceToSun = Defaults.DistanceToSun,
            };
        }

        public MyEnvironmentData EnvironmentData
        {
            get
            {
                MyEnvironmentData environment = new MyEnvironmentData()
                {
                    EnvironmentLight = EnvironmentLight,
                    SunMaterial = SunMaterial,
                    DistanceToSun = DistanceToSun,
                };

                environment.EnvironmentLight.SunColorRaw = environment.EnvironmentLight.SunColorRaw * SunIntensity;
                environment.EnvironmentLight.BackLightColor1Raw = environment.EnvironmentLight.BackLightColor1Raw * BackLightIntensity1;
                environment.EnvironmentLight.BackLightColor2Raw = environment.EnvironmentLight.BackLightColor2Raw * BackLightIntensity2;
                return environment;
            }
        }

        public Vector3 SunRotationAxis
        {
            get
            {
                Vector3 sunRotationAxis;
                float originalSunCosAngle = Math.Abs(Vector3.Dot(BaseSunDirectionNormalized, Vector3.Up));
                if (originalSunCosAngle > 0.95f)
                {
                    // original sun is too close to the poles
                    sunRotationAxis = Vector3.Cross(Vector3.Cross(BaseSunDirectionNormalized, Vector3.Left), BaseSunDirectionNormalized);
                }
                else
                {
                    sunRotationAxis = Vector3.Cross(Vector3.Cross(BaseSunDirectionNormalized, Vector3.Up), BaseSunDirectionNormalized);
                }

                sunRotationAxis.Normalize();
                return sunRotationAxis;
            }
        }

        /// <param name="interpolator">0 - use this object, 1 - use other object</param>
        public MySunProperties InterpolateWith(MySunProperties other, float interpolator)
        {
            var result = new MySunProperties();

            result.SunIntensity = MathHelper.Lerp(this.SunIntensity, other.SunIntensity, interpolator);
            result.BackLightIntensity1 = MathHelper.Lerp(this.BackLightIntensity1, other.BackLightIntensity1, interpolator);
            result.BackLightIntensity2 = MathHelper.Lerp(this.BackLightIntensity2, other.BackLightIntensity2, interpolator);
            result.EnvironmentLight = EnvironmentLight.InterpolateWith(ref other.EnvironmentLight, interpolator);
            if (result.SunDirectionNormalized.Dot(other.SunDirectionNormalized) > 0.0001f)
                result.SunDirectionNormalized = Vector3.Lerp(SunDirectionNormalized, other.SunDirectionNormalized, interpolator);
            else
                result.SunDirectionNormalized = SunDirectionNormalized;

            return result;
        }

        static class Defaults
        {
            public const float SunIntensity = 1.0f;
            public const float BackLightIntensity1 = 0.15f;
            public const float BackLightIntensity2 = 0.18f;
            public static readonly Vector3 SunDirectionNormalized = new Vector3(0.339467347f, 0.709795356f, -0.617213368f);
            public static readonly Vector3 BaseSunDirectionNormalized = new Vector3(0.339467347f, 0.709795356f, -0.617213368f);
            public const string SunMaterial = "SunDisk";
            public const float DistanceToSun = 1620.18518f;
        }
    }
}
