using System;
using VRageMath;
using VRageRender.Textures;

namespace VRageRender
{
    public class MyShadowConstants
    {
        public const int NumSplits = 4;    
    }

    public class MyRenderQualityProfile
    {
        public MyRenderQualityEnum RenderQuality;

        //LODs
        public float LodTransitionDistanceNear;
        public float LodTransitionDistanceFar;
        public float LodTransitionDistanceBackgroundStart;
        public float LodTransitionDistanceBackgroundEnd;
        public float[][] LodClipmapRanges;

        // LODs for Environment maps
        public float EnvironmentLodTransitionDistance;
        public float EnvironmentLodTransitionDistanceBackground;

        //Textures
        public TextureQuality TextureQuality;

        //Voxels
        public Effects.MyEffectVoxelsTechniqueEnum VoxelsRenderTechnique;

        //Models
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsRenderTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsBlendedRenderTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsMaskedRenderTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsHoloRenderTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsStencilTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsSkinnedTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsInstancedTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsInstancedSkinnedTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsInstancedGenericTechnique;
        public Effects.MyEffectModelsDNSTechniqueEnum ModelsInstancedGenericMaskedTechnique;

        //Shadows
        /// <summary>
        /// Determines last index of cascade, which will use LOD0 objects. Lower cascade index means
        /// closer cascade to camera. Ie. 0 means all cascaded will use LOD1 models (worst shadow quality),
        /// 5 means best quality, because all cascades will use LOD0 objects (we have 4 cascades currently);
        /// </summary>
        public int ShadowCascadeLODTreshold;

        /// <summary>
        /// Size in pixels for one shadow map cascade (of 4 total). Be carefull, we are limited by 8192 in texture size on PC.
        /// </summary>
        public int ShadowMapCascadeSize;
        public int SecondaryShadowMapCascadeSize; // For back camera

        public float ShadowBiasMultiplier;
        public float ShadowSlopeBiasMultiplier;
        public bool EnableCascadeBlending;

        //HDR
        public bool EnableHDR;

        //SSAO
        public bool EnableSSAO;

        //FXAA
        public bool EnableFXAA;

        //Environmentals
        public bool EnableEnvironmentals;

        //GodRays
        public bool EnableGodRays;

        //Geometry quality
        public bool UseNormals;

        // Spot shadow max distance multiplier 
        public float SpotShadowsMaxDistanceMultiplier;

        // Low resolution particles
        public bool LowResParticles;

        // Distant impostors
        public bool EnableDistantImpostors;

        //Explosion voxel debris
        public float ExplosionDebrisCountMultiplier;
    }

    static public class MyRenderGuiConstants
    {
        public static readonly Vector2 HUD_FREE_SPACE = new Vector2(0.01f, 0.01f);

        //  This is screen height we use as reference, so all fonts, textures, etc are made for it and if this height resolution used, it will be 1.0
        //  If e.g. we use vertical resolution 600, then averything must by scaled by 600 / 1200 = 0.5
        public const float REFERENCE_SCREEN_HEIGHT = 1080;

        public const float FONT_SCALE = 28.8f / 37f;  // Ratio between font size and line height has changed: old was 28, new is 37 (28.8 makes it closer to the font size change 18->23)
        public const float FONT_TOP_SIDE_BEARING = 3 * 23f / 18f;  // This is exact: old font size was 18, new font size 23, X padding is 7 and Y padding is 4, so (7-4)*23/18
    }

    static class MyTransparentGeometryConstants
    {
        public const int MAX_TRANSPARENT_GEOMETRY_COUNT = 50000;

        public const int MAX_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.05f);
        public const int MAX_NEW_PARTICLES_COUNT = (int)(MAX_TRANSPARENT_GEOMETRY_COUNT * 0.7f);
        public const int MAX_COCKPIT_PARTICLES_COUNT = 30;      //  We don't need much cockpit particles

        public const int TRIANGLES_PER_TRANSPARENT_GEOMETRY = 2;
        public const int VERTICES_PER_TRIANGLE = 3;
        public const int INDICES_PER_TRANSPARENT_GEOMETRY = TRIANGLES_PER_TRANSPARENT_GEOMETRY * VERTICES_PER_TRIANGLE;
        //public const int VERTICES_PER_TRANSPARENT_GEOMETRY = INDICES_PER_TRANSPARENT_GEOMETRY;
        public const int VERTICES_PER_TRANSPARENT_GEOMETRY = 4;
        public const int MAX_TRANSPARENT_GEOMETRY_VERTICES = MAX_TRANSPARENT_GEOMETRY_COUNT * VERTICES_PER_TRANSPARENT_GEOMETRY;
        public const int MAX_TRANSPARENT_GEOMETRY_INDICES = MAX_TRANSPARENT_GEOMETRY_COUNT * TRIANGLES_PER_TRANSPARENT_GEOMETRY * VERTICES_PER_TRIANGLE;

        //  Use this for all SOFT particles: dust, explosions, smoke, etc. Value was hand-picked.
        public const float SOFT_PARTICLE_DISTANCE_SCALE_DEFAULT_VALUE = 0.5f;

        //  Use this for all particles that will be near an object and you practically don't want soft-particle effect on them. 
        //  It will make them HARD particles. Value was hand-picked.
        public const float SOFT_PARTICLE_DISTANCE_SCALE_FOR_HARD_PARTICLES = 1000;

        //Use this only for decal particles, which reside always close to depth, but not cut into it
        public const float SOFT_PARTICLE_DISTANCE_DECAL_PARTICLES = 10000;
    }

    public static class MyRenderConstants
    {
        /// <summary>
        /// Maximum distance for which a light uses occlusion an occlusion query.
        /// </summary>
        public const float MAX_GPU_OCCLUSION_QUERY_DISTANCE = 150;

        public const int RENDER_STEP_IN_MILLISECONDS = (int)(1000.0f / 60.0f);
        public const float RENDER_STEP_IN_SECONDS = RENDER_STEP_IN_MILLISECONDS / 1000.0f;

        public static readonly int MAX_RENDER_ELEMENTS_COUNT = Environment.Is64BitProcess ? 2 * 65536 : 2 * 32768;
        public static readonly int DEFAULT_RENDER_MODULE_PRIORITY = 100;
        public static readonly int SPOT_SHADOW_RENDER_TARGET_COUNT = 16;
        public static readonly int ENVIRONMENT_MAP_SIZE = 128;
        public static readonly int MAX_OCCLUSION_QUERIES = 96;

        public static readonly int MIN_OBJECTS_IN_CULLING_STRUCTURE = 128;
        public static readonly int MAX_CULLING_OBJECTS = 64;

        public static readonly int MIN_PREFAB_OBJECTS_IN_CULLING_STRUCTURE = 32;
        public static readonly int MAX_CULLING_PREFAB_OBJECTS = 256;

        public static readonly int MIN_VOXEL_RENDER_CELLS_IN_CULLING_STRUCTURE = 2;
        public static readonly int MAX_CULLING_VOXEL_RENDER_CELLS = 128;

        public static float m_maxCullingPrefabObjectMultiplier = 1.0f;

        public static readonly float DISTANCE_CULL_RATIO = 100; //in meters, how far must be 1m radius object to be culled by distance
        public static readonly float DISTANCE_LIGHT_CULL_RATIO = 40;

        public static readonly int MAX_SHADER_BONES = 60;

        public static readonly MyRenderQualityProfile[] m_renderQualityProfiles = new MyRenderQualityProfile[Enum.GetValues(typeof(MyRenderQualityEnum)).Length];

        public static MyRenderQualityProfile RenderQualityProfile { get; private set; }
        public static event EventHandler OnRenderQualityChange = null;

        static MyRenderConstants()
        {
            m_renderQualityProfiles[(int)MyRenderQualityEnum.NORMAL] = new MyRenderQualityProfile()
            {
                RenderQuality = MyRenderQualityEnum.NORMAL,

                //LODs
                LodTransitionDistanceNear = 150,
                LodTransitionDistanceFar = 200,
                LodTransitionDistanceBackgroundStart = 1000,
                LodTransitionDistanceBackgroundEnd = 1100,
                LodClipmapRanges = new float[][]
                { // base was 32f * 4f
                    new float[] { 100f, 300f, 800f, 2000f, 6000f, 18000f, 35000f, 100000f, },
                    new float[] { 66f, },
                },

                // No need to set, env maps enabled only on high and extreme
                EnvironmentLodTransitionDistance = 200,
                EnvironmentLodTransitionDistanceBackground = 300,

                //Textures
                TextureQuality = TextureQuality.Half,

                //Voxels
                VoxelsRenderTechnique = Effects.MyEffectVoxelsTechniqueEnum.Normal,

                //Models
                ModelsRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Normal,
                ModelsBlendedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.NormalBlended,
                ModelsMaskedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.NormalMasked,
                ModelsHoloRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Holo,
                ModelsStencilTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Stencil,
                ModelsSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighSkinned,
                ModelsInstancedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.NormalInstanced,
                ModelsInstancedSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.NormalInstancedSkinned,
                ModelsInstancedGenericTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGeneric,
                ModelsInstancedGenericMaskedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGenericMasked,
                

                //Shadows
                ShadowCascadeLODTreshold = 3,
                ShadowMapCascadeSize = 1024,
                SecondaryShadowMapCascadeSize = 64,
                ShadowBiasMultiplier = 1,
                ShadowSlopeBiasMultiplier = 2.5f,
                EnableCascadeBlending = false,

                //HDR
                EnableHDR = false,

                //SSAO
                EnableSSAO = false,

                //FXAA
                EnableFXAA = false,

                //Environmentals
                EnableEnvironmentals = false,

                //GodRays
                EnableGodRays = false,


                //Geometry quality
                UseNormals = true,

                // Spot shadow max distance multiplier 
                SpotShadowsMaxDistanceMultiplier = 1.0f,

                // Low res particles
                LowResParticles = true,

                // Distant impostors
                EnableDistantImpostors = false,

                //Explosion voxel debris
                ExplosionDebrisCountMultiplier = 0.5f,
            };

            m_renderQualityProfiles[(int)MyRenderQualityEnum.LOW] = new MyRenderQualityProfile()
            {
                RenderQuality = MyRenderQualityEnum.LOW,

                //LODs
                LodTransitionDistanceNear = 60,
                LodTransitionDistanceFar = 80,
                LodTransitionDistanceBackgroundStart = 300,
                LodTransitionDistanceBackgroundEnd = 350,
                LodClipmapRanges = new float[][]
                { // base was 32f * 2f
                    new float[] { 80f, 240f, 600f, 1600f, 4800f, 14000f, 35000f, 100000f, },
                    new float[] { 66f, },
                },

                // No need to set, env maps enabled only on high and extreme
                EnvironmentLodTransitionDistance = 10,
                EnvironmentLodTransitionDistanceBackground = 20,

                //Textures
                TextureQuality = TextureQuality.OneFourth,

                //Voxels
                VoxelsRenderTechnique = Effects.MyEffectVoxelsTechniqueEnum.Low,

                //Models
                ModelsRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Low,
                ModelsBlendedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.LowBlended,
                ModelsMaskedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.LowMasked,
                ModelsHoloRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Holo,
                ModelsStencilTechnique = Effects.MyEffectModelsDNSTechniqueEnum.StencilLow,
                ModelsSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighSkinned,
                ModelsInstancedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighInstanced,
                ModelsInstancedSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighInstancedSkinned,
                ModelsInstancedGenericTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGeneric,
                ModelsInstancedGenericMaskedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGenericMasked,

                //Shadows
                ShadowCascadeLODTreshold = 3,
                ShadowMapCascadeSize = 512,
                SecondaryShadowMapCascadeSize = 32,
                ShadowBiasMultiplier = 1,
                ShadowSlopeBiasMultiplier = 1,
                EnableCascadeBlending = false,

                //HDR
                EnableHDR = false,

                //SSAO
                EnableSSAO = false,

                //FXAA
                EnableFXAA = false,

                //Environmentals
                EnableEnvironmentals = false,

                //GodRays
                EnableGodRays = false,

                //Geometry quality
                UseNormals = false,

                // Spot shadow max distance multiplier 
                SpotShadowsMaxDistanceMultiplier = 0.0f,

                // Low res particles
                LowResParticles = false,

                // Distant impostors
                EnableDistantImpostors = false,

                //Explosion voxel debris
                ExplosionDebrisCountMultiplier = 0,
            };

            m_renderQualityProfiles[(int)MyRenderQualityEnum.HIGH] = new MyRenderQualityProfile()
            {
                RenderQuality = MyRenderQualityEnum.HIGH,

                //LODs
                LodTransitionDistanceNear = 200,
                LodTransitionDistanceFar = 250,
                LodTransitionDistanceBackgroundStart = 1800,
                LodTransitionDistanceBackgroundEnd = 2000,
                LodClipmapRanges = new float[][]
                { // base was 32f * 6f
                    new float[] { 120f, 360f, 900f, 2000f, 6000f, 18000f, 35000f, 100000f, },
                    new float[] { 66f, },
                },

                EnvironmentLodTransitionDistance = 40,
                EnvironmentLodTransitionDistanceBackground = 80,

                //Textures
                TextureQuality = TextureQuality.Full,

                //Voxels
                VoxelsRenderTechnique = Effects.MyEffectVoxelsTechniqueEnum.High,

                //Models
                ModelsRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.High,
                ModelsBlendedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighBlended,
                ModelsMaskedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighMasked,
                ModelsHoloRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Holo,
                ModelsStencilTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Stencil,
                ModelsSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighSkinned,
                ModelsInstancedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighInstanced,
                ModelsInstancedSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.HighInstancedSkinned,
                ModelsInstancedGenericTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGeneric,
                ModelsInstancedGenericMaskedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGenericMasked,

                //Shadows
                ShadowCascadeLODTreshold = 3,
                ShadowMapCascadeSize = 1024,
                SecondaryShadowMapCascadeSize = 64,
                ShadowBiasMultiplier = 0.5f,
                ShadowSlopeBiasMultiplier = 2.5f,
                EnableCascadeBlending = true,

                //HDR
                EnableHDR = true,

                //SSAO
                EnableSSAO = true,

                //FXAA
                EnableFXAA = true,

                //Environmentals
                EnableEnvironmentals = true,

                //GodRays
                EnableGodRays = true,

                //Geometry quality
                UseNormals = true,

                // Spot shadow max distance multiplier 
                SpotShadowsMaxDistanceMultiplier = 2.5f,

                // Low res particles
                LowResParticles = false,

                // Distant impostors
                EnableDistantImpostors = true,

                //Explosion voxel debris
                ExplosionDebrisCountMultiplier = 0.8f,
            };

            m_renderQualityProfiles[(int)MyRenderQualityEnum.EXTREME] = new MyRenderQualityProfile()
            {
                RenderQuality = MyRenderQualityEnum.EXTREME,

                //LODs
                LodTransitionDistanceNear = 1000,
                LodTransitionDistanceFar = 1100,
                LodTransitionDistanceBackgroundStart = 4200,
                LodTransitionDistanceBackgroundEnd = 5000,
                LodClipmapRanges = new float[][]
                { // base was 32f * 8f
                    new float[] { 140f, 400f, 1000f, 2000f, 6000f, 18000f, 35000f, 100000f, },
                    new float[] { 66f, },
                },

                EnvironmentLodTransitionDistance = 50,
                EnvironmentLodTransitionDistanceBackground = 100,

                //Textures
                TextureQuality = TextureQuality.Full,

                //Voxels
                VoxelsRenderTechnique = Effects.MyEffectVoxelsTechniqueEnum.Extreme,

                //Models
                ModelsRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Extreme,
                ModelsBlendedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.ExtremeBlended,
                ModelsMaskedRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.ExtremeMasked,
                ModelsHoloRenderTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Holo,
                ModelsStencilTechnique = Effects.MyEffectModelsDNSTechniqueEnum.Stencil,
                ModelsSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.ExtremeSkinned,
                ModelsInstancedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.ExtremeInstanced,
                ModelsInstancedSkinnedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.ExtremeInstancedSkinned,
                ModelsInstancedGenericTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGeneric,
                ModelsInstancedGenericMaskedTechnique = Effects.MyEffectModelsDNSTechniqueEnum.InstancedGenericMasked,

                //Shadows
                ShadowCascadeLODTreshold = 4,
                ShadowMapCascadeSize = 2048,
                SecondaryShadowMapCascadeSize = 128,
                ShadowBiasMultiplier = 0.5f,
                ShadowSlopeBiasMultiplier = 1,
                EnableCascadeBlending = true,

                //HDR
                EnableHDR = true,

                //SSAO
                EnableSSAO = true,

                //FXAA
                EnableFXAA = true,

                //Environmentals
                EnableEnvironmentals = true,

                //GodRays
                EnableGodRays = true,

                //Geometry quality
                UseNormals = true,

                // Spot shadow max distance multiplier 
                SpotShadowsMaxDistanceMultiplier = 3.0f,

                // Low res particles
                LowResParticles = false,

                // Distant impostors
                EnableDistantImpostors = true,

                //Explosion voxel debris
                ExplosionDebrisCountMultiplier = 3.0f,
            };

            //Default value
            RenderQualityProfile = m_renderQualityProfiles[(int)MyRenderQualityEnum.NORMAL];
        }

        /// <summary>
        /// This should be called only from LoadContent
        /// </summary>
        public static void SwitchRenderQuality(MyRenderQualityEnum renderQuality)
        {
            RenderQualityProfile = m_renderQualityProfiles[(int)renderQuality];

            if (OnRenderQualityChange != null)
                OnRenderQualityChange(renderQuality, null);
        }
    }
}
