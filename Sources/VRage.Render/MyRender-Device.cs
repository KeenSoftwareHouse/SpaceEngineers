using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D9;
using VRage.Win32;
using VRage.Utils;

namespace VRageRender
{

    internal static partial class MyRender
    {
        public static Direct3D m_d3d;
        private static MyRenderDeviceSettings m_settings;
        private static IntPtr m_windowHandle;
        private static Device m_device;
        private static PresentParameters m_parameters;

        public static Device Device
        {
            get { return m_device; }
        }

        public static PresentParameters Parameters
        {
            get { return m_parameters; }
        }

        private static MyAdapterInfo[] m_adaptersList;

        public static MyAdapterInfo[] GetAdaptersList()
        {
            // if null return unchecked list from d3d
            if (m_adaptersList == null)
            {
                using (var tmpD3d = new Direct3D())
                {
                    return GetAdaptersList(tmpD3d);
                }
            }
            // else return list filled from correct device (not created during devicelost state when adapters params are incorrect)
            return m_adaptersList;
        }

#if !XB1
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();
#endif // !XB1

        public static MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            Debug.Assert(Device == null, "Device was not properly released");

            // try first settingsToTry (if available)
            //  if that doesn't work, try again using desktop fullscreen settings
            //  if that doesn't work, use fallback settings (800x600 or 640x480 in window) and hope for the best

            m_windowHandle = windowHandle;

            var deviceType = DeviceType.Hardware;
            int adapterOrdinal = 0;
            if (settingsToTry.HasValue)
            {
                adapterOrdinal = settingsToTry.Value.AdapterOrdinal;
            }
            EnablePerfHUD(m_d3d, ref adapterOrdinal, ref deviceType);

            bool deviceCreated = false;

            if (settingsToTry.HasValue)
            {
                try
                {
                    var settings = settingsToTry.Value;
                    var originalWindowMode = settings.WindowMode;
                    settings.AdapterOrdinal = adapterOrdinal;
                    settings.WindowMode = MyWindowModeEnum.Window;
                    TryCreateDeviceInternal(windowHandle, deviceType, settings, out m_device, out m_parameters);
                    Debug.Assert(m_device != null);
                    m_settings = settings;

                    bool modeExists = false;
                    foreach(var mode in m_adaptersList[settings.AdapterOrdinal].SupportedDisplayModes)
                    {
                        if(mode.Width == m_settings.BackBufferWidth && mode.Height == m_settings.BackBufferHeight && mode.RefreshRate == m_settings.RefreshRate)
                        {
                            modeExists = true;
                            break;
                        }
                    }

                    if(!modeExists)
                    {
                        var fallbackMode = m_adaptersList[settings.AdapterOrdinal].SupportedDisplayModes.Last(x => true);
                        m_settings.BackBufferHeight = fallbackMode.Height;
                        m_settings.BackBufferWidth = fallbackMode.Width;
                        m_settings.RefreshRate = fallbackMode.RefreshRate;
                    }

                    if (originalWindowMode != m_settings.WindowMode)
                    {
                        m_settings.WindowMode = originalWindowMode;
                        ApplySettings(m_settings);
                    }
                    deviceCreated = true;
                }
                catch
                {
                    /* These settings don't work so we'll try different. Dispose device in case it failed while switching to fullscreen. */
                    DisposeDevice();
                }
            }

            if (!deviceCreated)
            {
                // find the best match among supported display modes
                var adapters = GetAdaptersList();
                int i = 0;
                int j = 0;
                for (;i < adapters.Length; ++i)
                {
                    for (j = 0; j < adapters[i].SupportedDisplayModes.Length; ++j)
                    {
#if !XB1
                        var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                        if (adapters[i].SupportedDisplayModes[j].Width == bounds.Width &&
                            adapters[i].SupportedDisplayModes[j].Height == bounds.Height)
                        {
                            goto DISPLAY_MODE_FOUND_LABEL;
                        }
#else // XB1
                        System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
                    }
                }

            DISPLAY_MODE_FOUND_LABEL:
                if (i != adapters.Length) // found appropriate display mode
                {
                    var displayMode = adapters[i].SupportedDisplayModes[j];
                    var bestFitSettings = new MyRenderDeviceSettings()
                    {
                        AdapterOrdinal = i,
                        BackBufferWidth = displayMode.Width,
                        BackBufferHeight = displayMode.Height,
                        RefreshRate = displayMode.RefreshRate,
                        VSync = true,
                        WindowMode = MyWindowModeEnum.Window, // initially create windowed, we change it to fullscreen afterwards
                    };
                    try
                    {
                        TryCreateDeviceInternal(windowHandle, deviceType, bestFitSettings, out m_device, out m_parameters);
                        Debug.Assert(m_device != null);
                        m_settings = bestFitSettings;
                        m_settings.WindowMode = MyWindowModeEnum.Fullscreen;
                        ApplySettings(m_settings);
                        deviceCreated = true;
                    }
                    catch
                    {
                        /* Doesn't work again. */
                        DisposeDevice();
                    }
                }
            }

            if (!deviceCreated)
            {
                var simpleSettings = new MyRenderDeviceSettings()
                {
                    AdapterOrdinal = 0,
                    BackBufferHeight = 480,
                    BackBufferWidth = 640,
                    WindowMode = MyWindowModeEnum.Window,
                    VSync = true,
                };
                try
                {
                    TryCreateDeviceInternal(windowHandle, deviceType, simpleSettings, out m_device, out m_parameters);
                    Debug.Assert(m_device != null);
                    m_settings = simpleSettings;
                    deviceCreated = true;
                }
                catch
                {
                    // These settings don't work either so we're done here.
#if !XB1
                    MyMessageBox.Show("Unsupported graphics card", "Graphics card is not supported, please see minimum requirements");
#else // XB1
                    System.Diagnostics.Debug.Assert(false, "Unsupported graphics card");
#endif // XB1
                    throw;
                }
            }

            SupportsHDR = GetAdaptersList()[m_settings.AdapterOrdinal].HDRSupported;

            return m_settings;
        }

        private static void TryCreateDeviceInternal(IntPtr windowHandle, DeviceType deviceType, MyRenderDeviceSettings settingsToTry, out Device device, out PresentParameters parameters)
        {
            device = null;
            parameters = CreatePresentParameters(settingsToTry, windowHandle);
            while (device == null)
            {
                try
                {
                    // These calls are here to ensure that none of these calls throw exceptions (even if their results are not used).
                    m_d3d.Dispose();
                    m_d3d = new Direct3D();

                    var d3dCaps = m_d3d.GetDeviceCaps(settingsToTry.AdapterOrdinal, DeviceType.Hardware);

                    device = new Device(m_d3d, settingsToTry.AdapterOrdinal, deviceType, Parameters.DeviceWindowHandle,
                        CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
                        Parameters);
                    device.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(0, 0, 0, 1), 1.0f, 0);

                    var caps = Device.Capabilities;
                }
                catch (SharpDX.SharpDXException e)
                {
                    if (e.ResultCode == ResultCode.NotAvailable ||
#if !XB1
                        (e.ResultCode == ResultCode.InvalidCall && GetForegroundWindow() != Parameters.DeviceWindowHandle))
#else
                        // TODO [vicent] 
                        (e.ResultCode == ResultCode.InvalidCall))
#endif
                    {
                        // User has probably Alt+Tabbed or locked his computer before the game has started.
                        // To counter this, we try creating device again a bit later.
                        Thread.Sleep(2000);
                        MyLog.Default.WriteLine("Device creation failed with " + e.Message);
                    }
                    else
                    {
                        // Either settings or graphics card are not supported.
                        MyLog.Default.WriteLine(e);
                        throw;
                    }
                }

                try
                {
                    MyLog.Default.WriteLine("Loading adapters");
                    m_adaptersList = GetAdaptersList(m_d3d);
                    MyLog.Default.WriteLine("Found adapters");
                    foreach (var adapter in m_adaptersList)
                    {
                        adapter.LogInfo(MyLog.Default.WriteLine);
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(e);
                    throw;
                }
            }
        }

        public static void DisposeDevice()
        {
            // TODO: device cleanup?
            //Debug.Assert(Device != null, "Device already cleaned up");
            try
            {
                if (m_device != null)
                    m_device.Dispose();
                m_device = null;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error during device reset");
                MyLog.Default.WriteLine(e);
            }
        }

        private static PresentParameters CreatePresentParameters(MyRenderDeviceSettings settings, IntPtr windowHandle)
        {
            PresentParameters p = new PresentParameters();
            p.InitDefaults();

            switch (settings.WindowMode)
            {
                case MyWindowModeEnum.Fullscreen:
                    p.FullScreenRefreshRateInHz = settings.RefreshRate;
                    p.BackBufferHeight = settings.BackBufferHeight;
                    p.BackBufferWidth = settings.BackBufferWidth;
                    p.Windowed = false;
                    break;

                case MyWindowModeEnum.FullscreenWindow:
                    {
#if !XB1
                        WinApi.DEVMODE mode = new WinApi.DEVMODE();
                        WinApi.EnumDisplaySettings(null, WinApi.ENUM_REGISTRY_SETTINGS, ref mode);
                        p.FullScreenRefreshRateInHz = 0;
                        p.BackBufferHeight = mode.dmPelsHeight;
                        p.BackBufferWidth = mode.dmPelsWidth;
                        p.Windowed = true;
#else // XB1
                        System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
                    }
                    break;

                case MyWindowModeEnum.Window:
                    p.FullScreenRefreshRateInHz = 0;
                    p.BackBufferHeight = settings.BackBufferHeight;
                    p.BackBufferWidth = settings.BackBufferWidth;
                    p.Windowed = true;
                    break;
            }
            p.DeviceWindowHandle = windowHandle;

            p.AutoDepthStencilFormat = Format.D24S8;
            p.EnableAutoDepthStencil = true;
            p.BackBufferFormat = Format.X8R8G8B8;
            p.MultiSampleQuality = 0;
            p.PresentationInterval = settings.VSync ? PresentInterval.One : PresentInterval.Immediate;
            p.SwapEffect = SwapEffect.Discard;

            // PresentFlags.Video may cause crash when driver settings has overridden multisampling
            // We don't need it, it's just hint for driver
            p.PresentFlags = PresentFlags.DiscardDepthStencil;

            return p;
        }

        public static long GetAvailableTextureMemory()
        {
            return Device.AvailableTextureMemory;
        }

        public static MyRenderDeviceCooperativeLevel TestDeviceCooperativeLevel()
        {
            if (Device != null)
            {
                var result = Device.TestCooperativeLevel();

                if (result == ResultCode.Success)
                    return MyRenderDeviceCooperativeLevel.Ok;
                if (result == ResultCode.DeviceLost)
                    return MyRenderDeviceCooperativeLevel.Lost;
                else if (result == ResultCode.DeviceNotReset)
                    return MyRenderDeviceCooperativeLevel.NotReset;
            }

            return MyRenderDeviceCooperativeLevel.DriverError;
        }

        [Conditional("DEBUG")]
        private static void EnablePerfHUD(Direct3D d3d, ref int adapterOrdinal, ref DeviceType deviceType)
        {
            var perfHudAdapter = d3d.Adapters.FirstOrDefault(s => s.Details.DeviceName.Contains("PerfHUD"));
            if (perfHudAdapter != null)
            {
                adapterOrdinal = perfHudAdapter.Adapter;
                deviceType = DeviceType.Reference;
            }
        }

        public static bool ResetDevice()
        {
            int retries = 10;
            // in some scenarios (mutlimonitor + strange res) device calls return invalidcall after reset for few tries

            while(retries > 0)
            {
                try
                {
                    Device.Reset(Parameters);
                    Device.Viewport = new SharpDX.Viewport(0, 0, Parameters.BackBufferWidth, Parameters.BackBufferHeight);
                    Device.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(0, 0, 0, 1), 1.0f, 0);
                    return true;
                }
                catch (SharpDXException e)
                {
                    if (e.ResultCode == ResultCode.DeviceLost.Result)
                    {
                        return false;
                    }
                    else if(e.ResultCode == ResultCode.InvalidCall)
                    {
                        retries--;
                        Thread.Sleep(50);
                    }
                    else
                    {
                        Debug.Fail("Device reset failed, probably unreleased resources");
                    }
                }
            }

            Debug.Fail("Device reset failed, probably unreleased resources");
            return false;
        }

        public static bool SettingsChanged(MyRenderDeviceSettings settings)
        {
            return !m_settings.Equals(ref settings);
        }

        public static void ApplySettings(MyRenderDeviceSettings settings)
        {
            bool canReset = m_settings.AdapterOrdinal == settings.AdapterOrdinal;
            m_settings = settings;
            m_parameters = CreatePresentParameters(m_settings, m_windowHandle);
            SupportsHDR = GetAdaptersList()[m_settings.AdapterOrdinal].HDRSupported;

            if (canReset)
            {
                if (!Reset())
                    Recreate();
            }
            else
            {
                Recreate();
            }
        }

        /// <summary>
        /// Returns true when reset was OK, returns false when reset cannot be done now (app should wait for DeviceNotReset)
        /// </summary>
        public static bool Reset()
        {
            try
            {
                Device.Reset(Parameters);
                Device.Viewport = new SharpDX.Viewport(0, 0, Parameters.BackBufferWidth, Parameters.BackBufferHeight);
                Device.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(0, 0, 0, 1), 1.0f, 0);
            }
            catch (SharpDXException)
            {
                Debug.Fail("Device reset failed, probably unreleased resources");
                return false;
            }
            return true;
        }

        private static void Recreate()
        {
            DisposeDevice();

            CreateDeviceWithFallback();
        }

        private static void CreateDeviceWithFallback()
        {
            try
            {
                CreateDevice(m_windowHandle, m_settings);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Create device failed");
                MyGraphicTest.Write(Parameters, MyLog.Default.WriteLine);
                MyLog.Default.WriteLine(e);
                MyLog.Default.WriteLine("Trying to create fallback device 0");

                try
                {
                    CreateDeviceWithFallback(Format.X8R8G8B8, Format.D24S8);    
                }
                catch (Exception e1)
                {
                    try
                    {
                        MyLog.Default.WriteLine("Create device failed");
                        MyGraphicTest.Write(Parameters, MyLog.Default.WriteLine);
                        MyLog.Default.WriteLine(e1);
                        MyLog.Default.WriteLine("Trying to create fallback device 1");
                        CreateDeviceWithFallback(Format.A8B8G8R8, Format.D24S8); 
                    }
                    catch (Exception e2)
                    {
                        try
                        {
                            MyLog.Default.WriteLine("Create device failed");
                            MyGraphicTest.Write(Parameters, MyLog.Default.WriteLine);
                            MyLog.Default.WriteLine(e2);
                            MyLog.Default.WriteLine("Trying to create fallback device 2 (final)");
                            CreateDeviceWithFallback(Format.A8B8G8R8, Format.D24X8);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                
            }
        }

        private static void CreateDeviceWithFallback(Format backbufferFmt, Format depthFmt)
        {
            var p = Parameters;

            p.BackBufferFormat = backbufferFmt;
            p.AutoDepthStencilFormat = depthFmt;

            p.BackBufferHeight = 800;
            p.BackBufferWidth = 600;
            p.FullScreenRefreshRateInHz = 0;
            p.Windowed = true;
            m_parameters = p;

            CreateDevice(m_windowHandle, m_settings);
        }
    }
}
