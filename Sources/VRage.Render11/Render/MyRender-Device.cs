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
using SharpDX.WIC;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static Device Device { get; private set; }
        private static DeviceContext m_deviceContext;
        internal static DeviceContext DeviceContext
        {
            get
            {
                return m_deviceContext;
            }
            private set
            {
                MyRender11.Log.WriteLine("Device Context change");
                m_deviceContext = value;
                MyRenderContext.OnDeviceReset();
            }
        }
        internal static ImagingFactory WIC { get; private set; }

        private static MyRenderDeviceSettings m_settings = new MyRenderDeviceSettings { AdapterOrdinal = -1 };
        private static IntPtr m_windowHandle;
        internal static MyRenderDeviceSettings DeviceSettings { get { return m_settings; } }

        internal static Vector2 ResolutionF { get { return new Vector2(m_resolution.X, m_resolution.Y); } }
        internal static Vector2I ResolutionI { get { return m_resolution; } }

        //static private DeviceDebug DebugDevice { get; set; }
        static private InfoQueue DebugInfoQueue { get; set; }

        #region Debug
        internal static void AddDebugQueueMessage(string message)
        {
            if (DebugInfoQueue != null)
                DebugInfoQueue.AddApplicationMessage(MessageSeverity.Information, message);
        }
        [Conditional("DEBUG")]
        static private void InitDebugOutput()
        {
            if (VRage.MyCompilationSymbols.DX11Debug && VRage.MyCompilationSymbols.DX11DebugOutput)
            {
                DebugInfoQueue = Device.QueryInterface<InfoQueue>();
                DebugInfoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                DebugInfoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
                DebugInfoQueue.MessageCountLimit = 4096;
                DebugInfoQueue.ClearStorageFilter();
                if (! VRage.MyCompilationSymbols.DX11DebugOutputEnableInfo)
                {
                    InfoQueueFilter filter = new InfoQueueFilter();
                    filter.DenyList = new InfoQueueFilterDescription();
                    filter.DenyList.Severities = new MessageSeverity[1];
                    filter.DenyList.Severities[0] = MessageSeverity.Information;
                    DebugInfoQueue.AddStorageFilterEntries(filter);
                }
            }
        }
        private static long m_lastSkippedCount;
        [Conditional("DEBUG")]
        internal static void ProcessDebugOutput()
        {
            if (DebugInfoQueue != null && VRage.MyCompilationSymbols.DX11DebugOutput && MyRenderProxy.RenderThread.SystemThread == System.Threading.Thread.CurrentThread)
            {
                for (int i = 0; i < DebugInfoQueue.NumStoredMessages; i++)
                {
                    var msg = DebugInfoQueue.GetMessage(i);
                    string text = String.Format("D3D11 {0}: {1} [ {2} #{3}: {4} ] {5}/{6}", msg.Severity.ToString(), msg.Description.Replace("\0", ""), msg.Category.ToString(), (int)msg.Id, msg.Id.ToString(), i, DebugInfoQueue.NumStoredMessages);
                    System.Diagnostics.Debug.Print(text);
                    System.Diagnostics.Debug.WriteLine(String.Empty);
                }
                if ((DebugInfoQueue.NumMessagesDiscardedByMessageCountLimit - m_lastSkippedCount) > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Skipped messages: " + (DebugInfoQueue.NumMessagesDiscardedByMessageCountLimit - m_lastSkippedCount));
                    m_lastSkippedCount = DebugInfoQueue.NumMessagesDiscardedByMessageCountLimit;
                }
                DebugInfoQueue.ClearStoredMessages();
            }
        }
        #endregion

        internal static void HandleDeviceReset()
        {
            ResetAdaptersList();
            CreateDevice(m_windowHandle, m_settings);

            OnDeviceReset();
        }

        static bool m_initialized = false;
        static bool m_initializedOnce = false;

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

#if XB1
        private static MyRenderDeviceSettings CreateXB1Settings()
        {
            return new MyRenderDeviceSettings()
            {
                AdapterOrdinal = 0,
                BackBufferHeight = 720,
                BackBufferWidth = 1280,
                WindowMode = MyWindowModeEnum.Window,
                VSync = true,
            };
        }
#endif

        internal static MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            bool deviceCreated = CreateDeviceInternalSafe(windowHandle, settingsToTry);

#if !XB1
            if (!settingsToTry.HasValue || !settingsToTry.Value.SettingsMandatory)
            {
                if (!deviceCreated)
                {
                    if (settingsToTry.HasValue && settingsToTry.Value.UseStereoRendering)
                    {
                        var newSettings = settingsToTry.Value;
                        newSettings.UseStereoRendering = false;
                        deviceCreated = CreateDeviceInternalSafe(windowHandle, newSettings);
                    }
                }
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
                                    break;
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
                }
            }
#else
#if !XB1_SKIPASSERTFORNOW
            System.Diagnostics.Debug.Assert(false, "simpleSettings is initialized but not used?");
#endif // !XB1_SKIPASSERTFORNOW
            Log.WriteLine("XB1 res fallback.");
            var simpleSettings = CreateXB1Settings();
#endif


            if (!deviceCreated)
            {
#if !XB1
                VRage.Utils.MyMessageBox.Show("Unsupported graphics card", "Graphics card is not supported, please see minimum requirements");
#else // XB1
                System.Diagnostics.Debug.Assert(false, "Unsupported graphics card");
#endif // XB1
                throw new MyRenderException("No supported device detected!", MyRenderExceptionEnum.GpuNotSupported);
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
                Log.WriteLine("CreateDevice failed: " + ex.Message);
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
            WIC = null;

            if (settingsToTry != null)
            {
                Log.WriteLine("CreateDevice - original settings");
                Log.IncreaseIndent();
                var originalSettings = settingsToTry.Value;
                LogSettings(ref originalSettings);
            }

            FeatureLevel[] featureLevels = { FeatureLevel.Level_11_0 };
            DeviceCreationFlags flags = DeviceCreationFlags.None;
      
#if DEBUG
            if (VRage.MyCompilationSymbols.DX11Debug)
                flags |= DeviceCreationFlags.Debug;
#endif

#if !XB1
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
#else
            var settings = CreateXB1Settings();
#endif
            settings.AdapterOrdinal = ValidateAdapterIndex(settings.AdapterOrdinal);

            if (settings.AdapterOrdinal == -1)
            {
                throw new MyRenderException("No supported device detected!", MyRenderExceptionEnum.GpuNotSupported);
            }

            m_settings = settings;

            Log.WriteLine("CreateDevice settings");
            Log.IncreaseIndent();
            LogSettings(ref m_settings);

            // If this line crashes cmd this: Dism /online /add-capability /capabilityname:Tools.Graphics.DirectX~~~~0.0.1.0
            var adapters = GetAdaptersList();
            if (m_settings.AdapterOrdinal >= adapters.Length)
                throw new MyRenderException("No supported device detected!", MyRenderExceptionEnum.GpuNotSupported);
            var adapterId = adapters[m_settings.AdapterOrdinal].AdapterDeviceId;
            if (adapterId >= GetFactory().Adapters.Length)
                throw new MyRenderException("Invalid adapter id binding!", MyRenderExceptionEnum.GpuNotSupported);
            var adapter = GetFactory().Adapters[adapterId];
            TweakSettingsAdapterAdHoc(adapter);
            Device = new Device(adapter, flags, FeatureLevel.Level_11_0);
            WIC = new ImagingFactory();

            // HACK: This is required for Steam overlay to work. Apparently they hook only CreateDevice methods with DriverType argument.
            try
            {
                using (new Device(DriverType.Hardware, flags, FeatureLevel.Level_11_0)){}
            }
            catch { }

            InitDebugOutput();

            if(DeviceContext != null)
            {
                DeviceContext.Dispose();
                DeviceContext = null;
            }

            DeviceContext = Device.ImmediateContext;

            m_windowHandle = windowHandle;

            m_resolution = new Vector2I(m_settings.BackBufferWidth, m_settings.BackBufferHeight);

            if (!m_initializedOnce)
            {
                InitSubsystemsOnce();
                m_initializedOnce = true;
            }

            if (!m_initialized)
            {
                OnDeviceReset();
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

        private static void TweakSettingsAdapterAdHoc(Adapter adapter)
        {
            // Workaround for some AMD/ATI cards that manifest a dirty texture
            // when blurring for the highlight, showing for example blue grass on ME
            if (adapter.Description.VendorId == 0x1002)
                Settings.BlurCopyOnDepthStencilFail = true;
            else
                Settings.BlurCopyOnDepthStencilFail = false;
        }

        internal static void DisposeDevice()
        {
            ForceWindowed();

            OnDeviceEnd();
            
            m_initialized = false;

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
#if DEBUG
                if (VRage.MyCompilationSymbols.DX11Debug)
                {
                    var deviceDebug = new DeviceDebug(Device);
                    deviceDebug.ReportLiveDeviceObjects(ReportingLevel.Detail | ReportingLevel.Summary);
                    deviceDebug.Dispose();
                }
#endif

                Device.Dispose();
                Device = null;
                WIC = null;
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