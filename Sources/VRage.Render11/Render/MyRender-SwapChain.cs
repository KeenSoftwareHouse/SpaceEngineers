using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.DXGI;
using VRageMath;
using VRageRender.Resources;
using Resource = SharpDX.Direct3D11.Resource;
using Vector2 = VRageMath.Vector2;

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
        internal static MyRenderSettings1 RenderSettings => m_renderSettings;

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
            return MyShaderHelpers.FormatMacros(ShaderMultisamplingDefine());
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
            Log.WriteLine("MyRenderSettings1 = {");
            Log.IncreaseIndent();
            Log.WriteLine("AntialiasingMode = " + v.AntialiasingMode);
            Log.WriteLine("FoliageDetails = " + v.FoliageDetails);
            Log.WriteLine("MultithreadingEnabled = " + v.MultithreadingEnabled);
            Log.WriteLine("ShadowQuality = " + v.ShadowQuality);
            Log.WriteLine("TextureQuality = " + v.TextureQuality);
            Log.WriteLine("AnisotropicFiltering = " + v.AnisotropicFiltering);
            Log.WriteLine("InterpolationEnabled = " + v.InterpolationEnabled);
            Log.DecreaseIndent();
            Log.WriteLine("}");
        }

        internal static void UpdateRenderSettings(MyRenderSettings1 settings)
        {
            Log.WriteLine("UpdateRenderSettings");
            Log.IncreaseIndent();

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
                GlobalShaderHeader = MyShaderHelpers.FormatMacros(ShaderMultisamplingDefine(), FxaaEnabled ? "FXAA_ENABLED" : null);
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

            Log.DecreaseIndent();
        }

        // helpers
        internal static bool MultisamplingEnabled => RenderSettings.AntialiasingMode.IsMultisampled();
        internal static int MultisamplingSampleCount => RenderSettings.AntialiasingMode.SamplesCount();
        internal static bool FxaaEnabled => RenderSettings.AntialiasingMode == MyAntialiasingMode.FXAA;

        internal static bool CommandsListsSupported { get; set; }

        internal static bool DeferredContextsEnabled => !Settings.ForceImmediateContext && CommandsListsSupported;
        internal static bool MultithreadedRenderingEnabled => DeferredContextsEnabled && Settings.EnableParallelRendering && RenderSettings.MultithreadingEnabled;
        internal static bool LoopObjectThenPass => Settings.LoopObjectThenPass && DeferredContextsEnabled;


        static SwapChain m_swapchain;
        internal static MyBackbuffer Backbuffer { get; private set; }
        internal static Vector2I m_resolution;

        private static void ResizeSwapchain(int width, int height)
        {
            Context.ClearState();
            if (Backbuffer != null)
            {
                Backbuffer.Release();
                m_swapchain.ResizeBuffers(MyRender11Constants.BUFFER_COUNT, width, height, MyRender11Constants.BACKBUFFER_FORMAT, SwapChainFlags.AllowModeSwitch);
            }

            Backbuffer = new MyBackbuffer(m_swapchain.GetBackBuffer<Resource>(0));

            m_resolution = new Vector2I(width, height);
            CreateScreenResources();
        }

        internal static bool m_profilingStarted;

        const int PresentTimesStored = 5;
        internal static Queue<float> m_presentTimes = new Queue<float>();
        internal static Stopwatch m_presentTimer;
        internal static int m_consecutivePresentFails;

        internal static void Present()
        {
            if (m_swapchain != null)
            {
                if (m_screenshot.HasValue)
                {
                    if (m_screenshot.Value.SizeMult == Vector2.One)
                    {
                        SaveScreenshotFromResource(Backbuffer.m_resource);
                    }
                    else
                    {
                        TakeCustomSizedScreenshot(m_screenshot.Value.SizeMult);
                    }
                }

                GetRenderProfiler().StartProfilingBlock("Waiting for present");

                try
                {
                    m_swapchain.Present(m_settings.VSync ? 1 : 0, 0);
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
                    Log.WriteLine("Device removed - resetting device");
                    HandleDeviceReset();
                    Log.WriteLine("Device removed - resetting completed");

                    m_consecutivePresentFails++;

                    if (m_consecutivePresentFails == 5)
                    {
                        Log.WriteLine("Present failed");
                        Log.IncreaseIndent();

                        if (e.Descriptor == ResultCode.DeviceRemoved)
                        {
                            Log.WriteLine("Device removed: " + Device.DeviceRemovedReason);
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

                        Log.WriteLine("Last present timings = [ " + timings + " ]");
                        Log.DecreaseIndent();
                    }
                }

                GetRenderProfiler().EndProfilingBlock();

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

                    var list = m_adapterModes[m_settings.AdapterOrdinal];
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

                    m_changeToFullscreen = md;
                }
            }
        }

        static ModeDescription? m_changeToFullscreen;

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
                    m_swapchain.SetFullscreenState(true, GetFactory().Adapters[adapterDevId].Outputs[outputId]);

                    md.RefreshRate.Numerator = 0;
                    md.RefreshRate.Denominator = 0;
                    m_swapchain.ResizeTarget(ref md);

                    m_changeToFullscreen = null;

                    Log.WriteLine("DXGI SetFullscreenState succeded");
                }
                catch(SharpDXException e)
                {
                    // there might be some fatal exception, or minor exception saying that windows is overlapped/has no mouse focus and going to fullscreen should be done later
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
            Log.WriteLine("MyRenderDeviceSettings = {");
            Log.IncreaseIndent();
            Log.WriteLine("Adapter id = " + settings.AdapterOrdinal);
            Log.WriteLine("DXGIAdapter id = " + m_adapterInfoList[settings.AdapterOrdinal].AdapterDeviceId);
            Log.WriteLine("DXGIOutput id = " + m_adapterInfoList[settings.AdapterOrdinal].OutputId);
            Log.WriteLine(String.Format("Resolution = {0} x {1}", settings.BackBufferWidth, settings.BackBufferHeight));
            Log.WriteLine("Window mode = " + settings.WindowMode);
            Log.DecreaseIndent();
            Log.WriteLine("}");
        }

        internal static void CheckAdapterChange(MyRenderDeviceSettings settings)
        {
            bool differentAdapter = m_adapterInfoList[m_settings.AdapterOrdinal].AdapterDeviceId != m_adapterInfoList[settings.AdapterOrdinal].AdapterDeviceId;

            if (differentAdapter)
            {
                m_settings = settings;
                HandleDeviceReset();
            }
        }

        internal static void ApplySettings(MyRenderDeviceSettings settings)
        {
            Log.WriteLine("ApplySettings");
            Log.IncreaseIndent();

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

            bool deviceRemoved = false;

            try
            {
                ResizeSwapchain(settings.BackBufferWidth, settings.BackBufferHeight);
            }
            catch(SharpDXException e)
            {
                if (e.Descriptor == ResultCode.DeviceRemoved && Device.DeviceRemovedReason == ResultCode.DeviceRemoved)
                {
                    deviceRemoved = true;
                    Log.WriteLine("Device removed - resetting device");
                    HandleDeviceReset();
                    Log.WriteLine("Device removed - resetting completed");
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
                md.RefreshRate.Numerator = settings.RefreshRate;
                md.RefreshRate.Denominator = 1000;

                var list = m_adapterModes.Get(m_settings.AdapterOrdinal);
                if (list != null)
                {
                    for (int i = 0; i < list.Length; i++)
                    {
                        if (
                            list[i].Height == settings.BackBufferHeight &&
                            list[i].Width == settings.BackBufferWidth &&
                            list[i].RefreshRate.Numerator == settings.RefreshRate)
                        {
                            md.Scaling = list[i].Scaling;
                            md.ScanlineOrdering = list[i].ScanlineOrdering;
                            md.RefreshRate = list[i].RefreshRate;
                            break;
                        }
                    }
                }

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

            Log.DecreaseIndent();
        }

        internal static Vector2I BackBufferResolution
        {
            get
            {
                return m_resolution;
            }
            private set {}
        }

        internal static Vector2I ViewportResolution => BackBufferResolution;
    }
}