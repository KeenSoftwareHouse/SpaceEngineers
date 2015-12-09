using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Game.World
{
    public class MyFogProperties
    {
        // Fog
        public bool EnableFog;
        public float FogNear;
        public float FogFar;
        public float FogMultiplier;
        public float FogBacklightMultiplier;
        public float FogDensity;
        public Color FogColor;

        public static MyFogProperties Default = new MyFogProperties()
        {
            EnableFog = true,
            FogNear = 100,
            FogFar = 200,
            FogMultiplier = 0.13f,
            FogBacklightMultiplier = 1.0f,
            FogDensity = 0.003f,
            FogColor = Color.White
        };

        public void Serialize(MyObjectBuilder_EnvironmentDefinition builder)
        {
            builder.EnableFog = EnableFog;
            builder.FogNear = FogNear;
            builder.FogFar = FogFar;
            builder.FogMultiplier = FogMultiplier;
            builder.FogBacklightMultiplier = FogBacklightMultiplier;
            builder.FogColor = FogColor.ToVector3();
            builder.FogDensity = FogDensity;
        }

        public void Deserialize(MyObjectBuilder_EnvironmentDefinition builder)
        {
            EnableFog = builder.EnableFog;
            FogNear = builder.FogNear;
            FogFar = builder.FogFar;
            FogMultiplier = builder.FogMultiplier;
            FogBacklightMultiplier = builder.FogBacklightMultiplier;
            FogColor = new Color((Vector3)builder.FogColor);
            FogDensity = builder.FogDensity;
        }
    }
}
