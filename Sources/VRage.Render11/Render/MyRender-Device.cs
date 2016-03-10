using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using SharpDX;
using SharpDX.Diagnostics;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using VRageRender.Resources;
using VRageRender.Vertex;
using Device = SharpDX.Direct3D11.Device;
using Vector2 = VRageMath.Vector2;
using VRageMath;
using VRage.Win32;


namespace VRageRender
{
    partial class MyRender11
    {
        internal static Device Device { get; private set; }
        internal static DeviceContext DeviceContext { get; private set; }

        static MyRenderDeviceSettings m_settings = new MyRenderDeviceSettings { AdapterOrdinal = -1 };
        static IntPtr m_windowHandle;

        internal static Vector2 ResolutionF { get { return new Vector2(m_resolution.X, m_resolution.Y); } }
        internal static Vector2I ResolutionI { get { return m_resolution; } }

        static DeviceDebug DebugDevice { get; set; }
        static InfoQueue DebugInfoQueue { get; set; }

        #region Debug
        [Conditional("DEBUG_DEVICE")]
        static void PopErrorFilter()
        {
            if (DebugInfoQueue != null)
            {
                DebugInfoQueue.PopStorageFilter();
            }
        }

        static void ProcessDebugOutput()
        {
            using (var DebugInfoQueue = Device.QueryInterface<InfoQueue>())
            {
                System.Threading.Thread t = System.Threading.Thread.CurrentThread;
                bool running = true;

                Device.Disposing += (x, e) => { running = false; t.Join(); };

                DebugInfoQueue.MessageCountLimit = 4096;
                while (running)
                {
                    for (int i = 0; i < DebugInfoQueue.NumStoredMessages; i++)
                    {
                        var msg = DebugInfoQueue.GetMessage(i);
                        //string text = String.Format("D3D11 {0}: {1} [ {2} ERROR #{3}: {4} ]", FormatEnum(msg.Severity), msg.Description.Replace("\0", ""), FormatEnum(msg.Category), (int)msg.Id, FormatEnum(msg.Id));
                        string text = String.Format("D3D11 {0}: {1} [ {2} ERROR #{3}: {4} ]", msg.Severity.ToString(), msg.Description.Replace("\0", ""), msg.Category.ToString(), (int)msg.Id, msg.Id.ToString());
                        System.Diagnostics.Debug.Print(text);
                        System.Diagnostics.Debug.WriteLine(String.Empty);
                    }
                    DebugInfoQueue.ClearStoredMessages();
                    System.Threading.Thread.Sleep(16);
                }
            }
        }
        #endregion

        internal static void HandleDeviceReset()
        {
            ResetAdaptersList();
            DisposeDevice();
            CreateDevice(m_windowHandle, m_settings);

            MyRenderContext.OnDeviceReset();
            MyRenderContextPool.OnDeviceReset();

            OnDeviceReset();
        }

        static bool m_initialized = false;

        private static int GetPriorityAdapter()
        {
            var adapters = GetAdaptersList();
            var bestPriority = -1000;
            int bestIndex = -1;
            for (int i = 0; i < adapters.Length; i++)
            {
                if (adapters[i].IsDx11Supported && bestPriority < adapters[i].Priority)
                {
                    bestPriority = adapters[i].Priority;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        internal static int ValidateAdapterIndex(int adapterIndex)
        {
            var adapters = GetAdaptersList();

            bool adapterIndexNotValid =
                adapterIndex < 0
                || adapters.Length <= adapterIndex
                || !adapters[adapterIndex].IsDx11Supported;
            if (adapterIndexNotValid)
                return GetPriorityAdapter();
            return adapterIndex;
        }

        internal static MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            bool deviceCreated = CreateDeviceInternalSafe(windowHandle, settingsToTry);

            if (!deviceCreated)
            {
                Log.WriteLine("Primary desktop size fallback.");
                var adapters = GetAdaptersList();
                int i = 0;
                int j = 0;
                for (; i < adapters.Length; ++i)
                {
                    for (j = 0; j < adapters[i].SupportedDisplayModes.Length; ++j)
                    {
                        if (adapters[i].IsDx11Supported)
                        {
                            var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                            if (adapters[i].SupportedDisplayModes[j].Width == bounds.Width &&
                                adapters[i].SupportedDisplayModes[j].Height == bounds.Height)
                            {
                                var displayMode = adapters[i].SupportedDisplayModes[j];
                                var newSettings = new MyRenderDeviceSettings()
                                {
                                    AdapterOrdinal = i,
                                    BackBufferHeight = displayMode.Height,
                                    BackBufferWidth = displayMode.Width,
                                    WindowMode = MyWindowModeEnum.Fullscreen,
                                    RefreshRate = displayMode.RefreshRate,
                                    VSync = true
                                };

                                deviceCreated = CreateDeviceInternalSafe(windowHandle, newSettings);
                                if (deviceCreated)
                                    return m_settings;
                            }
                        }
                    }
                }
            }
            if (!deviceCreated)
            {
                Log.WriteLine("Lowest res fallback.");
                var simpleSettings = new MyRenderDeviceSettings()
                {
                    AdapterOrdinal = 0,
                    BackBufferHeight = 480,
                    BackBufferWidth = 640,
                    WindowMode = MyWindowModeEnum.Window,
                    VSync = true,
                };
                deviceCreated = CreateDeviceInternalSafe(windowHandle, simpleSettings);
                if (!deviceCreated)
                {
                    VRage.Utils.MyMessageBox.Show("Unsupported graphics card", "Graphics card is not supported, please see minimum requirements");
                    throw new MyRenderException("No supported device detected!", MyRenderExceptionEnum.GpuNotSupported);
                }
            }

            return m_settings;
        }

        private static bool CreateDeviceInternalSafe(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            try
            {
                CreateDeviceInternal(windowHandle, settingsToTry);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("CreateDevice failed: " + ex.ToString());
                DisposeDevice();
            }
            return false;
        }

        private static MyRenderDeviceSettings CreateDeviceInternal(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            if (Device != null)
            { 
                Device.Dispose();
                Device = null;
            }

            if (settingsToTry != null)
            {
                Log.WriteLine("CreateDevice - original settings");
                var originalSettings = settingsToTry.Value;
                LogSettings(ref originalSettings);
            }

            FeatureLevel[] featureLevels = { FeatureLevel.Level_11_0 };
            DeviceCreationFlags flags = DeviceCreationFlags.None;
      
    #if DEBUG_DEVICE && DEBUG
            flags |= DeviceCreationFlags.Debug;
    #endif

            WinApi.DEVMODE mode = new WinApi.DEVMODE();
            WinApi.EnumDisplaySettings(null, WinApi.ENUM_REGISTRY_SETTINGS, ref mode);

            var settings = settingsToTry ?? new MyRenderDeviceSettings()
            {
                AdapterOrdinal = -1,
                BackBufferHeight = mode.dmPelsHeight,
                BackBufferWidth = mode.dmPelsWidth,
                WindowMode = MyWindowModeEnum.Fullscreen,
                RefreshRate = 60000,
                VSync = false,
            };
            settings.AdapterOrdinal = ValidateAdapterIndex(settings.AdapterOrdinal);

            if (settings.AdapterOrdinal == -1)
            {
                throw new MyRenderException("No supported device detected!", MyRenderExceptionEnum.GpuNotSupported);
            }

            m_settings = settings;

            Log.WriteLine("CreateDevice settings");
            LogSettings(ref m_settings);

            // If this line crashes cmd this: Dism /online /add-capability /capabilityname:Tools.Graphics.DirectX~~~~0.0.1.0
            var adapters = GetAdaptersList();
            if (m_settings.AdapterOrdinal >= adapters.Length)
                throw new MyRenderException("No supported device detected!", MyRenderExceptionEnum.GpuNotSupported);
            var adapterId = adapters[m_settings.AdapterOrdinal].AdapterDeviceId;
            if (adapterId >= GetFactory().Adapters.Length)
                throw new MyRenderException("Invalid adapter id binding!", MyRenderExceptionEnum.GpuNotSupported);
            var adapter = GetFactory().Adapters[adapterId];
            Device = new Device(adapter, flags, FeatureLevel.Level_11_0);

            // HACK: This is required for Steam overlay to work. Apparently they hook only CreateDevice methods with DriverType argument.
            try
            {
                using (new Device(DriverType.Hardware, flags, FeatureLevel.Level_11_0)){}
            }
            catch { }

            if (flags.HasFlag(DeviceCreationFlags.Debug))
            {
                if (DebugDevice != null)
                {
                    DebugDevice.Dispose();
                    DebugDevice = null;
                }

                DebugDevice = new DeviceDebug(Device);
                DebugInfoQueue = DebugDevice.QueryInterface<InfoQueue>();

                new System.Threading.Thread(ProcessDebugOutput).Start();
            }

            if(DeviceContext != null)
            {
                DeviceContext.Dispose();
                DeviceContext = null;
            }

            DeviceContext = Device.ImmediateContext;

            m_windowHandle = windowHandle;

            m_resolution = new Vector2I(m_settings.BackBufferWidth, m_settings.BackBufferHeight);

            if (!m_initialized)
            {
                InitSubsystems();
                m_initialized = true;
            }

            if (m_swapchain != null)
            {
                m_swapchain.Dispose();
                m_swapchain = null;
            }

            if (m_swapchain == null)
            {
                SharpDX.DXGI.Device d = Device.QueryInterface<SharpDX.DXGI.Device>();
                Adapter a = d.GetParent<Adapter>();
                var factory = a.GetParent<Factory>();

                var scDesc = new SwapChainDescription();
                scDesc.BufferCount = MyRender11Constants.BUFFER_COUNT;
                scDesc.Flags = SwapChainFlags.AllowModeSwitch;
                scDesc.IsWindowed = true;
                scDesc.ModeDescription.Format = MyRender11Constants.DX11_BACKBUFFER_FORMAT;
                scDesc.ModeDescription.Height = m_settings.BackBufferHeight;
                scDesc.ModeDescription.Width = m_settings.BackBufferWidth;
                scDesc.ModeDescription.RefreshRate.Numerator = m_settings.RefreshRate;
                scDesc.ModeDescription.RefreshRate.Denominator = 1000;
                scDesc.ModeDescription.Scaling = DisplayModeScaling.Unspecified;
                scDesc.ModeDescription.ScanlineOrdering = DisplayModeScanlineOrder.Progressive;
                scDesc.SampleDescription.Count = 1;
                scDesc.SampleDescription.Quality = 0;
                scDesc.OutputHandle = m_windowHandle;
                scDesc.Usage = Usage.RenderTargetOutput;
                scDesc.SwapEffect = SwapEffect.Discard;

                m_swapchain = new SwapChain(factory, Device, scDesc);

                m_swapchain.GetParent<Factory>().MakeWindowAssociation(m_windowHandle, WindowAssociationFlags.IgnoreAll);
            }

            // we start with window always (DXGI recommended)
            m_settings.WindowMode = MyWindowModeEnum.Window;
            ApplySettings(settings);

            return m_settings;
        }

        internal static void DisposeDevice()
        {
            ForceWindowed();

            OnDeviceEnd();

            if (MyGBuffer.Main != null)
            {
                MyGBuffer.Main.Release();
                MyGBuffer.Main = null;
            }

            if (Backbuffer != null)
            {
                Backbuffer.Release();
                Backbuffer = null;
            }

            if (m_swapchain != null)
            {
                m_swapchain.Dispose();
                m_swapchain = null;
            }

            if (DeviceContext != null)
            {
                DeviceContext.Dispose();
                DeviceContext = null;
            }

            if (Device != null)
            {
    #if DEBUG_DEVICE
                var deviceDebug = new DeviceDebug(Device);
                deviceDebug.ReportLiveDeviceObjects(ReportingLevel.Detail);
                deviceDebug.Dispose();
    #endif

                Device.Dispose();
                Device = null;
            }

            if(m_factory != null)
            {
                m_factory.Dispose();
                m_factory = null;
            }
        }

        internal static long GetAvailableTextureMemory()
        {
            if (m_settings.AdapterOrdinal == -1)
            {
                return 0;
            }
            return (long)m_adapterInfoList[m_settings.AdapterOrdinal].VRAM;
        }
    }
}