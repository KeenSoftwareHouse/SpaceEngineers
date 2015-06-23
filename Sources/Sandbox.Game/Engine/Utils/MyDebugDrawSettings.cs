using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Graphics.GUI;
using System;
using VRage.Utils;

namespace Sandbox.Engine.Utils
{
    public static class MyDebugDrawSettings
    {
        //  If true, then debug draw rendering is enabled
        public static bool ENABLE_DEBUG_DRAW = false;
        public static bool ENABLE_DX11_RENDERER = false;

        // These debug draw constants can be set via the in-game debug menu
        public static bool DEBUG_DRAW_ENTITY_IDS = false;
        public static bool DEBUG_DRAW_ENTITY_IDS_ONLY_ROOT = true;
        public static bool DEBUG_DRAW_BLOCK_NAMES = false;
        public static bool DEBUG_DRAW_AUDIO = false;
        public static bool DEBUG_DRAW_COLLISION_PRIMITIVES = false;
        public static bool DEBUG_DRAW_MOUNT_POINTS = false;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AXIS_HELPERS = false;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AUTOGENERATE = false;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AXIS0 = true;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AXIS1 = true;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AXIS2 = true;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AXIS3 = true;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AXIS4 = true;
        public static bool DEBUG_DRAW_MOUNT_POINTS_AXIS5 = true;
        public static bool DEBUG_DRAW_MOUNT_POINTS_ALL = false;
        public static bool DEBUG_DRAW_MODEL_DUMMIES = false;
        public static bool DEBUG_DRAW_GAME_PRUNNING = false;
        public static bool DEBUG_DRAW_RADIO_BROADCASTERS = false;
        public static bool DEBUG_DRAW_STOCKPILE_QUANTITIES = false;
        public static bool DEBUG_DRAW_SUIT_BATTERY_CAPACITY = false;
        public static bool DEBUG_DRAW_CHARACTER_BONES = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_ANKLE_FINALPOS = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_SETTINGS = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_RAYCASTLINE = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_BONES = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_RAYCASTHITS = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_ANKLE_DESIREDPOSITION = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_CLOSESTSUPPORTPOSITION = false;
        public static bool DEBUG_DRAW_CHARACTER_IK_IKSOLVERS = false;
        public static MyCharacterMovementEnum DEBUG_DRAW_CHARACTER_IK_MOVEMENT_STATE = MyCharacterMovementEnum.Standing;
        public static bool DEBUG_DRAW_CHARACTER_RAGDOLL_ORIGINAL_RIG = false;
        public static bool DEBUG_DRAW_CHARACTER_RAGDOLL_POSE = false;
        public static bool DEBUG_DRAW_CHARACTER_RAGDOLL_COMPUTED_BONES = false;
        public static bool DEBUG_DRAW_CHARACTER_RAGDOLL_HIPPOSITIONS = false;
        public static bool DEBUG_DRAW_NEUTRAL_SHIPS = false;
        public static bool DEBUG_DRAW_DISPLACED_BONES = false;
        public static bool DEBUG_DRAW_CUBE_BLOCK_AABBS = false;
        public static bool DEBUG_DRAW_CHARACTER_MISC = false;
        public static bool DEBUG_DRAW_EVENTS = false;
        public static bool DEBUG_DRAW_POWER_RECEIVERS = false;
        public static bool DEBUG_DRAW_COCKPIT = false;
        public static bool DEBUG_DRAW_CONVEYORS = false;
        public static bool DEBUG_DRAW_CUBES = false;
        public static bool DEBUG_DRAW_TRIANGLE_PHYSICS = false;
        public static bool DEBUG_DRAW_GRID_GROUPS_PHYSICAL = false;
        public static bool DEBUG_DRAW_GRID_GROUPS_LOGICAL = false;
        public static bool DEBUG_DRAW_STRUCTURAL_INTEGRITY = false;
        public static bool DEBUG_DRAW_VOLUMETRIC_EXPLOSION_COLORING = false;
        public static bool DEBUG_DRAW_CONVEYORS_LINE_IDS = false;
        public static bool DEBUG_DRAW_CONVEYORS_LINE_CAPSULES = false;
        public static bool DEBUG_DRAW_SHIP_TOOLS = false;
        public static bool DEBUG_DRAW_REMOVE_CUBE_COORDS = false;
        public static bool DEBUG_DRAW_TRASH_REMOVAL = false;
        public static bool DEBUG_DRAW_GRID_COUNTER = false;
        public static bool DEBUG_DRAW_GRID_NAMES = false;
        public static bool DEBUG_DRAW_GRID_CONTROL = false;
        public static bool DEBUG_DRAW_GRID_TERMINAL_SYSTEMS = false;
        public static bool DEBUG_DRAW_CONNECTORS_AND_MERGE_BLOCKS = false;
        public static bool DEBUG_DRAW_COPY_PASTE = false;
        public static bool DEBUG_DRAW_GRID_ORIGINS = false;
        public static bool DEBUG_DRAW_THRUSTER_DAMAGE = false;
        public static bool DEBUG_DRAW_BLOCK_GROUPS = false;
        public static bool DEBUG_DRAW_ROTORS = false;
        public static bool DEBUG_DRAW_GYROS = false;
        public static bool DEBUG_DRAW_VOXEL_GEOMETRY_CELL = false;
        public static bool DEBUG_DRAW_VOXEL_MAP_AABB = false;
        public static bool DEBUG_DRAW_RESPAWN_SHIP_COUNTERS = false;
        public static bool DEBUG_DRAW_EXPLOSION_HAVOK_RAYCASTS = false;
        public static bool DEBUG_DRAW_EXPLOSION_DDA_RAYCASTS = false;
        public static bool DEBUG_DRAW_CONTROLLED_ENTITIES = false;
        public static bool DEBUG_DRAW_PHYSICS_CLUSTERS = false;
        public static bool DEBUG_DRAW_GRID_DIRTY_BLOCKS = false;
        public static bool DEBUG_DRAW_MERGED_GRIDS = false;
        public static bool DEBUG_DRAW_VOXEL_PHYSICS_PREDICTION = false;
        public static bool DEBUG_DRAW_VOXEL_MAP_BOUNDING_BOX = false;
        public static bool DEBUG_DRAW_FRACTURED_PIECES = false;
        public static bool DEBUG_DRAW_ENVIRONMENT_ITEMS = false;
        public static bool DEBUG_DRAW_SMALL_TO_LARGE_BLOCK_GROUPS = false;
        public static bool DEBUG_DRAW_ROPES = false;
        public static bool DEBUG_DRAW_BOTS = false;
        public static bool DEBUG_DRAW_OXYGEN = false;
        public static bool DEBUG_DRAW_ANIMALS = false;
        public static bool DEBUG_DRAW_VOICE_CHAT = false;
        public static bool DEBUG_DRAW_FLORA = false;
        public static bool DEBUG_DRAW_FLORA_SPAWN_INFO = false;
        public static bool DEBUG_DRAW_FLORA_REGROW_INFO = false;
        public static bool DEBUG_DRAW_FLORA_BOXES = false;
        public static bool DEBUG_DRAW_FLORA_SPAWNED_ITEMS = false;

        public static MyWEMDebugDrawMode DEBUG_DRAW_NAVMESHES = MyWEMDebugDrawMode.NONE;
        internal static MyVoxelDebugDrawMode DEBUG_DRAW_VOXELS_MODE = MyVoxelDebugDrawMode.None;

        // Red - server data, Yellow - client data, Orange - extrapolated server data, Blue - hard set server data
        public static bool DEBUG_DRAW_INTERPOLATION = false;

        // Put various debug draw here. If this cathegory becomes cluttered, create a separate one.
        public static bool DEBUG_DRAW_MISCELLANEOUS = false;

        //Destruction
        public static bool BREAKABLE_SHAPE_CHILD_COUNT = false;
        public static bool BREAKABLE_SHAPE_CONNECTIONS = false;

        public static bool DEBUG_DRAW_SHOW_DAMAGE = false;
        public static bool DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_ORIGINAL_RIG = false;
        public static bool DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_DESIRED = false;
        public static bool DEBUG_DRAW_BLOCK_INTEGRITY = false;
        public static bool DEBUG_DRAW_FIXED_BLOCK_QUERIES = false;
        public static bool DEBUG_DRAW_DRILLS = false;
    }
}
