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
        internal static DeviceContext ImmediateContext { get; private set; }
        internal static DeviceContext Context { get { return ImmediateContext; } }

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
            CreateDevice(m_windowHandle, m_settings);

            MyRenderContextPool.OnDeviceReset();

            OnDeviceReset();
        }

        static bool m_initialized = false;

        internal static int ValidateAdapterIndex(int adapterIndex)
        {
            var adapters = GetAdaptersList();

            bool adapterIndexNotValid =
                adapterIndex == -1
                || adapters.Length <= adapterIndex
                || !adapters[adapterIndex].IsDx11Supported;
            if (adapterIndexNotValid)
            {
                var bestPriority = -1000;

                for (int i = 0; i < adapters.Length; i++)
                {
                    if (adapters[i].IsDx11Supported)
                    {
                        bestPriority = (int)Math.Max(bestPriority, adapters[i].Priority);
                    }
                }

                // taking adapter with top priority
                for (int i = 0; i < adapters.Length; i++)
                {
                    if (adapters[i].IsDx11Supported && adapters[i].Priority == bestPriority)
                    {
                        adapterIndex = i;
                        break;
                    }
                }
            }

            return adapterIndex;
        }

        internal static MyRenderDeviceSettings CreateDevice(IntPtr windowHandle, MyRenderDeviceSettings? settingsToTry)
        {
            if (Device != null)
            { 
                Device.Dispose();
                Device = null;
            }

            FeatureLevel[] featureLevels = { FeatureLevel.Level_11_0 };
            DeviceCreationFlags flags = DeviceCreationFlags.None;
      
    #if DEBUG_DEVICE    
            flags |= DeviceCreationFlags.Debug;
    #endif

            WinApi.DEVMODE mode = new WinApi.DEVMODE();
            WinApi.EnumDisplaySettings(null, WinApi.ENUM_REGISTRY_SETTINGS, ref mode);

            var adapters = GetAdaptersList();

            int adapterIndex = settingsToTry.HasValue ? settingsToTry.Value.AdapterOrdinal : - 1;
            adapterIndex = ValidateAdapterIndex(adapterIndex);
            
            if(adapterIndex == -1)
            {
                throw new MyRenderException("No supporting device detected!", MyRenderExceptionEnum.GpuNotSupported);
            }

            var settings = settingsToTry ?? new MyRenderDeviceSettings()
            {
                AdapterOrdinal = adapterIndex,
                BackBufferHeight = mode.dmPelsHeight,
                BackBufferWidth = mode.dmPelsWidth,
                WindowMode = MyWindowModeEnum.Fullscreen,
                RefreshRate = 60000,
                VSync = false,
            };
            m_settings = settings;

            Device = new Device(GetFactory().Adapters[adapters[m_settings.AdapterOrdinal].AdapterDeviceId], flags, FeatureLevel.Level_11_0);

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

            if(ImmediateContext != null)
            {
                ImmediateContext.Dispose();
                ImmediateContext = null;
            }

            ImmediateContext = Device.ImmediateContext;

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
                scDesc.ModeDescription.Format = MyRender11Constants.BACKBUFFER_FORMAT;
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

            if (ImmediateContext != null)
            {
                ImmediateContext.Dispose();
                ImmediateContext = null;
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