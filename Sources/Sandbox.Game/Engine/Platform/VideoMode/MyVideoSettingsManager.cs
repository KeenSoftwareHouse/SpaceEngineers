using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Sandbox.Engine.Platform.VideoMode
{
    // mk:TODO backbuffer dimensions in Fullscreen Window mode could be customizable.
    // We need to stick to FullscreenWindow=Desktop on DX9, while we can customize them on DX11

    public struct MyGraphicsSettings : IEquatable<MyGraphicsSettings>
    {
        public bool EnableDamageEffects;
        public bool HardwareCursor;
        public float FieldOfView;
        public float VegetationDrawDistance;
        public MyRenderSettings1 Render;
        public MyStringId GraphicsRenderer;

        bool IEquatable<MyGraphicsSettings>.Equals(MyGraphicsSettings other)
        {
            return Equals(ref other);
        }

        public bool Equals(ref MyGraphicsSettings other)
        {
            return HardwareCursor == other.HardwareCursor &&
                   FieldOfView == other.FieldOfView &&
                   EnableDamageEffects == other.EnableDamageEffects &&
                   Render.Equals(ref other.Render) &&
                   GraphicsRenderer == other.GraphicsRenderer &&
                   VegetationDrawDistance == other.VegetationDrawDistance;
        }
    }

    public static class MyVideoSettingsManager
    {
        public enum ChangeResult
        {
            Success,
            NothingChanged,
            Failed,
        }

        private const MyAntialiasingMode DEFAULT_ANTI_ALIASING = MyAntialiasingMode.FXAA;
        private const MyShadowsQuality DEFAULT_SHADOW_QUALITY = MyShadowsQuality.HIGH;
        private const bool DEFAULT_AMBIENT_OCCLUSION_ENABLED = true;
        private const bool DEFAULT_TONEMAPPING = true;
        private const MyTextureQuality DEFAULT_TEXTURE_QUALITY = MyTextureQuality.MEDIUM;
        private const MyTextureAnisoFiltering DEFAULT_ANISOTROPIC_FILTERING = MyTextureAnisoFiltering.ANISO_4;
        private const MyFoliageDetails DEFAULT_FOLIAGE_DETAILS = MyFoliageDetails.MEDIUM;

        private static Dictionary<int, MyAspectRatio> m_recommendedAspectRatio;

        /// <summary>
        /// Adapters and their supported display modes as reported by the render.
        /// </summary>
        private static MyAdapterInfo[] m_adapters;
        private static readonly MyAspectRatio[] m_aspectRatios;
        private static MyRenderDeviceSettings m_currentDeviceSettings;
        private static bool m_currentDeviceIsTripleHead;
        private static MyGraphicsSettings m_currentGraphicsSettings;

        public static readonly MyDisplayMode[] DebugDisplayModes;

        public static MyAdapterInfo[] Adapters
        {
            get { return m_adapters; }
        }

        public static MyRenderDeviceSettings CurrentDeviceSettings
        {
            get { return m_currentDeviceSettings; }
        }

        public static MyGraphicsSettings CurrentGraphicsSettings
        {
            get
            {
                m_currentGraphicsSettings.Render.Dx9Quality = MyRenderConstants.RenderQualityProfile.RenderQuality;
                return m_currentGraphicsSettings;
            }
        }

        /// <summary>
        /// This is the renderer that is currently in use (the game was started with it).
        /// Current settings might have different one set, but change requires game restart.
        /// </summary>
        public static MyStringId RunningGraphicsRenderer
        {
            get;
            private set;
        }

        public static bool GpuUnderMinimum
        {
            get;
            private set;
        }

        static MyVideoSettingsManager()
        {
            m_aspectRatios = new MyAspectRatio[MyUtils.GetMaxValueFromEnum<MyAspectRatioEnum>() + 1];
            Action<bool, MyAspectRatioEnum, float, string, bool> add =
                delegate(bool isTripleHead, MyAspectRatioEnum aspectRatioEnum, float aspectRatioNumber, string textShort, bool isSupported)
            {
                m_aspectRatios[(int)aspectRatioEnum] = new MyAspectRatio(isTripleHead, aspectRatioEnum, aspectRatioNumber, textShort, isSupported);
            };

            add(false, MyAspectRatioEnum.Normal_4_3, 4.0f / 3.0f, "4:3", true);
            add(false, MyAspectRatioEnum.Normal_16_9, 16.0f / 9.0f, "16:9", true);
            add(false, MyAspectRatioEnum.Normal_16_10, 16.0f / 10.0f, "16:10", true);

            add(false, MyAspectRatioEnum.Dual_4_3, 2 * 4.0f / 3.0f, "Dual 4:3", true);
            add(false, MyAspectRatioEnum.Dual_16_9, 2 * 16.0f / 9.0f, "Dual 16:9", true);
            add(false, MyAspectRatioEnum.Dual_16_10, 2 * 16.0f / 10.0f, "Dual 16:10", true);

            add(true, MyAspectRatioEnum.Triple_4_3, 3 * 4.0f / 3.0f, "Triple 4:3", true);
            add(true, MyAspectRatioEnum.Triple_16_9, 3 * 16.0f / 9.0f, "Triple 16:9", true);
            add(true, MyAspectRatioEnum.Triple_16_10, 3 * 16.0f / 10.0f, "Triple 16:10", true);

            add(false, MyAspectRatioEnum.Unsupported_5_4, 5f / 4f, "5:4", false);

            if (MyFinalBuildConstants.IS_OFFICIAL)
            {
                DebugDisplayModes = new MyDisplayMode[0];
            }
            else
            {
                // For testing windowed mode dual/triple screens
                DebugDisplayModes = new MyDisplayMode[]
                {
                    new MyDisplayMode(1600, 600, 60),
                    new MyDisplayMode(1920, 480, 60),
                    new MyDisplayMode(1920, 817, 60),
                };
            }
        }

        public static MyRenderDeviceSettings? Initialize()
        {
            // Tell render to supply currently supported video modes.
            MyRenderProxy.RequestVideoAdapters();

            var config = MySandboxGame.Config;

            RunningGraphicsRenderer                                = config.GraphicsRenderer;
            m_currentGraphicsSettings.GraphicsRenderer             = config.GraphicsRenderer;
            m_currentGraphicsSettings.EnableDamageEffects          = config.EnableDamageEffects;
            m_currentGraphicsSettings.FieldOfView                  = config.FieldOfView;
            m_currentGraphicsSettings.HardwareCursor               = config.HardwareCursor;
            m_currentGraphicsSettings.Render.InterpolationEnabled  = config.RenderInterpolation;
            m_currentGraphicsSettings.Render.GrassDensityFactor    = config.GrassDensityFactor;
            m_currentGraphicsSettings.VegetationDrawDistance       = config.VegetationDrawDistance;
            m_currentGraphicsSettings.Render.AntialiasingMode      = config.AntialiasingMode ?? DEFAULT_ANTI_ALIASING;
            m_currentGraphicsSettings.Render.ShadowQuality         = config.ShadowQuality ?? DEFAULT_SHADOW_QUALITY;
            m_currentGraphicsSettings.Render.AmbientOcclusionEnabled  = config.AmbientOcclusionEnabled ?? DEFAULT_AMBIENT_OCCLUSION_ENABLED;
            //m_currentGraphicsSettings.Render.TonemappingEnabled    = config.Tonemapping ?? DEFAULT_TONEMAPPING;
            m_currentGraphicsSettings.Render.TextureQuality        = config.TextureQuality ?? DEFAULT_TEXTURE_QUALITY;
            m_currentGraphicsSettings.Render.AnisotropicFiltering  = config.AnisotropicFiltering ?? DEFAULT_ANISOTROPIC_FILTERING;
            m_currentGraphicsSettings.Render.FoliageDetails        = config.FoliageDetails ?? DEFAULT_FOLIAGE_DETAILS;
            m_currentGraphicsSettings.Render.Dx9Quality            = config.Dx9RenderQuality ?? MyRenderQualityEnum.HIGH;
            m_currentGraphicsSettings.Render.ModelQuality          = config.ModelQuality ?? MyRenderQualityEnum.HIGH;
            m_currentGraphicsSettings.Render.VoxelQuality          = config.VoxelQuality ?? MyRenderQualityEnum.HIGH;

            MySector.Lodding.SelectQuality(m_currentGraphicsSettings.Render.ModelQuality); // this is hack

            SetEnableDamageEffects(config.EnableDamageEffects);
            // Need to send both messages as I don't know which one will be used. One of them will be ignored.
            MyRenderProxy.SwitchRenderSettings(m_currentGraphicsSettings.Render);

            // Load previous device settings that will be used for device creation.
            // If there are no settings in the config (eg. game is run for the first time), null is returned, leaving the decision up
            // to render (it should let us know what settings it used later).
            int? screenWidth = config.ScreenWidth;
            int? screenHeight = config.ScreenHeight;
            int? videoAdapter = config.VideoAdapter;
            if (videoAdapter.HasValue && screenWidth.HasValue && screenHeight.HasValue)
            {
                var settings = new MyRenderDeviceSettings()
                {
                    AdapterOrdinal   = videoAdapter.Value,
                    NewAdapterOrdinal   = videoAdapter.Value,
                    BackBufferHeight = screenHeight.Value,
                    BackBufferWidth  = screenWidth.Value,
                    RefreshRate      = config.RefreshRate,
                    VSync            = config.VerticalSync,
                    WindowMode       = config.WindowMode,
                };

                if (MyPerGameSettings.DefaultRenderDeviceSettings.HasValue)
                {
                    settings.UseStereoRendering = MyPerGameSettings.DefaultRenderDeviceSettings.Value.UseStereoRendering;
                    settings.SettingsMandatory = MyPerGameSettings.DefaultRenderDeviceSettings.Value.SettingsMandatory;
                }

                if (MyCompilationSymbols.DX11ForceStereo)
                    settings.UseStereoRendering = true;

                return settings;
            }
            else
            {
                return MyPerGameSettings.DefaultRenderDeviceSettings;
            }
        }

        public static ChangeResult Apply(MyRenderDeviceSettings settings)
        {
            MySandboxGame.Log.WriteLine("MyVideoModeManager.Apply(MyRenderDeviceSettings)");
            using (MySandboxGame.Log.IndentUsing())
            {
                MySandboxGame.Log.WriteLine("VideoAdapter: " + settings.AdapterOrdinal);
                MySandboxGame.Log.WriteLine("Width: " + settings.BackBufferWidth);
                MySandboxGame.Log.WriteLine("Height: " + settings.BackBufferHeight);
                MySandboxGame.Log.WriteLine("RefreshRate: " + settings.RefreshRate);
                MySandboxGame.Log.WriteLine("WindowMode: " + ((settings.WindowMode == MyWindowModeEnum.Fullscreen) ? "Fullscreen" :
                                                              (settings.WindowMode == MyWindowModeEnum.Window) ? "Window" : "Fullscreen window"));
                MySandboxGame.Log.WriteLine("VerticalSync: " + settings.VSync);

                if (settings.Equals(ref m_currentDeviceSettings) && settings.NewAdapterOrdinal == settings.AdapterOrdinal) // NewAdapter is not included in Equals
                    return ChangeResult.NothingChanged;

                if (!IsSupportedDisplayMode(settings.AdapterOrdinal, settings.BackBufferWidth, settings.BackBufferHeight, settings.WindowMode))
                    return ChangeResult.Failed;

                m_currentDeviceSettings = settings;
                m_currentDeviceSettings.VSync = m_currentDeviceSettings.VSync && !VRage.MyCompilationSymbols.PerformanceOrMemoryProfiling;
                m_currentDeviceSettings.RefreshRate = MySandboxGame.Config.RefreshRate == 0 ? m_currentDeviceSettings.RefreshRate : MySandboxGame.Config.RefreshRate;
                MySandboxGame.Static.SwitchSettings(m_currentDeviceSettings);
                float aspectRatio = (float)m_currentDeviceSettings.BackBufferWidth / (float)m_currentDeviceSettings.BackBufferHeight;
                m_currentDeviceIsTripleHead = GetAspectRatio(GetClosestAspectRatio(aspectRatio)).IsTripleHead;

                // Update FoV in case bounds have changed.
                float fovMin, fovMax;
                GetFovBounds(aspectRatio, out fovMin, out fovMax);
                SetFov(MathHelper.Clamp(m_currentGraphicsSettings.FieldOfView, fovMin, fovMax));
            }

            return ChangeResult.Success;
        }

        private static void SetEnableDamageEffects(bool enableDamageEffects)
        {
            m_currentGraphicsSettings.EnableDamageEffects = enableDamageEffects;
            MySandboxGame.Static.EnableDamageEffects = enableDamageEffects;
        }

        private static void SetHardwareCursor(bool useHardwareCursor)
        {
            m_currentGraphicsSettings.HardwareCursor = useHardwareCursor;
            MySandboxGame.Static.SetMouseVisible(IsHardwareCursorUsed());
            MyGuiSandbox.SetMouseCursorVisibility(IsHardwareCursorUsed(), false);
        }

        public static ChangeResult Apply(MyGraphicsSettings settings)
        {
            MySandboxGame.Log.WriteLine("MyVideoModeManager.Apply(MyGraphicsSettings1)");
            using (MySandboxGame.Log.IndentUsing())
            {
                MySandboxGame.Log.WriteLine("HardwareCursor: " + settings.HardwareCursor);
                MySandboxGame.Log.WriteLine("Field of view: " + settings.FieldOfView);
                MySandboxGame.Log.WriteLine("Render.InterpolationEnabled: " + settings.Render.InterpolationEnabled);
                MySandboxGame.Log.WriteLine("Render.GrassDensityFactor: " + settings.Render.GrassDensityFactor);
                //MySandboxGame.Log.WriteLine("Render.TonemappingEnabled: " + settings.Render.TonemappingEnabled);
                MySandboxGame.Log.WriteLine("Render.AntialiasingMode: " + settings.Render.AntialiasingMode);
                MySandboxGame.Log.WriteLine("Render.ShadowQuality: " + settings.Render.ShadowQuality);
                MySandboxGame.Log.WriteLine("Render.AmbientOcclusionEnabled: " + settings.Render.AmbientOcclusionEnabled);
                MySandboxGame.Log.WriteLine("Render.TextureQuality: " + settings.Render.TextureQuality);
                MySandboxGame.Log.WriteLine("Render.AnisotropicFiltering: " + settings.Render.AnisotropicFiltering);
                MySandboxGame.Log.WriteLine("Render.FoliageDetails: " + settings.Render.FoliageDetails);

                if (m_currentGraphicsSettings.Equals(ref settings))
                    return ChangeResult.NothingChanged;
            
                SetEnableDamageEffects(settings.EnableDamageEffects);
                SetFov(settings.FieldOfView);
                SetHardwareCursor(settings.HardwareCursor);

                if (!m_currentGraphicsSettings.Render.Equals(ref settings.Render))
                {
                    MyRenderProxy.SwitchRenderSettings(settings.Render);
                }

                m_currentGraphicsSettings = settings;

                MySector.Lodding.SelectQuality(settings.Render.ModelQuality);
            }

            return ChangeResult.Success;
        }

        private static void SetFov(float fov)
        {
            if (m_currentGraphicsSettings.FieldOfView == fov)
                return;

            m_currentGraphicsSettings.FieldOfView = fov;
            if (MySector.MainCamera != null)
            {
                MySector.MainCamera.FieldOfView = fov;

                if (MySector.MainCamera.Zoom != null)
                {
                    MySector.MainCamera.Zoom.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                }
            }
        }

        public static ChangeResult ApplyVideoSettings(MyRenderDeviceSettings deviceSettings, MyGraphicsSettings graphicsSettings)
        {
            var res = Apply(deviceSettings);
            if (res == ChangeResult.Failed)
                return res;

            var res2 = Apply(graphicsSettings);
            Debug.Assert(res2 != ChangeResult.Failed, "Changing graphics settings should never fail, only device settings can!");
            return res == ChangeResult.Success ? res : res2;
        }

        private static bool IsSupportedDisplayMode(int videoAdapter, int width, int height, MyWindowModeEnum windowMode)
        {
            bool result = false;

            if (windowMode == MyWindowModeEnum.Fullscreen)
            {
                foreach (var mode in m_adapters[videoAdapter].SupportedDisplayModes)
                {
                    if (mode.Width == width && mode.Height == height)
                        result = true;
                }
            }
            else
            {
                result = true;
            }

            int maxTextureSize = m_adapters[MySandboxGame.Config.VideoAdapter].MaxTextureSize;
            if (width > maxTextureSize || height > maxTextureSize)
            {
                MySandboxGame.Log.WriteLine(
                    string.Format("VideoMode {0}x{1} requires texture size which is not supported by this HW (this HW supports max {2})",
                        width, height, maxTextureSize));
                result = false;
            }
            return result;
        }

        private static bool IsVirtualized(string manufacturer, string model)
        {
            manufacturer = manufacturer.ToLower();
            return manufacturer == "microsoft corporation" || manufacturer.Contains("vmware") || model == "VirtualBox" || model.ToLower().Contains("virtual");
        }

        public static void LogEnvironmentInformation()
        {
            MySandboxGame.Log.WriteLine("MyVideoModeManager.LogEnvironmentInformation - START");
            MySandboxGame.Log.IncreaseIndent();

            try
            {
                ManagementObjectSearcher mosComputer = new System.Management.ManagementObjectSearcher("Select Manufacturer, Model from Win32_ComputerSystem");
                if (mosComputer != null)
                {
                    foreach (var item in mosComputer.Get())
                    {
                        MySandboxGame.Log.WriteLine("Win32_ComputerSystem.Manufacturer: " + item["Manufacturer"]);
                        MySandboxGame.Log.WriteLine("Win32_ComputerSystem.Model: " + item["Model"]);
                        MySandboxGame.Log.WriteLine("Virtualized: " + IsVirtualized(item["Manufacturer"].ToString(), item["Model"].ToString()));
                    }
                }

                ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name FROM Win32_Processor");
                if (mos != null)
                {
                    foreach (ManagementObject mo in mos.Get())
                    {
                        MySandboxGame.Log.WriteLine("Environment.ProcessorName: " + mo["Name"]);
                    }
                }

#if !XB1
                //  Get info about memory
                var memory = new WinApi.MEMORYSTATUSEX();
                WinApi.GlobalMemoryStatusEx(memory);

                MySandboxGame.Log.WriteLine("ComputerInfo.TotalPhysicalMemory: " + MyValueFormatter.GetFormatedLong((long)memory.ullTotalPhys) + " bytes");
                MySandboxGame.Log.WriteLine("ComputerInfo.TotalVirtualMemory: " + MyValueFormatter.GetFormatedLong((long)memory.ullTotalVirtual) + " bytes");
                MySandboxGame.Log.WriteLine("ComputerInfo.AvailablePhysicalMemory: " + MyValueFormatter.GetFormatedLong((long)memory.ullAvailPhys) + " bytes");
                MySandboxGame.Log.WriteLine("ComputerInfo.AvailableVirtualMemory: " + MyValueFormatter.GetFormatedLong((long)memory.ullAvailVirtual) + " bytes");
#else // XB1
                MySandboxGame.Log.WriteLine("ComputerInfo.TotalPhysicalMemory: N/A (XB1 TODO?)");
                MySandboxGame.Log.WriteLine("ComputerInfo.TotalVirtualMemory: N/A (XB1 TODO?)");
                MySandboxGame.Log.WriteLine("ComputerInfo.AvailablePhysicalMemory: N/A (XB1 TODO?)");
                MySandboxGame.Log.WriteLine("ComputerInfo.AvailableVirtualMemory: N/A (XB1 TODO?)");
#endif // XB1

                //  Get info about hard drives
                ConnectionOptions oConn = new ConnectionOptions();
                ManagementScope oMs = new ManagementScope("\\\\localhost", oConn);
                ObjectQuery oQuery = new ObjectQuery("select FreeSpace,Size,Name from Win32_LogicalDisk where DriveType=3");
                using (ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery))
                {
                    ManagementObjectCollection oReturnCollection = oSearcher.Get();
                    foreach (ManagementObject oReturn in oReturnCollection)
                    {
                        string capacity = MyValueFormatter.GetFormatedLong(Convert.ToInt64(oReturn["Size"]));
                        string freeSpace = MyValueFormatter.GetFormatedLong(Convert.ToInt64(oReturn["FreeSpace"]));
                        string name = oReturn["Name"].ToString();
                        MySandboxGame.Log.WriteLine("Drive " + name + " | Capacity: " + capacity + " bytes | Free space: " + freeSpace + " bytes");
                    }
                    oReturnCollection.Dispose();
                }
            }
            catch (Exception e)
            {
                MySandboxGame.Log.WriteLine("Error occured during enumerating environment information. Application is continuing. Exception: " + e.ToString());
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVideoModeManager.LogEnvironmentInformation - END");
        }

        public static void LogApplicationInformation()
        {
            MySandboxGame.Log.WriteLine("MyVideoModeManager.LogApplicationInformation - START");
            MySandboxGame.Log.IncreaseIndent();

            try
            {
#if !XB1
                Assembly assembly = Assembly.GetExecutingAssembly();
                MySandboxGame.Log.WriteLine("Assembly.GetName: " + assembly.GetName().ToString());
                MySandboxGame.Log.WriteLine("Assembly.FullName: " + assembly.FullName);
                MySandboxGame.Log.WriteLine("Assembly.Location: " + assembly.Location);
                MySandboxGame.Log.WriteLine("Assembly.ImageRuntimeVersion: " + assembly.ImageRuntimeVersion);
#else // XB1
                MySandboxGame.Log.WriteLine("Assembly.GetName: N/A (on XB1)");
                MySandboxGame.Log.WriteLine("Assembly.FullName: N/A (on XB1)");
                MySandboxGame.Log.WriteLine("Assembly.Location: N/A (on XB1)");
                MySandboxGame.Log.WriteLine("Assembly.ImageRuntimeVersion: N/A (on XB1)");
#endif // XB1
            }
            catch (Exception e)
            {
                MySandboxGame.Log.WriteLine("Error occured during enumerating application information. Application will still continue. Detail description: " + e.ToString());
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyVideoModeManager.LogApplicationInformation - END");
        }

        public static bool IsTripleHead()
        {
            return m_currentDeviceIsTripleHead;
        }

        public static bool IsTripleHead(Vector2I screenSize)
        {
            float aspectRatio = (float)screenSize.X / (float)screenSize.Y;
            return GetAspectRatio(GetClosestAspectRatio(aspectRatio)).IsTripleHead;
        }

        public static bool IsHardwareCursorUsed()
        {
#if !XB1
            // Never use hardware cursor in the exteral editor
            if (Sandbox.AppCode.MyExternalAppBase.Static != null) return false;

            var osVersion = Environment.OSVersion;

            // Windows Vista
            if (osVersion.Platform == PlatformID.Win32NT && osVersion.Version.Major == 6 && osVersion.Version.Minor == 0)
                return false;

            // Windows XP
            if (osVersion.Platform == PlatformID.Win32NT && osVersion.Version.Major == 5 && osVersion.Version.Minor == 1)
                return false;

            return m_currentGraphicsSettings.HardwareCursor;
#else // XB1
            return false;
#endif // XB1
        }

        public static MyAspectRatio GetAspectRatio(MyAspectRatioEnum aspectRatioEnum)
        {
            return m_aspectRatios[(int)aspectRatioEnum];
        }

        public static MyAspectRatio GetRecommendedAspectRatio(int adapterIndex)
        {
            return m_recommendedAspectRatio[adapterIndex];
        }

        //  Finds aspect ratio that is closest to aspectRatio paremeter aspect ratio (we assume that this aspect ration is good)
        public static MyAspectRatioEnum GetClosestAspectRatio(float aspectRatio)
        {
            MyAspectRatioEnum closestAspectRatioEnum = default(MyAspectRatioEnum);

            float closestDistance = float.MaxValue;
            for (int i = 0; i < m_aspectRatios.Length; i++)
            {
                float tempDistance = Math.Abs(aspectRatio - m_aspectRatios[i].AspectRatioNumber);
                if (tempDistance < closestDistance)
                {
                    closestDistance = tempDistance;
                    closestAspectRatioEnum = m_aspectRatios[i].AspectRatioEnum;
                }
            }

            return closestAspectRatioEnum;
        }

        public static void GetFovBounds(out float minRadians, out float maxRadians)
        {
            GetFovBounds((float)m_currentDeviceSettings.BackBufferWidth / (float)m_currentDeviceSettings.BackBufferHeight,
                out minRadians, out maxRadians);
        }

        public static void GetFovBounds(float aspectRatio, out float minRadians, out float maxRadians)
        {
            minRadians = MyConstants.FIELD_OF_VIEW_CONFIG_MIN;
            if (aspectRatio >= (12.0 / 3.0))
            {
                maxRadians = MyConstants.FIELD_OF_VIEW_CONFIG_MAX_TRIPLE_HEAD;
            }
            else if (aspectRatio >= (8.0 / 3.0))
            {
                maxRadians = MyConstants.FIELD_OF_VIEW_CONFIG_MAX_DUAL_HEAD;
            }
            else
            {
                maxRadians = MyConstants.FIELD_OF_VIEW_CONFIG_MAX;
            }
        }

        internal static void OnVideoAdaptersResponse(MyRenderMessageVideoAdaptersResponse message)
        {
            MyRenderProxy.Log.WriteLine("MyVideoSettingsManager.OnVideoAdaptersResponse");
            using (MyRenderProxy.Log.IndentUsing(LoggingOptions.NONE))
            {
                m_adapters = message.Adapters;

                int currentAdapterIndex = -1;
                MyAdapterInfo currentAdapter;
                currentAdapter.Priority = 1000;
                try
                {
                    currentAdapterIndex = MySandboxGame.Static.GameRenderComponent.RenderThread.CurrentAdapter;
                    currentAdapter = m_adapters[currentAdapterIndex];
                    GpuUnderMinimum = !(currentAdapter.Has512MBRam || currentAdapter.VRAM >= (512 * 1024 * 1024));
                }
                catch { }

                m_recommendedAspectRatio = new Dictionary<int, MyAspectRatio>();

                if (m_adapters.Length == 0)
                {
                    MyRenderProxy.Log.WriteLine("ERROR: Adapters count is 0!");
                }

                for (int adapterIndex = 0; adapterIndex < m_adapters.Length; ++adapterIndex)
                {
                    var adapter = m_adapters[adapterIndex];
                    MyRenderProxy.Log.WriteLine(string.Format("Adapter {0}", adapter));
                    using (MyRenderProxy.Log.IndentUsing(LoggingOptions.NONE))
                    {
                        float aspectRatio = (float)adapter.CurrentDisplayMode.Width / (float)adapter.CurrentDisplayMode.Height;
                        m_recommendedAspectRatio.Add(adapterIndex, GetAspectRatio(GetClosestAspectRatio(aspectRatio)));

                        if (adapter.SupportedDisplayModes.Length == 0)
                        {
                            MyRenderProxy.Log.WriteLine(string.Format("WARNING: Adapter {0} count of supported display modes is 0!", adapterIndex));
                        }

                        int maxTextureSize = adapter.MaxTextureSize;
                        foreach (var mode in adapter.SupportedDisplayModes)
                        {
                            MyRenderProxy.Log.WriteLine(mode.ToString());
                            if (mode.Width > maxTextureSize || mode.Height > maxTextureSize)
                            {
                                MyRenderProxy.Log.WriteLine(
                                    string.Format("WARNING: Display mode {0} requires texture size which is not supported by this HW (this HW supports max {1})",
                                        mode, maxTextureSize));
                            }
                        }
                    }

                    MySandboxGame.ShowIsBetterGCAvailableNotification |= 
                        currentAdapterIndex != adapterIndex && currentAdapter.Priority < adapter.Priority;
                }
            }
        }

        internal static void OnCreatedDeviceSettings(MyRenderMessageCreatedDeviceSettings message)
        {
            m_currentDeviceSettings = message.Settings;
            m_currentDeviceSettings.NewAdapterOrdinal = m_currentDeviceSettings.AdapterOrdinal;

            float aspectRatio = (float)m_currentDeviceSettings.BackBufferWidth / (float)m_currentDeviceSettings.BackBufferHeight;
            m_currentDeviceIsTripleHead = GetAspectRatio(GetClosestAspectRatio(aspectRatio)).IsTripleHead;
        }

        public static void SaveCurrentSettings()
        {
            var config = MySandboxGame.Config;

            config.VideoAdapter        = m_currentDeviceSettings.NewAdapterOrdinal; // Use the new value for the next game startup
            config.ScreenWidth         = m_currentDeviceSettings.BackBufferWidth;
            config.ScreenHeight        = m_currentDeviceSettings.BackBufferHeight;
            config.RefreshRate         = m_currentDeviceSettings.RefreshRate;
            config.WindowMode          = m_currentDeviceSettings.WindowMode;
            config.VerticalSync        = m_currentDeviceSettings.VSync;
            config.HardwareCursor      = m_currentGraphicsSettings.HardwareCursor;
            config.Dx9RenderQuality    = MyRenderConstants.RenderQualityProfile.RenderQuality;
            config.FieldOfView         = m_currentGraphicsSettings.FieldOfView;
            config.GraphicsRenderer    = m_currentGraphicsSettings.GraphicsRenderer;
            config.VegetationDrawDistance = m_currentGraphicsSettings.VegetationDrawDistance;

            // Don't want these to show up in configs for now
            var render = m_currentGraphicsSettings.Render;
            config.RenderInterpolation    = render.InterpolationEnabled;
            config.GrassDensityFactor     = render.GrassDensityFactor;
            //config.Tonemapping            = render.TonemappingEnabled == DEFAULT_TONEMAPPING ? (bool?)null : render.TonemappingEnabled;
            config.AntialiasingMode       = render.AntialiasingMode == DEFAULT_ANTI_ALIASING ? (MyAntialiasingMode?)null : render.AntialiasingMode;
            config.ShadowQuality          = render.ShadowQuality == DEFAULT_SHADOW_QUALITY ? (MyShadowsQuality?)null : render.ShadowQuality;
            config.AmbientOcclusionEnabled = render.AmbientOcclusionEnabled == DEFAULT_AMBIENT_OCCLUSION_ENABLED ? (bool?)null : render.AmbientOcclusionEnabled;
            config.TextureQuality         = render.TextureQuality == DEFAULT_TEXTURE_QUALITY ? (MyTextureQuality?)null : render.TextureQuality;
            config.AnisotropicFiltering   = render.AnisotropicFiltering == DEFAULT_ANISOTROPIC_FILTERING ? (MyTextureAnisoFiltering?)null : render.AnisotropicFiltering;
            config.FoliageDetails         = render.FoliageDetails == DEFAULT_FOLIAGE_DETAILS ? (MyFoliageDetails?)null : render.FoliageDetails;
            config.ModelQuality           = render.ModelQuality == MyRenderQualityEnum.HIGH ? (MyRenderQualityEnum?)null : render.ModelQuality;
            config.VoxelQuality           = render.VoxelQuality == MyRenderQualityEnum.HIGH ? (MyRenderQualityEnum?)null : render.VoxelQuality;

            config.LowMemSwitchToLow = MyConfig.LowMemSwitch.ARMED;
            config.Save();
        }

    }
}