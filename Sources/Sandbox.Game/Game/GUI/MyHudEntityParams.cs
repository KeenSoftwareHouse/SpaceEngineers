#region Using

using Sandbox.Common;
using Sandbox.Game.Entities;
using System;
using System.Text;
using VRageMath;


#endregion

namespace Sandbox.Game.Gui
{
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

    public struct MyHudEntityParams
    {
        public MyEntity Entity { get; set; }
        public StringBuilder Text { get; set; }
        public bool OffsetText { get; set; }
        public MyHudTexturesEnum? Icon { get; set; }
        public Vector2 IconOffset { get; set; }
        public Vector2 IconSize { get; set; }
        public Color? IconColor { get; set; }
        public MyHudIndicatorFlagsEnum FlagsEnum { get; set; }
        public MyRelationsBetweenPlayerAndBlock TargetMode { get; set; }
        public float MaxDistance { get; set; }
        public bool MustBeDirectlyVisible { get; set; }
        
        public MyEntity Parent { get; set; }
        public Vector3 RelativePosition { get; set; }
        public float BlinkingTime { get; set; }

        public void ResetBlinking()
        {
            BlinkingTime = 0;
        }

        /// <summary>
        /// Function that checks whether indicator should be drawn.
        /// Useful when reacting to some player settings.
        /// </summary>
        public Func<bool> ShouldDraw { get; set; }

        public MyHudEntityParams(StringBuilder text, MyRelationsBetweenPlayerAndBlock targetMode, float maxDistance, MyHudIndicatorFlagsEnum flagsEnum)
            : this()
        {
            this.Text = text;
            this.FlagsEnum = flagsEnum;
            this.MaxDistance = maxDistance;
            this.TargetMode = targetMode;
        }
    }
}
