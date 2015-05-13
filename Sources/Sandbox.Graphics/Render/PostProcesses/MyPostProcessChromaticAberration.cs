using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Graphics.Render
{
    public static class MyPostProcessChromaticAberration
    {
        public static bool Enabled = false;
        public static float DistortionLens = -0.145f;
        public static float DistortionCubic = 0.0f;
        public static float DistortionWeightRed = 1.0f;
        public static float DistortionWeightGreen = 0.9f;
        public static float DistortionWeightBlue = 0.8f;
    }
}
