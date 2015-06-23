using Sandbox.Common;
using Sandbox.Game.Entities;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sandbox.Engine.Utils
{
    public static class MyFakes
    {
        static MyFakes()
        {
            // Called after all the fields have been initialized to override any settings with local ones.
            RuntimeHelpers.RunClassConstructor(typeof(MyFakesLocal).TypeHandle);
        }

        /// <summary>
        /// This should be const
        /// </summary>
        public const bool DETECT_LEAKS = false;

        public static bool ENABLE_LONG_DISTANCE_GIZMO_DRAWING = false;

        public static MyQuickLaunchType? QUICK_LAUNCH = null;

        public static string SINGLE_VOXEL_MATERIAL = null;//"Ice_01";s

        public static bool ALT_AS_DEBUG_KEY = true;

        public static bool ENABLE_MENU_VIDEO_BACKGROUND = true;

        public static bool ENABLE_LOGOS = false;

        public static bool ENABLE_SPLASHSCREEN = !MyFinalBuildConstants.IS_DEBUG;

        internal static MyEntity FakeTarget = null;

        public static bool ENABLE_EDGES = true;
        public static bool ENABLE_GRAVITY_PHANTOM = true;

        public static bool ENABLE_INFINITE_REACTOR_FUEL = false; // When enabled, generator generates new uranium when it runs out of old stock.
        public static bool ENABLE_BATTERY_SELF_RECHARGE = false; // When enabled, battery is able to recharge itself even if not plugged in.

        // To make player experience better, slowdown will be faster than acceleration by this ratio
        public static float SLOWDOWN_FACTOR_THRUST_MULTIPLIER = 10.0f;
        public static float SLOWDOWN_FACTOR_TORQUE_MULTIPLIER = 5.0f;
        public static float SLOWDOWN_FACTOR_TORQUE_MULTIPLIER_LARGE_SHIP = 2.0f;

        public static bool MANUAL_CULL_OBJECTS = true;

        public static bool ENABLE_JOYSTICK_SETTINGS = true;

        public static bool UNLIMITED_CHARACTER_BUILDING = false;

        public static bool SMALL_SHIP_LEVITATION = false;

        public static bool DETECT_DISCONNECTS = true;

        public static bool DRAW_GUI_SCREEN_BORDERS = false;

        public static bool SHOW_DAMAGE_EFFECTS = true;

        public static float THRUST_FORCE_RATIO = 1.0f;

        // Higher number makes deformation and destruction faster
        public static float DEFORMATION_RATIO = 1.0f;

        public static bool ENABLE_AUTOSAVE = true;

        public static bool ENABLE_TRANSPARENT_CUBE_BUILDER = true;
        public static bool HIDE_ENGINEER_TOOL_HIGHLIGHT = false;

        public static bool ENABLE_SIMPLE_GRID_PHYSICS = false;

        // Draw software cursor even when hardware cursor is enabled.
        public static bool FORCE_SOFTWARE_MOUSE_DRAW = false;

        public static bool ENABLE_GATLING_TURRETS = true;
        public static bool ENABLE_MISSILE_TURRETS = true;
        public static bool ENABLE_INTERIOR_TURRETS = true;

        //October release (true = enable october release features)
        public static bool OCTOBER_RELEASE_DISABLE_WEAPONS_AND_TOOLS = false;
        public static bool OCTOBER_RELEASE_HIDE_WORLD_PARAMS = true;
        public static bool OCTOBER_RELEASE_ASSEMBLER_ENABLED = true;
        public static bool OCTOBER_RELEASE_REFINERY_ENABLED = true;
        public static readonly String[] OCTOBER_RELEASE_DISABLED_HANDHELD_WEAPONS = new String[]
        {
            "AngleGrinderItem",
            "WelderItem",
        };
        public static bool ENABLE_DAMAGED_COMPONENTS = false;

        public static bool GAME_SAVES_COMPRESSED_BY_DEFAULT = false;

        public static bool RANDOM_CARGO_PLACEMENT = false;

        // Useful when making videos
        public static bool SHOW_HUD_DISTANCES = true;

        public static bool CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE = false;

        public static bool ENABLE_EXPORT_TO_OBJ = false;

        public static bool SHOW_INVALID_TRIANGLES = false;

        public static bool ENABLE_NEW_SOUNDS = false;

        public static bool ENABLE_NON_PUBLIC_BLOCKS = false;

        public static bool ENABLE_COLOR_MASK_FOR_EVERYTHING = false;

        public static bool ENABLE_PRODUCTION_SYNC = true;

        public static bool ENABLE_CHARACTER_AND_DEBRIS_COLLISIONS = false;
        
        //  When true, every update will contain few miliseconds of delay - use only for testing/debugging
        public static bool SIMULATE_SLOW_UPDATE = false;

        public static bool SHOW_AUDIO_DEV_SCREEN = false;
        public static bool ENABLE_SCRAP = true;

        public static bool SIMULATE_NO_SOUND_CARD = false;

        public static bool INACTIVE_THRUSTER_DMG = false;

        public static bool ENABLE_CARGO_SHIPS = true;

        public static bool ENABLE_METEOR_SHOWERS = true;

        public static bool ENABLE_DX11_RENDERER = true;

        public static bool APRIL2014_ENABLED = false;
        public static bool SHOW_INVENTORY_ITEM_IDS = false;

        // Reduces CPU usage by using timer and wait instead of spin
        public static bool ENABLE_UPDATE_WAIT = true;

        public static bool SIMULATE_QUICK_TRIGGER = false;

        public static bool REPORT_INVALID_ROTORS = false;

        public static float SIMULATION_SPEED = 1.0f;

        public static bool AUDIO_TEST = false;

        public static bool ENABLE_STRUCTURAL_INTEGRITY = true;

        public static bool TEST_PREFABS_FOR_INCONSISTENCIES = false;

        public static bool SHOW_PRODUCTION_QUEUE_ITEM_IDS = false;

        public static bool ENABLE_MP_DATA_HASHES = false;

        public static bool ENABLE_CONNECT_COMMAND_LINE = true;

        public static bool ENABLE_WHEEL_CONTROLS_IN_COCKPIT = true;

        public static bool TEST_NEWS = false;

        public static bool ENABLE_TRASH_REMOVAL = true;

        public static bool SHOW_FACTIONS_GUI = true;
        public static bool ENABLE_RADIO_HUD = true;

        public static bool ENABLE_BUILDING_IN_OCCUPIED_AREA_HACK = !MyFinalBuildConstants.IS_OFFICIAL;

        // Workaround to keep game ballance after fixing
        // error in removed voxel content calculation.
        public static bool ENABLE_REMOVED_VOXEL_CONTENT_HACK = true;

        public static bool ENABLE_CENTER_OF_MASS = true;

        // With debugger attached, throw exceptions during loading.
        public static bool THROW_LOADING_ERRORS = Debugger.IsAttached;

        public static bool ENABLE_VIDEO_PLAYER = true;

        public static bool ENABLE_LOADING_CONTENT_WORLDS = !MyFinalBuildConstants.IS_OFFICIAL;

        public static bool ENABLE_COPY_GROUP = true;

        public static bool LANDING_GEAR_BREAKABLE = true;

        // Landing gear ignore contacts with grid they're attached to
        public static bool LANDING_GEAR_IGNORE_DAMAGE_CONTACTS = true;

        public static bool ENABLE_WORKSHOP_MODS = true;

        public static bool ENABLE_BATTERY = true;

        public static bool ENABLE_SHIP_BLOCKS_TOOLBAR = true;

        public static bool ENABLE_BLOCK_SHIP_SWAP = false;

        public static bool SKIP_VOXELS_DURING_LOAD = false;

        public static bool ENABLE_PISTON = true;

        public static int QUANTIZER_VALUE = 8;

        public static bool ENABLE_OCTREE_STORAGE = false;
        public static bool ENABLE_FORCED_SINGLE_CORE_PRECALC = false;

        public static bool DEDICATED_SERVER_USE_SOCKET_SHARE = true;

        public static bool ENABLE_TERMINAL_PROPERTIES = true;

        public static bool ENABLE_GYRO_OVERRIDE = true;

        public static bool TEST_MODELS = !MyFinalBuildConstants.IS_OFFICIAL;

        public static bool DISABLE_SOUND_POOLING = true;

        public static bool MOVE_WINDOW_TO_CORNER = false;

        public static bool ENABLE_GRAVITY_GENERATOR_SPHERE = true;

        public static bool ENABLE_CAMERA_BLOCK = true;

        public static bool ENABLE_REMOTE_CONTROL = true;

        public static bool ENABLE_DAMPENERS_OVERRIDE = true;

        public static bool ENABLE_BLOCK_PLACEMENT_ON_VOXEL = false;

        public static bool ENABLE_VOXEL_MODIFIER_EVERYWHERE = false;

        public static bool ENABLE_MULTIPLAYER_VELOCITY_COMPENSATION = true;

        public static bool ENABLE_MULTIPLAYER_CONSTRAINT_COMPENSATION = true;

        public static bool ENABLE_COMPOUND_BLOCKS = false;

        public static bool ENABLE_LIGHT_WITHOUT_POWER = false;

        public static bool ENABLE_VOXEL_PHYSICS_PRECALC = false;

        public static bool ENABLE_SCRIPTS = true;

        public static bool ENABLE_ISO_MESHER_FROM_PLUGIN = true;

        public static bool ENABLE_SCRIPTS_PDB = false;

        public static bool ENABLE_TURRET_CONTROL = true;

        public static bool ENABLE_SPAWN_MENU_ASTEROIDS = true;
        public static bool ENABLE_SPAWN_MENU_PROCEDURAL_ASTEROIDS = false;

        public static bool ENABLE_VOLUMETRIC_EXPLOSION = true;

        public static bool ENABLE_DUMMY_MIRROR_MATRIX_CHECK = false;

        public static bool ENABLE_ASSEMBLER_COOPERATION = true;
        
        public static bool ENABLE_WELDER_HELP_OTHERS = true;

        public static bool ENABLE_MISSION_SCREEN = false;

        public static bool ENABLE_OBJECTIVE_LINE = true;

        public static bool ENABLE_SUBBLOCKS = false;

        public static bool ENABLE_MULTIBLOCKS = false;
        public static bool ENABLE_MULTIBLOCKS_IN_SURVIVAL = false;

        public static bool RUN_SCRIPT_UT = true;

        public static bool ENABLE_PARTICLE_VELOCITY = true;

        public static bool USE_CUSTOM_HEIGHT_MAP = false;
        public static bool USE_CUSTOM_BIOME_MAP  = false;

        public static bool ENABLE_STATIC_SMALL_GRID_ON_LARGE = false;

        public static bool ENABLE_NETGRAPH = true;

        public static bool ENABLE_LARGE_OFFSET = false;
        public static bool ENABLE_PROJECTOR_BLOCK = true;

        public static bool DESTRUCTION_ONLY_FOR_LARGE_GRIDS = false;

        public static bool ENABLE_ASTEROID_FIELDS = true;

        public static bool ENABLE_USE_OBJECT_HIGHLIGHT = true;

        public static float MAX_PRECALC_TIME_IN_MILLIS = 20f;
        public static bool ENABLE_YIELDING_IN_PRECALC_TASK = false;

        public static bool LOAD_UNCONTROLLED_CHARACTERS = false;

        public static bool ENABLE_PREFAB_THROWER = true; // Enabled on default

        // Allow shrinking of block bounding box for placing it in occupied areas (small grid on large grid or large/large with placed small grid areas).
        public static bool ENABLE_BLOCK_PLACING_IN_OCCUPIED_AREA = false;

        public static bool ENABLE_DYNAMIC_SMALL_GRID_MERGING = false;
        public static bool SKIP_MECHANICAL_UPDATE = false;

        public static bool ENABLE_ENVIRONMENT_ITEMS = true;

        // Artificial Inteligence
        public static bool NAVMESH_PRESUMES_DOWNWARD_GRAVITY = false;
        public static bool ENABLE_BARBARIANS = true;
        public static bool ENABLE_PATHFINDING = false;
        public static bool BARBARIANS_SPAWN_NEAR_PLAYER = false;
        public static bool DEBUG_DRAW_NAVMESH_PROCESSED_VOXEL_CELLS = false;
        public static bool REMOVE_VOXEL_NAVMESH_CELLS = true;
        public static bool DEBUG_DRAW_VOXEL_CONNECTION_HELPER = false;
        public static bool DEBUG_DRAW_FOUND_PATH = false;
        public static bool DEBUG_DRAW_FUNNEL = false;
        public static bool DEBUG_DRAW_NAVMESH_CELL_BORDERS = false;
        public static bool DEBUG_DRAW_NAVMESH_HIERARCHY = false;
        public static bool DEBUG_DRAW_NAVMESH_HIERARCHY_LITE = false;
        public static bool DEBUG_DRAW_NAVMESH_EXPLORED_HL_CELLS = false;
        public static bool DEBUG_DRAW_NAVMESH_FRINGE_HL_CELLS = false;
        public static bool DEBUG_DRAW_NAVMESH_LINKS = false;
        public static bool SHOW_PATH_EXPANSION_ASSERTS = false;

        public static bool ENABLE_AFTER_REPLACE_BODY = true;

        public static float CHARACTER_FACE_FORWARD = 0.0f;

        public static bool ENABLE_BLOCK_PLACING_ON_INTERSECTED_POSITION = false;

        public static bool ENABLE_COMMUNICATION = true;

        public static bool ENABLE_GUI_HIDDEN_CUBEBLOCKS = false;

        public static bool ENABLE_BLOCK_STAGES = false;
        public static bool SHOW_REMOVE_GIZMO = true;

        public static bool ENABLE_PROGRAMMABLE_BLOCK = true;

        public static bool CLIPBOARD_CUT_CONFIRMATION = true;

        public static bool ENABLE_DESTRUCTION_EFFECTS = true;

        public static bool ENABLE_COLLISION_EFFECTS = true;

        // Minimum velocity of prefab thrower
        public static float PREFAB_THROWER_MIN_VELOCITY = 1;

        // Maximum velocity of prefab thrower
        public static float PREFAB_THROWER_MAX_VELOCITY = 80;

        // How many seconds it takes to hold the button when throwing prefab to reach max velocity
        public static float PREFAB_THROWER_MAX_PULL_LENGTH_SECONDS = 1.0f;

        public static bool ENABLE_PHYSICS_HIGH_FRICTION = false;
        public static float PHYSICS_HIGH_FRICTION = 0.7f;

        public static bool REUSE_OLD_PLAYER_IDENTITY = true;

        public static bool MAKE_SMALL_WORLD_WITH_QUICKSTART = false; // ME setting

        public static bool ENABLE_MOD_CATEGORIES = true;

        public static bool ENABLE_GENERATED_BLOCKS = false;

        public static bool ENABLE_HAVOK_MULTITHREADING = false;

        public static bool ENABLE_GPS = true;

        public static bool LOG_RENDER_LOADED_FILES = false;

        public static bool SHOW_FORBIDDEN_ENITIES_VOXEL_HAND = true;

        public static bool ENABLE_WEAPON_TERMINAL_CONTROL = true;
        public static bool ENABLE_WAIT_UNTIL_CLIPMAPS_READY = !MyFinalBuildConstants.IS_DEBUG;

        public static bool SHOW_MISSING_DESTRUCTION = MyFinalBuildConstants.IS_DEBUG;

        public static bool ENABLE_DEBUG_DRAW_TEXTURE_NAMES = false;

        // Fake for disabling Gyro, Thrusters, Conveyors, Cameras etc
        public static bool ENABLE_GRID_SYSTEM_UPDATE = true;
        public static bool ENABLE_GRID_SYSTEM_ONCE_BEFORE_FRAME_UPDATE = ENABLE_GRID_SYSTEM_UPDATE;

        public static string CHANGE_ALL_TREES = null; // "Tree03_v2";

        public static bool REDUCE_FRACTURES_COUNT = false;
        public static bool REMOVE_GENERATED_BLOCK_FRACTURES = true;

        public static bool ENABLE_TOOL_SHAKE = true;

        public static bool OVERRIDE_LANDING_GEAR_INERTIA = false;
        public static float LANDING_GEAR_INTERTIA = 0.1f;

        public static bool ASSERT_CHANGES_IN_SIMULATION = false;

        public static bool USE_HAVOK_MODELS = false;

        public static bool ENABLE_DEVELOPER_SPECTATOR_CONTROLS = MyFinalBuildConstants.IS_DEBUG;

        public static bool LAZY_LOAD_DESTRUCTION = true;

        public static bool ENABLE_STANDARD_AXES_ROTATION = false;

        public static bool ENABLE_ARMOR_HAND = false;

        public static bool ENABLE_CUBE_BUILDER_DYNAMIC_MODE = false;

        public static bool ENABLE_SIMPLE_SURVIVAL = false;

        public static bool ASSERT_NON_PUBLIC_BLOCKS = false; 
        public static bool REMOVE_NON_PUBLIC_BLOCKS = false;

        public static bool ENABLE_ROTATION_HINTS = true;

        public static bool ENABLE_TUTORIAL_PROMPT = true;

        public static bool ENABLE_NOTIFICATION_BLOCK_NOT_AVAILABLE = true;

        public static bool ENABLE_GRID_CLIPBOARD_CHANGE_TO_DYNAMIC = false;//needed for planets to set to true

        public static bool ENABLE_BEHAVIOR_TREE_TOOL_COMMUNICATION = true;

        public static bool PAUSE_PHYSICS = false;
        public static bool STEP_PHYSICS = false;

        public static bool ENABLE_VOXEL_PHYSICS_SHAPE_DISCARDING = true;

        public static bool ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS = false;

        public static bool ENABLE_ALTERNATIVE_CLIPBOARD = false;

        public static bool ENABLE_LARGE_STATIC_GROUP_COPY_FIRST = false;
        
        public static bool CLONE_SHAPES_ON_WORKER = true;

        public static bool FRACTURED_BLOCK_AABB_MOUNT_POINTS = true;

        public static bool ENABLE_BLOCK_COLORING = true;
        
        public static bool CHANGE_BLOCK_CONVEX_RADIUS = true;

        public static bool ENABLE_TEST_BLOCK_CONNECTIVITY_CHECK = false;

        public static bool ENABLE_CUSTOM_CHARACTER_IMPACT = false;

        public static bool ENABLE_FOOT_IK = false;

        public static bool ENABLE_JETPACK_IN_SURVIVAL = true;
        
        public static bool CHARACTER_TOOLS = true;

        public static bool ENABLE_RAGDOLL_ANIMATION = false;

        public static bool ENABLE_CHARACTER_VIRTUAL_PHYSICS = false;

        public static bool ENABLE_RAKNET = false;

        public static bool ENABLE_MEDIEVAL_CHARACTER_DAMAGE = false;

        public static bool ENABLE_RAGDOLL_COLLISION_WITH_CHARACTER_BODY = true;

        public static bool ENABLE_FOOT_IK_USE_HAVOK_RAYCAST = true;

        public static bool ENABLE_TREE_CUTTING = false;

        public static bool RAGDOLL_3DSMAX_IDENTITY_ENABLED = false;

        public static bool ALWAYS_MORNING_FOG = false;

        public static bool ME_MULTIPLAYER = true;
        public static bool ME_PLAYERS_SPAWN_NEAR_PLAYER = true;
        public static bool CHARACTER_SERVER_SYNC = false;
        

        public static bool ALWAYS_NOON = false;

        public static bool DEVELOPMENT_PRESET = false;

        public static bool SHOW_CURRENT_VOXEL_MAP_AABB_IN_VOXEL_HAND = true;

        public static bool ENABLE_BATTLE_SYSTEM = false;

        public static bool ENABLE_DRAW_VOXEL_STORAGE_PLAYER_POSITION = false;

        public static bool ENABLE_OXYGEN_SOUNDS = false;

        public static bool ENABLE_ROPE_UNWINDING_TORQUE = false;
        public static bool ENABLE_LOCKABLE_ROPE_DRUM = true;

        public static bool ENABLE_BONES_AND_ANIMATIONS_DEBUG = false;

        public static bool ENABLE_MISSION_TRIGGERS = true;

        public static bool ENABLE_RAGDOLL_DEFAULT_PROPERTIES = false;

        public static bool XBOX_PREVIEW = false;
                
        public static bool ENABLE_RAGDOLL_BONES_TRANSLATION = true;
        
        public static bool ENABLE_COLLISONS_ON_RAGDOLL = true;

        public static bool ENABLE_STATION_ROTATION = true;

        public static bool ENABLE_CONTROLLER_HINTS = true;

        public static bool ENABLE_SUN_BILLBOARD = true;

        public static bool ENABLE_PHYSICS_SETTINGS = false;

        // Enables blueprints in Data/Blueprints folder) to be visible in game.
        public static bool ENABLE_DEFAULT_BLUEPRINTS = false;

        public static bool ENABLE_VOICE_CHAT_DEBUGGING = false;
        
        public static bool ENABLE_RAGDOLL_CLIENT_SYNC = false;

        public static bool ENABLE_GENERATED_INTEGRITY_FIX = true; //forces generated blocks to have same stack and integrity as owner

        public static bool ENABLE_VOXEL_MAP_AABB_CORNER_TEST = false;
        
        public static bool ENABLE_RAGDOLL_DEACTIVATION = false;

        public static bool ENABLE_PERMANENT_SIMULATIONS_COMPUTATION = true;
        
        public static bool ENABLE_SYNCED_CHARACTER_MOVE_AND_ROTATE = false;
        
        public static bool ENABLE_RAGDOLL_DEBUG = false;
        
        public static bool ENABLE_JETPACK_RAGDOLL_COLLISIONS = false;

        public static bool NEW_CHARACTER_DAMAGE = false;

        public static bool ENABLE_ADMIN_SPECTATOR_BUILDING = false;

        public static bool MANIPULATION_TOOL_VELOCITY_LIMIT = false;

        public static bool ENABLE_GATHERING = true;
        
        public static string QUICK_LAUNCH_SCENARIO = String.Empty;

        public static bool ENABLE_DEBUG_DRAW_GENERATING_BLOCK = false;
        
        public static bool ENABLE_MEDIEVAL_INVENTORY = false;

        public static bool ENABLE_PLANETS = false;
    }
}
