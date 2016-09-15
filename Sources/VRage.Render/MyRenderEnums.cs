using System;

namespace VRageRender
{

    #region Enums

    /// <summary>
    /// Entity flags.
    /// </summary>
    [Flags]
    public enum RenderFlags
    {
        /// <summary>
        /// Skip the object in render if detected that it is too small
        /// </summary>
        SkipIfTooSmall = 1 << 0,

        /// <summary>
        /// Needs resolve cast shadows flag (done by parallel raycast to sun)
        /// </summary>
        NeedsResolveCastShadow = 1 << 1,

        /// <summary>
        /// Casts only one raycast to determine shadow casting
        /// </summary>
        FastCastShadowResolve = 1 << 2,

        /// <summary>
        /// Tells if this object should cast shadows
        /// </summary>
        CastShadows = 1 << 3,

        /// <summary>
        /// Specifies whether draw this entity or not.
        /// </summary>
        Visible = 1 << 4,

		/// <summary>
		/// Specifies whether this entity should be drawn even when it is outside the set view distance
		/// </summary>
		DrawOutsideViewDistance = 1 << 5,

        /// <summary>
        /// Specifies whether entity is "near", near entities are cockpit and weapons, these entities are rendered in special way
        /// </summary>
        Near = 1 << 6,

        /// <summary>
        /// Tells if this object should use PlayerHeadMatrix as matrix for draw
        /// </summary>
        UseCustomDrawMatrix = 1 << 7,

        /// <summary>
        /// Use local AABB box for shadow LOD, not used
        /// </summary>
        ShadowLodBox = 1 << 8,

		/// <summary>
		/// No culling of back faces
		/// </summary>
		NoBackFaceCulling = 1 << 9,

        SkipInMainView = 1 << 10,
    }

    public static class MyRenderFlagsExtensions
    {
        public static bool HasFlags(this RenderFlags renderFlags, RenderFlags flags)
        {
            return (renderFlags & flags) == flags;
        }
    }


    public enum CullingOptions
    {
        Default,
        Prefab,
        VoxelMap
    }

    public enum MyLodTypeEnum
    {
        LOD0,     //  Use when cell contains data without LOD, so they are as they are
        LOD1,         //  Use when cell contains LOD-ed data (less detail, ...)
        LOD_NEAR,    // Used for cockpit and weapons
        LOD_BACKGROUND  //used for planets
    }

    /// <summary>
    /// Light type, flags, could be combined
    /// </summary>
    [Flags]
    public enum LightTypeEnum
    {
        None = 0,
        PointLight = 1 << 0,
        Spotlight = 1 << 1,
    }

    public enum MyDecalTexturesEnum : byte
    {
        BulletHoleOnMetal,
        BulletHoleOnRock
    }

    namespace Lights
    {
        public enum MyGlareTypeEnum
        {
            /// <summary>
            /// This is the glare that is dependent on occlusion queries.
            /// Physically, this phenomenon originates in the lens.
            /// </summary>
            Normal = 0,

            /// <summary>
            /// This is the glare that you see even if the light itself is occluded.
            /// It gives the impression of scattering in a medium (like fog).
            /// </summary>
            Distant,

            /// <summary>
            /// Like normal, but gets dimmed with camera vs. reflector angle
            /// </summary>
            Directional,
        }
    }

    public enum MyRenderQualityEnum
    {
        NORMAL = 0,
        HIGH = 1,
        EXTREME = 2,
        LOW = 3
    }

    public enum MyGraphicsRenderer
    {
        NONE,
        DX11
    }

    public enum MyRenderModuleEnum
    {
        Cockpit,
        CockpitGlass,
        SunGlareAndLensFlare,
        UpdateOcclusions,
        AnimatedParticlesPrepare,
        TransparentGeometry,
        ParticlesDustField,
        VoxelHand,
        DistantImpostors,
        Decals,
        CockpitWeapons,
        SunGlow,
        SectorBorder,
        DrawSectorBBox, // SectorBorderRendering
        DrawCoordSystem,
        Explosions,
        BackgroundCube,
        GPS,
        TestField,
        AnimatedParticles,
        Lights,
        Projectiles,
        DebrisField,
        ThirdPerson,
        Editor,
        PrunningStructure,
        SunWind,
        IceStormWind,
        PrefabContainerManager,
        InfluenceSpheres,
        SolarAreaBorders,
        PhysicsPrunningStructure,
        NuclearExplosion,
        AttackingBots,
        Atmosphere,
    }

    namespace Textures
    {
        /// <summary>
        /// Reresent loading quality for textures.
        /// This works only for dds textures with mipmaps. Other textures will retains their original properties.
        /// </summary>
        public enum TextureQuality
        {
            /// <summary>
            /// Full quality.
            /// </summary>
            Full,

            /// <summary>
            /// 1/2 quality.
            /// </summary>
            Half,

            /// <summary>
            /// 1/4 quality
            /// </summary>
            OneFourth,

            /// <summary>
            /// 1/8 quality
            /// </summary>
            OneEighth,

            /// <summary>
            /// 1/16 quality
            /// </summary>
            OneSixteenth,
        }   
    }

    namespace Effects
    {
        public enum MyEffectVoxelsTechniqueEnum
        {
            Low,
            Normal,
            High,
            Extreme,
        }

        public enum MyEffectModelsDNSTechniqueEnum
        {
            Low,
            //LowInstanced,
            LowBlended,
            LowMasked,

            Normal,
            //Normalnstanced,
            NormalBlended,
            NormalMasked,

            High,
            HighBlended,
            HighMasked,

            Extreme,
            ExtremeBlended,
            ExtremeMasked,

            Holo,
            HoloIgnoreDepth,

            Stencil,
            StencilLow,

            NormalInstanced,
            NormalInstancedSkinned,

            InstancedGeneric,
            InstancedGenericMasked,

            HighSkinned,
            HighInstanced,
            HighInstancedSkinned,

            ExtremeSkinned,
            ExtremeInstanced,
            ExtremeInstancedSkinned,
        }
    }

    #endregion
}
