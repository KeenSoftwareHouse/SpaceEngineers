using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using VRageRender.Resources;
using Resource = SharpDX.Direct3D11.Resource;
using System.Diagnostics;

using System.Timers;

namespace VRageRender
{
    public static class MyRenderSettings1Extensions
    {
        public static bool IsMultisampled(this MyAntialiasingMode aaMode)
        {
            switch(aaMode)
            {
                case MyAntialiasingMode.MSAA_2:
                case MyAntialiasingMode.MSAA_4:
                case MyAntialiasingMode.MSAA_8:
                    return true;
            }
            return false;
        }

        public static int SamplesCount(this MyAntialiasingMode aaMode)
        {
            switch (aaMode)
            {
                case MyAntialiasingMode.NONE:
                case MyAntialiasingMode.FXAA:
                    return 1;
                case MyAntialiasingMode.MSAA_2:
                    return 2;
                case MyAntialiasingMode.MSAA_4:
                    return 4;
                case MyAntialiasingMode.MSAA_8:
                    return 8;
                default:
                    return -1;

            }
        }

        public static int Resolution(this MyShadowsQuality shadowQuality)
        {
            switch (shadowQuality)
            {
                case MyShadowsQuality.LOW:
                    return 512;
                case MyShadowsQuality.HIGH:
                    return 1024;
            }
            return -1;
        }

        public static int MipmapsToSkip(this MyTextureQuality quality, int width, int height)
        {
            if (MyRender11Options.NsightDebugging)
            {
                return (int)Math.Max(0, (((uint)Math.Log(Math.Min(width, height), 2)) - 3));
            }

            switch( quality)
            {
                case MyTextureQuality.LOW:
                    return (width > 32) && (height > 32) ? 2 : 0;
                    
                case MyTextureQuality.MEDIUM:
                    return (width > 32) && (height > 32) ? 1 : 0;
                    
                case MyTextureQuality.HIGH:
                    return 0;
            }
            return 0;
        }

        public static int GrassDrawDistance(this MyFoliageDetails foliageDetails)
        {
            switch (foliageDetails)
            {
                case MyFoliageDetails.DISABLED:
                    return 0;
                case MyFoliageDetails.LOW:
                    return 50;
                case MyFoliageDetails.MEDIUM:
                    return 100;
                case MyFoliageDetails.HIGH:
                    return 250;
            }
            return 0;
        }
    }


    internal partial class MyRender11
    {
        internal static MyRenderSettings1 m_renderSettings;
        internal static MyRenderSettings1 RenderSettings { get { return m_renderSettings; } }

        internal static string ShaderMultisamplingDefine()
        {
            if (MultisamplingEnabled)
            {
                return String.Format("MS_SAMPLE_COUNT {0}", MultisamplingSampleCount);
            }
            return null;
        }

        internal static string ShaderSampleFrequencyDefine()
        {
            return "SAMPLE_FREQ_PASS";
        }

        internal static string ShaderMultisamplingHeader()
        {
            return MyShaderHelpers.FormatMacros(MyRender11.ShaderMultisamplingDefine());
        }

        internal static OnSettingsChangedDelegate m_settingsChangedListeners;

        internal static void RegisterSettingsChangedListener(OnSettingsChangedDelegate func)
        {
            if(m_settingsChangedListeners == null)
            {
                m_settingsChangedListeners = func;
            }
            else
            {
                m_settingsChangedListeners += func;
            }
        }

        internal static void LogUpdateRenderSettings(ref MyRenderSettings1 v)
        {
            MyRender11.Log.WriteLine("MyRenderSettings1 = {");
            MyRender11.Log.IncreaseIndent();
            MyRender11.Log.WriteLine("AntialiasingMode = " + v.AntialiasingMode);
            MyRender11.Log.WriteLine("FoliageDetails = " + v.FoliageDetails);
            MyRender11.Log.WriteLine("MultithreadingEnabled = " + v.MultithreadingEnabled);
            MyRender11.Log.WriteLine("TonemappingEnabled = " + v.TonemappingEnabled);
            MyRender11.Log.WriteLine("ShadowQuality = " + v.ShadowQuality);
            MyRender11.Log.WriteLine("TextureQuality = " + v.TextureQuality);
            MyRender11.Log.WriteLine("AnisotropicFiltering = " + v.AnisotropicFiltering);
            MyRender11.Log.WriteLine("InterpolationEnabled = " + v.InterpolationEnabled);
            MyRender11.Log.DecreaseIndent();
            MyRender11.Log.WriteLine("}");
        }

        internal static void UpdateRenderSettings(MyRenderSettings1 settings)
        {
            MyRender11.Log.WriteLine("UpdateRenderSettings");
            MyRender11.Log.IncreaseIndent();

            var prevSettings = m_renderSettings;
            m_renderSettings = settings;

            LogUpdateRenderSettings(ref settings);

            MyRenderProxy.Settings.EnableCameraInterpolation = settings.InterpolationEnabled;
            MyRenderProxy.Settings.EnableObjectInterpolation = settings.InterpolationEnabled;

            if (settings.ShadowQuality != prevSettings.ShadowQuality)
            {
                MyShadows.ResizeCascades();
            }

            if(settings.AntialiasingMode != prevSettings.AntialiasingMode)
            {
                GlobalShaderHeader = MyShaderHelpers.FormatMacros(MyRender11.ShaderMultisamplingDefine(), MyRender11.FxaaEnabled ? "FXAA_ENABLED" : null);
                MyShaders.Recompile();
                MyMaterialShaders.Recompile();
                MyRenderableComponent.MarkAllDirty();

                if (settings.AntialiasingMode.SamplesCount() != prevSettings.AntialiasingMode.SamplesCount())
                {
                    CreateScreenResources();
                }
            }

            if (settings.AnisotropicFiltering != prevSettings.AnisotropicFiltering)
            {
                UpdateTextureSampler();
            }
            
            if(settings.TextureQuality != prevSettings.TextureQuality)
            {
                //MyTextureManager.UnloadTextures();

                MyVoxelMaterials1.InvalidateMaterials();
                MyMeshMaterials1.InvalidateMaterials();
                MyTextures.ReloadQualityDependantTextures();

                //MyVoxelMaterials.ReloadTextures();
                //MyMaterialProxyFactory.ReloadTextures();
            }

            m_settingsChangedListeners();

            MyRender11.Log.DecreaseIndent();
        }

        // helpers
        internal static bool MultisamplingEnabled { get { return RenderSettings.AntialiasingMode.IsMultisampled(); } }
        internal static int MultisamplingSampleCount { get { return RenderSettings.AntialiasingMode.SamplesCount(); } }
        internal static bool FxaaEnabled { get { return RenderSettings.AntialiasingMode == MyAntialiasingMode.FXAA; } }

        internal static bool CommandsListsSupported { get; set; }
        internal static bool IsIntelBrokenCubemapsWorkaround { get; set; }

        internal static bool DeferredContextsEnabled { get { return !MyRender11.Settings.ForceImmediateContext && CommandsListsSupported; } }
        internal static bool MultithreadedRenderingEnabled { get { return DeferredContextsEnabled && MyRender11.Settings.EnableParallelRendering && MyRender11.RenderSettings.MultithreadingEnabled; } }
        internal static bool LoopObjectThenPass { get { return MyRender11.Settings.LoopObjectThenPass && DeferredContextsEnabled; } }


        static SwapChain m_swapchain = null;
        internal static MyBackbuffer Backbuffer { get; private set; }
        internal static Vector2I m_resolution;

        private static void ResizeSwapchain(int width, int height)
        {
            MyRender11.Context.ClearState();
            if (Backbuffer != null)
            {
                Backbuffer.Release();
                m_swapchain.ResizeBuffers(MyRender11Constants.BUFFER_COUNT, width, height, MyRender11Constants.BACKBUFFER_FORMAT, SwapChainFlags.AllowModeSwitch);
            }

            Backbuffer = new MyBackbuffer(m_swapchain.GetBackBuffer<Resource>(0));

            m_resolution = new Vector2I(width, height);
            CreateScreenResources();
        }

        internal static bool m_profilingStarted = false;

        const int PresentTimesStored = 5;
        internal static Queue<float> m_presentTimes = new Queue<float>();
        internal static Stopwatch m_presentTimer;
        internal static int m_consecutivePresentFails = 0;

        internal static void Present()
        {
            if (m_swapchain != null)
            {
                if (m_screenshot.HasValue)
                {
                    if (m_screenshot.Value.SizeMult == VRageMath.Vector2.One)
                    {
                        MyCopyToRT.ClearAlpha(Backbuffer);
                        SaveScreenshotFromResource(Backbuffer.m_resource);
                    }
                    else
                    {
                        TakeCustomSizedScreenshot(m_screenshot.Value.SizeMult);
                    }
                }

                MyRender11.GetRenderProfiler().StartProfilingBlock("Waiting for present");

                try
                {
                    m_swapchain.Present(MyRender11.m_settings.VSync ? 1 : 0, 0);
                    m_consecutivePresentFails = 0;

                    if (m_presentTimer == null)
                    {
                        m_presentTimer = new Stopwatch();
                    }
                    else
                    {
                        m_presentTimer.Stop();

                        if (m_presentTimes.Count >= PresentTimesStored)
                        {
                            m_presentTimes.Dequeue();
                        }
                        m_presentTimes.Enqueue(m_presentTimer.ElapsedMilliseconds);
                    }

                    m_presentTimer.Restart();
                }
                catch(SharpDXException e)
                {
                    MyRender11.Log.WriteLine("Device removed - resetting device");
                    HandleDeviceReset();
                    MyRender11.Log.WriteLine("Device removed - resetting completed");

                    m_consecutivePresentFails++;

                    if (m_consecutivePresentFails == 5)
                    {
                        MyRender11.Log.WriteLine("Present failed");
                        MyRender11.Log.IncreaseIndent();

                        if (e.Descriptor == SharpDX.DXGI.ResultCode.DeviceRemoved)
                        {
                            MyRender11.Log.WriteLine("Device removed: " + Device.DeviceRemovedReason);
                        }

                        var timings = "";
                        while (m_presentTimes.Count > 0)
                        {
                            timings += m_presentTimes.Dequeue();
                            if (m_presentTimes.Count > 0)
                            {
                                timings += ", ";
                            }
                        }

                        MyRender11.Log.WriteLine("Last present timings = [ " + timings + " ]");
                        MyRender11.Log.DecreaseIndent();
                    }
                }

                MyRender11.GetRenderProfiler().EndProfilingBlock();

                if(m_profilingStarted)
                {
                    MyGpuProfiler.IC_EndBlock();
                }

                MyGpuProfiler.EndFrame();
                MyGpuProfiler.StartFrame();
                m_profilingStarted = true;

                // waiting for change to fullscreen - window migh overlap or some other dxgi excuse to fail :(
                TryChangeToFullscreen();
            }
        }

        internal static void ClearBackbuffer(ColorBGRA clearColor)
        {
            if(Backbuffer != null)
                ImmediateContext.ClearRenderTargetView((Backbuffer as IRenderTargetBindable).RTV, new Color4(0.005f, 0, 0.01f, 1));            
        }

        internal static bool SettingsChanged(MyRenderDeviceSettings settings)
        {
            return !m_settings.Equals(ref settings);
        }

        internal static void FixModeDescriptionForFullscreen(ref ModeDescription md)
        {
            var list = m_adapterModes.Get(m_settings.AdapterOrdinal);
            if (list != null)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    if (
                        list[i].Height == m_settings.BackBufferHeight &&
                        list[i].Width == m_settings.BackBufferWidth &&
                        list[i].RefreshRate.Numerator == m_settings.RefreshRate)
                    {
                        md.Scaling = list[i].Scaling;
                        md.ScanlineOrdering = list[i].ScanlineOrdering;
                        md.RefreshRate = list[i].RefreshRate;
                        break;
                    }
                }
            }
        }

        internal static void RestoreFullscreenMode()
        {
            if (!m_changeToFullscreen.HasValue)
            {
                if (!m_swapchain.IsFullScreen && m_settings.WindowMode == MyWindowModeEnum.Fullscreen)
                {
                    ModeDescription md = new ModeDescription();
                    md.Format = MyRender11Constants.BACKBUFFER_FORMAT;
                    md.Height = m_settings.BackBufferHeight;
                    md.Width = m_settings.BackBufferWidth;
                    md.Scaling = DisplayModeScaling.Unspecified;
                    md.ScanlineOrdering = DisplayModeScanlineOrder.Progressive;
                    md.RefreshRate.Numerator = m_settings.RefreshRate;
                    md.RefreshRate.Denominator = 1000;

                    FixModeDescriptionForFullscreen(ref md);

                    m_changeToFullscreen = md;
                }
            }
        }

        static ModeDescription? m_changeToFullscreen = null;

        static void TryChangeToFullscreen()
        {
            if (m_changeToFullscreen.HasValue)
            {
                var md = m_changeToFullscreen.Value;

                try
                { 
                    var adapterDevId = m_adapterInfoList[m_settings.AdapterOrdinal].AdapterDeviceId;
                    var outputId = m_adapterInfoList[m_settings.AdapterOrdinal].OutputId;

                    m_swapchain.ResizeTarget(ref md);
                    m_swapchain.SetFullscreenState(true, GetFactory().Adapters[adapterDevId].Outputs.Length > outputId ? GetFactory().Adapters[adapterDevId].Outputs[outputId] : null);

                    md.RefreshRate.Numerator = 0;
                    md.RefreshRate.Denominator = 0;
                    m_swapchain.ResizeTarget(ref md);

                    m_changeToFullscreen = null;

                    MyRender11.Log.WriteLine("DXGI SetFullscreenState succeded");
                }
                catch(SharpDX.SharpDXException e)
                {
                    if(e.ResultCode == SharpDX.DXGI.ResultCode.Unsupported) 
                        m_changeToFullscreen = null;

                    MyRender11.Log.WriteLine("TryChangeToFullscreen failed with " + e.ResultCode);
                }
            }
        }

        static void ForceWindowed()
        {
            if (m_settings.WindowMode == MyWindowModeEnum.Fullscreen && m_swapchain != null)
            {
                m_swapchain.SetFullscreenState(false, null);     
            }
        }

        internal static void LogSettings(ref MyRenderDeviceSettings settings)
        {
            MyRender11.Log.WriteLine("MyRenderDeviceSettings = {");
            MyRender11.Log.IncreaseIndent();
            MyRender11.Log.WriteLine("Adapter id = " + settings.AdapterOrdinal);
            MyRender11.Log.WriteLine("DXGIAdapter id = " + m_adapterInfoList[settings.AdapterOrdinal].AdapterDeviceId);
            MyRender11.Log.WriteLine("DXGIOutput id = " + m_adapterInfoList[settings.AdapterOrdinal].OutputId);
            MyRender11.Log.WriteLine(String.Format("Resolution = {0} x {1}", settings.BackBufferWidth, settings.BackBufferHeight));
            MyRender11.Log.WriteLine("Window mode = " + settings.WindowMode);
            MyRender11.Log.DecreaseIndent();
            MyRender11.Log.WriteLine("}");
        }

        internal static void CheckAdapterChange(ref MyRenderDeviceSettings settings)
        {
            settings.AdapterOrdinal = ValidateAdapterIndex(settings.AdapterOrdinal);

            bool differentAdapter = m_adapterInfoList[m_settings.AdapterOrdinal].AdapterDeviceId != m_adapterInfoList[settings.AdapterOrdinal].AdapterDeviceId;

            if (differentAdapter)
            {
                m_settings = settings;
                HandleDeviceReset();
            }
        }

        internal static void ApplySettings(MyRenderDeviceSettings settings)
        {
            MyRender11.Log.WriteLine("ApplySettings");
            MyRender11.Log.IncreaseIndent();

            //bool differentAdapter = m_adapterInfoList[m_settings.AdapterOrdinal].AdapterDeviceId != m_adapterInfoList[settings.AdapterOrdinal].AdapterDeviceId;

            //if (differentAdapter)
            //{
            //    m_settings = settings;
            //    HandleDeviceReset();
            //}
            //else
            //{
            LogSettings(ref settings);

            CommandsListsSupported = m_adapterInfoList[m_settings.AdapterOrdinal].MultithreadedRenderingSupported;
            IsIntelBrokenCubemapsWorkaround = m_adapterInfoList[m_settings.AdapterOrdinal].Priority == 1; // 1 is intel

            bool deviceRemoved = false;

            try
            {
                ResizeSwapchain(settings.BackBufferWidth, settings.BackBufferHeight);
            }
            catch(SharpDXException e)
            {
                if (e.Descriptor == SharpDX.DXGI.ResultCode.DeviceRemoved && Device.DeviceRemovedReason == SharpDX.DXGI.ResultCode.DeviceRemoved)
                {
                    deviceRemoved = true;
                    MyRender11.Log.WriteLine("Device removed - resetting device");
                    HandleDeviceReset();
                    MyRender11.Log.WriteLine("Device removed - resetting completed");
                }
            }

            if(!deviceRemoved)
            {
                ModeDescription md = new ModeDescription();
                md.Format = MyRender11Constants.BACKBUFFER_FORMAT;
                md.Height = settings.BackBufferHeight;
                md.Width = settings.BackBufferWidth;
                md.Scaling = DisplayModeScaling.Unspecified;
                md.ScanlineOrdering = DisplayModeScanlineOrder.Progressive;
                md.RefreshRate.Numerator = 60000;
                md.RefreshRate.Denominator = 1000;

                FixModeDescriptionForFullscreen(ref md);

                // to fullscreen
                if (settings.WindowMode == MyWindowModeEnum.Fullscreen)
                {
                    if (settings.WindowMode != m_settings.WindowMode)
                    {
                        m_changeToFullscreen = md;
                    }
                    else
                    {
                        m_swapchain.ResizeTarget(ref md);
                        md.RefreshRate.Denominator = 0;
                        md.RefreshRate.Numerator = 0;
                        m_swapchain.ResizeTarget(ref md);
                    }
                }
                // from fullscreen
                else if (settings.WindowMode != m_settings.WindowMode && m_settings.WindowMode == MyWindowModeEnum.Fullscreen)
                {
                    m_swapchain.ResizeTarget(ref md);
                    m_swapchain.SetFullscreenState(false, null);
                }

                m_settings = settings;

                TryChangeToFullscreen();
            }

            MyRender11.Log.DecreaseIndent();
        }

        internal static Vector2I BackBufferResolution
        {
            get
            {
                return m_resolution;
            }
            private set {}
        }

        internal static Vector2I ViewportResolution
        {
            get 
            { 
                return BackBufferResolution; 
            }
        }


    }
}