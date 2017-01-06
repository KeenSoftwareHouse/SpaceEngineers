
using System;
using VRageMath;

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
        public int NewAdapterOrdinal; // A new value for the adapter that will be used after a restart
        public MyWindowModeEnum WindowMode;
        public int BackBufferWidth;
        public int BackBufferHeight;
        public int RefreshRate; // Used only in Fullscreen
        public bool VSync;
        public bool DebugDrawOnly;
        public bool UseStereoRendering;
        public bool SettingsMandatory;
#if BLIT || XB1
        public MyRenderDeviceSettings(int adapter)
        {
            this.AdapterOrdinal = adapter;
            this.NewAdapterOrdinal = adapter;
            this.WindowMode = MyWindowModeEnum.Window;
            this.SettingsMandatory = false;
            this.BackBufferWidth = 0;
            this.BackBufferHeight = 0;
            this.RefreshRate = 0;
            this.VSync = true;
            this.UseStereoRendering = false;

            DebugDrawOnly = false;
        }
#endif

        public MyRenderDeviceSettings(int adapter, MyWindowModeEnum windowMode, int width, int height, int refreshRate, bool vsync, bool useStereoRendering, bool settingsMandatory)
        {
            this.AdapterOrdinal = adapter;
            this.NewAdapterOrdinal = adapter;
            this.WindowMode = windowMode;
            this.BackBufferWidth = width;
            this.BackBufferHeight = height;
            this.RefreshRate = refreshRate;
            this.VSync = vsync;
            this.UseStereoRendering = useStereoRendering;
            this.SettingsMandatory = settingsMandatory;

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
                && VSync == other.VSync
                && UseStereoRendering == other.UseStereoRendering
                && SettingsMandatory == other.SettingsMandatory;
        }

        public override string ToString()
        {
            string settings = "MyRenderDeviceSettings: {\n";
            settings += "AdapterOrdinal: " + AdapterOrdinal + "\n";
            settings += "NewAdapterOrdinal: " + NewAdapterOrdinal + "\n";
            settings += "WindowMode: " + WindowMode + "\n";
            settings += "BackBufferWidth: " + BackBufferWidth + "\n";
            settings += "BackBufferHeight: " + BackBufferHeight + "\n";
            settings += "RefreshRate: " + RefreshRate + "\n";
            settings += "VSync: " + VSync + "\n";
            settings += "DebugDrawOnly: " + DebugDrawOnly + "\n";
            settings += "UseStereoRendering: " + UseStereoRendering + "\n";
            settings += "SettingsMandatory: " + SettingsMandatory + "\n";
            settings += "}";
            return settings;
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

        public MyViewport(Vector2I resolution)
        {
            OffsetX = 0;
            OffsetY = 0;
            Width = resolution.X;
            Height = resolution.Y;
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
    public struct MyRenderSettings
    {
        public static readonly MyRenderSettings Default;

        public const int EnvMapResolution = 256; // it needs to be initialised on the game startup (it cannot be changed runtime)
        public bool UseGeometryArrayTextures; // it needs to be initialised on the game startup (it cannot be changed during game)

        public bool EnableHWOcclusionQueries;

        public bool SkipLOD_NEAR;
        public bool SkipLOD_0;
        public bool SkipLOD_1;

        public bool SkipVoxels;

        //Debug properties
        public bool MultimonTest;
        public bool ShowEnvironmentScreens;
        public bool ShowBlendedScreens;
        public bool ShowGreenBackground;
        public bool ShowLod1WithRedOverlay;

        public bool EnableLightsRuntime;

        public bool ShowEnhancedRenderStatsEnabled;
        public bool ShowResourcesStatsEnabled;
        public bool ShowTexturesStatsEnabled;

        public bool EnableSun;
        public bool EnableShadows;
        public bool EnableAsteroidShadows;
        public bool EnableFog;

        public bool DebugRenderClipmapCells;
        public bool DebugClipmapLodColor;
        public bool SkipLodUpdates;

        public bool Wireframe;
        public bool EnableStencilOptimization;
        public bool EnableStencilOptimizationLOD1;
        public bool ShowStencilOptimization;

        public bool CheckDiffuseTextures;
        public bool CheckNormalTextures;

        public bool ShowSpecularIntensity;
        public bool ShowSpecularPower;
        public bool ShowEmissivity;
        public bool ShowReflectivity;

        public bool EnableSpotShadows;

        public bool VisualizeOverdraw;
        
        // Render interpolation time, lower time equals less smooth but more responsive
        // This value should be from interval (0, 2x update interval), good value is "update interval" + "upper usual update time"
        public float InterpolationLagMs;
        public float LagFeedbackMult;

        //
        public bool DisplayGbufferColor;
        public bool DisplayGbufferAlbedo;
        public bool DisplayGbufferNormal;
        public bool DisplayGbufferNormalView;
        public bool DisplayGbufferGlossiness;
        public bool DisplayGbufferMetalness;
        public bool DisplayGbufferLOD;
        public bool DisplayMipmap;
        public bool DisplayGbufferAO;
        public bool DisplayEmissive;
        public bool DisplayEdgeMask;
        public bool DisplayNDotL;
        public bool DisplayDepth;
        public bool DisplayReprojectedDepth;
        public bool DisplayStencil;
        public bool DisplayEnvProbe;

        public bool DisplayBloomFilter;
        public bool DisplayBloomMin;

        public bool DisplayTransparencyHeatMap;
        public bool DisplayTransparencyHeatMapInGrayscale;

        public bool DisplayAO;

        public float AlbedoMultiplier;
        public float MetalnessMultiplier;
        public float GlossMultiplier;
        public float AoMultiplier;
        public float EmissiveMultiplier;
        public float ColorMaskMultiplier;

        public float AlbedoShift;
        public float MetalnessShift;
        public float GlossShift;
        public float AoShift;
        public float EmissiveShift;
        public float ColorMaskShift;

        public bool DisplayAmbientDiffuse;
        public bool DisplayAmbientSpecular;

        public bool DisplayIDs;
        public bool DisplayAabbs;

        public bool DrawMeshes;
        public bool DrawInstancedMeshes;
        public bool DrawGlass;
        public bool DrawAlphamasked;
        public bool DrawBillboards;
        public bool DrawImpostors;
        public bool DrawVoxels;
        public bool DrawMergeInstanced;
        public bool DrawNonMergeInstanced;
        public bool DrawOcclusionQueriesDebug;

        public float TerrainDetailD0;
        public float TerrainDetailD1;
        public float TerrainDetailD2;
        public float TerrainDetailD3;

        public bool FreezeTerrainQueries;

        public bool GrassPostprocess;
        public float GrassPostprocessCloseDistance;
        public float GrassGeometryClippingDistance;
        public float GrassGeometryScalingNearDistance;
        public float GrassGeometryScalingFarDistance;
        public float GrassGeometryDistanceScalingFactor;
        public float GrassMaxDrawDistance;
        
        // Shadows
        public bool DisplayShadowsWithDebug;
        public bool DrawCascadeTextures;

        // Resource management:
        // if any texture will be not borrowed by this count of frames, it will be disposed:
        public int RwTexturePool_FramesToPreserveTextures;
        // during loading of textures, all missing file textures will be replaced by debug textures:
        public bool UseDebugMissingFileTextures;
       
        public float EnvMapDepth;

        public bool EnableParallelRendering;
        public bool ForceImmediateContext;
        public bool AmortizeBatchWork;
        public float RenderBatchSize;
        public bool RenderThreadAsWorker;
        public bool ForceSlowCPU;

        //public bool EnableTonemapping = true;
        public bool DisplayHistogram;
        public bool DisplayHdrIntensity;

        public float FogDensity;
        public float FogMult;
        public Vector4 FogColor;

        public float WindStrength;
        public float WindAzimuth;

        public float FoliageLod0Distance;
        public float FoliageLod1Distance;
        public float FoliageLod2Distance;
        public float FoliageLod3Distance;
        public bool EnableFoliageDebug;
        public bool FreezeFoliageViewer;

        public bool DebugDrawDecals;

        public bool PerInstanceLods;

        public bool OffscreenSpritesRendering;

        public MyRenderSettings1 User;

        static MyRenderSettings()
        {
            Default = new MyRenderSettings()
            {
                UseGeometryArrayTextures = false,

                EnableHWOcclusionQueries = true,
                SkipLOD_NEAR = false,
                SkipLOD_0 = false,
                SkipLOD_1 = false,
                SkipVoxels = false,
                MultimonTest = false,
                ShowEnvironmentScreens = false,
                ShowBlendedScreens = false,
                ShowGreenBackground = false,
                ShowLod1WithRedOverlay = false,
                EnableLightsRuntime = true,
                ShowEnhancedRenderStatsEnabled = false,
                ShowResourcesStatsEnabled = false,
                ShowTexturesStatsEnabled = false,
                EnableSun = true,
                EnableShadows = true,
                EnableAsteroidShadows = false,
                EnableFog = true,
                DebugRenderClipmapCells = false,
                DebugClipmapLodColor = false,
                SkipLodUpdates = false,
                Wireframe = false,
                EnableStencilOptimization = true,
                EnableStencilOptimizationLOD1 = true,
                ShowStencilOptimization = false,
                CheckDiffuseTextures = true,
                CheckNormalTextures = false,
                ShowSpecularIntensity = false,
                ShowSpecularPower = false,
                ShowEmissivity = false,
                ShowReflectivity = false,
                EnableSpotShadows = true,
                VisualizeOverdraw = false,
                InterpolationLagMs = 22,
                LagFeedbackMult = 0.25f,
                DisplayGbufferColor = false,
                DisplayGbufferAlbedo = false,
                DisplayGbufferNormal = false,
                DisplayGbufferNormalView = false,
                DisplayGbufferGlossiness = false,
                DisplayGbufferMetalness = false,
                DisplayGbufferLOD = false,
                DisplayMipmap = false,
                DisplayGbufferAO = false,
                DisplayEmissive = false,
                DisplayEdgeMask = false,
                DisplayNDotL = false,
                DisplayDepth = false,
                DisplayReprojectedDepth = false,
                DisplayStencil = false,
                DisplayEnvProbe = false,
                DisplayBloomFilter = false,
                DisplayBloomMin = false,
                DisplayAO = false,
                AlbedoMultiplier = 1.0f,
                MetalnessMultiplier = 1.0f,
                GlossMultiplier = 1.0f,
                AoMultiplier = 1.0f,
                EmissiveMultiplier = 1.0f,
                ColorMaskMultiplier = 1.0f,
                AlbedoShift = 0.0f,
                MetalnessShift = 0.0f,
                GlossShift = 0.0f,
                AoShift = 0.0f,
                EmissiveShift = 0.0f,
                ColorMaskShift = 0.0f,
                DisplayAmbientDiffuse = false,
                DisplayAmbientSpecular = false,
                DisplayIDs = false,
                DisplayAabbs = false,
                DrawMeshes = true,
                DrawInstancedMeshes = true,
                DrawGlass = true,
                DrawAlphamasked = true,
                DrawImpostors = true,
                DrawBillboards = true,
                DrawVoxels = true,
                DrawMergeInstanced = true,
                DrawNonMergeInstanced = true,
                TerrainDetailD0 = 5,
                TerrainDetailD1 = 40,
                TerrainDetailD2 = 125.0f,
                TerrainDetailD3 = 200,
                FreezeTerrainQueries = false,
                GrassPostprocess = true,
                GrassPostprocessCloseDistance = 25f,
                GrassGeometryClippingDistance = 500f,
                GrassGeometryScalingNearDistance = 50f,
                GrassGeometryScalingFarDistance = 350f,
                GrassGeometryDistanceScalingFactor = 5f,
                GrassMaxDrawDistance = 250,
                DisplayShadowsWithDebug = false,
                DrawCascadeTextures = false,
                RwTexturePool_FramesToPreserveTextures = 16,
                UseDebugMissingFileTextures = false,
                EnvMapDepth = 100.0f,
                EnableParallelRendering = true,
                ForceImmediateContext = false,
                AmortizeBatchWork = true,
                RenderBatchSize = 100,
                RenderThreadAsWorker = true,
                DisplayHistogram = false,
                FogDensity = 0.003f,
                FogMult = 0.295f,
                FogColor = new VRageMath.Vector4(60 / 255f, 119 / 255f, 255 / 255f, 0),
                WindStrength = 0.3f,
                WindAzimuth = 0,
                FoliageLod0Distance = 25,
                FoliageLod1Distance = 50,
                FoliageLod2Distance = 100,
                FoliageLod3Distance = 100,
                EnableFoliageDebug = false,
                FreezeFoliageViewer = false,
                DebugDrawDecals = false,
                PerInstanceLods = true,
                OffscreenSpritesRendering = false,
            };
        }
    }

    /// <summary>
    /// VRage.Render11 only.
    /// </summary>
    public enum MyAntialiasingMode
    {
        NONE,
        FXAA,
        //MSAA_2,
        //MSAA_4,
        //MSAA_8
    }

    /// <summary>
    /// VRage.Render11 only.
    /// </summary>
    public enum MyShadowsQuality
    {
        LOW,
		MEDIUM,
        HIGH,
        DISABLED,
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
        public bool AmbientOcclusionEnabled;
        public MyShadowsQuality ShadowQuality;
        //public bool TonemappingEnabled;
        public MyTextureQuality TextureQuality;
        public MyTextureAnisoFiltering AnisotropicFiltering;
        public MyFoliageDetails FoliageDetails;
        public MyRenderQualityEnum ModelQuality;
        public MyRenderQualityEnum VoxelQuality;

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
                ModelQuality == other.ModelQuality &&
                VoxelQuality == other.VoxelQuality &&
                AntialiasingMode == other.AntialiasingMode &&
                ShadowQuality == other.ShadowQuality &&
                AmbientOcclusionEnabled == other.AmbientOcclusionEnabled &&
             //   TonemappingEnabled == other.TonemappingEnabled &&
                TextureQuality == other.TextureQuality &&
                AnisotropicFiltering == other.AnisotropicFiltering &&
                FoliageDetails == other.FoliageDetails;
        }
    }
}
