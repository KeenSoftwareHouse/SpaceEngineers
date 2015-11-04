using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public static class ColorExtensions
    {
        public static Vector3 ColorToHSV(this Color rgb)
        {
            System.Drawing.Color color = System.Drawing.Color.FromArgb(rgb.R, rgb.G, rgb.B);
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            float hue = color.GetHue() / 360f;
            float saturation = (max == 0) ? 0 : 1f - (1f * min / max);
            float value = max / 255f;
            return new Vector3(hue, saturation, value);
        }


        /// <summary>
        /// Use this for HSV in DX11 Renderer, X = Hue 0..1, Y = Saturation -1..1, Z = Value -1..1
        /// </summary>
        public static Vector3 ColorToHSVDX11(this Color rgb)
        {
            System.Drawing.Color color = System.Drawing.Color.FromArgb(rgb.R, rgb.G, rgb.B);

            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            float hue = color.GetHue() / 360f;
            float saturation = (max == 0) ? -1f : 1f - (2f * min / max);
            float value = -1f + 2f * max / 255f;
            return new Vector3(hue, saturation, value);
        }

        private static Vector3 Hue(float H)
        {
            float R = Math.Abs(H * 6 - 3) - 1;
            float G = 2 - Math.Abs(H * 6 - 2);
            float B = 2 - Math.Abs(H * 6 - 4);
            return (new Vector3(MathHelper.Clamp(R, 0f, 1f), MathHelper.Clamp(G, 0f, 1f), MathHelper.Clamp(B, 0f, 1f)));
        }

        public static Color HSVtoColor(this Vector3 HSV)
        {
            return new Color(((Hue(HSV.X) - 1) * HSV.Y + 1) * HSV.Z);
        }

        public static uint PackHSVToUint(this Vector3 HSV)
        {
            int h = (int)Math.Round(HSV.X * 360);
            int s = (int)Math.Round(HSV.Y * 100 + 100);
            int v = (int)Math.Round(HSV.Z * 100 + 100);
            s <<= 16;
            v <<= 24;
            return (uint)(h | s | v);
        }

        public static Vector3 UnpackHSVFromUint(uint packed)
        {
            UInt16 h = (UInt16)packed;
            byte s = (byte)(packed >> 16);
            byte v = (byte)(packed >> 24);
            return new Vector3(h / 360f, (s - 100) / 100f, (v - 100) / 100f);
        }

        public static float HueDistance(this Color color, float hue)
        {
            var h1 = color.ColorToHSV().X;
            float dist = Math.Abs(h1 - hue);
            return Math.Min(dist, 1 - dist);
        }

        public static float HueDistance(this Color color, Color otherColor)
        {
            return color.HueDistance(otherColor.ColorToHSV().X);
        }
    }
}
