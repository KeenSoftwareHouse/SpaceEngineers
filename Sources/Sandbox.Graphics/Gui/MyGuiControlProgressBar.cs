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
    public class MyGuiControlProgressBar : MyGuiControlBase
    {
        /// <summary>
        /// Color of the progress bar.
        /// </summary>
        public Color ProgressColor;

        /// <summary>
        /// Value in specifying progress percentage in range from 0 to 1.
        /// </summary>
		private float m_value = 1.0f;
        public float Value { get { return m_value; } set { m_value = MathHelper.Clamp(value, 0.0f, 1.0f); }}

		public bool IsHorizontal = true;

		private MyGuiControlPanel m_progressForeground;

        #region Static default values
        private static readonly Color DEFAULT_PROGRESS_COLOR = Color.White;
        #endregion

		public MyGuiControlProgressBar(	Vector2? position = null, Vector2? size = null, Color? progressBarColor = null,
										MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiCompositeTexture backgroundTexture = null, bool isHorizontal = true)
            : base( position: position,
                    size: size,
					backgroundTexture: backgroundTexture,
					originAlign: originAlign,
                    colorMask: null,
                    toolTip: null)
        {
            ProgressColor = (progressBarColor.HasValue ? progressBarColor.Value : DEFAULT_PROGRESS_COLOR);
			IsHorizontal = isHorizontal;
			var pixelHorizontal = 1.1f/MyGuiManager.GetFullscreenRectangle().Width;
			var pixelVertical = 1.1f / MyGuiManager.GetFullscreenRectangle().Height;
			m_progressForeground = new MyGuiControlPanel(	position: new Vector2(-Size.X/2.0f + pixelHorizontal, 0.0f),
															originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
															backgroundColor: ProgressColor);
			m_progressForeground.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;
			Elements.Add(m_progressForeground);
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
			var pixelHorizontal = 1.1f/MyGuiManager.GetFullscreenRectangle().Width;
			var pixelVertical = 1.1f / MyGuiManager.GetFullscreenRectangle().Height;
			var paddedSize = Size + new Vector2(-2.1f * pixelHorizontal, -2.0f * pixelVertical);
			var progressFillSize = paddedSize * new Vector2((IsHorizontal ? Value : 1.0f), (IsHorizontal ? 1.0f : Value));
			m_progressForeground.Size = progressFillSize;

            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }
    }
}
