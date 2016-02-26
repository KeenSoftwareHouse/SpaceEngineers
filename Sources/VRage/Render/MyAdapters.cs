
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageRender
{
    [DebuggerDisplay("{Width}x{Height}@{RefreshRate}Hz")]
    public struct MyDisplayMode
    {
        public int Width;
        public int Height;
        public int RefreshRate;
        public int ? RefreshRateDenominator;
        public float AspectRatio;

        public float RefreshRateF { get { return RefreshRateDenominator.HasValue ? RefreshRate / (float)RefreshRateDenominator.Value : RefreshRate; } }

        public MyDisplayMode(int width, int height, int refreshRate, int ? refreshRateDenominator = null)
        {
            Width = width;
            Height = height;
            RefreshRate = refreshRate;
            RefreshRateDenominator = refreshRateDenominator;
            AspectRatio = (float)width / (float)height;
        }

        public override string ToString()
        {
            if (RefreshRateDenominator.HasValue)
            {
                return string.Format("{0}x{1}@{2}Hz", Width, Height, RefreshRate / (float) RefreshRateDenominator.Value);
            }
            else
            {
                return string.Format("{0}x{1}@{2}Hz", Width, Height, RefreshRate);
            }
        }
    };

    [DebuggerDisplay("DeviceName: '{DeviceName}', Description: '{Description}'")]
    public struct MyAdapterInfo
    {
        public string Name;
        public MyDisplayMode CurrentDisplayMode;
        public MyDisplayMode[] SupportedDisplayModes;
        public string DeviceName;
        public string OutputName;
        public string Description;
        public int AdapterDeviceId;
        public int OutputId;

        // for dx 9
        public bool HDRSupported;
        public int MaxTextureSize;
		public bool IsDx9Supported;
        public bool Has512MBRam;

        // for dx 11
        public bool IsDx11Supported;
        public int Priority;
        public bool FallbackDisplayModes;
        public ulong VRAM;
        public bool MultithreadedRenderingSupported;
        public MyTextureQuality MaxTextureQualitySupported;
        public MyAntialiasingMode MaxAntialiasingModeSupported;

        public int VendorId;
        public int DeviceId;

        public void LogInfo(Action<String> lineWriter)
        {
            lineWriter("Adapter: " + Name);
            lineWriter("VendorId: " + VendorId);
            lineWriter("DeviceId: " + DeviceId);
            lineWriter("Details: " + DeviceName);
            lineWriter("Description: " + Description);
        }

        public override string ToString()
        {
            return string.Format("DeviceName: '{0}', Description: '{1}'", DeviceName, Description);
        }
    };

    public class MyRefreshRatePriorityComparer : IComparer<MyDisplayMode>
    {
        static float[] m_refreshRates = new float[] { 60, 75, 59, 72, 100 };

        public int Compare(MyDisplayMode x, MyDisplayMode y)
        {
            if (x.Width == y.Width)
            {
                if (x.Height == y.Height)
                {
                    if (x.RefreshRateF == y.RefreshRateF)
                        return 0;

                    for (int i = 0; i < m_refreshRates.Length; i++)
                    {
                        if (x.RefreshRateF == m_refreshRates[i])
                            return -1;
                        if (y.RefreshRateF == m_refreshRates[i])
                            return 1;
                    }

                    return x.RefreshRate.CompareTo(y.RefreshRate);
                }
                else
                    return x.Height.CompareTo(y.Height);
            }
            else
                return x.Width.CompareTo(y.Width);
        }
    }
}
