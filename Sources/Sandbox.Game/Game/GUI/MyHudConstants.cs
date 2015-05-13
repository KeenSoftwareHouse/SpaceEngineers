using Sandbox.Common;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public static class MyHudConstants
    {
        public const int MAX_HUD_TEXTS_COUNT = 2000;//300;

        public static readonly Color HUD_COLOR = new Color(180, 180, 180, 180);

        public static readonly Color HUD_COLOR_LIGHT = new Color(255, 255, 255, 255);

        public const float HUD_DIRECTION_INDICATOR_SIZE = 0.006667f;
        public const float HUD_TEXTS_OFFSET = 0.0125f;
        public const float HUD_UNPOWERED_OFFSET = 0.01f;
        public const float DIRECTION_INDICATOR_MAX_SCREEN_DISTANCE = 0.425f;
        public const float DIRECTION_INDICATOR_MAX_SCREEN_TARGETING_DISTANCE = 0.25f;
        public static readonly Vector2 DIRECTION_INDICATOR_SCREEN_CENTER = new Vector2(0.5f, 0.5f);

        public static readonly Color MARKER_COLOR_BLUE = Color.CornflowerBlue;
        public static readonly Color MARKER_COLOR_RED = new Color(1.0f, 0.0f, 0.0f, 1.0f);        
        public static readonly Color MARKER_COLOR_GRAY = new Color(0.8f, 0.8f, 0.8f, 1.0f);       
        public static readonly Color MARKER_COLOR_WHITE = new Color(1.0f, 1.0f, 1.0f, 1.0f);      
        public static readonly Color MARKER_COLOR_GREEN = new Color(0.0f, 0.7f, 0.0f, 1.0f);

        public static readonly Color GPS_COLOR = HUD_COLOR;

        public static readonly Vector4 FRIEND_CUBE_COLOR = new Vector4(0.0f, 0.7f, 0.0f, 1.0f) * 0.2f;   // premultiplied alfa color for bounding cube
        public static readonly Vector4 ENEMY_CUBE_COLOR = new Vector4(0.8f, 0.3f, 0.3f, 1.0f) * 0.2f;    // premultiplied alfa color for bounding cube
        public static readonly Vector4 NEUTRAL_CUBE_COLOR = new Vector4(0.8f, 0.8f, 0.8f, 1.0f) * 0.2f;  // premultiplied alfa color for bounding cube

        public const float PLAYER_MARKER_MULTIPLIER = 0.3f;
        //public static readonly Vector3 ORIGINAL_CAMERA_POSITON_2D_SECOND_DRAW = new Vector3(0, 300f, 300f);
        public const float RADAR_BOUNDING_BOX_SIZE = 3000;
        public const float RADAR_BOUNDING_BOX_SIZE_HALF = RADAR_BOUNDING_BOX_SIZE / 2.0f;
        public const float DIRECTION_TO_SUN_LINE_LENGTH = 100;
        public const float DIRECTION_TO_SUN_LINE_LENGTH_HALF = DIRECTION_TO_SUN_LINE_LENGTH / 0.7f;
        public const float DIRECTION_TO_SUN_LINE_THICKNESS = 0.7f;
        public const float NAVIGATION_MESH_LINE_THICKNESS = 3f;
        public const float NAVIGATION_MESH_DISTANCE = 100;
        public const int NAVIGATION_MESH_LINES_COUNT_HALF = 10;
        public static readonly Color HUD_RADAR_BACKGROUND_COLOR = new Color(HUD_COLOR.ToVector4().X * 0.1f, HUD_COLOR.ToVector4().Y * 0.1f, HUD_COLOR.ToVector4().Z * 0.1f, 0.9f);
        public static readonly Color HUD_RADAR_BACKGROUND_COLOR2D = Color.White;
        public static readonly Vector3 HUD_RADAR_PHYS_OBJECT_POINT_DELTA = new Vector3(0, 0, 1);    //  Every phys object point on radar must be a bit closer to camera so then it's not behind the vertical line
        public const int MIN_RADAR_TYPE_SWITCH_TIME_MILLISECONDS = 500;

        public static Color HUD_STATUS_BACKGROUND_COLOR = new Color(0.482f, 0.635f, 0.643f, 0.35f);
        public static Color HUD_STATUS_DEFAULT_COLOR = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        public static Color HUD_STATUS_BAR_COLOR_GREEN_STATUS = new Color(124, 174, 125, 205);//new Color(57, 113, 73, 255);
        public static Color HUD_STATUS_BAR_COLOR_YELLOW_STATUS = new Color(218, 213, 125, 205);//new Color(218, 213, 125, 255);//new Color(238, 190, 15, 255);
        public static Color HUD_STATUS_BAR_COLOR_ORANGE_STATUS = new Color(236, 163, 97, 205);//new Color(247, 208, 123, 255);//new Color(247, 143, 43, 255);
        public static Color HUD_STATUS_BAR_COLOR_RED_STATUS = new Color(187, 0, 0, 205);//new Color(187, 62, 56, 255);//new Color(187, 30, 35, 255);
        public const float HUD_STATUS_BAR_COLOR_GRADIENT_OFFSET = 2.0f;

        public static Vector2 HUD_STATUS_POSITION = new Vector2(0.02f, -0.02f);
        public static Vector2 HUD_MISSIONS_POSITION = new Vector2(-0.02f, 0.02f);
        //        public static Vector2 HUD_STATUS_SIZE = new Vector2(0.05f, 0.05f);
        public static Vector2 HUD_STATUS_ICON_SIZE = new Vector2(0.012f, 0.01f) * 0.85f;
        public static Vector2 HUD_STATUS_SPEED_ICON_SIZE = new Vector2(0.0145f, 0.0145f);
        //public static Vector2 HUD_STATUS_BAR_SIZE = new Vector2(HUD_STATUS_ICON_SIZE.X / 2.0f, HUD_STATUS_ICON_SIZE.Y / 2.0f);
        public static Vector2 HUD_STATUS_BAR_SIZE = new Vector2(0.0095f, 0.006f);
        public const float HUD_STATUS_ICON_DISTANCE = 0.0075f;//0.012f / 2.0f;
        //public const float HUD_STATUS_BAR_DISTANCE = 0.005f;
        public const float HUD_STATUS_BAR_DISTANCE = 0.0010f;
        public const int HUD_STATUS_BAR_MAX_PIECES_COUNT = 5;

        public const int PREFAB_PREVIEW_SIZE = 128;

        public static readonly Vector2 LOW_FUEL_WARNING_POSITION = new Vector2(0.5f, .65f);

        public const float HUD_MAX_DISTANCE_ENEMIES = 2500f;
        public const float HUD_MAX_DISTANCE_NORMAL = 800f;
        public const float HUD_MAX_DISTANCE_ALPHA = 0.2f;
        public const float HUD_MIN_DISTANCE_ALPHA = 1f;
        public const float HUD_MAX_DISTANCE_TO_ALPHA_CORRECT_NORMAL = HUD_MAX_DISTANCE_NORMAL;
        public const float HUD_MAX_DISTANCE_TO_ALPHA_CORRECT_ENEMIES = HUD_MAX_DISTANCE_ENEMIES;
        public const float HUD_MIN_DISTANCE_TO_ALPHA_CORRECT = 50f;
    }
}
