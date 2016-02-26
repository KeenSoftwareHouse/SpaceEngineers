using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using VRage.Game;
using VRageRender;

namespace Sandbox.Game.World
{

    public class MySunProperties //must be class because of debug screens
    {

        // Sun & ambient
        public float SunIntensity;
        public Color SunDiffuse;
        public Color SunSpecular;
        public string SunMaterial;

        public float[] AdditionalSunIntensity;
        public Color[] AdditionalSunDiffuse;
        public Vector2[] AdditionalSunDirection;

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

            AdditionalSunIntensity = new float[from.AdditionalSunIntensity.Length];
            AdditionalSunDiffuse = new Color[from.AdditionalSunDiffuse.Length];
            AdditionalSunDirection = new Vector2[from.AdditionalSunDirection.Length];

            for (int lightIndex = 0; lightIndex < from.AdditionalSunDirection.Length; ++lightIndex)
            {
                AdditionalSunIntensity[lightIndex] = from.AdditionalSunIntensity[lightIndex];
                AdditionalSunDiffuse[lightIndex] = from.AdditionalSunDiffuse[lightIndex];
                AdditionalSunDirection[lightIndex] = from.AdditionalSunDirection[lightIndex];
            }

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
            AdditionalSunIntensity = new float[] { 0.0f },
            AdditionalSunDiffuse = new Color[] { new Color(0.784313738f, 0.784313738f, 0.784313738f) },
            AdditionalSunDirection = new Vector2[] { new Vector2(0, 0) },
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
            
            result.AdditionalSunIntensity = new float[AdditionalSunIntensity.Length];
            result.AdditionalSunDiffuse = new Color[AdditionalSunDiffuse.Length];
            result.AdditionalSunDirection = new Vector2[AdditionalSunDirection.Length];
            for (int lightIndex = 0; lightIndex < AdditionalSunDirection.Length; ++lightIndex)
            {
                result.AdditionalSunIntensity[lightIndex] = MathHelper.Lerp(AdditionalSunIntensity[lightIndex], otherProperties.AdditionalSunIntensity[lightIndex], interpolator);
                result.AdditionalSunDiffuse[lightIndex] = Color.Lerp(AdditionalSunDiffuse[lightIndex], otherProperties.AdditionalSunDiffuse[lightIndex], interpolator);
                result.AdditionalSunDirection[lightIndex] = Vector2.Lerp(AdditionalSunDirection[lightIndex], otherProperties.AdditionalSunDirection[lightIndex], interpolator);
            }

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

            builder.AdditionalSunDirection = new VRage.SerializableVector2[AdditionalSunDirection.Length];
            for (int lightIndex = 0; lightIndex < builder.AdditionalSunDirection.Length; ++lightIndex)
            {
                builder.AdditionalSunDirection[lightIndex] = AdditionalSunDirection[lightIndex];
            }
            builder.BackLightIntensity = AdditionalSunIntensity[0];
            builder.BackLightDiffuse = AdditionalSunDiffuse[0].ToVector3();

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
            AdditionalSunDirection = new Vector2[Math.Min(builder.AdditionalSunDirection.Length, MyRenderMessageUpdateRenderEnvironment.MaxAdditionalSuns)];
            AdditionalSunDiffuse = new Color[AdditionalSunDirection.Length];
            AdditionalSunIntensity = new float[AdditionalSunDirection.Length];

            for (int lightIndex = 0; lightIndex < AdditionalSunDirection.Length; ++lightIndex)
            {
                AdditionalSunDirection[lightIndex] = builder.AdditionalSunDirection[lightIndex];
                AdditionalSunIntensity[lightIndex] = builder.BackLightIntensity;
                AdditionalSunDiffuse[lightIndex] = new Color((Vector3)builder.BackLightDiffuse);
            }

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
