using Sandbox.Common;
using Sandbox.Game.Entities;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using VRage.Game;
using VRage.Game.Entity;

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

        public static bool ENABLE_DAMAGED_COMPONENTS = false;

        public static bool GAME_SAVES_COMPRESSED_BY_DEFAULT = false;

        public static bool RANDOM_CARGO_PLACEMENT = false;

        // Useful when making videos
        public static bool SHOW_HUD_DISTANCES = true;

        public static bool CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE = false;

        public static bool ENABLE_EXPORT_TO_OBJ = false;

        public static bool SHOW_INVALID_TRIANGLES = false;

        //Sound Myfakes
        public static bool ENABLE_NEW_SOUNDS = false;
        public static bool ENABLE_NEW_SOUNDS_QUICK_UPDATE = false;
        public static bool ENABLE_NEW_SMALL_SHIP_SOUNDS = true;
        public static bool ENABLE_NEW_LARGE_SHIP_SOUNDS = true;
        public static bool ENABLE_MUSIC_CONTROLLER = true;
        public static bool ENABLE_REALISTIC_LIMITER = true;

        public static bool ENABLE_NON_PUBLIC_BLOCKS = false;
        public static bool ENABLE_NON_PUBLIC_SCENARIOS = !MyFinalBuildConstants.IS_OFFICIAL;
        public static bool ENABLE_NON_PUBLIC_CATEGORY_CLASSES = false;
        public static bool ENABLE_NON_PUBLIC_BLUEPRINTS = false;
        public static bool ENABLE_NON_PUBLIC_GUI_ELEMENTS = false;

        public static bool ENABLE_COLOR_MASK_FOR_EVERYTHING = false;

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

        public static bool SHOW_INVENTORY_ITEM_IDS = false;
        
        public static bool SIMULATE_QUICK_TRIGGER = false;

        public static float SIMULATION_SPEED = 1.0f;

        public static bool AUDIO_TEST = false;

        public static bool ENABLE_STRUCTURAL_INTEGRITY = false;

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

        public static bool ENABLE_AUTO_HEAL = false;

        public static bool ENABLE_CENTER_OF_MASS = true;

        // With debugger attached, throw exceptions during loading.
        public static bool THROW_LOADING_ERRORS = false;

        public static bool ENABLE_VIDEO_PLAYER = true;

        public static bool ENABLE_LOADING_CONTENT_WORLDS = !MyFinalBuildConstants.IS_OFFICIAL;

        public static bool ENABLE_COPY_GROUP = true;

        public static bool LANDING_GEAR_BREAKABLE = false;

        // Landing gear ignore contacts with grid they're attached to
        public static bool LANDING_GEAR_IGNORE_DAMAGE_CONTACTS = true;

#if !XB1 // XB1_NOWORKSHOP
        public static bool ENABLE_WORKSHOP_MODS = true;
#endif // !XB1

        public static bool ENABLE_BATTERY = true;

        public static bool ENABLE_SHIP_BLOCKS_TOOLBAR = true;

        public static bool ENABLE_BLOCK_SHIP_SWAP = false;

        public static bool SKIP_VOXELS_DURING_LOAD = false;

        // When enabled replication distance is 100m and sleep time 30 seconds.
        public static bool MULTIPLAYER_REPLICATION_TEST = false;
        
        // When enabled, specific asserts in multiplayer code no longer trigger
        public static bool DISABLE_MULTIPLAYER_ASSERTS = true;

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
        public static bool ENABLE_SPAWN_MENU_PROCEDURAL_ASTEROIDS = true;
        public static bool ENABLE_SPAWN_MENU_EMPTY_VOXEL_MAPS = MyFinalBuildConstants.IS_OFFICIAL;

        public static bool ENABLE_VOLUMETRIC_EXPLOSION = true;

        public static bool ENABLE_DUMMY_MIRROR_MATRIX_CHECK = false;
       
        public static bool ENABLE_WELDER_HELP_OTHERS = true;

        public static bool ENABLE_MISSION_SCREEN = false;

        public static bool ENABLE_OBJECTIVE_LINE = true;

        public static bool ENABLE_SUBBLOCKS = false;

        public static bool ENABLE_MULTIBLOCKS = false;
        public static bool ENABLE_MULTIBLOCKS_IN_SURVIVAL = false;
        public static bool ENABLE_MULTIBLOCK_PART_IDS = false;
        public static bool ENABLE_MULTIBLOCK_CONSTRUCTION = false;

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

        public static bool ENABLE_USE_NEW_OBJECT_HIGHLIGHT = true;

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
        public static bool ENABLE_BARBARIANS = false;
        public static bool BARBARIANS_SPAWN_NEAR_PLAYER = false;
        public static bool DEBUG_DRAW_NAVMESH_PROCESSED_VOXEL_CELLS = false;
        public static bool DEBUG_DRAW_NAVMESH_PREPARED_VOXEL_CELLS = false;
        public static bool DEBUG_DRAW_NAVMESH_CELLS_ON_PATHS = false;
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
        public static bool DEBUG_ONE_AI_STEP_SETTING = false;    // allow only one step od AI (by setting of flag DEBUG_ONE_AI_STEP)
        public static bool DEBUG_ONE_AI_STEP = false;
        public static bool DEBUG_ONE_VOXEL_PATHFINDING_STEP_SETTING = false;// allow only one step of voxel pathfinding (and 5 steps of other AI stuff) - it has higher priority than DEBUG_ONE_AI_STEP_SETTING
        public static bool DEBUG_ONE_VOXEL_PATHFINDING_STEP = false;
        public static bool DO_SOME_ACTION = false;  // variable for case that we want to make some action after a key press
        public static bool DEBUG_BEHAVIOR_TREE = false;     // stepping of behavior tree processing enabled/disabled
        public static bool DEBUG_BEHAVIOR_TREE_ONE_STEP = false;    // allow of one step of behaviour tree processing

        public static bool LOG_NAVMESH_GENERATION = false;
        public static bool REPLAY_NAVMESH_GENERATION = false;
        public static bool REPLAY_NAVMESH_GENERATION_TRIGGER = false;

        public static bool ENABLE_AFTER_REPLACE_BODY = true;

        public static bool ENABLE_BLOCK_PLACING_ON_INTERSECTED_POSITION = false;

        public static bool ENABLE_COMMUNICATION = true;

        public static bool ENABLE_GUI_HIDDEN_CUBEBLOCKS = true;

        public static bool ENABLE_BLOCK_STAGES = true;
        public static bool SHOW_REMOVE_GIZMO = true;

        public static bool ENABLE_PROGRAMMABLE_BLOCK = true;

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

        public static bool LAZY_LOAD_DESTRUCTION = true;

        public static bool ENABLE_STANDARD_AXES_ROTATION = false;

        public static bool ENABLE_ARMOR_HAND = false;

        //public static bool ENABLE_CUBE_BUILDER_DYNAMIC_MODE = true;

        public static bool ASSERT_NON_PUBLIC_BLOCKS = false; 
        public static bool REMOVE_NON_PUBLIC_BLOCKS = false;

        public static bool ENABLE_ROTATION_HINTS = true;

        public static bool ENABLE_TUTORIAL_PROMPT = true;

        public static bool ENABLE_NOTIFICATION_BLOCK_NOT_AVAILABLE = true;

        public static bool ENABLE_BEHAVIOR_TREE_TOOL_COMMUNICATION = true;

        public static bool PAUSE_PHYSICS = false;
        public static bool STEP_PHYSICS = false;

        public static bool ENABLE_VOXEL_PHYSICS_SHAPE_DISCARDING = true;

        public static bool ENABLE_SMALL_BLOCK_TO_LARGE_STATIC_CONNECTIONS = false;

        public static bool ENABLE_ADVANCED_CLIPBOARD = false;

        public static bool ENABLE_LARGE_STATIC_GROUP_COPY_FIRST = false;
        
        public static bool CLONE_SHAPES_ON_WORKER = true;

        public static bool FRACTURED_BLOCK_AABB_MOUNT_POINTS = true;

        public static bool ENABLE_BLOCK_COLORING = true;
        
        public static bool CHANGE_BLOCK_CONVEX_RADIUS = true;

        public static bool ENABLE_TEST_BLOCK_CONNECTIVITY_CHECK = false;

        public static bool ENABLE_CUSTOM_CHARACTER_IMPACT = false;

        public static bool ENABLE_FOOT_IK = false;

        public static bool ENABLE_JETPACK_IN_SURVIVAL = true;
        
        public static bool ENABLE_RAKNET = false;

        public static bool ENABLE_MEDIEVAL_CHARACTER_DAMAGE = false;

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

        public static bool ENABLE_OXYGEN_SOUNDS = true;

        public static bool ENABLE_ROPE_UNWINDING_TORQUE = false;
        public static bool ENABLE_LOCKABLE_ROPE_DRUM = true;

        public static bool ENABLE_BONES_AND_ANIMATIONS_DEBUG = false;

        public static bool ENABLE_MISSION_TRIGGERS = true;

        public static bool XBOX_PREVIEW = false;

        // Ragdoll
        public static bool ENABLE_RAGDOLL_ANIMATION = false;
        public static bool ENABLE_RAGDOLL_COLLISION_WITH_CHARACTER_BODY = true;
        public static bool ENABLE_RAGDOLL_BONES_TRANSLATION = false;
        public static bool ENABLE_COLLISONS_ON_RAGDOLL = true;
        public static bool ENABLE_RAGDOLL_DEFAULT_PROPERTIES = false;
        public static bool ENABLE_RAGDOLL_CLIENT_SYNC = false;
        public static bool FORCE_RAGDOLL_DEACTIVATION = false;
        public static bool ENABLE_RAGDOLL_DEBUG = false;
        public static bool ENABLE_JETPACK_RAGDOLL_COLLISIONS = false;

        public static bool ENABLE_STATION_ROTATION = true;

        public static bool ENABLE_CONTROLLER_HINTS = true;

        public static bool ENABLE_SUN_BILLBOARD = true;

        public static bool ENABLE_PHYSICS_SETTINGS = false;

        // Enables blueprints in Data/Blueprints folder) to be visible in game.
        public static bool ENABLE_DEFAULT_BLUEPRINTS = false;

        public static bool ENABLE_VOICE_CHAT_DEBUGGING = false;

        public static bool ENABLE_GENERATED_INTEGRITY_FIX = true; //forces generated blocks to have same stack and integrity as owner

        public static bool ENABLE_VOXEL_MAP_AABB_CORNER_TEST = false;

        public static bool ENABLE_PERMANENT_SIMULATIONS_COMPUTATION = true;
        
        public static bool ENABLE_SYNCED_CHARACTER_MOVE_AND_ROTATE = false;

        public static bool NEW_CHARACTER_DAMAGE = true;

        public static bool ENABLE_ADMIN_SPECTATOR_BUILDING = false;

        public static bool MANIPULATION_TOOL_VELOCITY_LIMIT = false;

        public static bool ENABLE_GATHERING = true;
        
        public static string QUICK_LAUNCH_SCENARIO = String.Empty;

        public static bool ENABLE_DEBUG_DRAW_GENERATING_BLOCK = false;
        
        public static bool ENABLE_MEDIEVAL_INVENTORY = false;

        /// <summary>
        /// If true, container grid mass will be static
        /// If false, container grid mass includes the content mass
        /// </summary>
        public static bool ENABLE_STATIC_INVENTORY_MASS = false;

        public static bool ENABLE_PLANETS = true;

        public static bool ENABLE_NEW_TRIGGERS = true;

        public static bool ENABLE_USE_OBJECT_CORNERS = true;
        
        public static bool ENABLE_PLANETS_JETPACK_LIMIT_IN_CREATIVE = false;
        
        public static bool ENABLE_WEAPON_USE = false;

        public static bool ENABLE_STATS_GUI = true;

        public static bool NEW_POS_UPDATE_TIMING = false;

        public static bool ENABLE_CUBE_BUILDER_MULTIBLOCK = false;

        public static bool ENABLE_DOUBLED_KINEMATIC = true;

        public static bool WELD_LANDING_GEARS = true;

        public static bool ENABLE_PLANET_FROZEN_SEA = false;

        public static bool ENFORCE_CONTROLLER = false;

        public static bool ENABLE_ALL_IN_SURVIVAL = false;

        public static bool ENABLE_SURVIVAL_SWITCHING = false;

		public static bool ENABLE_ATMOSPHERIC_ENTRYEFFECT = false;
		public static bool ENABLE_DRIVING_PARTICLES = false;

        public static bool ENABLE_BLOCKS_IN_VOXELS_TEST = false;

        public static bool USE_BOX_FOR_PLANET = false;
        public static bool USE_HEIGHT_MATERIALS_PLANET = false;

        public static bool ENABLE_TURRET_LASERS = false;

        public static bool SKIP_BIOME_MAP = false;
        public static bool SKIP_ENVIRONMENT_ITEM_RULES = false;
        public static bool ENABLE_DEFINITION_ENVIRONMENTS = true;
        public static bool ENABLE_VOXEL_ENVIRONEMNT_ITEMS = true;

        public static bool PRIORITIZE_PRECALC_JOBS = true;
        public static bool DISABLE_COMPOSITE_MATERIAL = false;
		public static bool ENABLE_PLANETARY_CLOUDS = true;
        public static bool ENABLE_CLOUD_FOG = false;
        public static bool ENABLE_ENLARGING_EVENTS = false;
        public static bool ENVIRONMENT_ITEMS_ONE_INSTANCEBUFFER = false;
        public static bool CLIENTS_SIMULATE_SINGLE_WORLD = false;
        public static bool ENABLE_PLANET_SURFACE_INTERPOLATION = true;
        public static bool ENABLE_PLANET_OCCLUSION_MAP = true;

        public static bool ENABLE_FRACTURE_COMPONENT = false;
        public static bool TESTING_VEHICLES = false;
	    public static bool ENABLE_WALKING_PARTICLES = true;
        public const bool UNRELIABLE_POSITION_SYNC = false;

        public static bool ENABLE_HYDROGEN_FUEL = true;
        public static bool WELD_PISTONS = true;
        public static bool WELD_ROTORS = true;
        public static bool ENABLE_INFINARIO = false;
        public static bool SUSPENSION_POWER_RATIO = false;
        public static bool WHEEL_SOFTNESS = false;
        public static bool USE_BICUBIC_HEIGHTMAP_SMOOTHING = true;
        public static bool FORCE_SINGLE_WORKER = false;
        public static bool DISABLE_CLIPBOARD_PLACEMENT_TEST = false;
        public static bool ENABLE_LIMITED_CHARACTER_BODY = false;
        public static bool ENABLE_VOXEL_COMPUTED_OCCLUSION = false;
        public static bool ENABLE_SPLIT_VOXEL_READ_QUERIES = false;
        public static bool ENABLE_COMPOUND_BLOCK_COLLISION_DUMMIES = false;

        public static bool ENABLE_MULTIPLAYER_ENTITY_SUPPORT = true;
        public static bool ENABLE_EXTENDED_PLANET_OPTIONS = false;

        public static bool ENABLE_JOIN_STARTED_BATTLE = false;
        public static bool ENABLE_JOIN_SCREEN_REMAINING_TIME = false;
        public static bool ENABLE_INVENTORY_FIX = true;
        public static bool ENABLE_VOXEL_LOD_MORPHING = true;
        public static bool ENABLE_LAZY_VOXEL_PHYSICS = true;
        public static bool ENABLE_PLANET_HIERARCHY = true;

        public static bool ENABLE_FLORA_COMPONENT_DEBUG = false;

        // Coord sys
        public static bool ENABLE_DEBUG_DRAW_COORD_SYS = false;

        public static bool SKIP_PISTON_TOP_REMOVAL = true;
        public static bool GRID_IGNORE_VOXEL_OVERLAP = false;
        public static bool COMPENSATE_SPEED_WITH_SUPPORT = false;

        public static bool ENABLE_FRACTURE_PIECE_SHAPE_CHECK = false;

        public static bool ENABLE_PLANET_FIREFLIES = true;

        public static bool ENABLE_XMAS15_CONTENT = true;
        
        public static bool ENABLE_DURABILITY_DEBUG = false;
        
        public static bool ENABLE_DURABILITY_COMPONENT = true;

        public static bool SPAWN_SPACE_FAUNA_IN_CREATIVE = true; // space fauna spawns by default only in survival game, this flag can change it to spawn in creative mode too

        public static bool ENABLE_INVENTORY_SIGHT_CHECK = false;

        public static bool DISABLE_MANIPULATION_TOOL_HOLD_VOXEL_CONTACT = false;

        public static bool ENABLE_MEDIEVAL_FACTIONS = false;

#if XB1
        public static bool ENABLE_RUN_WITHOUT_STEAM = true;
#else
        public static bool ENABLE_RUN_WITHOUT_STEAM = false;
#endif

        public static bool PRECISE_SIM_SPEED = false;
        public static bool ENABLE_SIMSPEED_LOCKING = false;

        public static bool BACKGROUND_OXYGEN = true;

        public static bool ENABLE_GATHERING_SMALL_BLOCK_FROM_GRID = false;

        public static bool ENABLE_COMPONENT_BLOCKS = true;
        public static bool ENABLE_SMALL_GRID_BLOCK_INFO = true;
        public static bool ENABLE_SMALL_GRID_BLOCK_COMPONENT_INFO = true;

        public static bool ENABLE_MEDIEVAL_AREA_INVENTORY = false;
        public static bool ENABLE_MEDIEVAL_CREATIVE_OWNERSHIP = true;

        public static bool ENABLE_BOUNDINGBOX_SHRINKING = true;

        public static bool ENABLE_HUD_PICKED_UP_ITEMS = false;
        public static bool USE_NEW_ENVIRONMENT_SECTORS = true;

        public static bool ENABLE_SENT_GROUP_AT_ONCE = false;

        public static bool ENABLE_QUICKLAUNCH_SKIP_MAIN_MENU = false;

        public static bool ENABLE_REGROWTH_EVENT = true;

        public static bool DISABLE_VOXEL_PHYSICS = false;

        public static bool ENABLE_VR_DRONE_COLLISIONS = false;
        public static bool ENABLE_VR_BLOCK_DEFORMATION_RATIO = false;
        public static bool ENABLE_VR_REMOTE_BLOCK_AUTOPILOT_SPEED_LIMIT = false;
        // Enable damage for some blocks even when grid is not destructible
        public static bool ENABLE_VR_FORCE_BLOCK_DESTRUCTIBLE = false;
        public static bool ENABLE_VR_REMOTE_CONTROL_WAYPOINTS_FAST_MOVEMENT = false;
        public static bool ENABLE_VR_BUILDING = false;

        public static bool ENABLE_LOAD_NEEDED_SESSION_COMPONENTS = false;
        public static bool ENABLE_SMALL_GRIDS_IN_SURVIVAL_TOOLBAR_CONFIG = true;

        public static bool ENABLE_CHARACTER_CONTROL_ON_SERVER = true;
        public static bool ENABLE_SHIP_CONTROL_ON_SERVER = true;

        public static bool ENABLE_SEPARATE_USE_AND_PICK_UP_KEY = false;

        public static bool ENABLE_USE_DEFAULT_DAMAGE_DECAL = false;

        public static bool ENABLE_QUICK_WARDROBE = false;
        public static bool ENABLE_TYPES_FROM_MODS = false;

        public static bool ENABLE_PRELOAD_DEFINITIONS = true;
        public static bool ENABLE_ME_DOOR_COLLISION_CHECK = true;

#if XB1
        public static bool XB1_PREVIEW = true;
#else // !XB1
        public static bool XB1_PREVIEW = false;
#endif // !XB1 

        public static bool ENABLE_ROSLYN_SCRIPTS = true;

        public static bool ENABLE_ROSLYN_SCRIPT_DIAGNOSTICS = false;      
       
    }
}
