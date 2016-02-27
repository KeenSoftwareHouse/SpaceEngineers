#region Using

using System;
using System.Text;
using VRage.ModAPI;
using VRageMath;


#endregion

namespace VRage.Game.Gui
{
    public struct MyHudEntityParams
    {
        public IMyEntity Entity { get; set; }
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
        public float BlinkingTime { get; set; }

        /// <summary>
        /// Function that checks whether indicator should be drawn.
        /// Useful when reacting to some player settings.
        /// </summary>
        public Func<bool> ShouldDraw { get; set; }

        public MyHudEntityParams(StringBuilder text, MyRelationsBetweenPlayerAndBlock targetMode, 
            float maxDistance, MyHudIndicatorFlagsEnum flagsEnum) : this()
        {
            this.Text = text;
            this.FlagsEnum = flagsEnum;
            this.MaxDistance = maxDistance;
            this.TargetMode = targetMode;
        }
    }
}
