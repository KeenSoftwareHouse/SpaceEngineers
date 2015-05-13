
namespace Sandbox.Graphics.Render
{
    public static class MyPostProcessHDR 
    {
        public static float Exposure;
        public static float Threshold;
        public static float BloomIntensity;
        public static float BloomIntensityBackground;
        public static float VerticalBlurAmount;
        public static float HorizontalBlurAmount;
        public static float NumberOfBlurPasses;

        public static bool DebugHDRChecked;

        static MyPostProcessHDR()
        {
            DebugHDRChecked = true;

            Exposure = 2.0f;
            Threshold = 1.278f;
            BloomIntensity = 2.0f;
            BloomIntensityBackground = 0.4f;
            VerticalBlurAmount = 2.5f;
            HorizontalBlurAmount = 2.5f;
            NumberOfBlurPasses = 1;
        }
    }
}
