using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Game.World
{

    public class MySunProperties //must be class because of debug screens
    {
        // Sun & ambient
        public float SunIntensity;
        public Color SunDiffuse;
        public Color SunSpecular;
        public string SunMaterial;

        public float BackSunIntensity;
        public Color BackSunDiffuse;

        public Color AmbientColor;
        public float AmbientMultiplier;
        public float EnvironmentAmbientIntensity;
        public float SunSizeMultiplier;

        public Color BackgroundColor;
        public Vector3 SunDirectionNormalized;
        public Vector3 BaseSunDirectionNormalized;

        public MySunProperties() { }
        public MySunProperties(MySunProperties from)
        {
            SunIntensity = from.SunIntensity;
            SunDiffuse = from.SunDiffuse;
            SunSpecular = from.SunSpecular;
            SunMaterial = from.SunMaterial;

            BackSunIntensity = from.BackSunIntensity;
            BackSunDiffuse = from.BackSunDiffuse;

            AmbientColor = from.AmbientColor;
            AmbientMultiplier = from.AmbientMultiplier;
            EnvironmentAmbientIntensity = from.EnvironmentAmbientIntensity;
            SunSizeMultiplier = from.SunSizeMultiplier;

            BackgroundColor = from.BackgroundColor;
            SunDirectionNormalized = from.SunDirectionNormalized;
            BaseSunDirectionNormalized = from.BaseSunDirectionNormalized;
        }


        public static MySunProperties Default = new MySunProperties()
        {
            SunIntensity = 1,
            SunDiffuse = Color.White,
            SunSpecular = Color.White,
            AmbientColor = new Color(0.1f),
            SunSizeMultiplier = 100,
            BackgroundColor = Color.White,
            SunDirectionNormalized = new Vector3(0.339467347f, 0.709795356f, -0.617213368f),
            BaseSunDirectionNormalized = new Vector3(0.339467347f, 0.709795356f, -0.617213368f)
        };

        /// <param name="interpolator">0 - use this object, 1 - use other object</param>
        public MySunProperties InterpolateWith(MySunProperties otherProperties, float interpolator)
        {
            var result = new MySunProperties();

            result.SunDiffuse = Color.Lerp(SunDiffuse, otherProperties.SunDiffuse, interpolator);
            result.SunIntensity = MathHelper.Lerp(SunIntensity, otherProperties.SunIntensity, interpolator);
            result.SunSpecular = Color.Lerp(SunSpecular, otherProperties.SunSpecular, interpolator);

            result.BackSunIntensity = MathHelper.Lerp(BackSunIntensity, otherProperties.BackSunIntensity, interpolator);
            result.BackSunDiffuse = Color.Lerp(BackSunDiffuse, otherProperties.BackSunDiffuse, interpolator);

            result.AmbientColor = Color.Lerp(AmbientColor, otherProperties.AmbientColor, interpolator);
            result.AmbientMultiplier = MathHelper.Lerp(AmbientMultiplier, otherProperties.AmbientMultiplier, interpolator);
            result.EnvironmentAmbientIntensity = MathHelper.Lerp(EnvironmentAmbientIntensity, otherProperties.EnvironmentAmbientIntensity, interpolator);
            result.SunSizeMultiplier = MathHelper.Lerp(SunSizeMultiplier, otherProperties.SunSizeMultiplier, interpolator);

            result.BackgroundColor = Color.Lerp(BackgroundColor, otherProperties.BackgroundColor, interpolator);

            if (result.SunDirectionNormalized.Dot(otherProperties.SunDirectionNormalized) > 0.0001f)
                result.SunDirectionNormalized = Vector3.Lerp(SunDirectionNormalized, otherProperties.SunDirectionNormalized, interpolator);
            else
                result.SunDirectionNormalized = SunDirectionNormalized;

            return result;
        }

        public void Serialize(MyObjectBuilder_EnvironmentDefinition builder)
        {
            builder.SunIntensity = SunIntensity;
            builder.SunDiffuse = SunDiffuse.ToVector3();
            builder.SunSpecular = SunSpecular.ToVector3();
            builder.BackLightIntensity = BackSunIntensity;
            builder.BackLightDiffuse = BackSunDiffuse.ToVector3();
            builder.AmbientColor = AmbientColor.ToVector3();
            builder.AmbientMultiplier = AmbientMultiplier;
            builder.EnvironmentAmbientIntensity = EnvironmentAmbientIntensity;
            builder.SunSizeMultiplier = SunSizeMultiplier;
            builder.BackgroundColor = BackgroundColor.ToVector3();
            builder.SunMaterial = SunMaterial;
            builder.SunDirection = BaseSunDirectionNormalized;
        }

        public void Deserialize(MyObjectBuilder_EnvironmentDefinition builder)
        {
            SunIntensity = builder.SunIntensity;
            SunDiffuse = new Color((Vector3)builder.SunDiffuse);
            SunSpecular = new Color((Vector3)builder.SunSpecular);
            BackSunIntensity = builder.BackLightIntensity;
            BackSunDiffuse = new Color((Vector3)builder.BackLightDiffuse);
            AmbientColor = new Color((Vector3)builder.AmbientColor);
            AmbientMultiplier = builder.AmbientMultiplier;
            EnvironmentAmbientIntensity = builder.EnvironmentAmbientIntensity;
            SunSizeMultiplier = builder.SunSizeMultiplier;
            BackgroundColor = new Color((Vector3)builder.BackgroundColor);
            SunMaterial = builder.SunMaterial;
            SunDirectionNormalized = Vector3.Normalize(builder.SunDirection);
            BaseSunDirectionNormalized = SunDirectionNormalized;
        }
    }
}
