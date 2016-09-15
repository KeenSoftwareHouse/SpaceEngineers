using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace VRageMath
{
    public static class ColorExtensions
    {
        public static Vector3 ColorToHSV(this Color rgb)
        {
#if UNSHAPER_TMP
			Debug.Assert(false);
			return new Vector3(0,0,0);
#else
            System.Drawing.Color color = System.Drawing.Color.FromArgb(rgb.R, rgb.G, rgb.B);
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            float hue = color.GetHue() / 360f;
            float saturation = (max == 0) ? 0 : 1f - (1f * min / max);
            float value = max / 255f;
            return new Vector3(hue, saturation, value);
#endif
        }


        /// <summary>
        /// Use this for HSV in DX11 Renderer, X = Hue 0..1, Y = Saturation -1..1, Z = Value -1..1
        /// </summary>
        public static Vector3 ColorToHSVDX11(this Color rgb)
        {
#if UNSHAPER_TMP
			Debug.Assert(false);
			return new Vector3(0,0,0);
#else
            System.Drawing.Color color = System.Drawing.Color.FromArgb(rgb.R, rgb.G, rgb.B);

            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            float hue = color.GetHue() / 360f;
            float saturation = (max == 0) ? -1f : 1f - (2f * min / max);
            float value = -1f + 2f * max / 255f;
            return new Vector3(hue, saturation, value);
#endif
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

        // Reference: http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
        // Assume temperature to be in range [1000,40000]
        public static Vector3 TemperatureToRGB(float temperature)
        {
            Vector3 ret = new Vector3();
            temperature = temperature / 100;

            if (temperature <= 66)
            {
                ret.X = 1;
                ret.Y = (float)(MathHelper.Saturate(0.390081579 * Math.Log(temperature) - 0.631841444));
            }
            else
            {
                float temp = temperature - 60;
                ret.X = (float)(MathHelper.Saturate(1.292936186 * Math.Pow(temp, -0.1332047592)));
                ret.Y = (float)(MathHelper.Saturate(1.129890861 * Math.Pow(temp, -0.0755148492)));
            }

            if (temperature >= 66)
                ret.Z = 1;
            else if (temperature <= 19)
                ret.Z = 0;
            else
                ret.Z = (float)(MathHelper.Saturate(0.543206789 * Math.Log(temperature - 10) - 1.196254089));

            return ret;
        }


        public static Vector4 UnmultiplyColor(this Vector4 c)
        {
            if (c.W == 0)
                return Vector4.Zero;
            return new Vector4(c.X / c.W, c.Y / c.W, c.Z / c.W, c.W);
        }
        public static Vector4 PremultiplyColor(this Vector4 c)
        {
            return new Vector4(c.X * c.W, c.Y * c.W, c.Z * c.W, c.W);
        }
        public static Vector4 ToSRGB(this Vector4 c)
        {
            return new Vector4(ToSRGBComponent(c.X), ToSRGBComponent(c.Y), ToSRGBComponent(c.Z), ToSRGBComponent(c.W));
        }

        public static Vector4 ToLinearRGB(this Vector4 c)
        {
            return new Vector4(ToLinearRGBComponent(c.X), ToLinearRGBComponent(c.Y), ToLinearRGBComponent(c.Z), ToLinearRGBComponent(c.W));
        }

        public static Vector3 ToLinearRGB(this Vector3 c)
        {
            return new Vector3(ToLinearRGBComponent(c.X), ToLinearRGBComponent(c.Y), ToLinearRGBComponent(c.Z));
        }

        public static Vector3 ToSRGB(this Vector3 c)
        {
            return new Vector3(ToSRGBComponent(c.X), ToSRGBComponent(c.Y), ToSRGBComponent(c.Z));
        }

        // Linear to sRGB and back approximated conversions, see:
        // http://chilliant.blogspot.cz/2012/08/srgb-approximations-for-hlsl.html
        public static float ToLinearRGBComponent(float c)
        {
            return (float)Math.Pow(c, 2.2f);
            // Optimized version
            // return c * (c * (c * 0.305306011f + 0.682171111f) + 0.012522878f);
        }

        public static float ToSRGBComponent(float c)
        {
            return (float)Math.Pow(c, 1 / 2.2f);
            /* Optimized version
            double S1 = Math.Sqrt(c);
            double S2 = Math.Sqrt(S1);
            double S3 = Math.Sqrt(S2);
            double sRGB = Math.Max(0.662002687 * S1 + 0.684122060 * S2 - 0.323583601 * S3 - 0.0225411470 * c, 0);
            return (float)sRGB;
            */
        }
    }
}
