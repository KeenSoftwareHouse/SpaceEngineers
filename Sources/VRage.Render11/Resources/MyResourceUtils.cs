using SharpDX.Direct3D11;
using SharpDX.DXGI;
namespace VRage.Render11.Resources
{
    internal static class MyResourceUtils
    {
        public static Format MakeSrgb(Format fmt)
        {
            switch (fmt)
            {
                case Format.R8G8B8A8_UNorm:
                    return Format.R8G8B8A8_UNorm_SRgb;
                case Format.B8G8R8A8_UNorm:
                    return Format.B8G8R8A8_UNorm_SRgb;
                case Format.B8G8R8X8_UNorm:
                    return Format.B8G8R8X8_UNorm_SRgb;
                case Format.BC1_UNorm:
                    return Format.BC1_UNorm_SRgb;
                case Format.BC2_UNorm:
                    return Format.BC2_UNorm_SRgb;
                case Format.BC3_UNorm:
                    return Format.BC3_UNorm_SRgb;
                case Format.BC7_UNorm:
                    return Format.BC7_UNorm_SRgb;
            }
            return fmt;
        }

        public static Format MakeLinear(Format fmt)
        {
            switch (fmt)
            {
                case Format.R8G8B8A8_UNorm_SRgb:
                    return Format.R8G8B8A8_UNorm;
                case Format.B8G8R8A8_UNorm_SRgb:
                    return Format.B8G8R8A8_UNorm;
                case Format.B8G8R8X8_UNorm_SRgb:
                    return Format.B8G8R8X8_UNorm;
                case Format.BC1_UNorm_SRgb:
                    return Format.BC1_UNorm;
                case Format.BC2_UNorm_SRgb:
                    return Format.BC2_UNorm;
                case Format.BC3_UNorm_SRgb:
                    return Format.BC3_UNorm;
                case Format.BC7_UNorm_SRgb:
                    return Format.BC7_UNorm;
            }
            return fmt;
        }

        public static bool IsSrgb(Format fmt)
        {
            switch (fmt)
            {
                case Format.R8G8B8A8_UNorm_SRgb:
                    return true;
                case Format.B8G8R8A8_UNorm_SRgb:
                    return true;
                case Format.B8G8R8X8_UNorm_SRgb:
                    return true;
                case Format.BC1_UNorm_SRgb:
                    return true;
                case Format.BC2_UNorm_SRgb:
                    return true;
                case Format.BC3_UNorm_SRgb:
                    return true;
                case Format.BC7_UNorm_SRgb:
                    return true;
            }
            return false;
        }

        public static bool CheckTexturesConsistency(Texture2DDescription desc1, Texture2DDescription desc2)
        {
            return desc1.Format == desc2.Format
                && desc1.MipLevels == desc2.MipLevels
                && desc1.Width == desc2.Width
                && desc1.Height == desc2.Height;
        }
    }
}
