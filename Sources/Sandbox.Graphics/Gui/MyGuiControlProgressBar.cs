using VRage.Game;
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
        public float Value 
        { 
            get { return m_value; } 
            set 
            {
                System.Diagnostics.Debug.Assert(!float.IsNaN(value), "Passing NaN value!");
                m_value = MathHelper.Clamp(value, 0.0f, 1.0f); 
            }
        }

		public bool IsHorizontal = true;

        public bool EnableBorderAutohide = false;
        public float BorderAutohideThreshold = 0.01f;

		MyGuiControlPanel m_potentialBar;
		public MyGuiControlPanel PotentialBar { get { return m_potentialBar; } }

		private MyGuiControlPanel m_progressForeground;
		public MyGuiControlPanel ForegroundBar { get { return m_progressForeground; } }
        
		private MyGuiControlPanel m_progressBarLine;
		public MyGuiControlPanel ForegroundBarEndLine { get { return m_progressBarLine; } }

        #region Static default values
        private static readonly Color DEFAULT_PROGRESS_COLOR = Color.White;
        #endregion

		public MyGuiControlProgressBar(	Vector2? position = null,
										Vector2? size = null,
										Color? progressBarColor = null,
										MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
										MyGuiCompositeTexture backgroundTexture = null,
										bool isHorizontal = true,
										bool potentialBarEnabled = true,
                                        bool enableBorderAutohide = false,
                                        float borderAutohideThreshold = 0.01f)
            : base( position: position,
                    size: size,
					backgroundTexture: backgroundTexture,
					originAlign: originAlign,
                    colorMask: null,
                    toolTip: null)
        {
            ProgressColor = (progressBarColor.HasValue ? progressBarColor.Value : DEFAULT_PROGRESS_COLOR);
			IsHorizontal = isHorizontal;
            EnableBorderAutohide = enableBorderAutohide;
            BorderAutohideThreshold = borderAutohideThreshold;

			m_progressForeground = new MyGuiControlPanel(	position: new Vector2(-Size.X/2.0f, 0.0f),
															originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
															backgroundColor: ProgressColor);
			m_progressForeground.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;

			m_potentialBar = new MyGuiControlPanel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
													size: new Vector2(0f, Size.Y));
			m_potentialBar.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;
			m_potentialBar.ColorMask = new Vector4(ProgressColor, 0.7f);
			m_potentialBar.Visible = false;
			m_potentialBar.Enabled = potentialBarEnabled;

			Elements.Add(m_potentialBar);
			Elements.Add(m_progressForeground);
        }

		public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
		{
			var paddedSize = Size;
			var progressFillSize = paddedSize * new Vector2((IsHorizontal ? Value : 1.0f), (IsHorizontal ? 1.0f : Value));
			m_progressForeground.Size = progressFillSize;

            if (EnableBorderAutohide && Value <= BorderAutohideThreshold)
            {
                m_progressForeground.BorderEnabled = false;
            }
            else
            {
                m_progressForeground.BorderEnabled = true;
            }

			base.Draw(transitionAlpha, backgroundTransitionAlpha);
		}

		public Vector2 CalculatePotentialBarPosition()
		{
			return new Vector2(m_progressForeground.Position.X + m_progressForeground.Size.X, 0f);
		}
    }
}
