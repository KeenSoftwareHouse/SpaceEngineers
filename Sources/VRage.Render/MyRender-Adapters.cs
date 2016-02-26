
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D9;

namespace VRageRender
{
    internal static partial class MyRender
    {
        static MyRefreshRatePriorityComparer m_refreshRatePriorityComparer = new MyRefreshRatePriorityComparer();
        private static Format m_backbufferFormat = Format.X8R8G8B8;

        private static MyAdapterInfo[] GetAdaptersList(Direct3D d3d)
        {
            MyAdapterInfo[] result = new MyAdapterInfo[d3d.AdapterCount];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new MyAdapterInfo();
                var details = d3d.GetAdapterIdentifier(i);

                var currentDisplayMode = d3d.GetAdapterDisplayMode(i);
                result[i].CurrentDisplayMode = new MyDisplayMode { Height = currentDisplayMode.Height, Width = currentDisplayMode.Width, RefreshRate = currentDisplayMode.RefreshRate, AspectRatio = currentDisplayMode.AspectRatio };
                result[i].DeviceName = details.DeviceName;
                result[i].VendorId = details.VendorId;
                result[i].DeviceId = details.DeviceId;
                result[i].Description = details.Description;
                result[i].Name = details.Description + " (" + details.DeviceName.Replace("\\", "").Replace(".", "") + ")";
                result[i].SupportedDisplayModes = new MyDisplayMode[0];

                bool retry = false;
                try
                {
                    result[i].SupportedDisplayModes = GetSupportedDisplayModes(d3d, i);
                }
                catch (SharpDXException dxgiException)
                {
                    if (dxgiException.ResultCode != ResultCode.NotAvailable)
                    {
                        throw;
                    }

                    m_backbufferFormat = Format.A8B8G8R8;
                    retry = true;
                }

                if (retry)
                {
                    try
                    {
                        result[i].SupportedDisplayModes = GetSupportedDisplayModes(d3d, i);
                    }
                    catch (SharpDXException dxgiException)
                    {
                        if (dxgiException.ResultCode != ResultCode.NotAvailable)
                        {
                            throw;
                        }
                    }
                }
            }

            MyGraphicTest test = new MyGraphicTest();
            test.TestDX(d3d, ref result);

            return result;
        }

        private static MyDisplayMode[] GetSupportedDisplayModes(Direct3D d3d, int adapterOrdinal)
        {
            var modeAvailable = new List<MyDisplayMode>();
            var modeMap = new Dictionary<string, MyDisplayMode>();

            SharpDX.Direct3D9.Format format = m_backbufferFormat;

            int modeCount = format == Format.Unknown ? 0 : d3d.GetAdapterModeCount(adapterOrdinal, format);
            for (int modeIndex = 0; modeIndex < modeCount; modeIndex++)
            {
                var mode = d3d.EnumAdapterModes(adapterOrdinal, (SharpDX.Direct3D9.Format)format, modeIndex);

                string key = format + ";" + mode.Width + ";" + mode.Height + ";" + mode.RefreshRate;

                MyDisplayMode oldMode;
                if (!modeMap.TryGetValue(key, out oldMode))
                {
                    var displayMode = new MyDisplayMode()
                    {
                        Width = mode.Width,
                        Height = mode.Height,
                        RefreshRate = mode.RefreshRate,
                        AspectRatio = mode.AspectRatio
                    };

                    modeMap.Add(key, displayMode);
                    modeAvailable.Add(displayMode);
                }
            }

            modeAvailable.Sort(m_refreshRatePriorityComparer);
            return modeAvailable.ToArray();
        }
    }
}
