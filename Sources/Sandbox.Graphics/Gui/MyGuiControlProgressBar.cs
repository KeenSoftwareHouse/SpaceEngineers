using Sandbox.Common.ObjectBuilders.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlProgressBar))]
    class MyGuiControlProgressBar : MyGuiControlBase
    {
        /// <summary>
        /// Color of the progress bar.
        /// </summary>
        public Color ProgressColor;

        /// <summary>
        /// Value in specifying progress percentage in range from 0 to 1.
        /// </summary>
        public float Value = 1;

        #region Static default values
        private static readonly Color DEFAULT_PROGRESS_COLOR = Color.White;
        #endregion

        public MyGuiControlProgressBar(Vector2? position = null)
            : base( position: position,
                    size: null,
                    colorMask: null,
                    toolTip: null)
        {
            ProgressColor = DEFAULT_PROGRESS_COLOR;
        }

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            var ob = objectBuilder as MyObjectBuilder_GuiControlProgressBar;
            Debug.Assert(ob != null);

            MyGuiControlBase.ReadIfHasValue(ref ProgressColor, ob.ProgressColor);
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_GuiControlProgressBar;
            Debug.Assert(ob != null);

            ob.ProgressColor = ProgressColor.ToVector4();

            return ob;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            // draw progress bar
            var progressFillSize = Size * (new Vector2(Value, 1.0f));
            var color = ApplyColorMaskModifiers(ColorMask * ProgressColor.ToVector4(), Enabled, transitionAlpha);
            MyGuiManager.DrawSpriteBatch(MyGuiConstants.PROGRESS_BAR, GetPositionAbsoluteTopLeft(), progressFillSize, color, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
        }
    }
}
