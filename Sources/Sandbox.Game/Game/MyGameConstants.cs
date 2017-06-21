using System.Collections.Generic;
using VRage.Game;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game
{
    public static class MyProjectilesConstants
    {
        //  Max count of active (aka flying) projectiles
        public const int MAX_PROJECTILES_COUNT = 8192;
        public const float HIT_STRENGTH_IMPULSE = 500;

        static readonly Dictionary<int, Vector3> m_projectileTrailColors = new Dictionary<int, Vector3>(10);
        static MyProjectilesConstants()
        {
            m_projectileTrailColors.Add((int)MyAmmoType.Unknown, Vector3.One);
            m_projectileTrailColors.Add((int)MyAmmoType.Missile, Vector3.One);
            m_projectileTrailColors.Add((int)MyAmmoType.HighSpeed, new Vector3(10.0f, 10.0f, 10.0f));
            m_projectileTrailColors.Add((int)MyAmmoType.Plasma, Vector3.One);
            m_projectileTrailColors.Add((int)MyAmmoType.Laser, Vector3.One);
            m_projectileTrailColors.Add((int)MyAmmoType.Basic, Vector3.One);
        }

        public static Vector3 GetProjectileTrailColorByType(MyAmmoType ammoType) 
        {
            Vector3 output;
            bool found = m_projectileTrailColors.TryGetValue((int)ammoType, out output);
            MyDebug.AssertDebug(found, "Could not find the trail color value. The default will be loaded. You can ignore this error.");
            if (!found)
            {
                found = m_projectileTrailColors.TryGetValue((int)MyAmmoType.Unknown, out output);
                MyDebug.AssertDebug(found, "Could not find the default value for the trail color.");
            }
            return output;
        }

        //public static readonly Vector3 EXPLOSIVE_PROJECTILE_TRAIL_COLOR = new Vector3(1.0f, 0.5f, 0.5f);
        //public static readonly Vector3 HIGH_SPEED_PROJECTILE_TRAIL_COLOR = new Vector3(10.0f, 10.0f, 10.0f);
        //public static readonly Vector3 BIOCHEM_PROJECTILE_TRAIL_COLOR = new Vector3(0.5f, 2.5f, 0.5f);
        //public static readonly Vector3 PIERCING_PROJECTILE_TRAIL_COLOR = new Vector3(0.5f, 0.5f, 1.5f);
        //public static readonly Vector3 EMP_PROJECTILE_TRAIL_COLOR = new Vector3(0.5f, 0.5f, 2.5f);

        public static readonly float AUTOAIMING_PRECISION = 500.0f;
    }

    class MyGuidedMissileConstants
    {
        //  We will generate smoke trail particles on missile's way. This number tells us how many particles per 1 meter.
        public const float GENERATE_SMOKE_TRAIL_PARTICLE_DENSITY_PER_METER = 4f;

        //  This number needs to be calculated in regard to max count of player, max count of missiles fired per second and timeout of each missile
        public const int MAX_MISSILES_COUNT = 50;

        public const int MISSILE_LAUNCHER_SHOT_INTERVAL_IN_MILISECONDS = 1000;            //  Interval between two missile launcher shots
        public const float MISSILE_BLEND_VELOCITIES_IN_MILISECONDS = 400.0f; //time to get full speed from init speed
        public const int MISSILE_TIMEOUT = 15 * 1000;       //  Max time missile can survive without hiting any object

        public static readonly Vector4 MISSILE_LIGHT_COLOR = new Vector4(1.5f, 1.5f, 1.0f, 1.0f);       //  Alpha should be 1, because we draw flare billboard with it

        public static float MISSILE_TURN_SPEED = 5.0f; //max radians per second

        public const int MISSILE_INIT_TIME = 500; //ms
        public static readonly Vector3 MISSILE_INIT_DIR = new Vector3(0, -0.5f, -10.0f);

        public const float MISSILE_PREDICATION_TIME_TRESHOLD = 0.05f; //if time to hit is lower than this treshold, missille navigates directly to target

        public const int MISSILE_TARGET_UPDATE_INTERVAL_IN_MS = 100;
        public const float VISUAL_GUIDED_MISSILE_FOV = 40.0f;
        public const float VISUAL_GUIDED_MISSILE_RANGE = 1000.0f;
        public const float ENGINE_GUIDED_MISSILE_RADIUS = 200.0f;
    }

    public static class MyLargeTurretsConstants
    {
        public const float AIMING_SOUND_DELAY = 120.0f; //ms
        public const float ROTATION_AND_ELEVATION_MIN_CHANGE = 0.007f; //rad
        public const float ROTATION_SPEED = 0.005f; //rad per update
        public const float ELEVATION_SPEED = 0.005f; //rad per update
    }

    public static class MyAutomaticRifleGunConstants
    {
        //Interval between two shots
        public const int SHOT_INTERVAL_IN_MILISECONDS = 100;

        //  this is time when we stop looping sound and play rel sound for machine gun
        public const float RELEASE_TIME_AFTER_FIRE = 250;

        public const int MUZZLE_FLASH_MACHINE_GUN_LIFESPAN = 40;
        public const float BACKKICK_IMPULSE = 3.2f;
    }

    public static class MyDrillConstants
    {
        public const float DRILL_SHIP_REAL_LENGTH = 0.98f;
        public const float DRILL_HAND_REAL_LENGTH = 0.98f;
        public const MyParticleEffectsIDEnum DRILL_HAND_DUST_EFFECT = MyParticleEffectsIDEnum.Smoke_HandDrillDust;
        public const MyParticleEffectsIDEnum DRILL_HAND_DUST_STONES_EFFECT = MyParticleEffectsIDEnum.Smoke_HandDrillDustStones;
        public const MyParticleEffectsIDEnum DRILL_HAND_SPARKS_EFFECT = MyParticleEffectsIDEnum.CollisionSparksHandDrill;

        public const MyParticleEffectsIDEnum DRILL_SHIP_DUST_EFFECT = MyParticleEffectsIDEnum.Smoke_DrillDust;
        public const MyParticleEffectsIDEnum DRILL_SHIP_DUST_STONES_EFFECT = MyParticleEffectsIDEnum.Smoke_DrillDust;
        public const MyParticleEffectsIDEnum DRILL_SHIP_SPARKS_EFFECT = MyParticleEffectsIDEnum.Collision_Sparks;

        //public const float DRILL_UPDATE_INTERVAL_IN_MILISECONDS = 150;
        public const int DRILL_UPDATE_INTERVAL_IN_FRAMES = 90;
        public const int DRILL_UPDATE_DISTRIBUTION_IN_FRAMES = 10;
        public const float DRILL_UPDATE_INTERVAL_IN_MILISECONDS = 325;
        public const float DRILL_RELEASE_TIME_IN_MILISECONDS = 350; // Should be higher than update interval, otherwise sound stops every once in a while.
        public const float PARTICLE_EFFECT_DURATION = 500;
        public const float VOXEL_HARVEST_RATIO = 0.009f;
        public const float MAX_DROP_CUBIC_METERS = 0.150f;

        public const float WHOLE_VOXEL_HARVEST_VOLUME = VOXEL_HARVEST_RATIO * MyVoxelConstants.VOXEL_VOLUME_IN_METERS;
    }

    public static class MyExplosionsConstants
    {
        //  Max possible radius for explosions. This is for caching voxels during calculating explosions.
        public const float EXPLOSION_RADIUS_MAX = 100;

        //  Max number of explosions we can have in a scene. This number doesn't mean we will update/draw this explosions. It's just that we can hold so many explosions.
        public const int MAX_EXPLOSIONS_COUNT = 1024;

        public const int MIN_OBJECT_SIZE_TO_CAUSE_EXPLOSION_AND_CREATE_DEBRIS = 5;

        public const float OFFSET_LINE_FOR_DIRT_DECAL = 0.5f;

        public const float EXPLOSION_STRENGTH_IMPULSE = 100;
        public const float EXPLOSION_STRENGTH_ANGULAR_IMPULSE = 50000;
        public const float EXPLOSION_STRENGTH_ANGULAR_IMPULSE_PLAYER_MULTIPLICATOR = 0.25f;
        public const float EXPLOSION_RADIUS_MULTIPLIER_FOR_IMPULSE = 1.25f;          //  If we multiply this number by explosion radius (which is used for cuting voxels and drawing particles), we get radius for applying throwing force to surounding objects
        public const float EXPLOSION_RADIUS_MULTPLIER_FOR_DIRT_GLASS_DECALS = 3;          //  If we multiply this number by explosion radius (which is used for cuting voxels and drawing particles), we get radius for applying dirt decals on ship glass
        public const float EXPLOSION_RANDOM_RADIUS_MAX = 25;
        public const float EXPLOSION_RANDOM_RADIUS_MIN = EXPLOSION_RANDOM_RADIUS_MAX * 0.8f;
        public const int EXPLOSION_LIFESPAN = 700;
        public const float EXPLOSION_CASCADE_FALLOFF = 0.33f; // for cascading explosions (e.g. missiles in a row), this is the explosion influence radius multiplier for each level of the cascade

        //public static readonly Vector4 EXPLOSION_LIGHT_COLOR = new Vector4(154.0f / 255.0f * 6.0f, 83.0f / 255.0f * 6.0f, 63.0f / 255.0f * 6.0f, 1)
        public static readonly Vector4 EXPLOSION_LIGHT_COLOR = new Vector4(248.0f / 255.0f, 179.0f / 255.0f, 12.0f / 255.0f, 1);

        public const float CLOSE_EXPLOSION_DISTANCE = 15; // in meters. explosions closer than this value will look different

        public const int FRAMES_PER_SPARK = 30; // Applies for full damage ratio (100%). 50% has half frequency of sparks etc. Lower value - more frequent sparks
        public const float DAMAGE_SPARKS = 0.4f; // percentage for damage, above which spark effects get generated (e.g. for 0.4 -> when health is below 60%)
        public const float EXPLOSION_FORCE_RADIUS_MULTIPLIER = 0.33f;

        // prefabs that are supposed to have their explosion larger than this will have the 'huge' explosion particle effect
        public const int EXPLOSION_EFFECT_SIZE_FOR_HUGE_EXPLOSION = 300;

        public const int CAMERA_SHAKE_TIME_MS = 300;
    }

    public static class MyEnergyConstants
    {
        public const float BATTERY_MAX_POWER_OUTPUT = 45.0f / 5000; // MW
        public const float BATTERY_MAX_POWER_INPUT = 90.0f / 50000; // MW
        public const float BATTERY_MAX_CAPACITY = 0.5f / 50000; // MWh
        public const float REQUIRED_INPUT_HAND_DRILL = 2.0f / 50000; // MW
        public const float REQUIRED_INPUT_ENGINEERING_TOOL = 5f / 50000;
        public const float REQUIRED_INPUT_CHARACTER_LIGHT = 0.1f / 50000;
        public const float REQUIRED_INPUT_LIFE_SUPPORT = 0.5f / 50000;
        public const float REQUIRED_INPUT_LIFE_SUPPORT_WITHOUT_HELMET = 0.05f / 50000;
        public const float REQUIRED_INPUT_JETPACK = 1.0f / 50000;
        public const int RECHARGE_TIMEOUT = 100; // timeout used to disable recharging when recharger is not used.

        public const float MAX_RADIO_POWER_RANGE = 50000;
        public const float MAX_SMALL_RADIO_POWER_RANGE = 5000;

        public const float MAX_REQUIRED_POWER_TURRET = 100.0f / 50000; // MW
        public const float MAX_REQUIRED_POWER_ORE_DETECTOR = 100.0f / 50000;
        public const float MAX_REQUIRED_POWER_ANTENNA = 100.0f / 50000;
        public const float MAX_REQUIRED_POWER_BEACON = 1000.0f / 50000;
        public const float MAX_REQUIRED_POWER_DOOR = 0.00003f; // 30 W
        public const float MAX_REQUIRED_POWER_SHIP_DRILL = 100.0f / 50000;
        public const float MAX_REQUIRED_POWER_SHIP_GRINDER = 100.0f / 50000;
        public const float MAX_REQUIRED_POWER_SHIP_GUN = 10.0f / 50000;
        public const float MAX_REQUIRED_POWER_MEDICAL_ROOM = 100.0f / 50000;
        public const float MAX_REQUIRED_POWER_SOUNDBLOCK = 10.0f / 50000;

        public const float MIN_REQUIRED_POWER_THRUST_CHANGE_THRESHOLD = 0.001f / 50000;

        public const float MAX_REQUIRED_POWER_CONNECTOR = 0.001f;
        public const float REQUIRED_INPUT_CONVEYOR_LINE = 0.00002f; // 20 W per tube
    }

    public static class MyDebrisConstants
    {
        public const int EXPLOSION_VOXEL_DEBRIS_LIFESPAN_MIN_IN_MILISECONDS = 10000;
        public const int EXPLOSION_VOXEL_DEBRIS_LIFESPAN_MAX_IN_MILISECONDS = 20000;
        public const int EXPLOSION_MODEL_DEBRIS_LIFESPAN_MIN_IN_MILISECONDS = 4000;
        public const int EXPLOSION_MODEL_DEBRIS_LIFESPAN_MAX_IN_MILISECONDS = 7000;
        public const int EXPLOSION_DEBRIS_OBJECTS_MAX = 150;
        public const float EXPLOSION_DEBRIS_INITIAL_SPEED_MIN = 4.0f;
        public const float EXPLOSION_DEBRIS_INITIAL_SPEED_MAX = 8.0f;
        public const float EXPLOSION_MODEL_DEBRIS_INITIAL_SCALE_MIN = 0.25f;
        public const float EXPLOSION_MODEL_DEBRIS_INITIAL_SCALE_MAX = 0.75f;
        public const float EXPLOSION_VOXEL_DEBRIS_INITIAL_SCALE_MIN = 0.25f;
        public const float EXPLOSION_VOXEL_DEBRIS_INITIAL_SCALE_MAX = 0.5f;

        public const int EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT = 2;
        public const int EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT_3 = EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT * EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT * EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT;

        // only approximate, will always be higher (see MyDebris.GeneratePositions for usage)
        public const int APPROX_NUMBER_OF_DEBRIS_OBJECTS_PER_MODEL_EXPLOSION = 3;
        public const float CUT_TREE_IN_MILISECONDS = 1500;
    }

    public static class MyInventoryConstants
    {
        public const int GUI_DISPLAY_MAX_DECIMALS = 2;
        public const string GUI_DISPLAY_FORMAT = "N";
        public const string GUI_DISPLAY_VOLUME_FORMAT = "N0";
    }

    public static class MyShipGrinderConstants
    {
        public const int GRINDER_RELEASE_TIME_IN_MILISECONDS = 500;
        public const int GRINDER_COOLDOWN_IN_MILISECONDS = 250;
        public const int GRINDER_HEATUP_FRAMES = 15;
        public const float GRINDER_AMOUNT_PER_SECOND = 2.0f;
        public const float GRINDER_MAX_SPEED_RPM = 150f;
        public const float GRINDER_ACCELERATION_RPMPS = 300f;
        public const float GRINDER_DECELERATION_RPMPS = 200f;
    }

    public static class MyMeteorShowerEventConstants
    {
        public const float NORMAL_HOSTILITY_MIN_TIME = 16.0f;
        public const float NORMAL_HOSTILITY_MAX_TIME = 24.0f;
        public const float CATACLYSM_HOSTILITY_MIN_TIME = 1.0f;
        public const float CATACLYSM_HOSTILITY_MAX_TIME = 1.5f;
        public const float CATACLYSM_UNREAL_HOSTILITY_MIN_TIME = 0.1f;
        public const float CATACLYSM_UNREAL_HOSTILITY_MAX_TIME = 0.3f;
    }
}
