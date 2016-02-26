using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Engine.Platform
{
    static class MyGpuIds
    {
        static readonly int[] UnsupportedIntels = new int[]
        {
            // Intel third generation, Intel GMA, SM2.0
            0x2582, 0x2782, 0x2592, 0x2792, // GMA 900
            0x2772, 0x2776, 0x27A2, 0x27A6, 0x27AE,// GMA 950
            0x29D2, 0x29D3, 0x29B2, 0x29B3, 0x29C2, 0x29C3, // GMA 3100
            0xA001, 0xA002, 0xA011, 0xA012, // GMA 3150
            0x2972, 0x2973, 0x2992, 0x2993, // GMA 3100
        };

        static readonly int[] UnderMinimumIntels = new int[]
        {
            // Intel fourth generation, Intel GMA, slow
            0x29A2, 0x29A3, // X3000
            0x2982, 0x2983, // X3500
            0x2A02, 0x2A03, 0x2A12, 0x2A13, // X3100
            0x2E42, 0x2E43, 0x2E92, 0x2E93, 0x2E12, 0x2E13, // 4500
            0x2E32, 0x2E33, 0x2E22, // X4500
            0x2E23, // X4500 HD
            0x2A42, 0x2A43, // 4500 MHD

            // Intel fifth generation, first Core i- architecture, Intel HD graphics
            0x0042, // Clarkdale CPU (desktop)
            0x0046, // Arrandale CPU (mobile)

            // Intel sixth generation, Sandy Bridge, HD Graphics, HD Graphics 2000, HD Graphics 3000, HD Graphics P3000
            0x0102, 0x0106, 0x0112, 0x0116, 0x0122, 0x0126, 0x010A,

            // Intel seventh generation
            0x0152, // Intel HD 4000
            0x0162, // Intel HD Graphics, low-end Ivy Bridge CPU
            0x0166, // Intel HD Graphics 2500/4000
            0x0402, // Intel HD Graphics, low-end Haswell CPU
        };

        static readonly int[] UnsupportedRadeons = new int[]
        {
            0x791E, // Mobility Radeon X1200 (DX 9.0b, SM 2.0)
            0x791F, // Mobility Radeon X1100 (DX 9.0b, SM 2.0)
            0x7145, // Mobility Radeon X1400
        };

        static readonly Dictionary<int, int[]> Unsupported = new Dictionary<int, int[]>()
        {
            { 0x1002, UnsupportedRadeons },
            { 0x8086, UnsupportedIntels}, 
        };

        static readonly Dictionary<int, int[]> UnderMinimum = new Dictionary<int, int[]>()
        {
            { 0x15AD, new int[] { 0x0405 }}, // VMware SVGA II, VMware SVGA 3D
            { 0x1AB8, new int[] { 0x4005 }}, // iMac Parallels, too slow, but game should run on Windows Vista and higher, because VRAM is virtualized
            { 0x8086, UnderMinimumIntels}, 
        };

        public static bool IsUnsupported(int vendorId, int deviceId)
        {
            int[] unsupportedByVendor;
            return Unsupported.TryGetValue(vendorId, out unsupportedByVendor) && unsupportedByVendor.Contains(deviceId);
        }

        public static bool IsUnderMinimum(int vendorId, int deviceId)
        {
            int[] underMinimumByVendor;
            return IsUnsupported(vendorId, deviceId) || (UnderMinimum.TryGetValue(vendorId, out underMinimumByVendor) && underMinimumByVendor.Contains(deviceId));
        }
    }
}
