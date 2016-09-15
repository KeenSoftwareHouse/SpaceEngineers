using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.Direct3D;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageMath;

using Resource = SharpDX.Direct3D11.Resource;


namespace VRageRender
{
    public static class MyRenderSettings1Extensions
    {
        public static bool IsMultisampled(this MyAntialiasingMode aaMode)
        {
            //switch(aaMode)
            //{
            //    case MyAntialiasingMode.MSAA_2:
            //    case MyAntialiasingMode.MSAA_4:
            //    case MyAntialiasingMode.MSAA_8:
            //        return true;
            //}
            return false;
        }

        public static int SamplesCount(this MyAntialiasingMode aaMode)
        {
            switch (aaMode)
            {
                case MyAntialiasingMode.NONE:
                case MyAntialiasingMode.FXAA:
                    return 1;
                //case MyAntialiasingMode.MSAA_2:
                //    return 2;
                //case MyAntialiasingMode.MSAA_4:
                //    return 4;
                //case MyAntialiasingMode.MSAA_8:
                //    return 8;
                default:
                    return -1;

            }
        }

        public static int ShadowCascadeResolution(this MyShadowsQuality shadowQuality)
        {
            switch (shadowQuality)
            {
                case MyShadowsQuality.DISABLED:
                case MyShadowsQuality.LOW:
                    return 512;
				case MyShadowsQuality.MEDIUM:
					return 1024;
                case MyShadowsQuality.HIGH:
                    return 1024;
            }
            return -1;
        }

		public static int BackOffset(this MyShadowsQuality shadowQuality)
		{
			switch(shadowQuality)
			{
                // CHECK-ME: This is no sense. Previous revision is the same
                case MyShadowsQuality.DISABLED:
                case MyShadowsQuality.LOW:
				case MyShadowsQuality.MEDIUM:
				case MyShadowsQuality.HIGH:
                    return (int)MyShadowCascades.Settings.Data.ShadowCascadeZOffset;
			}
			return -1;
		}

		public static float ShadowCascadeSplit(this MyShadowsQuality shadowQuality, int cascade)
		{
			if (cascade == 0)
				return 1.0f;

            var lastCascadeSplit = MyShadowCascades.Settings.Data.ShadowCascadeMaxDistance;
			switch(shadowQuality)
			{
				case MyShadowsQuality.MEDIUM:
                    lastCascadeSplit *= MyShadowCascades.Settings.Data.ShadowCascadeMaxDistanceMultiplierMedium;
					break;
				case MyShadowsQuality.HIGH:
                    lastCascadeSplit *= MyShadowCascades.Settings.Data.ShadowCascadeMaxDistanceMultiplierHigh;
					break;
			}

            var spreadFactor = MyShadowCascades.Settings.Data.ShadowCascadeSpreadFactor;
            float logSplit = (float)Math.Pow(lastCascadeSplit, (cascade + spreadFactor) / (MyShadowCascades.Settings.NewData.CascadesCount + spreadFactor));
            return logSplit;
		}

        public static MyShadowsQuality GetShadowsQuality(this MyShadowsQuality shadowQuality)
        {
            return shadowQuality;
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
            int drawDistance = (int)MyRender11.Settings.GrassMaxDrawDistance;
            switch (foliageDetails)
            {
                case MyFoliageDetails.DISABLED:
                    drawDistance *= 0;
                    break;
                case MyFoliageDetails.LOW:
                    drawDistance = (int)(drawDistance*0.2f);
                    break;
                case MyFoliageDetails.MEDIUM:
                    drawDistance = (int)(drawDistance*0.4f);
                    break;
                case MyFoliageDetails.HIGH:
                    drawDistance *= 1;
                    break;
            }
            return drawDistance;
        }
    }


    internal partial class MyRender11
    {
        internal static MyRenderSettings1 m_renderSettings;
        internal static MyRenderSettings1 RenderSettings { get { return m_renderSettings; } }

        internal static ShaderMacro? ShaderMultisamplingDefine()
        {
            if (FxaaEnabled)
                return new ShaderMacro("FXAA_ENABLED", null);
            else if (MultisamplingSampleCount > 1)
                return new ShaderMacro("MS_SAMPLE_COUNT", MultisamplingSampleCount);
            else return null;
        }

        private static readonly ShaderMacro[] m_shaderSampleFrequencyDefine = {new ShaderMacro("SAMPLE_FREQ_PASS", null)};
        internal static ShaderMacro[] ShaderSampleFrequencyDefine()
        {
            return m_shaderSampleFrequencyDefine;
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
            //Log.WriteLine("TonemappingEnabled = " + v.TonemappingEnabled);
            Log.WriteLine("ShadowQuality = " + v.ShadowQuality);
            Log.WriteLine("TextureQuality = " + v.TextureQuality);
            Log.WriteLine("AnisotropicFiltering = " + v.AnisotropicFiltering);
            Log.WriteLine("InterpolationEnabled = " + v.InterpolationEnabled);
            Log.DecreaseIndent();
            Log.WriteLine("}");
        }

        private static void UpdateAntialiasingMode(MyAntialiasingMode mode, MyAntialiasingMode oldMode)
        {
            var defineList = new List<ShaderMacro>();
            var msDefine = ShaderMultisamplingDefine();
            if (msDefine.HasValue)
                defineList.Add(msDefine.Value);

            MyShaders.SetGlobalMacros(defineList);

            MyShaders.Recompile();
            MyMaterialShaders.Recompile();
            MyRenderableComponent.MarkAllDirty();

            if (mode.SamplesCount() != oldMode.SamplesCount())
            {
                CreateScreenResources();
            }
        }

        internal static void UpdateRenderSettings(MyRenderSettings1 settings)
        {
            Log.WriteLine("UpdateRenderSettings");
            Log.IncreaseIndent();

            var prevSettings = m_renderSettings;
            m_renderSettings = settings;

            LogUpdateRenderSettings(ref settings);

            MyRenderConstants.SwitchRenderQuality(settings.Dx9Quality); //because of lod ranges

            // THIS IS SHIT! MOVE TO USE JUST ONE STRUCT FOR RENDER SETTINGS!!!!!
            MyRender11.Settings.EnableCameraInterpolation = settings.InterpolationEnabled;
            MyRender11.Settings.EnableObjectInterpolation = settings.InterpolationEnabled;

            MyRender11.Settings.GrassDensityFactor = settings.GrassDensityFactor;
            if (settings.GrassDensityFactor == 0.0f) m_renderSettings.FoliageDetails = MyFoliageDetails.DISABLED;
            MyRender11.Settings.VoxelQuality = settings.VoxelQuality;

            //       if(settings.GrassDensityFactor != prevSettings.GrassDensityFactor)
            //           MyRenderProxy.ReloadGrass();

            if (settings.ShadowQuality != prevSettings.ShadowQuality)
            {
                ResetShadows(MyShadowCascades.Settings.NewData.CascadesCount, settings.ShadowQuality.ShadowCascadeResolution());
            }

            if (settings.AntialiasingMode != prevSettings.AntialiasingMode)
                UpdateAntialiasingMode(settings.AntialiasingMode, prevSettings.AntialiasingMode);

            if (settings.AnisotropicFiltering != prevSettings.AnisotropicFiltering)
            {
                MySamplerStateManager.UpdateFiltering();
            }
            
            if(settings.TextureQuality != prevSettings.TextureQuality)
            {
                MyVoxelMaterials1.InvalidateMaterials();
                MyMeshMaterials1.InvalidateMaterials();
                MyManagers.FileTextures.DisposeTex(MyFileTextureManager.MyFileTextureHelper.IsQualityDependantFilter);

                //MyVoxelMaterials.ReloadTextures();
                //MyMaterialProxyFactory.ReloadTextures();
            }

            m_settingsChangedListeners();

            Log.DecreaseIndent();
        }

        // helpers
        internal static bool MultisamplingEnabled { get { return RenderSettings.AntialiasingMode.IsMultisampled(); } }
        internal static int MultisamplingSampleCount { get { return RenderSettings.AntialiasingMode.SamplesCount(); } }
        internal static bool FxaaEnabled { get { return RenderSettings.AntialiasingMode == MyAntialiasingMode.FXAA && m_debugOverrides.Postprocessing && m_debugOverrides.Fxaa; } }

        internal static bool CommandsListsSupported { get; set; }
        internal static bool IsIntelBrokenCubemapsWorkaround { get; set; }

        internal static bool DeferredContextsEnabled { get { return !Settings.ForceImmediateContext && CommandsListsSupported; } }
        internal static bool MultithreadedRenderingEnabled { get { return DeferredContextsEnabled && Settings.EnableParallelRendering; } }


        internal static SwapChain m_swapchain = null;
        internal static MyBackbuffer Backbuffer { get; private set; }
        internal static Vector2I m_resolution;

        private static void ResizeSwapchain(int width, int height)
        {
            RC.ClearState();
            if (Backbuffer != null)
            {
                Backbuffer.Release();
                m_swapchain.ResizeBuffers(MyRender11Constants.BUFFER_COUNT, width, height, MyRender11Constants.DX11_BACKBUFFER_FORMAT, SwapChainFlags.AllowModeSwitch);
            }

            Backbuffer = new MyBackbuffer(m_swapchain.GetBackBuffer<Texture2D>(0));
       
            m_resolution = new Vector2I(width, height);
            CreateScreenResources();
            ResetShadows(MyShadowCascades.Settings.NewData.CascadesCount, RenderSettings.ShadowQuality.ShadowCascadeResolution());
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
                GetRenderProfiler().StartProfilingBlock("Screenshot");
                if (m_screenshot.HasValue)
                {
                    if (m_screenshot.Value.SizeMult == VRageMath.Vector2.One)
                    {
                        MyCopyToRT.ClearAlpha(Backbuffer);
                        SaveScreenshotFromResource(Backbuffer.Resource);
                    }
                    else
                    {
                        TakeCustomSizedScreenshot(m_screenshot.Value.SizeMult);
                    }
                }
                GetRenderProfiler().EndProfilingBlock();

                try
                {
                    MyManagers.OnFrameEnd();
                    MyGpuProfiler.IC_BeginBlock("Waiting for present");
                    GetRenderProfiler().StartProfilingBlock("Waiting for present");
                    m_swapchain.Present(m_settings.VSync ? 1 : 0, 0);
                    GetRenderProfiler().EndProfilingBlock();
                    MyGpuProfiler.IC_EndBlock();

                    MyStatsUpdater.Timestamps.Update(ref MyStatsUpdater.Timestamps.Present, ref MyStatsUpdater.Timestamps.PreviousPresent);

                    if (VRage.OpenVRWrapper.MyOpenVR.Static != null)
                    {
                        MyGpuProfiler.IC_BeginBlock("OpenVR.FrameDone");
                        GetRenderProfiler().StartProfilingBlock("OpenVR.FrameDone");
                        /*var handle=MyOpenVR.GetOverlay("menu");
                        MyOpenVR.SetOverlayTexture(handle, MyRender11.Backbuffer.m_resource.NativePointer);
                         */
                        VRage.OpenVRWrapper.MyOpenVR.FrameDone();
                        GetRenderProfiler().EndProfilingBlock();
                        MyGpuProfiler.IC_EndBlock();
                    }

                    m_consecutivePresentFails = 0;

                    GetRenderProfiler().StartProfilingBlock("Stopwatch");
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
                    GetRenderProfiler().EndProfilingBlock();
                }
                catch(SharpDXException e)
                {
                    Log.WriteLine("Device removed - resetting device; reason: " + e.Message);
                    HandleDeviceReset();
                    Log.WriteLine("Device removed - resetting completed");

                    m_consecutivePresentFails++;

                    if (m_consecutivePresentFails == 5)
                    {
                        Log.WriteLine("Present failed");
                        Log.IncreaseIndent();

                        if (e.Descriptor == SharpDX.DXGI.ResultCode.DeviceRemoved)
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

                GetRenderProfiler().StartProfilingBlock("GPU profiler");
                MyGpuProfiler.EndFrame();
                MyGpuProfiler.StartFrame();
                m_profilingStarted = true;
                GetRenderProfiler().EndProfilingBlock();

                // waiting for change to fullscreen - window migh overlap or some other dxgi excuse to fail :(
                GetRenderProfiler().StartProfilingBlock("TryChangeToFullscreen");
                TryChangeToFullscreen();
                GetRenderProfiler().EndProfilingBlock();
            }
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
                if (m_swapchain != null && !m_swapchain.IsFullScreen && m_settings.WindowMode == MyWindowModeEnum.Fullscreen)
                {
                    ModeDescription md = new ModeDescription();
                    md.Format = MyRender11Constants.DX11_BACKBUFFER_FORMAT;
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

                    Log.WriteLine("DXGI SetFullscreenState succeded");
                }
                catch(SharpDX.SharpDXException e)
                {
                    if(e.ResultCode == SharpDX.DXGI.ResultCode.Unsupported) 
                        m_changeToFullscreen = null;

                    Log.WriteLine("TryChangeToFullscreen failed with " + e.ResultCode);
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
            if (settings.AdapterOrdinal >= 0 && settings.AdapterOrdinal < GetAdaptersList().Length)
            {
                Log.WriteLine("DXGIAdapter id = " + GetAdaptersList()[settings.AdapterOrdinal].AdapterDeviceId);
                Log.WriteLine("DXGIOutput id = " + GetAdaptersList()[settings.AdapterOrdinal].OutputId);
            }
            else 
            {
                Log.WriteLine("DXGIAdapter id = <autodetect>");
                Log.WriteLine("DXGIOutput id = <autodetect>");
            }
            Log.WriteLine(String.Format("Resolution = {0} x {1}", settings.BackBufferWidth, settings.BackBufferHeight));
            Log.WriteLine("Window mode = " + settings.WindowMode);
            Log.DecreaseIndent();
            Log.WriteLine("}");
        }

        internal static void CheckAdapterChange(ref MyRenderDeviceSettings settings)
        {
            settings.AdapterOrdinal = ValidateAdapterIndex(settings.AdapterOrdinal);

            bool differentAdapter = m_adapterInfoList[m_settings.AdapterOrdinal].AdapterDeviceId != m_adapterInfoList[settings.AdapterOrdinal].AdapterDeviceId;

            if (differentAdapter)
            {
                if (m_settings.UseStereoRendering)
                    settings.UseStereoRendering = true;
                m_settings = settings;
                HandleDeviceReset();
            }
        }

        internal static void ApplySettings(MyRenderDeviceSettings settings)
        {
            Log.WriteLine("ApplySettings");
            Log.IncreaseIndent();
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
                    Log.WriteLine("Device removed - resetting device" + e.Message);
                    HandleDeviceReset();
                    Log.WriteLine("Device removed - resetting completed");
                }
            }

            if(!deviceRemoved)
            {
                ModeDescription md = new ModeDescription();
                md.Format = MyRender11Constants.DX11_BACKBUFFER_FORMAT;
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

                if (m_settings.UseStereoRendering)
                    settings.UseStereoRendering = true;
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

        internal static Vector2I ViewportResolution
        {
            get 
            { 
                return BackBufferResolution; 
            }
        }


    }
}