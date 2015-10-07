﻿
using System;

namespace VRageRender
{
    public enum MyWindowModeEnum : byte
    {
        // tutorials on web rely on these values
        Window = 0,
        FullscreenWindow = 1,
        Fullscreen = 2,
    }

    public struct MyRenderDeviceSettings : IEquatable<MyRenderDeviceSettings>
    {
        public int AdapterOrdinal;
        public MyWindowModeEnum WindowMode;
        public int BackBufferWidth;
        public int BackBufferHeight;
        public int RefreshRate; // Used only in Fullscreen
        public bool VSync;
        public bool DebugDrawOnly;

        public MyRenderDeviceSettings(int adapter, MyWindowModeEnum windowMode, int width, int height, int refreshRate, bool vsync)
        {
            this.AdapterOrdinal = adapter;
            this.WindowMode = windowMode;
            this.BackBufferWidth = width;
            this.BackBufferHeight = height;
            this.RefreshRate = refreshRate;
            this.VSync = vsync;

            DebugDrawOnly = false;
        }

        bool IEquatable<MyRenderDeviceSettings>.Equals(MyRenderDeviceSettings other)
        {
            return Equals(ref other);
        }

        public bool Equals(ref MyRenderDeviceSettings other)
        {
            return AdapterOrdinal == other.AdapterOrdinal
                && WindowMode == other.WindowMode
                && BackBufferWidth == other.BackBufferWidth
                && BackBufferHeight == other.BackBufferHeight
                && RefreshRate == other.RefreshRate
                && VSync == other.VSync;
        }
    }

    public struct MyViewport
    {
        public float OffsetX;
        public float OffsetY;
        public float Width;
        public float Height;

        public MyViewport(float width, float height)
        {
            OffsetX = 0;
            OffsetY = 0;
            Width = width;
            Height = height;
        }

        public MyViewport(float x, float y, float width, float height)
        {
            OffsetX = x;
            OffsetY = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Settings for whole render. To make settings per draw, use RenderSetup
    /// </summary>
    public class MyRenderSettings
    {
        public bool EnableHWOcclusionQueries = true;

        public bool SkipLOD_NEAR = false;
        public bool SkipLOD_0 = false;
        public bool SkipLOD_1 = false;

        public bool SkipVoxels = false;

        //Debug properties
        public bool TearingTest = false;
        public bool MultimonTest = false;
        public bool ShowEnvironmentScreens = false;
        public bool ShowBlendedScreens = false;
        public bool ShowGreenBackground = false;
        public bool ShowLod1WithRedOverlay = false;

        public bool EnableLightsRuntime = true;
        public bool EnableSpotLights = true;
        public bool EnablePointLights = true;
        public bool EnableLightGlares = true;

        public bool ShowEnhancedRenderStatsEnabled = false;
        public bool ShowResourcesStatsEnabled = false;
        public bool ShowTexturesStatsEnabled = false;

        public bool EnableSun = true;
        public bool EnableShadows = true;
        public bool EnableAsteroidShadows = false;
        public bool EnableFog = true;

        public bool DebugRenderClipmapCells = false;
        public static bool DebugClipmapLodColor = false;

        bool m_enableEnvironmentMapAmbient = true;
        public bool EnableEnvironmentMapAmbient
        {
            get { return m_enableEnvironmentMapAmbient; }
            set
            {
                if (m_enableEnvironmentMapAmbient != value)
                {
                    m_enableEnvironmentMapAmbient = value;
                    if (value)
                        MyRenderProxy.ResetEnvironmentProbes();
                }
            }
        }

        bool m_enableEnvironmentMapReflection = true;
        public bool EnableEnvironmentMapReflection
        {
            get { return m_enableEnvironmentMapReflection; }
            set
            {
                if (m_enableEnvironmentMapReflection != value)
                {
                    m_enableEnvironmentMapReflection = value;
                    if (value)
                        MyRenderProxy.ResetEnvironmentProbes();
                }
            }
        }

        public static bool EnableVoxelAo = true;
        public static float VoxelAoMin = 0.600f;
        public static float VoxelAoMax = 1.000f;
        public static float VoxelAoOffset = 0.210f;

        public bool Wireframe = false;
        public bool EnableStencilOptimization = true;
        public bool EnableStencilOptimizationLOD1 = true;
        public bool ShowStencilOptimization = false;

        public bool CheckDiffuseTextures = true;
        public bool CheckNormalTextures = false;

        public bool ShowSpecularIntensity = false;
        public bool ShowSpecularPower = false;
        public bool ShowEmissivity = false;
        public bool ShowReflectivity = false;

        public bool EnableSpotShadows = true;

        public bool VisualizeOverdraw = false;
       
        // Render interpolation time, lower time equals less smooth but more responsive
        // This value should be from interval (0, 2x update interval), good value is "update interval" + "upper usual update time"
        public float InterpolationLagMs = 22;
        public float LagFeedbackMult = 0.25f;
        public bool EnableCameraInterpolation = false;
        public bool EnableObjectInterpolation = false;

        //
        public bool DisplayGbufferColor = false;
        public bool DisplayGbufferColorLinear = false;
        public bool DisplayGbufferNormal = false;
        public bool DisplayGbufferGlossiness = false;
        public bool DisplayGbufferMetalness = false;
        public bool DisplayGbufferMaterialID = false;
        public bool DisplayAO = false;
        public bool DisplayEmissive = false;
        public bool DisplayEdgeMask = false;
        public bool DisplayNDotL = false;

        public bool DisplayAmbientDiffuse = false;
        public bool DisplayAmbientSpecular = false;
        
        public bool DisplayIDs = false;
        public bool DisplayAabbs = false;
        public bool DrawOnlyMergedMeshes = false;

        public float TerrainDetailD0 = 5;
        public float TerrainDetailD1 = 40;
        public float TerrainDetailD2 = 125.0f;
        public float TerrainDetailD3 = 200;

        public bool FreezeTerrainQueries = false;

        public bool GrassPostprocess = true;
        public float GrassPostprocessCloseDistance = 25f;
        public float GrassGeometryClippingDistance = 500f;
        public float GrassGeometryScalingNearDistance = 100f;
        public float GrassGeometryScalingFarDistance = 400f;
        public float GrassGeometryDistanceScalingFactor = 4f;
		public float GrassDensityFactor = 1.0f;

        // Shadows
		public float FarShadowDistanceOverride = -1.0f;	
		public double LongShadowFactor = -1.0;          
		public float ShadowFadeoutMultiplier = 0.0f;    // 1 - [Shadow opacity]

        public bool DisplayShadowsWithDebug = false;
        public bool ShadowInterleaving = false;         // Blinking moving asteroids

        // Shadow cascades
        public int ShadowCascadeCount = 4;
        public bool ShowShadowCascadeSplits = false;
        public bool UpdateCascadesEveryFrame = true;
        public float ShadowCascadeMaxDistance = 1000.0f;
        public float ShadowCascadeMaxDistanceMultiplierMedium = 2f;
        public float ShadowCascadeMaxDistanceMultiplierHigh = 3.5f;
		public float ShadowCascadeSpreadFactor = 0.5f;
		public float ShadowCascadeZOffset = 400;
        public bool[] ShadowCascadeFrozen;
        public float[] ShadowCascadeSmallSkipThresholds;
        /*public float Cascade0SmallSkipThreshold = 0;
        public float Cascade1SmallSkipThreshold = 730.0f;
        public float Cascade2SmallSkipThreshold = 730.0f;
        public float Cascade3SmallSkipThreshold = 8000.0f;*/

        public bool EnableParallelRendering = true;
        public bool ForceImmediateContext = false;
        public bool AmortizeBatchWork = true;
        public float RenderBatchSize = 100;
        public bool LoopObjectThenPass = false;
        public bool RenderThreadAsWorker = true;

        public bool EnableTonemapping = true;
        public bool DispalyHdrDebug = false;
        public float AdaptationTau = 0.3f;
        public float LuminanceExposure = 0.51f;
        public float Contrast = 0.006f;
        public float Brightness = 0;
        public float MiddleGrey = 0;
        public float BloomExposure = 0.5f;
        public float BloomMult = 0.25f;
        public float MiddleGreyCurveSharpness = 3.0f;
        public float MiddleGreyAt0 = 0.005f;
        public float BlueShiftRapidness = 0.01f;
        public float BlueShiftScale = 0.5f;

        public float BacklightMult = 0;
        public float EnvMult = 1;

        public float FogDensity = 0.003f;
        public float FogMult = 0.295f;
        public float FogYOffset = 720.0f;
        public VRageMath.Vector4 FogColor = new VRageMath.Vector4(60/255f, 119/255f, 255/255f, 0);

        public float WindStrength = 0.3f;
        public float WindAzimuth = 0;

        public float FoliageLod0Distance = 25;
        public float FoliageLod1Distance = 50;
        public float FoliageLod2Distance = 100;
        public float FoliageLod3Distance = 100;
        public bool EnableFoliageDebug = false;
        public bool FreezeFoliageViewer = false;

        public static bool PerInstanceLods = false;

        internal void Synchronize(MyRenderSettings settings)
        {
			CheckArrays(settings);

            EnableHWOcclusionQueries = settings.EnableHWOcclusionQueries;

            SkipLOD_NEAR = settings.SkipLOD_NEAR;
            SkipLOD_0 = settings.SkipLOD_0;
            SkipLOD_1 = settings.SkipLOD_1;

            SkipVoxels = settings.SkipVoxels;

            //Debug properties
            ShowEnvironmentScreens = settings.ShowEnvironmentScreens;
            ShowBlendedScreens = settings.ShowBlendedScreens;
            ShowGreenBackground = settings.ShowGreenBackground;
            ShowLod1WithRedOverlay = settings.ShowLod1WithRedOverlay;

            EnableLightsRuntime = settings.EnableLightsRuntime;
            ShowEnhancedRenderStatsEnabled = settings.ShowEnhancedRenderStatsEnabled;
            ShowResourcesStatsEnabled = settings.ShowResourcesStatsEnabled;
            ShowTexturesStatsEnabled = settings.ShowTexturesStatsEnabled;

            EnableSun = settings.EnableSun;
            EnableShadows = settings.EnableShadows;
            EnableAsteroidShadows = settings.EnableAsteroidShadows;
            EnableFog = settings.EnableFog;

            DebugRenderClipmapCells = settings.DebugRenderClipmapCells;

            EnableEnvironmentMapAmbient = settings.EnableEnvironmentMapAmbient;
            EnableEnvironmentMapReflection = settings.EnableEnvironmentMapReflection;

            ShowShadowCascadeSplits = settings.ShowShadowCascadeSplits;
            ShadowInterleaving = settings.ShadowInterleaving;
            UpdateCascadesEveryFrame = settings.UpdateCascadesEveryFrame;
			ShadowCascadeMaxDistanceMultiplierMedium = settings.ShadowCascadeMaxDistanceMultiplierMedium;
			ShadowCascadeMaxDistanceMultiplierHigh = settings.ShadowCascadeMaxDistanceMultiplierHigh;
			ShadowCascadeZOffset = settings.ShadowCascadeZOffset;
			ShadowCascadeSpreadFactor = settings.ShadowCascadeSpreadFactor;
			ShadowFadeoutMultiplier = settings.ShadowFadeoutMultiplier;
			ShadowCascadeMaxDistance = settings.ShadowCascadeMaxDistance;

			//for (int index = 0; index < settings.ShadowCascadeCount; ++index)
			//{
			//	ShadowCascadeFrozen[index] = settings.ShadowCascadeFrozen[index];
			//	ShadowCascadeSmallSkipThresholds[index] = settings.ShadowCascadeSmallSkipThresholds[index];
			//}

            Wireframe = settings.Wireframe;
            EnableStencilOptimization = settings.EnableStencilOptimization;
            EnableStencilOptimizationLOD1 = settings.EnableStencilOptimizationLOD1;
            ShowStencilOptimization = settings.ShowStencilOptimization;

            CheckDiffuseTextures = settings.CheckDiffuseTextures;
            CheckNormalTextures = settings.CheckNormalTextures;

            ShowSpecularIntensity = settings.ShowSpecularIntensity;
            ShowSpecularPower = settings.ShowSpecularPower;
            ShowEmissivity = settings.ShowEmissivity;
            ShowReflectivity = settings.ShowReflectivity;

            EnableSpotShadows = settings.EnableSpotShadows;

            VisualizeOverdraw = settings.VisualizeOverdraw;
            MultimonTest = settings.MultimonTest;

            EnableCameraInterpolation = settings.EnableCameraInterpolation;
            EnableObjectInterpolation = settings.EnableObjectInterpolation;
            InterpolationLagMs = settings.InterpolationLagMs;
            LagFeedbackMult = settings.LagFeedbackMult;

            DisplayGbufferColor = settings.DisplayGbufferColor;
            DisplayGbufferColorLinear = settings.DisplayGbufferColorLinear;
            DisplayGbufferNormal = settings.DisplayGbufferNormal;
            DisplayGbufferGlossiness = settings.DisplayGbufferGlossiness;
            DisplayGbufferMetalness = settings.DisplayGbufferMetalness;
            DisplayGbufferMaterialID = settings.DisplayGbufferMaterialID;
            DisplayAO = settings.DisplayAO;
            DisplayEmissive = settings.DisplayEmissive;
            DisplayEdgeMask = settings.DisplayEdgeMask;

            DisplayAmbientDiffuse = settings.DisplayAmbientDiffuse;
            DisplayAmbientSpecular = settings.DisplayAmbientSpecular;
            DisplayIDs = settings.DisplayIDs;
            DisplayAabbs = settings.DisplayAabbs;
            DrawOnlyMergedMeshes = settings.DrawOnlyMergedMeshes;

            TerrainDetailD0 = settings.TerrainDetailD0;
            TerrainDetailD1 = settings.TerrainDetailD1;
            TerrainDetailD2 = settings.TerrainDetailD2;
            TerrainDetailD3 = settings.TerrainDetailD3;
            FreezeTerrainQueries = settings.FreezeTerrainQueries;

            GrassPostprocess = settings.GrassPostprocess;
            GrassPostprocessCloseDistance = settings.GrassPostprocessCloseDistance;
            GrassGeometryClippingDistance = settings.GrassGeometryClippingDistance;
            GrassGeometryScalingNearDistance = settings.GrassGeometryScalingNearDistance;
            GrassGeometryScalingFarDistance = settings.GrassGeometryScalingFarDistance;
            GrassGeometryDistanceScalingFactor = settings.GrassGeometryDistanceScalingFactor;
			GrassDensityFactor = settings.GrassDensityFactor;

            WindStrength = settings.WindStrength;
            WindAzimuth = settings.WindAzimuth;

            DisplayShadowsWithDebug = settings.DisplayShadowsWithDebug;
            DisplayNDotL = settings.DisplayNDotL;

            EnableParallelRendering = settings.EnableParallelRendering;
            ForceImmediateContext = settings.ForceImmediateContext;
            RenderBatchSize = settings.RenderBatchSize;
            AmortizeBatchWork = settings.AmortizeBatchWork;
            LoopObjectThenPass = settings.LoopObjectThenPass;
            RenderThreadAsWorker = settings.RenderThreadAsWorker;

            EnableTonemapping = settings.EnableTonemapping;
            DispalyHdrDebug = settings.DispalyHdrDebug;
            AdaptationTau = settings.AdaptationTau;
            LuminanceExposure = settings.LuminanceExposure;
            Contrast = settings.Contrast;
            Brightness = settings.Brightness;
            MiddleGrey = settings.MiddleGrey;
            BloomExposure = settings.BloomExposure;
            BloomMult = settings.BloomMult;
            MiddleGreyCurveSharpness = settings.MiddleGreyCurveSharpness;
            MiddleGreyAt0 = settings.MiddleGreyAt0;
            BlueShiftRapidness = settings.BlueShiftRapidness;
            BlueShiftScale = settings.BlueShiftScale;

            BacklightMult = settings.BacklightMult;
            EnvMult = settings.EnvMult;

            FogDensity = settings.FogDensity;
            FogMult = settings.FogMult;
            FogYOffset = settings.FogYOffset;
            FogColor = settings.FogColor;

            FoliageLod0Distance = settings.FoliageLod0Distance;
            FoliageLod1Distance = settings.FoliageLod1Distance;
            FoliageLod2Distance = settings.FoliageLod2Distance;
            FoliageLod3Distance = settings.FoliageLod3Distance;
            EnableFoliageDebug = settings.EnableFoliageDebug;
            FreezeFoliageViewer = settings.FreezeFoliageViewer;
        }

		private void CheckArrays(MyRenderSettings settings)
		{
			CheckArrays();
			if (ShadowCascadeFrozen.Length < settings.ShadowCascadeFrozen.Length)
				ShadowCascadeFrozen = new bool[settings.ShadowCascadeFrozen.Length];
            if (ShadowCascadeSmallSkipThresholds == null || (settings.ShadowCascadeSmallSkipThresholds != null && ShadowCascadeSmallSkipThresholds.Length < settings.ShadowCascadeSmallSkipThresholds.Length))
				ShadowCascadeSmallSkipThresholds = new float[settings.ShadowCascadeSmallSkipThresholds.Length];
		}

		private void CheckArrays()
		{
			if (ShadowCascadeFrozen == null)
				ShadowCascadeFrozen = new bool[ShadowCascadeCount];
			if (ShadowCascadeSmallSkipThresholds == null)
            {
				ShadowCascadeSmallSkipThresholds = new float[ShadowCascadeCount];

                if (ShadowCascadeCount == 4)
                {
                    //NOTE(AF) This is a quick fix until shadows are implemented properly
                    ShadowCascadeSmallSkipThresholds[0] = 1000.0f;
                    ShadowCascadeSmallSkipThresholds[1] = 5000.0f;
                    ShadowCascadeSmallSkipThresholds[2] = 200.0f;
                    ShadowCascadeSmallSkipThresholds[3] = 1000.0f;
                }
            }
		}

		public MyRenderSettings()
		{
			CheckArrays();
		}
        internal bool IsDirty()
        {
            // Temporary method so merge works ok
            return true;
        }
    }

    /// <summary>
    /// VRage.Render11 only.
    /// </summary>
    public enum MyAntialiasingMode
    {
        NONE,
        FXAA,
        MSAA_2,
        MSAA_4,
        MSAA_8
    }

    /// <summary>
    /// VRage.Render11 only.
    /// </summary>
    public enum MyShadowsQuality
    {
        LOW,
		MEDIUM,
        HIGH
    }

    /// <summary>
    /// VRage.Render11 only.
    /// </summary>
    public enum MyTextureQuality
    {
        LOW,
        MEDIUM,
        HIGH
    }

    /// <summary>
    /// VRage.Render11 only.
    /// </summary>
    public enum MyTextureAnisoFiltering
    {
        NONE,
        ANISO_1,
        ANISO_4,
        ANISO_8,
        ANISO_16
    }

    public enum MyFoliageDetails
    {
        DISABLED,
        LOW,
        MEDIUM,
        HIGH
    }

    /// <summary>
    /// Naming convention from DX. Newer version for Dx11 render.
    /// Put only settings that player can control (either directly or indirectly) using options here.
    /// Don't put debug crap here!
    /// </summary>
    public struct MyRenderSettings1 : IEquatable<MyRenderSettings1>
    {
        // Common
        public bool InterpolationEnabled;
        public float GrassDensityFactor;

        // DX9
        public MyRenderQualityEnum Dx9Quality;

        //Dx11; All new renderers should be designed with these in mind.
        public MyAntialiasingMode AntialiasingMode;
        public MyShadowsQuality ShadowQuality;
        public bool MultithreadingEnabled;
        public bool TonemappingEnabled;
        public MyTextureQuality TextureQuality;
        public MyTextureAnisoFiltering AnisotropicFiltering;
        public MyFoliageDetails FoliageDetails;

        bool IEquatable<MyRenderSettings1>.Equals(MyRenderSettings1 other)
        {
            return Equals(ref other);
        }

        public bool Equals(ref MyRenderSettings1 other)
        {
            return
                InterpolationEnabled == other.InterpolationEnabled &&
                GrassDensityFactor == other.GrassDensityFactor &&
                Dx9Quality == other.Dx9Quality &&
                AntialiasingMode == other.AntialiasingMode &&
                ShadowQuality == other.ShadowQuality &&
                MultithreadingEnabled == other.MultithreadingEnabled &&
                TonemappingEnabled == other.TonemappingEnabled &&
                TextureQuality == other.TextureQuality &&
                AnisotropicFiltering == other.AnisotropicFiltering &&
                FoliageDetails == other.FoliageDetails;
        }
    }
}
