#region Using

using System;

#endregion

namespace VRage.Game.Gui
{
    //  This enums must have same name as source texture files used to create texture atlas (only ".tga" files are supported)
    //  IMPORTANT: If you change order or names in this enum, update it also in MyEnumsToStrings
    public enum MyHudTexturesEnum : byte
    {
        corner,
        crosshair,
        HudOre,
        Target_enemy,
        Target_friend,
        Target_neutral,
        Target_me,
        TargetTurret,
        DirectionIndicator,
        gravity_point_red,
        gravity_point_white,
        gravity_arrow,
        hit_confirmation,
    }

    [Flags]
    public enum MyHudIndicatorFlagsEnum
    {
        NONE = 0,

        SHOW_TEXT                      = 1 << 0,
        SHOW_BORDER_INDICATORS         = 1 << 1,
//        SHOW_HEALTH_BARS               = 1 << 2,
        //SHOW_ONLY_IF_DETECTED_BY_RADAR = 1 << 3,
        SHOW_DISTANCE                  = 1 << 4,
        ALPHA_CORRECTION_BY_DISTANCE   = 1 << 5,
//        SHOW_MISSION_MARKER            = 1 << 6,
//        SHOW_FACTION_RELATION_MARKER   = 1 << 7,
//        SHOW_LOCKED_TARGET             = 1 << 8,
//        SHOW_LOCKED_SIDE_TARGET        = 1 << 9,
        SHOW_ICON                      = 1 << 10,
        SHOW_FOCUS_MARK                = 1 << 11,

        SHOW_ALL = ~0,

    }

    [Flags]
    enum MyHudDrawElementEnum
    {
        NONE = 0,

        DIRECTION_INDICATORS   = 1 << 0,
        CROSSHAIR              = 1 << 1,
        DAMAGE_INDICATORS      = 1 << 2,
        AMMO                   = 1 << 3,
        HARVEST_MATERIAL       = 1 << 4,
        BARGRAPHS_PLAYER_SHIP  = 1 << 6,
        BARGRAPHS_LARGE_WEAPON = 1 << 7,
        DIALOGUES              = 1 << 8,
        MISSION_OBJECTIVES     = 1 << 9,
        BACK_CAMERA            = 1 << 10,
        WHEEL_CONTROL          = 1 << 11,
        CROSSHAIR_DYNAMIC      = 1 << 12,
    }

}
