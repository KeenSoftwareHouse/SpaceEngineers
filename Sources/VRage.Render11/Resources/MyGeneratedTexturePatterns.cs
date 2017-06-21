using System.Collections.Generic;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRageRender;

namespace VRage.Render11.Resources
{
    static class MyGeneratedTexturePatterns
    {
        public static readonly byte[] ColorMetal_BC7_SRgb;
        public static readonly byte[] NormalGloss_BC7;
        public static readonly byte[] Extension_BC7_SRgb;
        public static readonly byte[] Alphamask_BC4;

        static MyGeneratedTexturePatterns()
        {
            ColorMetal_BC7_SRgb = new byte[16] { 0x08, 0xfc, 0xff, 0xff, 0x3f, 0x00, 0x00, 0x00, 0xfc, 0xff, 0xff, 0x3f, 0x00, 0x00, 0x00, 0x00 };
            NormalGloss_BC7 = new byte[16] { 0xc0, 0xdf, 0xef, 0xf7, 0xfb, 0xff, 0x01, 0x80, 0x36, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33 };
            Extension_BC7_SRgb = new byte[16] { 0x10, 0xff, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Alphamask_BC4 = new byte[8] { 0xff, 0, 0, 0, 0, 0, 0, 0 };
        }


        public static byte[] GetBytePattern(MyChannel channel, Format format)
        {
            if (format == Format.Unknown)
                return null;
            switch (channel)
            {
                case MyChannel.ColorMetal:
                    if (format == Format.BC7_UNorm_SRgb
                        || format == Format.BC7_UNorm)
                        return MyGeneratedTexturePatterns.ColorMetal_BC7_SRgb;
                    else
                        break;
                case MyChannel.NormalGloss:
                    if (format == Format.BC7_UNorm)
                        return MyGeneratedTexturePatterns.NormalGloss_BC7;
                    else
                        break;
                case MyChannel.Extension:
                    if (format == Format.BC7_UNorm_SRgb
                        || format == Format.BC7_UNorm)
                        return MyGeneratedTexturePatterns.Extension_BC7_SRgb;
                    else
                        break;
                case MyChannel.Alphamask:
                    if (format == Format.BC4_UNorm)
                        return MyGeneratedTexturePatterns.Alphamask_BC4;
                    else
                        break;
                default:
                    break;
            }

            // No correct pattern is found, therefore we will use generated:
            int texelBitSize = MyResourceUtils.GetTexelBitSize(format);
            const int bitsInByte = 8;
            // Blocks are 4x4 texels in memory
            const int blockTexelCount = 16;
            int blockBitCount = texelBitSize * blockTexelCount;
            return new byte[blockBitCount / bitsInByte];
        }
    }
}
