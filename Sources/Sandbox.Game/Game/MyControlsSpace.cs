using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game
{
    public static class MyControlsSpace
    {
        public static readonly MyStringId FORWARD = MyStringId.GetOrCompute("FORWARD");
        public static readonly MyStringId BACKWARD = MyStringId.GetOrCompute("BACKWARD");
        public static readonly MyStringId STRAFE_LEFT = MyStringId.GetOrCompute("STRAFE_LEFT");
        public static readonly MyStringId STRAFE_RIGHT = MyStringId.GetOrCompute("STRAFE_RIGHT");
        public static readonly MyStringId ROLL_LEFT = MyStringId.GetOrCompute("ROLL_LEFT");
        public static readonly MyStringId ROLL_RIGHT = MyStringId.GetOrCompute("ROLL_RIGHT");
        public static readonly MyStringId SPRINT = MyStringId.GetOrCompute("SPRINT");
        public static readonly MyStringId PRIMARY_TOOL_ACTION = MyStringId.GetOrCompute("PRIMARY_TOOL_ACTION");
        public static readonly MyStringId SECONDARY_TOOL_ACTION = MyStringId.GetOrCompute("SECONDARY_TOOL_ACTION"); //rmb
        public static readonly MyStringId JUMP = MyStringId.GetOrCompute("JUMP"); // move up, jump
        public static readonly MyStringId CROUCH = MyStringId.GetOrCompute("CROUCH"); // move down, crouch
        public static readonly MyStringId SWITCH_WALK = MyStringId.GetOrCompute("SWITCH_WALK");
        public static readonly MyStringId USE = MyStringId.GetOrCompute("USE"); // interact
        public static readonly MyStringId PICK_UP = MyStringId.GetOrCompute("PICK_UP"); // pick into inventory
        public static readonly MyStringId TERMINAL = MyStringId.GetOrCompute("TERMINAL");
        public static readonly MyStringId HELP_SCREEN = MyStringId.GetOrCompute("HELP_SCREEN");
        public static readonly MyStringId CONTROL_MENU = MyStringId.GetOrCompute("CONTROL_MENU");
        public static readonly MyStringId FACTIONS_MENU = MyStringId.GetOrCompute("FACTIONS_MENU");

        //Advanced controls
        public static readonly MyStringId ROTATION_LEFT = MyStringId.GetOrCompute("ROTATION_LEFT");
        public static readonly MyStringId ROTATION_RIGHT = MyStringId.GetOrCompute("ROTATION_RIGHT");
        public static readonly MyStringId ROTATION_UP = MyStringId.GetOrCompute("ROTATION_UP");
        public static readonly MyStringId ROTATION_DOWN = MyStringId.GetOrCompute("ROTATION_DOWN");
        public static readonly MyStringId HEADLIGHTS = MyStringId.GetOrCompute("HEADLIGHTS");
        public static readonly MyStringId SCREENSHOT = MyStringId.GetOrCompute("SCREENSHOT");
        public static readonly MyStringId LOOKAROUND = MyStringId.GetOrCompute("LOOKAROUND"); // looking inside cockpit
        public static readonly MyStringId TOGGLE_SIGNALS = MyStringId.GetOrCompute("TOGGLE_SIGNALS"); // Toggling signals render mode on/off
        public static readonly MyStringId SWITCH_LEFT = MyStringId.GetOrCompute("SWITCH_LEFT"); // Previous Color. Default key '['.
        public static readonly MyStringId SWITCH_RIGHT = MyStringId.GetOrCompute("SWITCH_RIGHT"); // Next Color. Default key ']'.
        public static readonly MyStringId CUBE_COLOR_CHANGE = MyStringId.GetOrCompute("CUBE_COLOR_CHANGE");
        public static readonly MyStringId TOGGLE_REACTORS = MyStringId.GetOrCompute("TOGGLE_REACTORS");

        // Building controls
        public static readonly MyStringId BUILD_SCREEN = MyStringId.GetOrCompute("BUILD_SCREEN"); // G key
        public static readonly MyStringId CUBE_ROTATE_VERTICAL_POSITIVE = MyStringId.GetOrCompute("CUBE_ROTATE_VERTICAL_POSITIVE");
        public static readonly MyStringId CUBE_ROTATE_VERTICAL_NEGATIVE = MyStringId.GetOrCompute("CUBE_ROTATE_VERTICAL_NEGATIVE");
        public static readonly MyStringId CUBE_ROTATE_HORISONTAL_POSITIVE = MyStringId.GetOrCompute("CUBE_ROTATE_HORISONTAL_POSITIVE");
        public static readonly MyStringId CUBE_ROTATE_HORISONTAL_NEGATIVE = MyStringId.GetOrCompute("CUBE_ROTATE_HORISONTAL_NEGATIVE");
        public static readonly MyStringId CUBE_ROTATE_ROLL_POSITIVE = MyStringId.GetOrCompute("CUBE_ROTATE_ROLL_POSITIVE");
        public static readonly MyStringId CUBE_ROTATE_ROLL_NEGATIVE = MyStringId.GetOrCompute("CUBE_ROTATE_ROLL_NEGATIVE");
        public static readonly MyStringId SYMMETRY_SWITCH = MyStringId.GetOrCompute("SYMMETRY_SWITCH");
        public static readonly MyStringId USE_SYMMETRY = MyStringId.GetOrCompute("USE_SYMMETRY");
        public static readonly MyStringId SWITCH_COMPOUND = MyStringId.GetOrCompute("SWITCH_COMPOUND");
        public static readonly MyStringId SWITCH_BUILDING_MODE = MyStringId.GetOrCompute("SWITCH_BUILDING_MODE");
        public static readonly MyStringId VOXEL_HAND_SETTINGS = MyStringId.GetOrCompute("VOXEL_HAND_SETTINGS");
        public static readonly MyStringId MISSION_SETTINGS = MyStringId.GetOrCompute("MISSION_SETTINGS");
        public static readonly MyStringId COCKPIT_BUILD_MODE = MyStringId.GetOrCompute("COCKPIT_BUILD_MODE");
        public static readonly MyStringId CUBE_BUILDER_CUBESIZE_MODE = MyStringId.GetOrCompute("CUBE_BUILDER_CUBESIZE_MODE");
        public static readonly MyStringId CUBE_DEFAULT_MOUNTPOINT = MyStringId.GetOrCompute("CUBE_DEFAULT_MOUNTPOINT"); // T key

        // Weapon selection slots
        public static readonly MyStringId SLOT1 = MyStringId.GetOrCompute("SLOT1");
        public static readonly MyStringId SLOT2 = MyStringId.GetOrCompute("SLOT2");
        public static readonly MyStringId SLOT3 = MyStringId.GetOrCompute("SLOT3");
        public static readonly MyStringId SLOT4 = MyStringId.GetOrCompute("SLOT4");
        public static readonly MyStringId SLOT5 = MyStringId.GetOrCompute("SLOT5");
        public static readonly MyStringId SLOT6 = MyStringId.GetOrCompute("SLOT6");
        public static readonly MyStringId SLOT7 = MyStringId.GetOrCompute("SLOT7");
        public static readonly MyStringId SLOT8 = MyStringId.GetOrCompute("SLOT8");
        public static readonly MyStringId SLOT9 = MyStringId.GetOrCompute("SLOT9");
        public static readonly MyStringId SLOT0 = MyStringId.GetOrCompute("SLOT0");

        public static readonly MyStringId TOOLBAR_UP = MyStringId.GetOrCompute("TOOLBAR_UP");
        public static readonly MyStringId TOOLBAR_DOWN = MyStringId.GetOrCompute("TOOLBAR_DOWN");
        public static readonly MyStringId TOOLBAR_NEXT_ITEM = MyStringId.GetOrCompute("TOOLBAR_NEXT_ITEM");
        public static readonly MyStringId TOOLBAR_PREV_ITEM = MyStringId.GetOrCompute("TOOLBAR_PREV_ITEM");
        public static readonly MyStringId TOGGLE_HUD = MyStringId.GetOrCompute("TOGGLE_HUD");
        public static readonly MyStringId DAMPING = MyStringId.GetOrCompute("DAMPING");
        public static readonly MyStringId THRUSTS = MyStringId.GetOrCompute("THRUSTS");
        public static readonly MyStringId CAMERA_MODE = MyStringId.GetOrCompute("CAMERA_MODE");
        public static readonly MyStringId BROADCASTING = MyStringId.GetOrCompute("BROADCASTING");
        public static readonly MyStringId HELMET = MyStringId.GetOrCompute("HELMET");
        public static readonly MyStringId CHAT_SCREEN = MyStringId.GetOrCompute("CHAT_SCREEN");
        public static readonly MyStringId CONSOLE = MyStringId.GetOrCompute("CONSOLE");
        public static readonly MyStringId SUICIDE = MyStringId.GetOrCompute("SUICIDE");
        public static readonly MyStringId LANDING_GEAR = MyStringId.GetOrCompute("LANDING_GEAR");
        public static readonly MyStringId INVENTORY = MyStringId.GetOrCompute("INVENTORY");
        public static readonly MyStringId PAUSE_GAME = MyStringId.GetOrCompute("PAUSE_GAME");
        public static readonly MyStringId SPECTATOR_NONE = MyStringId.GetOrCompute("SPECTATOR_NONE");
        public static readonly MyStringId SPECTATOR_DELTA = MyStringId.GetOrCompute("SPECTATOR_DELTA");
        public static readonly MyStringId SPECTATOR_FREE = MyStringId.GetOrCompute("SPECTATOR_FREE");
        public static readonly MyStringId SPECTATOR_STATIC = MyStringId.GetOrCompute("SPECTATOR_STATIC");
        public static readonly MyStringId FREE_ROTATION = MyStringId.GetOrCompute("FREE_ROTATION");
        public static readonly MyStringId VOICE_CHAT = MyStringId.GetOrCompute("VOICE_CHAT");

        // NOT BINDABLE
        public static readonly MyStringId VOXEL_PAINT = MyStringId.GetOrCompute("VOXEL_PAINT");
        public static readonly MyStringId BUILD_MODE = MyStringId.GetOrCompute("BUILD_MODE");
        public static readonly MyStringId NEXT_BLOCK_STAGE = MyStringId.GetOrCompute("NEXT_BLOCK_STAGE");
        public static readonly MyStringId PREV_BLOCK_STAGE = MyStringId.GetOrCompute("PREV_BLOCK_STAGE");
        public static readonly MyStringId MOVE_CLOSER = MyStringId.GetOrCompute("MOVE_CLOSER");
        public static readonly MyStringId MOVE_FURTHER = MyStringId.GetOrCompute("MOVE_FURTHER");
        public static readonly MyStringId PRIMARY_BUILD_ACTION = MyStringId.GetOrCompute("PRIMARY_BUILD_ACTION");
        public static readonly MyStringId SECONDARY_BUILD_ACTION = MyStringId.GetOrCompute("SECONDARY_BUILD_ACTION");
        public static readonly MyStringId COPY_PASTE_ACTION = MyStringId.GetOrCompute("COPY_PASTE_ACTION");
    }
}
