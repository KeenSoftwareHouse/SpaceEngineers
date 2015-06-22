using System;
using VRageMath;
using Sandbox.Engine.Platform.VideoMode;
using VRage.Utils;


using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Common;
using VRage;
using Sandbox.Game;

namespace Sandbox.Engine.Utils
{
    public static class MyConstants
    {
        //  Place here all asserts related to consts
        static MyConstants()
        {
        }


        //  Camera
        //  Lucky thing about XNA's Matrix.CreatePerspectiveFieldOfView() method is that it creates good perspective matrix for any resolution, and still calculates good FOV.
        //  So I don't have to change this angle. I am assuming that this FOV is vertical angle, and above method calculates horizontal angle on aspect ratio. Thus users with
        //  wider aspect ratio (e.g. 3x16:10) will see bigger horizontal FOV, but same vertical FOV. And that's good.
        public static readonly float FIELD_OF_VIEW_CONFIG_MIN = MathHelper.ToRadians(40);
        public static readonly float FIELD_OF_VIEW_CONFIG_MAX = MathHelper.ToRadians(90);
        public static readonly float FIELD_OF_VIEW_CONFIG_DEFAULT = MathHelper.ToRadians(60);
        public static readonly float FIELD_OF_VIEW_CONFIG_MAX_DUAL_HEAD = MathHelper.ToRadians(80);
        public static readonly float FIELD_OF_VIEW_CONFIG_MAX_TRIPLE_HEAD = MathHelper.ToRadians(70);

        // FIELD_OF_VIEW_MAX replaced by MyCamera.FieldOfView (could be changed in video options)
        //public static readonly float FIELD_OF_VIEW_MAX = MathHelper.ToRadians(70);
        public static readonly float FIELD_OF_VIEW_MIN = MathHelper.ToRadians(40);

        //  Two times bigger than sector's diameter because we want to draw impostor voxel maps in surrounding sectors
        //  According to information from xna creators site, far plane distance doesn't have impact on depth buffer precission, but near plane has.
        //  Therefore far plane distance can be any large number, but near plane distance can't be too small.

        //  This value is 60 seconds in the past. It used to setup 'last time' values during initialization to some time that is far in the past.
        //  I can't set it to int.MinValue, because than I will get overflow problems.
        public const int FAREST_TIME_IN_PAST = -60 * 1000;
        
        public static readonly Vector3D GAME_PRUNING_STRUCTURE_AABB_EXTENSION = new Vector3D(3.0f);
        public const int PRUNING_PROXY_ID_UNITIALIZED = -1;

        public static float DEFAULT_INTERACTIVE_DISTANCE = 5;//m
        public static float DEFAULT_GROUND_SEARCH_DISTANCE = 1.0f;//m

        public static float MAX_THRUST = 1.5f;
    }

    //  These are texts that I don't want or I can't have in localized text wrapper. They are pure system messages.
    public static class MyTextConstants
    {
        public static string SESSION_THUMB_NAME_AND_EXTENSION = "thumb.jpg";
    }

    static class MyHeadShakeConstants
    {
        public const float HEAD_SHAKE_AMOUNT_AFTER_GUN_SHOT = 2.6f;
        public const float HEAD_SHAKE_AMOUNT_AFTER_EXPLOSION = 20;
        public const float HEAD_SHAKE_AMOUNT_AFTER_PROJECTILE_HIT = 5;
        public const float HEAD_SHAKE_AMOUNT_AFTER_SMOKE_PUSH = 2;

        //  Sun wind head shaking
        public const float HEAD_SHAKE_AMOUNT_DURING_SUN_WIND_MIN = 0;
        public const float HEAD_SHAKE_AMOUNT_DURING_SUN_WIND_MAX = 1;
    }


    public static class MyLightsConstants
    {
        //  Max number of lights we can have in a scene. This number doesn't mean we will draw this lights. It's just that we can hold so many lights.
        public const int MAX_LIGHTS_COUNT = 4000;

        //  Max number of lights we use for sorting them. Only this many lights can be in influence distance.
        //  This number doesn't have to be same as 'max for effect'. It should be more, so sorting can be nice.
        //  Put there: 2 * 'max for effect'
        public const int MAX_LIGHTS_COUNT_WHEN_DRAWING = 16;

        //  This number tells us how many light can be enabled during drawing using one effect. 
        //  IMPORTANT: This number is also hardcoded inside of hlsl effect file.
        //  IMPORTANT: So if you change it here, change it too in MyCommonEffects.fxh
        //  It means, how many lights can player see (meaning light as lighted triangleVertexes, not light flare, etc).
        public const int MAX_LIGHTS_FOR_EFFECT = 8;

        // Maximum radius for all types of point lights. Any bigger value will assert
        public const int MAX_POINTLIGHT_RADIUS = 120;

        // Maximum bounding box diagonal for all types of spot lights. Any bigger value will assert
        // Diagonal size is influented by spot range and spot cone (and also by current camera angle - because of AABB)
        //public const int MAX_SPOTLIGHT_AABB_DIAGONAL = 2500;
        public const float MAX_SPOTLIGHT_RANGE = 1200;
        public const float MAX_SPOTLIGHT_SHADOW_RANGE = 200;
        public static readonly float MAX_SPOTLIGHT_ANGLE = 80;
        public static readonly float MAX_SPOTLIGHT_ANGLE_COS = 1.0f - (float)Math.Cos(MathHelper.ToRadians(MAX_SPOTLIGHT_ANGLE));
    }



    

    static class MyDecalsConstants
    {
        public const int DECAL_BUFFERS_COUNT = 10;
        public const int DECALS_FADE_OUT_INTERVAL_MILISECONDS = 1000;

        public const int MAX_DECAL_TRIANGLES_IN_BUFFER = 128;
        public const int MAX_DECAL_TRIANGLES_IN_BUFFER_SMALL = 128;
        public const int MAX_DECAL_TRIANGLES_IN_BUFFER_LARGE = 32;

        public const int TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES = 36;
        public const float TEXTURE_LARGE_FADING_OUT_START_LIMIT_PERCENT = 0.7f;     //  Number of decal triangles for large texture (explosion smut). It's used for voxels and phys objects too.
        public const float TEXTURE_LARGE_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT = 1 - TEXTURE_LARGE_FADING_OUT_START_LIMIT_PERCENT;

        public const int TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES = 32;
        public const float TEXTURE_SMALL_FADING_OUT_START_LIMIT_PERCENT = 0.7f;      //  Number of decal triangles for small texture (bullet hole). It's used for voxels and phys objects too.
        public const float TEXTURE_SMALL_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT = 1 - TEXTURE_SMALL_FADING_OUT_START_LIMIT_PERCENT;

        public const int VERTEXES_PER_DECAL = 3;
        public static readonly float MAX_NEIGHBOUR_ANGLE = MathHelper.ToRadians(80);

        //  This will how far or distance decals we have to draw. Every decal that is two times farest than reflector spot won't be drawn.
        public const float MAX_DISTANCE_FOR_DRAWING_DECALS_MULTIPLIER_FOR_REFLECTOR = 2.0f;

        //  Don't create decals if it is farther than this distance
        public const float MAX_DISTANCE_FOR_ADDING_DECALS = 500;

        //  Don't draw decals if it is farther than this distance
        public const float MAX_DISTANCE_FOR_DRAWING_DECALS = 200;

        //  We will draw large decals in larger distance
        public const float DISTANCE_MULTIPLIER_FOR_LARGE_DECALS = 3.5f;

        //  This value isn't really needed, because models doesn't have sun defined in triangles, but in shade per object
        //  It's only because some parts of decals are same for voxels and I want this information not lost.
        //public const byte SUN_FOR_MODEL_DECALS = 255;

        public static readonly Vector4 PROJECTILE_DECAL_COLOR = new Vector4(1.0f, 0.6f, 0.1f, 0);

        // These values give the percentage of how much we move decals in the direction of the dominant normal.
        public const float DECAL_OFFSET_BY_NORMAL = 0.10f;
        public const float DECAL_OFFSET_BY_NORMAL_FOR_SMUT_DECALS = 0.25f;
    }

    
    public enum MyParticleEffectsIDEnum
    {
        Dummy = 0,

        Prefab_LeakingFire_x2 = 8,
        Prefab_LeakingBiohazard = 11,
        Prefab_LeakingBiohazard2 = 12,
        Prefab_LeakingSmoke = 14,
        Prefab_Fire_Field = 15,

        Grid_Deformation = 21,
        Grid_Destruction = 22,

        CollisionSparksLargeDistant = 24,
        CollisionSparksLargeClose = 25,

        CollisionSparksHandDrill = 26,

        Welder = 27,
        AngleGrinder = 28,
        WelderSecondary = 37,
        MeteorParticle = 42,
        MeteorAsteroidCollision = 43,
        MeteorParticleAfterHit = 44,

        MeteorTrail_Smoke = 100,
        MeteorTrail_FireAndSmoke = 101,

        // damage effects
        Damage_Sparks = 200,
        Damage_Smoke = 201,
        Damage_SmokeDirectionalA = 202,
        Damage_SmokeDirectionalB = 203,
        Damage_SmokeDirectionalC = 204,
        Damage_SmokeBiochem = 205,

        Damage_Radioactive = 210,//Damage_Reactor_Damaged
        Damage_Gravitons = 211,//Damage_GravGen_Damaged
        Damage_Mechanical = 212,//Damage_HeavyMech_Damaged
        Damage_WeapExpl = 213,//Damage_WeapExpl_Damaged
        Damage_Electrical = 214,//Damage_Electrical_Damaged

        // prefab particle effects
        Prefab_LeakingSteamWhite = 300,
        Prefab_LeakingSteamGrey = 301,
        Prefab_LeakingSteamBlack = 302,
        Prefab_DustyArea = 303,
        Prefab_EMP_Storm = 304,
        Prefab_LeakingElectricity = 305,
        Prefab_LeakingFire = 306,

        // special ammunition
        UniversalLauncher_DecoyFlare = 400,
        UniversalLauncher_IlluminatingShell = 401,
        UniversalLauncher_SmokeBomb = 402,

        // drills
        Drill_Laser = 450,
        Drill_Saw = 451,
        Drill_Nuclear_Original = 452,
        Drill_Thermal = 453,
        Drill_Nuclear = 454,
        Drill_Pressure_Charge = 455,
        Drill_Pressure_Fire = 456,
        Drill_Pressure_Impact = 457,
        Drill_Pressure_Impact_Metal = 458,

        //fire
        FireTorch = 48,

        // smoke
        Smoke_Autocannon = 500,
        Smoke_CannonShot = 501,
        Smoke_Missile = 502,
        Smoke_MissileStart = 503,
        Smoke_LargeGunShot = 504,
        Smoke_SmallGunShot = 505,
        Smoke_DrillDust = 506,
        Smoke_HandDrillDust = 23,
        Smoke_HandDrillDustStones = 41,
        Smoke_Construction = 38,
        Smoke_Collector = 45,
        Prefab_DestructionSmoke = 47,

        // drilling and harvesting
        Harvester_Harvesting = 550,
        Harvester_Finished = 551,

        // explosions
        Explosion_Ammo = 600,
        Explosion_Blaster = 601,
        Explosion_Smallship = 604,
        Explosion_Bomb = 605,
        Explosion_Missile = 666,
        Explosion_SmallPrefab = 607,
        Explosion_Plasma = 630,
        Explosion_Nuclear = 640,
        Explosion_BioChem = 667,
        Explosion_EMP = 669,
        Explosion_Large = 3,
        Explosion_Huge = 4,
        Explosion_Asteroid = 6,
        Explosion_Medium = 7,
        Explosion_Warhead_15 = 33,
        Explosion_Warhead_02 = 34,
        Explosion_Warhead_30 = 35,
        Explosion_Warhead_50 = 36,


        // Close versions of explosions
        Explosion_Missile_Close = 616,

        // asteroid reaction to explosion (billboard debris)
        MaterialExplosion_Destructible = 650,

        // projectile impact effects except for autocannon and shotgun
        Hit_ExplosiveAmmo = 700,
        Hit_ChemicalAmmo = 701,
        Hit_HighSpeedAmmo = 702,
        Hit_PiercingAmmo = 703,
        Hit_BasicAmmo = 704,
        Hit_EMPAmmo = 710,

        Hit_BasicAmmoSmall = 29,
        MaterialHit_DestructibleSmall = 30,
        MaterialHit_IndestructibleSmall = 31,
        MaterialHit_MetalSmall = 32,

        // projectile impact for autocannon and shotgun
        Hit_AutocannonBasicAmmo = 705,
        Hit_AutocannonChemicalAmmo = 706,
        Hit_AutocannonHighSpeedAmmo = 707,
        Hit_AutocannonPiercingAmmo = 708,
        Hit_AutocannonExplosiveAmmo = 709,
        Hit_AutocannonEMPAmmo = 711,

        // material reaction to projectile impact
        MaterialHit_Destructible = 720,
        MaterialHit_Indestructible = 721,
        MaterialHit_Metal = 722,
        MaterialHit_Autocannon_Destructible = 730,
        MaterialHit_Autocannon_Indestructible = 731,
        MaterialHit_Autocannon_Metal = 732,
        MaterialHit_Character = 39,
        MaterialHit_CharacterSmall = 40,
        
        
        // collisions
        Collision_Smoke = 800,
        Collision_Sparks = 801,
                          
        // Destructions
        DestructionSmokeLarge = 802,
        DestructionHit = 803,

        //craft
        ChipOff_Wood = 50,
        ChipOff_Gravel = 49,

        // thrusters
        EngineThrust = 900,

        // projectile trails
        Trail_Shotgun = 950,

        Explosion_Meteor = 951,
    }


    static class MyConfigConstants
    {
        //  This password is used for saving/loading some string into config file (e.g. plain password)
        public static readonly string SYMMETRIC_PASSWORD = "63Gasjh4fqA";
    }


    static class MyMainMenuConstants
    {
        public const int BLINK_INTERVAL = 500;                                              // in ms
        public const float BUY_BUTTON_SIZE_MULTIPLICATOR = 1.2f;
        public const float BUY_BUTTON_WIDTH_MULTIPLICATOR = 0.9f;
        public static readonly Vector4 BUY_BUTTON_BACKGROUND_COLOR = new Vector4(0.8f, 0.8f, 0.8f, 0.95f);
        public static readonly Vector4 BUY_BUTTON_TEXT_COLOR = new Vector4(0.7f, 0.45f, 0f, 0.7f);
    }

    static class MyNotificationConstants
    {
        public const int MAX_HUD_NOTIFICATIONS_COUNT = 100;
        public static Vector2 DEFAULT_NOTIFICATION_MESSAGE_NORMALIZED_POSITION = new Vector2(0.5f, 0.66f);
        public const int MAX_DISPLAYED_NOTIFICATIONS_COUNT = 9;
    }

    static class MyMissileConstants
    {
        //  We will generate smoke trail particles on missile's way. This number tells us how many particles per 1 meter.
        public const float GENERATE_SMOKE_TRAIL_PARTICLE_DENSITY_PER_METER = 4f;

        //  This number needs to be calculated in regard to max count of player, max count of missiles fired per second and timeout of each missile
        public const int MAX_MISSILES_COUNT = 512;

//       public const int MISSILE_LAUNCHER_SHOT_INTERVAL_IN_MILISECONDS = 900;            //  Interval between two missile launcher shots
//        public const float MISSILE_ACCELERATION = 600; // m/s2, 60 G is usual for missiles // This should be per-ammo
//        public const int MISSILE_TIMEOUT = 3 * 1000;       //  Max time missile can survive without hiting any object
//        public const float MISSILE_INITIAL_SPEED = 10; //Initial speed for missile right after shot

//        public const float MISSILE_BACKKICK = 700;

        public static readonly Vector4 MISSILE_LIGHT_COLOR = new Vector4(1.5f, 1.5f, 1.0f, 1.0f);       //  Alpha should be 1, because we draw flare billboard with it

        public const int MISSILE_INIT_TIME = 10; //ms
        public static readonly Vector3 MISSILE_INIT_DIR = MyUtils.Normalize(new Vector3(0, 0, -1));


        /// <summary>
        /// The distance to check whether missile will collide soon after launch with something.
        /// For more info, see ticket 3422.
        /// </summary>
        public const int DISTANCE_TO_CHECK_MISSILE_CORRECTION = 10;

        public const float MISSILE_LIGHT_RANGE = 70;
    }

    static class MyGatlingConstants
    {
        //  How many times machine gun rotates while firing.
        public const float ROTATION_SPEED_PER_SECOND = 2 * MathHelper.TwoPi;

        //  How long it takes until autocanon stops rotating after last shot
        public const int ROTATION_TIMEOUT = 2000;

        //  Interval between two machine gun shots
        public const int SHOT_INTERVAL_IN_MILISECONDS = 95;

        public const int REAL_SHOTS_PER_SECOND = 45;

        //  Interval between two machine gun shots
        // apparently for sounds
        public const int MIN_TIME_RELEASE_INTERVAL_IN_MILISECONDS = 204;

        // not used
        public static readonly float SHOT_PROJECTILE_DEBRIS_MAX_DEVIATION_ANGLE = MathHelper.ToRadians(30);

        // not used
        public static readonly float COCKPIT_GLASS_PROJECTILE_DEBRIS_MAX_DEVIATION_ANGLE = MathHelper.ToRadians(10);


        public const int SMOKE_INCREASE_PER_SHOT = SHOT_INTERVAL_IN_MILISECONDS * 2 / SMOKES_INTERVAL_IN_MILISECONDS;
        public const int SMOKE_DECREASE = 1;
        public const int SMOKES_MAX = 50;
        public const int SMOKES_MIN = 40;

        //  This number is not dependent on rate of shoting. It tells how often we generate new smoke (if large, it will look ugly)
        public const int SMOKES_INTERVAL_IN_MILISECONDS = 10;
    }

    static class MyGridConstants
    {
        //  How much damage is inflicted from the deformations
        public const float DEFORMATION_DAMAGE_MODIFIER = 15.0f;

        //  How much the bones move according to deformation tables
        public const float DEFORMATION_TABLE_BASE_MOVE_DIST = 0.25f;

        // How much the corner bones will be moved; relative to the grid size
        public const float CORNER_BONE_MOVE_DISTANCE = 0.25f;

        public const int HACKING_ATTEMPT_TIME_MS = 1000;
        public const int HACKING_INDICATION_TIME_MS = 10000;

        //Transparency values
        public const float BUILDER_TRANSPARENCY = 0.25f;
        public static float PROJECTOR_TRANSPARENCY = 0.5f;
    }

    public static class MySteamConstants
    {
        public static string URL_GUIDE_DEFAULT              = "http://steamcommunity.com/sharedfiles/filedetails/?id=185301069#ig_bottom";
        public static string URL_GUIDE_BATTLE               = URL_GUIDE_DEFAULT;
        public static string URL_HELP_MAIN_MENU             = URL_GUIDE_DEFAULT;
        public static string URL_HELP_TERMINAL_SCREEN       = URL_GUIDE_DEFAULT;
        public static string URL_HELP_ASSEMBLER_SCREEN      = URL_GUIDE_DEFAULT;
        public static string URL_BROWSE_WORKSHOP_MODS       = "http://steamcommunity.com/workshop/browse/?appid=244850&requiredtags%5B%5D=mod";
        public static string URL_BROWSE_WORKSHOP_WORLDS     = "http://steamcommunity.com/workshop/browse/?appid=244850&requiredtags%5B%5D=world";
        public static string URL_BROWSE_WORKSHOP_BLUEPRINTS = "http://steamcommunity.com/workshop/browse/?appid=244850&requiredtags%5B%5D=blueprint";
        public static string URL_BROWSE_WORKSHOP_INGAMESCRIPTS = "http://steamcommunity.com/workshop/browse/?appid=244850&requiredtags%5B%5D=ingamescript";
        public static string URL_BROWSE_WORKSHOP_INGAMESCRIPTS_HELP = "http://steamcommunity.com/sharedfiles/filedetails/?id=360966557";
        public static string URL_BROWSE_WORKSHOP_SCENARIOS = "http://steamcommunity.com/workshop/browse/?appid=244850&requiredtags%5B%5D=scenario";
        public static string URL_WORKSHOP_VIEW_ITEM_FORMAT = "http://steamcommunity.com/sharedfiles/filedetails/?id={0}";
        public static string URL_RECOMMEND_GAME             = "http://store.steampowered.com/recommended/recommendgame/244850";
    }

    static class MySunWindConstants
    {
        public const float SUN_COLOR_INCREASE_DISTANCE = 10000;
        public const float SUN_COLOR_INCREASE_STRENGTH_MIN = 3;
        public const float SUN_COLOR_INCREASE_STRENGTH_MAX = 4;

        //  This is half of the sun wind's length, or in other words, it is distance from camera where sun wind starts, then 
        //  travels through camera and travels again to disapear. So full travel distance is two times this number.
        public const float SUN_WIND_LENGTH_TOTAL = 60000;//MyConstants.FAR_PLANE_DISTANCE * 2;        
        public const float SUN_WIND_LENGTH_HALF = SUN_WIND_LENGTH_TOTAL / 2;// MyConstants.FAR_PLANE_DISTANCE;

        public static readonly Vector2I LARGE_BILLBOARDS_SIZE = new Vector2I(10, 10);
        public static readonly Vector2I LARGE_BILLBOARDS_SIZE_HALF = new Vector2I(LARGE_BILLBOARDS_SIZE.X / 2, LARGE_BILLBOARDS_SIZE.Y / 2);
        public const float LARGE_BILLBOARD_RADIUS_MIN = 20000;
        public const float LARGE_BILLBOARD_RADIUS_MAX = 35000;
        public const float LARGE_BILLBOARD_DISTANCE = 7500; //LARGE_BILLBOARD_RADIUS_MIN * 2;
        public const float LARGE_BILLBOARD_POSITION_DELTA_MIN = -50;
        public const float LARGE_BILLBOARD_POSITION_DELTA_MAX = 50;
        public const float LARGE_BILLBOARD_ROTATION_SPEED_MIN = 0.5f;
        public const float LARGE_BILLBOARD_ROTATION_SPEED_MAX = 1.2f;
        public const float LARGE_BILLBOARD_DISAPEAR_DISTANCE = SUN_WIND_LENGTH_HALF * 0.9f;

        public static readonly Vector2I SMALL_BILLBOARDS_SIZE = new Vector2I(20, 20);
        public static readonly Vector2I SMALL_BILLBOARDS_SIZE_HALF = new Vector2I(SMALL_BILLBOARDS_SIZE.X / 2, SMALL_BILLBOARDS_SIZE.Y / 2);
        public const float SMALL_BILLBOARD_RADIUS_MIN = 250;
        public const float SMALL_BILLBOARD_RADIUS_MAX = 500;
        public const float SMALL_BILLBOARD_DISTANCE = 350;
        public const float SMALL_BILLBOARD_POSITION_DELTA_MIN = -300;
        public const float SMALL_BILLBOARD_POSITION_DELTA_MAX = 300;
        //public const float SMALL_BILLBOARD_MAX_DISTANCE_FROM_CENTER = LARGE_BILLBOARD_MAX_DISTANCE_FROM_CENTER;

        public const float SMALL_BILLBOARD_ROTATION_SPEED_MIN = 1.4f;//0.06f;
        public const float SMALL_BILLBOARD_ROTATION_SPEED_MAX = 3.5f;//0.10f;
        public const int SMALL_BILLBOARD_TAIL_COUNT_MIN = 8;//28
        public const int SMALL_BILLBOARD_TAIL_COUNT_MAX = 14;//30
        public const float SMALL_BILLBOARD_TAIL_DISTANCE_MIN = 300;//300
        public const float SMALL_BILLBOARD_TAIL_DISTANCE_MAX = 450;//450

        public const float PARTICLE_DUST_DECREAS_DISTANCE = LARGE_BILLBOARD_DISAPEAR_DISTANCE;

        public const float SWITCH_LARGE_AND_SMALL_BILLBOARD_RADIUS = 7000;
        public const float SWITCH_LARGE_AND_SMALL_BILLBOARD_DISTANCE = 10000;//SMALL_BILLBOARD_TAIL_COUNT_MAX * SMALL_BILLBOARD_TAIL_DISTANCE_MAX * 0.8f;
        public const float SWITCH_LARGE_AND_SMALL_BILLBOARD_DISTANCE_HALF = SWITCH_LARGE_AND_SMALL_BILLBOARD_DISTANCE / 3.0f;

        public static readonly float FORCE_ANGLE_RANDOM_VARIATION_IN_RADIANS = MathHelper.ToRadians(70);
        public const float FORCE_IMPULSE_RANDOM_MAX = 500000f;         //  This is only MAX value of random impulse, not exact impulse value.
        public const float FORCE_IMPULSE_POSITION_DISTANCE = 1000;       //  This tells us how far from phys object is source. Too far means low throw impulse. Always in oposite direction of force.

        public const float SPEED_MIN = 1300;
        public const float SPEED_MAX = 1500;

        public const float HEALTH_DAMAGE = 80;
        public const float SHIP_DAMAGE = 50;

        public const float SECONDS_FOR_SMALL_BILLBOARDS_INITIALIZATION = 1.0f;  // This is time in which all small billboards will have MaxDistance initialized
        public const float RAY_CAST_DISTANCE = 30000; // We ignore all entities in ray cast except those that are x meters away from player
    }

    static class MyChatConstants
    {
        public const int MAX_CHAT_STRING_LENGTH = 200;
        public const int MAX_PLAYER_CHAT_HISTORY_COUNT = 20;
        public const int MAX_FACTION_CHAT_HISTORY_COUNT = 50;
        public const int MAX_GLOBAL_CHAT_HISTORY_COUNT = 100;
    }

    public static class MyOxygenConstants
    {
        public const float OXYGEN_REGEN_PER_SECOND = 2000f;
    }
}