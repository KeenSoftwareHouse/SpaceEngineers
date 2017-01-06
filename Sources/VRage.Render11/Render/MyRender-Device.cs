using System;
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
using VRageRender.Vertex;
using Device = SharpDX.Direct3D11.Device;
using Vector2 = VRageMath.Vector2;
using VRageMath;
using VRage.Win32;
using SharpDX.WIC;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static Device Device { get; private set; }
        private static MyRenderContext m_rc;
        internal static MyRenderContext RC
        {
            get
            {
                return m_rc;
            }
            private set
            {
                MyRender11.Log.WriteLine("Device Context change");
                m_rc = value;
            }
        }
        internal static ImagingFactory WIC { get; private set; }

        private static MyRenderDeviceSettings m_settings = new MyRenderDeviceSettings { AdapterOrdinal = -1 };
        private static IntPtr m_windowHandle;
        internal static MyRenderDeviceSettings DeviceSettings { get { return m_settings; } }

        internal static Vector2 ResolutionF { get { return new Vector2(m_resolution.X, m_resolution.Y); } }
        internal static Vector2I ResolutionI { get { return m_resolution; } }

        [ThreadStatic]
        private static StringBuilder m_debugStringBuilder;
        static private InfoQueue DebugInfoQueue { get; set; }
        private static long m_lastSkippedCount;

        #region Debug

        static StringBuilder DebugStringBuilder
        {
            get
            {
                if (m_debugStringBuilder == null)
                    m_debugStringBuilder = new StringBuilder();

                return m_debugStringBuilder;
            }
        }

        private static void AddDebugQueueMessage(string message)
        {
            if (DebugInfoQueue != null)
                DebugInfoQueue.AddApplicationMessage(MessageSeverity.Information, message);
        }

        private static void InitDebugOutput(bool debugDevice)
        {
            if (!debugDevice)
                return;

            DebugInfoQueue = Device.QueryInterface<InfoQueue>();
            DebugInfoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
            DebugInfoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
            DebugInfoQueue.MessageCountLimit = 4096;
            DebugInfoQueue.ClearStorageFilter();
            if (!MyCompilationSymbols.DX11DebugOutputEnableInfo)
            {
                InfoQueueFilter filter = new InfoQueueFilter();
                filter.DenyList = new InfoQueueFilterDescription();
                filter.DenyList.Severities = new MessageSeverity[1];
                filter.DenyList.Severities[0] = MessageSeverity.Information;
                DebugInfoQueue.AddStorageFilterEntries(filter);
            }
        }

        [Conditional(MyCompilationSymbols.DX11DebugSymbol)]
        internal static void ProcessDebugOutput()
        {
            if (VRage.MyCompilationSymbols.DX11DebugOutput)
            {
                var output = GetDebugOutput();
                if (output.Length != 0)
                    Debug.Write(output);
            }
        }

        private static string GetDebugOutput()
        {
            var stringBuilder = DebugStringBuilder;
            if (DebugInfoQueue != null && 
                // only main render thread has to be able to log to the output, otherwise race conditions happen
                MyRenderProxy.RenderThread.SystemThread == System.Threading.Thread.CurrentThread)
            {
                stringBuilder.Clear();
                for (int i = 0; i < DebugInfoQueue.NumStoredMessagesAllowedByRetrievalFilter; i++)
                {
                    var msg = DebugInfoQueue.GetMessage(i);
                    string text = String.Format("D3D11 {0}: {1} [ {2} #{3}: {4} ] {5}/{6}\n", msg.Severity.ToString(), msg.Description.Replace("\0", ""), msg.Category.ToString(), (int) msg.Id, msg.Id.ToString(), i, DebugInfoQueue.NumStoredMessages);
                    stringBuilder.AppendLine(text);
                }
                if ((DebugInfoQueue.NumMessagesDiscardedByMessageCountLimit - m_lastSkippedCount) > 0)
                {
                    stringBuilder.Append("Skipped messages: ");
                    stringBuilder.Append(DebugInfoQueue.NumMessagesDiscardedByMessageCountLimit - m_lastSkippedCount);
                    m_lastSkippedCount = DebugInfoQueue.NumMessagesDiscardedByMessageCountLimit;
                }

                DebugInfoQueue.ClearStoredMessages();
            }
            return stringBuilder.ToString();
        }

        #endregion

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
                return 0; // 0 should be attached to primary output; user can later decide to use better adapter; GetPriorityAdapter();
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
            MyRenderExceptionEnum exceptionEnum;
            bool deviceCreated = CreateDeviceInternalSafe(windowHandle, settingsToTry, false, out exceptionEnum);
            Log.WriteLine("CreateDevice: deviceCreated = " + deviceCreated);
            Log.WriteLine("CreateDevice: deviceCreated = " + deviceCreated);

#if !XB1
            if (!settingsToTry.HasValue || !settingsToTry.Value.SettingsMandatory)
            {
                if (!deviceCreated)
                {
                    if (settingsToTry.HasValue && settingsToTry.Value.UseStereoRendering)
                    {
                        Log.WriteLine("CreateDevice: Attempt to create stereo renderer");
                        var newSettings = settingsToTry.Value;
                        newSettings.UseStereoRendering = false;
                        deviceCreated = CreateDeviceInternalSafe(windowHandle, newSettings, false, out exceptionEnum);
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
                                        WindowMode = MyWindowModeEnum.FullscreenWindow,
                                        RefreshRate = displayMode.RefreshRate,
                                        VSync = true
                                    };

                                    deviceCreated = CreateDeviceInternalSafe(windowHandle, newSettings, false, out exceptionEnum);
                                    if (deviceCreated)
                                        break;
                                }
                            }
                        }
                        if (deviceCreated)
                            break;
                    }
                }
                if (!deviceCreated)
                {                   
                    Log.WriteLine("Lowest res fallback.");
                    var adapters = GetAdaptersList();
                    for (int i = 0; i < adapters.Length; i++)
                    {
                        var simpleSettings = new MyRenderDeviceSettings()
                        {
                            AdapterOrdinal = i,
                            BackBufferHeight = 480,
                            BackBufferWidth = 640,
                            WindowMode = MyWindowModeEnum.Window,
                            VSync = true,
                        };
                        deviceCreated = CreateDeviceInternalSafe(windowHandle, simpleSettings, false, out exceptionEnum);
                        if (deviceCreated)
                            break;
                    }
                }

                if (!deviceCreated)
                {
                    Log.WriteLine("Debug device fallback");
                    deviceCreated = CreateDeviceInternalSafe(windowHandle, settingsToTry, true, out exceptionEnum);
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
                // always display user friendly text to update drivers
                string message = string.Format("Graphics card could not be initialized.\n\nThis problem may be caused by your graphics card, because it does not meet minimum requirements. Please, check the minimum requirents for the game.\n\nIf the requirements are met, please apply windows updates and update to the latest graphics drivers.");
                VRage.Utils.MyMessageBox.Show("Unable to initialize Direct3D11",
                    message);
                throw new MyRenderException("No supported device detected!\nPlease apply windows updates and update to latest graphics drivers.", MyRenderExceptionEnum.GpuNotSupported);
            }
            return m_settings;
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        private static bool CreateDeviceInternalSafe(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry, bool forceDebugDevice, out MyRenderExceptionEnum exceptionType)
        {
            exceptionType = MyRenderExceptionEnum.Unassigned;

            bool success = false;
            try
            {
                if (settingsToTry.HasValue)
                    Log.WriteLine(settingsToTry.Value.ToString());
                else
                    Log.WriteLine("settingsToTry is null!");
                CreateDeviceInternal(windowHandle, settingsToTry, forceDebugDevice);
                success = true;
            }
            catch (MyRenderException ex)
            {
                Log.WriteLine("CreateDevice failed: MyRenderException occurred");
                Log.IncreaseIndent();
                Log.WriteLine(ex);
                Log.DecreaseIndent();

                exceptionType = ex.Type;
            }
            catch (Exception ex)
            {
                Log.WriteLine("CreateDevice failed: Regular exception occurred");
                Log.IncreaseIndent();
                Log.WriteLine(ex);
                Log.DecreaseIndent();
            }

            if (!success)
            {
                Log.WriteLine("CreateDevice failed: Disposing Device");
                DisposeDevice();
                return false;
            }

            return true;
        }

        private static MyRenderDeviceSettings CreateDeviceInternal(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry, bool forceDebugDevice)
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

            bool isEnabledDebugOutput = forceDebugDevice | MyCompilationSymbols.DX11Debug;
            if (isEnabledDebugOutput)
            {
                flags |= DeviceCreationFlags.Debug;
            }
#if !XB1
            var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            //var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            var settings = settingsToTry ?? new MyRenderDeviceSettings()
            {
                AdapterOrdinal = -1,
                BackBufferHeight = bounds.Width,
                BackBufferWidth = bounds.Height,
                WindowMode = MyWindowModeEnum.FullscreenWindow,
                RefreshRate = 60000,
                VSync = false,
            };
#else
            var settings = CreateXB1Settings();
#endif
            settings.AdapterOrdinal = ValidateAdapterIndex(settings.AdapterOrdinal);

            if (settings.AdapterOrdinal == -1)
            {
                throw new MyRenderException("No supported device detected!\nPlease apply windows updates and update to latest graphics drivers.", MyRenderExceptionEnum.GpuNotSupported);
            }

            m_settings = settings;

            Log.WriteLine("CreateDeviceInteral settings");

            // If this line crashes cmd this: Dism /online /add-capability /capabilityname:Tools.Graphics.DirectX~~~~0.0.1.0
            var factory = GetFactory();

            var adapters = GetAdaptersList();
            if (m_settings.AdapterOrdinal >= adapters.Length)
                throw new MyRenderException("No supported device detected!\nPlease apply windows updates and update to latest graphics drivers.", MyRenderExceptionEnum.GpuNotSupported);
            var adapterId = adapters[m_settings.AdapterOrdinal].AdapterDeviceId;
            if (adapterId >= factory.Adapters.Length)
                throw new MyRenderException("Invalid adapter id binding!", MyRenderExceptionEnum.GpuNotSupported);
            var adapter = factory.Adapters[adapterId];

            Log.WriteLine("CreateDeviceInteral TweakSettingsAdapterAdHoc");
            TweakSettingsAdapterAdHoc(adapter);

            if (m_settings.WindowMode == MyWindowModeEnum.Fullscreen && adapter.Outputs.Length == 0)
                m_settings.WindowMode = MyWindowModeEnum.FullscreenWindow;
            Log.IncreaseIndent();
            LogSettings(ref m_settings);

            Log.WriteLine("CreateDeviceInteral create device");
            if (MyCompilationSymbols.CreateRefenceDevice)
                Device = new Device(DriverType.Reference, flags, FeatureLevel.Level_11_0);
            else
                Device = new Device(adapter, flags, FeatureLevel.Level_11_0);

            Log.WriteLine("CreateDeviceInteral create ImagingFactory");
            WIC = new ImagingFactory();

            // HACK: This is required for Steam overlay to work. Apparently they hook only CreateDevice methods with DriverType argument.
            try
            {
                Log.WriteLine("CreateDeviceInteral Steam Overlay integration");
                using (new Device(DriverType.Hardware, flags, FeatureLevel.Level_11_0)) { }
                Log.WriteLine("CreateDeviceInteral Steam Overlay OK");
            }
            catch
            {
                Log.WriteLine("CreateDeviceInteral Steam Overlay Failed");
            }

            Log.WriteLine("CreateDeviceInteral InitDebugOutput");
            InitDebugOutput(isEnabledDebugOutput);

            Log.WriteLine("CreateDeviceInteral RC Dispose");
            if(RC != null)
            {
                RC.Dispose();
                RC = null;
            }

            Log.WriteLine("CreateDeviceInteral RC Create");
            RC = new MyRenderContext();
            Log.WriteLine("CreateDeviceInteral RC Initialize");
            RC.Initialize(Device.ImmediateContext);

            m_windowHandle = windowHandle;

            m_resolution = new Vector2I(m_settings.BackBufferWidth, m_settings.BackBufferHeight);

            Log.WriteLine("CreateDeviceInteral m_initializedOnce (" + m_initializedOnce + ")");
            if (!m_initializedOnce)
            {
                InitSubsystemsOnce();
                m_initializedOnce = true;
            }

            Log.WriteLine("CreateDeviceInteral m_initialized (" + m_initialized + ")");
            if (!m_initialized)
            {
                OnDeviceReset();
                InitSubsystems();
                m_initialized = true;
            }

            Log.WriteLine("CreateDeviceInteral m_swapchain (" + m_swapchain + ")");
            if (m_swapchain != null)
            {
                m_swapchain.Dispose();
                m_swapchain = null;
            }

            Log.WriteLine("CreateDeviceInteral create swapchain");
            if (m_swapchain == null)
            {
                //SharpDX.DXGI.Device d = Device.QueryInterface<SharpDX.DXGI.Device>();
                //Adapter a = d.GetParent<Adapter>();
                //var factory = a.GetParent<Factory>();

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

                try
                {
                    m_swapchain = new SwapChain(factory, Device, scDesc);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("SwapChain factory = " + factory);
                    Log.WriteLine("SwapChain Device = " + Device);

                    Log.WriteLine("SwapChainDescription.BufferCount = " + scDesc.BufferCount);
                    Log.WriteLine("SwapChainDescription.Flags = " + scDesc.Flags);
                    Log.WriteLine("SwapChainDescription.ModeDescription.Format = " + scDesc.ModeDescription.Format);
                    Log.WriteLine("SwapChainDescription.ModeDescription.Height = " + scDesc.ModeDescription.Height);
                    Log.WriteLine("SwapChainDescription.ModeDescription.Width = " + scDesc.ModeDescription.Width);
                    Log.WriteLine("SwapChainDescription.ModeDescription.RefreshRate.Numerator = " + scDesc.ModeDescription.RefreshRate.Numerator);
                    Log.WriteLine("SwapChainDescription.ModeDescription.RefreshRate.Denominator = " + scDesc.ModeDescription.RefreshRate.Denominator);
                    Log.WriteLine("SwapChainDescription.ModeDescription.Scaling = " + scDesc.ModeDescription.Scaling);
                    Log.WriteLine("SwapChainDescription.ModeDescription.ScanlineOrdering = " + scDesc.ModeDescription.ScanlineOrdering);
                    Log.WriteLine("SwapChainDescription.SampleDescription.Count = " + scDesc.SampleDescription.Count);
                    Log.WriteLine("SwapChainDescription.SampleDescription.Quality = " + scDesc.SampleDescription.Quality);
                    Log.WriteLine("SwapChainDescription.BufferCount = " + scDesc.BufferCount);
                    Log.WriteLine("SwapChainDescription.Usage = " + scDesc.Usage);
                    Log.WriteLine("SwapChainDescription.SwapEffect = " + scDesc.SwapEffect);

                    throw ex;
                }

                factory.MakeWindowAssociation(m_windowHandle, WindowAssociationFlags.IgnoreAll);
            }

            // we start with window always (DXGI recommended)
            Log.WriteLine("CreateDeviceInteral Apply Settings");
            m_settings.WindowMode = MyWindowModeEnum.Window;
            ApplySettings(settings);

            Log.WriteLine("CreateDeviceInteral done (" + m_settings + ")");
            return m_settings;
        }

        private static void TweakSettingsAdapterAdHoc(Adapter adapter)
        {
            // Vendor/device specific workarounds here
        }

        internal static void DisposeDevice()
        {
            ForceWindowed();

            OnDeviceEnd();
            
            m_initialized = false;

            MyHBAO.ReleaseScreenResources();

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

            if (RC != null)
            {
                RC.Dispose();
                RC = null;
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