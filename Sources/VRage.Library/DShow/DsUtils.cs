/******************************************************
                  DirectShow .NET
		      netmaster@swissonline.ch
*******************************************************/
//					DsUtils
// DirectShow utility classes, partial from the SDK Common sources

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DShowNET
{
    [StructLayout(LayoutKind.Sequential), ComVisible(false)]
    public struct DsRECT		// RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2), ComVisible(false)]
    public struct DsBITMAPINFOHEADER
    {
        public int Size;
        public int Width;
        public int Height;
        public short Planes;
        public short BitCount;
        public int Compression;
        public int ImageSize;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public int ClrUsed;
        public int ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential), ComVisible(false)]
    public class DsOptInt64
    {
        public DsOptInt64(long Value)
        {
            this.Value = Value;
        }
        public long Value;
    }
} // namespace DShowNET
