
namespace Sandbox.Graphics.Render
{

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  Volumetric SSAO 2
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static class MyPostProcessVolumetricSSAO2 
    {
        public static bool Enabled = true;

        public static float MinRadius = 0.080f;
        public static float MaxRadius = 93.374f;
        public static float RadiusGrowZScale = 3.293f;
        public static float CameraZFarScale = 1.00114f;

        public static float Bias = 0.380f;
        public static float Falloff = 10.0f;
        public static float NormValue = 1.084f;
        public static float Contrast = 4.347f;

        public static bool ShowOnlySSAO;
        public static bool UseBlur = true;
    }
}
