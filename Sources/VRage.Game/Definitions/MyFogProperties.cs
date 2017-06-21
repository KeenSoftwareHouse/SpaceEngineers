using System.ComponentModel;
using VRageMath;

namespace VRage.Game
{
    public struct MyFogProperties
    {
        [StructDefault]
        public static readonly MyFogProperties Default;

        public float FogMultiplier;
        public float FogDensity;
        public Vector3 FogColor;

        static MyFogProperties()
        {
            Default = new MyFogProperties()
            {
                FogMultiplier = Defaults.FogMultiplier,
                FogDensity = Defaults.FogDensity,
                FogColor = Defaults.FogColor
            };
        }

        private static class Defaults
        {
            public static readonly Vector3 FogColor = new Vector3(0, 0, 0);
            public const float FogMultiplier = 0.13f;
            public const float FogDensity = 0.003f;
        }
    }
}
