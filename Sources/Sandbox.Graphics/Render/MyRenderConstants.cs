//using System;
//using VRageRender.Textures;
//using VRageRender.Effects;
//using VRageRender;
////using VRageRender.Effects;

//namespace Sandbox.Graphics.Render
//{
//    public class MyRenderQualityProfile
//    {
//        public VRageRender.MyRenderQualityEnum RenderQuality;

//        public bool IsDirty = false;

//        //LODs
//        public float LodTransitionDistanceNear;
//        public float LodTransitionDistanceFar;
//        public float LodTransitionDistanceBackgroundStart;
//        public float LodTransitionDistanceBackgroundEnd;

//        // LODs for Environment maps
//        public float EnvironmentLodTransitionDistance;
//        public float EnvironmentLodTransitionDistanceBackground;

//        //Textures
//        public VRageRender.Textures.TextureQuality TextureQuality;

      
//        //Shadows
//        /// <summary>
//        /// Determines last index of cascade, which will use LOD0 objects. Lower cascade index means
//        /// closer cascade to camera. Ie. 0 means all cascaded will use LOD1 models (worst shadow quality),
//        /// 5 means best quality, because all cascades will use LOD0 objects (we have 4 cascades currently);
//        /// </summary>
//        //public int ShadowCascadeLODTreshold;

//        /// <summary>
//        /// Size in pixels for one shadow map cascade (of 4 total). Be carefull, we are limited by 8192 in texture size on PC.
//        /// </summary>
//        //public int ShadowMapCascadeSize;
//        //public int SecondaryShadowMapCascadeSize; // For back camera

//        //public int ShadowBiasMultiplier;
//        public bool EnableCascadeBlending;

//        //HDR
//        public bool EnableHDR;

//        //SSAO
//        public bool EnableSSAO;

//        //FXAA
//        public bool EnableFXAA;

//        //Environmentals
//        public bool EnableEnvironmentals;

//        //GodRays
//        public bool EnableGodRays;

//        //Geometry quality
//        public bool UseNormals;
//        public bool NeedReloadContent;

//        // Spot shadow max distance multiplier 
//        //public float SpotShadowsMaxDistanceMultiplier;

//        // Low resolution particles
//        public bool LowResParticles;

//        // Distant impostors
//        public bool EnableDistantImpostors;

//        // Flying debris
//        public bool EnableFlyingDebris;

//        // Decals
//        public bool EnableDecals;

//        //Explosion voxel debris
//        public float ExplosionDebrisCountMultiplier;
//    }

//    public static class MyRenderConstants
//    {
//        //  Physics
//        public const float PHYSICS_STEPS_PER_SECOND = 60;       //  Looks like if I set it bellow 100 (e.g. to 60), mouse rotation seems not-seamless...
//        public const float PHYSICS_STEP_SIZE_IN_SECONDS = 1.0f / PHYSICS_STEPS_PER_SECOND;
//        public const int PHYSICS_STEP_SIZE_IN_MILLISECONDS = (int)(1000.0f / PHYSICS_STEPS_PER_SECOND);



//        public static bool EnableFog = true;

//        /// <summary>
//        /// Maximum distance for which a light uses occlusion an occlusion query.
//        /// </summary>
//        public const float MAX_GPU_OCCLUSION_QUERY_DISTANCE = 150;

//        public const int RENDER_STEP_IN_MILLISECONDS = (int)(1000.0f / 60.0f);

//        public static readonly int MAX_RENDER_ELEMENTS_COUNT = 32768;
//        public static readonly int DEFAULT_RENDER_MODULE_PRIORITY = 100;
//        public static readonly int SPOT_SHADOW_RENDER_TARGET_COUNT = 4;
//        public static readonly int ENVIRONMENT_MAP_SIZE = 128;
//        public static readonly int MAX_OCCLUSION_QUERIES = 96;

//        public static readonly int MIN_OBJECTS_IN_CULLING_STRUCTURE = 128;
//        public static readonly int MAX_CULLING_OBJECTS = 64;

//        public static readonly int MIN_PREFAB_OBJECTS_IN_CULLING_STRUCTURE = 32;
//        public static readonly int MAX_CULLING_PREFAB_OBJECTS = 256;

//        public static readonly int MIN_VOXEL_RENDER_CELLS_IN_CULLING_STRUCTURE = 2;
//        public static readonly int MAX_CULLING_VOXEL_RENDER_CELLS = 128;

//        public static float m_maxCullingPrefabObjectMultiplier = 1.0f;

//        public static readonly float DISTANCE_CULL_RATIO = 100; //in meters, how far must be 1m radius object to be culled by distance
//        public static readonly float DISTANCE_LIGHT_CULL_RATIO = 40;

//        static readonly MyRenderQualityProfile[] m_renderQualityProfiles = new MyRenderQualityProfile[Enum.GetValues(typeof(MyRenderQualityEnum)).Length];

//        public static MyRenderQualityProfile RenderQualityProfile { get; private set; }
//        public static event EventHandler OnRenderQualityChange = null;

//        static MyRenderConstants()
//        {
//            m_renderQualityProfiles[(int)MyRenderQualityEnum.NORMAL] = new MyRenderQualityProfile()
//            {
//                RenderQuality = MyRenderQualityEnum.NORMAL,

//                //LODs
//                LodTransitionDistanceNear = 150,
//                LodTransitionDistanceFar = 200,
//                LodTransitionDistanceBackgroundStart = 1000,
//                LodTransitionDistanceBackgroundEnd = 1100,

//                // No need to set, env maps enabled only on high and extreme
//                EnvironmentLodTransitionDistance = 200,
//                EnvironmentLodTransitionDistanceBackground = 300,

//                //Textures
//                TextureQuality = TextureQuality.Half,

           
//                //Shadows
//                //ShadowCascadeLODTreshold = 2,
//                //ShadowMapCascadeSize = 1024,
//                //SecondaryShadowMapCascadeSize = 64,
//                //ShadowBiasMultiplier = 5,
//                EnableCascadeBlending = false,

//                //HDR
//                EnableHDR = false,

//                //SSAO
//                EnableSSAO = false,

//                //FXAA
//                EnableFXAA = false,

//                //Environmentals
//                EnableEnvironmentals = false,

//                //GodRays
//                EnableGodRays = false,


//                //Geometry quality
//                UseNormals = true,
//                NeedReloadContent = true, //because normal->high and vertex channels
                
//                // Spot shadow max distance multiplier 
//                //SpotShadowsMaxDistanceMultiplier = 1.0f,

//                // Low res particles
//                LowResParticles = true,

//                // Distant impostors
//                EnableDistantImpostors = false,

//                // Flying debris
//                EnableFlyingDebris = false,

//                // Decals
//                EnableDecals = false,

//                //Explosion voxel debris
//                ExplosionDebrisCountMultiplier = 0.5f,
//            };

//            m_renderQualityProfiles[(int)MyRenderQualityEnum.LOW] = new MyRenderQualityProfile()
//            {
//                RenderQuality = MyRenderQualityEnum.LOW,

//                //LODs
//                LodTransitionDistanceNear = 60,
//                LodTransitionDistanceFar = 80,
//                LodTransitionDistanceBackgroundStart = 300,
//                LodTransitionDistanceBackgroundEnd = 350,

//                // No need to set, env maps enabled only on high and extreme
//                EnvironmentLodTransitionDistance = 10,
//                EnvironmentLodTransitionDistanceBackground = 20,

//                //Textures
//                TextureQuality = TextureQuality.OneFourth,

       
//                //Shadows
//                //ShadowCascadeLODTreshold = 2,
//                //ShadowMapCascadeSize = 512,
//                //SecondaryShadowMapCascadeSize = 32,
//                //ShadowBiasMultiplier = 10,
//                EnableCascadeBlending = false,

//                //HDR
//                EnableHDR = false,

//                //SSAO
//                EnableSSAO = false,

//                //FXAA
//                EnableFXAA = false,

//                //Environmentals
//                EnableEnvironmentals = false,

//                //GodRays
//                EnableGodRays = false,

//                //Geometry quality
//                UseNormals = false,
//                NeedReloadContent = true,
                
//                // Spot shadow max distance multiplier 
//                //SpotShadowsMaxDistanceMultiplier = 0.0f,

//                // Low res particles
//                LowResParticles = false,

//                // Distant impostors
//                EnableDistantImpostors = false,

//                // Flying debris
//                EnableFlyingDebris = false,

//                // Decals
//                EnableDecals = false,

//                //Explosion voxel debris
//                ExplosionDebrisCountMultiplier = 0,
//            };

//            m_renderQualityProfiles[(int)MyRenderQualityEnum.HIGH] = new MyRenderQualityProfile()
//            {
//                RenderQuality = MyRenderQualityEnum.HIGH,

//                //LODs
//                LodTransitionDistanceNear = 200,
//                LodTransitionDistanceFar = 250,
//                LodTransitionDistanceBackgroundStart = 1800,
//                LodTransitionDistanceBackgroundEnd = 2000,

//                EnvironmentLodTransitionDistance = 40,
//                EnvironmentLodTransitionDistanceBackground = 80,

//                //Textures
//                TextureQuality = TextureQuality.Full,

          
//                //Shadows
//                //ShadowCascadeLODTreshold = 2,
//                //ShadowMapCascadeSize = 1024,
//                //SecondaryShadowMapCascadeSize = 64,
//                //ShadowBiasMultiplier = 2,
//                EnableCascadeBlending = true,

//                //HDR
//                EnableHDR = true,

//                //SSAO
//                EnableSSAO = true,

//                //FXAA
//                EnableFXAA = true,

//                //Environmentals
//                EnableEnvironmentals = true,

//                //GodRays
//                EnableGodRays = true,

//                //Geometry quality
//                UseNormals = true,
//                NeedReloadContent = false,
                
//                // Spot shadow max distance multiplier 
//                //SpotShadowsMaxDistanceMultiplier = 2.5f,

//                // Low res particles
//                LowResParticles = false,

//                // Distant impostors
//                EnableDistantImpostors = true,

//                // Flying debris
//                EnableFlyingDebris = true,

//                // Decals
//                EnableDecals = true,

//                //Explosion voxel debris
//                ExplosionDebrisCountMultiplier = 0.8f,
//            };

//            m_renderQualityProfiles[(int)MyRenderQualityEnum.EXTREME] = new MyRenderQualityProfile()
//            {
//                RenderQuality = MyRenderQualityEnum.EXTREME,

//                //LODs
//                LodTransitionDistanceNear = 1000,
//                LodTransitionDistanceFar = 1100,
//                LodTransitionDistanceBackgroundStart = 4200,
//                LodTransitionDistanceBackgroundEnd = 5000,

//                EnvironmentLodTransitionDistance = 500,
//                EnvironmentLodTransitionDistanceBackground = 1000,

//                //Textures
//                TextureQuality = TextureQuality.Full,

//                //Shadows
//                //ShadowCascadeLODTreshold = 4,
//                //ShadowMapCascadeSize = 2048,
//                //SecondaryShadowMapCascadeSize = 128,
//                //ShadowBiasMultiplier = 2,
//                EnableCascadeBlending = true,

//                //HDR
//                EnableHDR = true,

//                //SSAO
//                EnableSSAO = true,

//                //FXAA
//                EnableFXAA = true,

//                //Environmentals
//                EnableEnvironmentals = true,

//                //GodRays
//                EnableGodRays = true,

//                //Geometry quality
//                UseNormals = true,
//                NeedReloadContent = false,
                
//                // Spot shadow max distance multiplier 
//                //SpotShadowsMaxDistanceMultiplier = 3.0f,

//                // Low res particles
//                LowResParticles = false,

//                // Distant impostors
//                EnableDistantImpostors = true,

//                // Flying debris
//                EnableFlyingDebris = true,

//                // Decals
//                EnableDecals = true,

//                //Explosion voxel debris
//                ExplosionDebrisCountMultiplier = 3.0f,
//            };

//            //Default value
//            RenderQualityProfile = m_renderQualityProfiles[(int)MyRenderQualityEnum.HIGH];
//        }

//        public static void SwitchRenderQuality(MyRenderQualityEnum renderQuality)
//        {
//            // Make sure we never switch to low, not even using config
//            if (renderQuality == MyRenderQualityEnum.LOW)
//                renderQuality = MyRenderQualityEnum.NORMAL;

//            RenderQualityProfile = m_renderQualityProfiles[(int)renderQuality];

//            if (OnRenderQualityChange != null)
//                OnRenderQualityChange(renderQuality, null);
//        }
//    }
//}
