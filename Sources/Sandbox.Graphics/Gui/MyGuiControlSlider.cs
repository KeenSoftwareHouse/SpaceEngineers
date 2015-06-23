using Sandbox.Common;
using System;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public enum MyGuiControlSliderStyleEnum
    {
        Default,
        //Debug,
        //DebugColorSlider,
        Hue,
    }

    public class MyGuiControlSlider : MyGuiControlBase
    {
        #region Styles
        public class StyleDefinition
        {
            public MyGuiCompositeTexture RailTexture;
            public MyGuiCompositeTexture RailHighlightTexture;
            public MyGuiHighlightTexture ThumbTexture;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlSlider()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlSliderStyleEnum>() + 1];
            m_styles[(int)MyGuiControlSliderStyleEnum.Default] = new StyleDefinition()
            {
                RailTexture          = MyGuiConstants.TEXTURE_SLIDER_RAIL,
                RailHighlightTexture = MyGuiConstants.TEXTURE_SLIDER_RAIL_HIGHLIGHT,
                ThumbTexture         = MyGuiConstants.TEXTURE_SLIDER_THUMB_DEFAULT,
            };

            m_styles[(int)MyGuiControlSliderStyleEnum.Hue] = new StyleDefinition()
            {
                RailTexture = MyGuiConstants.TEXTURE_HUE_SLIDER_RAIL,
                RailHighlightTexture = MyGuiConstants.TEXTURE_HUE_SLIDER_RAIL_HIGHLIGHT,
                ThumbTexture = MyGuiConstants.TEXTURE_HUE_SLIDER_THUMB_DEFAULT,
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlSliderStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion

        public Action<MyGuiControlSlider> ValueChanged;

        public int LabelDecimalPlaces { get; set; }

        float m_minValue;        //  This is min value that can be selected on slider in original units, e.g. meters, so it can be for example 100 meters
        float m_maxValue;        //  This is max value that can be selected on slider in original units, e.g. meters, so it can be for example 10,000 meters
        bool m_controlCaptured = false;

        private string m_thumbTexture;
        private MyGuiControlLabel m_label;
        private MyGuiCompositeTexture m_railTexture;
        private float m_labelSpaceWidth;
        private float m_debugScale = 1f;

        bool m_intValue = false;

        public float? DefaultValue;

        public MyGuiControlSliderStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlSliderStyleEnum m_visualStyle;
        private StyleDefinition m_styleDef;

        /// <summary>
        /// This is values selected on slider in original units, e.g. meters, so it can be for example 1000 meters.
        /// </summary>
        public float Value
        {
            get { return m_value; }
            set
            {
                value = MathHelper.Clamp(value, m_minValue, m_maxValue);
                if (m_value != value)
                {
                    m_value = m_intValue ? (int)Math.Round(value) : value;
                    UpdateLabel();
                    UpdateNormalizedValue();

                    //  Change callback
                    if (ValueChanged != null)
                        ValueChanged(this);
                }
            }
        }
        float m_value;

        /// <summary>
        /// Normalized value selected on slider (range 0, 1).
        /// </summary>
        public float ValueNormalized
        {
            get { return m_valueNormalized; }
            private set { m_valueNormalized = MathHelper.Clamp(value, 0f, 1f); }
        }
        float m_valueNormalized;

        public Func<MyGuiControlSlider, bool> SliderClicked;
        private int m_lastTimeArrowPressed;

        public float DebugScale
        {
            get { return m_debugScale; }
            set
            {
                if (m_debugScale != value)
                {
                    m_debugScale = value;
                    RefreshInternals();
                }
            }
        }

        public MyGuiControlSlider(
            Vector2? position                       = null,
            float minValue                          = 0f,
            float maxValue                          = 1f,
            float width                             = 464f/1600f,
            float? defaultValue                     = null,
            Vector4? color                          = null,
            String labelText                 = null,
            int labelDecimalPlaces                  = 1,
            float labelScale                        = MyGuiConstants.DEFAULT_TEXT_SCALE,
            float labelSpaceWidth                   = 0f,
            MyFontEnum labelFont                    = MyFontEnum.White,
            String toolTip                   = null,
            MyGuiControlSliderStyleEnum visualStyle = MyGuiControlSliderStyleEnum.Default,
            MyGuiDrawAlignEnum originAlign          = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            bool intValue                           = false)
            : base(
                 position: position,
                 toolTip: toolTip,
                 isActiveControl: true,
                 originAlign: originAlign,
                 canHaveFocus: true)
        {
            m_minValue = minValue;
            m_maxValue = maxValue;
            DefaultValue = defaultValue;
            m_value = defaultValue.HasValue ? defaultValue.Value : minValue;
            m_labelSpaceWidth = labelSpaceWidth;
            m_intValue = intValue;

            MyDebug.AssertDebug(m_maxValue > m_minValue && m_maxValue != m_minValue);
            m_label = new MyGuiControlLabel(
                text: labelText ?? String.Empty,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                textScale: labelScale,
                font: labelFont);
            Elements.Add(m_label);

            LabelDecimalPlaces = labelDecimalPlaces;
            VisualStyle = visualStyle;
            Size = new Vector2(width, Size.Y);
            UpdateNormalizedValue();
            UpdateLabel();
        }

        public void SetBounds(float minValue, float maxValue)
        {
            m_minValue = minValue;
            m_maxValue = maxValue;
            Value = Value;
            UpdateNormalizedValue();
        }

        public override void OnRemoving()
        {
            SliderClicked = null;
            base.OnRemoving();
        }

        //  Method returns true if input was captured by control, so no other controls, nor screen can use input in this update
        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase ret = base.HandleInput();
            if (ret != null)
                return ret;

            if (!Enabled)
                return null;

            //float valuePrevious = m_value;

            if (IsMouseOver && MyInput.Static.IsNewPrimaryButtonPressed())
            {
                if (SliderClicked == null || !SliderClicked(this))
                {
                    m_controlCaptured = true;
                }
            }

            if (MyInput.Static.IsNewPrimaryButtonReleased())
            {
                m_controlCaptured = false;
            }

            if (IsMouseOver)
            {
                if (m_controlCaptured)
                {
                    float lineHorizontalPositionStart = GetStart();
                    float lineHorizontalPositionEnd = GetEnd();

                    Value = ((MyGuiManager.MouseCursorPosition.X - lineHorizontalPositionStart) / (lineHorizontalPositionEnd - lineHorizontalPositionStart)) * (m_maxValue - m_minValue) + m_minValue;
                    ret = this;
                }
                else if (MyInput.Static.IsNewSecondaryButtonPressed() && DefaultValue.HasValue)
                {
                    Value = DefaultValue.Value;
                    ret = this;
                }
            }

            if (HasFocus)
            {
                const float MOVEMENT_DELTA_NORMALIZED = 0.001f;

                if ((MyInput.Static.IsKeyPress(MyKeys.Left) || MyInput.Static.IsGamepadKeyLeftPressed()) && (MyGuiManager.TotalTimeInMilliseconds - m_lastTimeArrowPressed > MyGuiConstants.REPEAT_PRESS_DELAY))
                {
                    m_lastTimeArrowPressed = MyGuiManager.TotalTimeInMilliseconds;
                    MoveForward(-MOVEMENT_DELTA_NORMALIZED);
                    ret = this;
                }

                if ((MyInput.Static.IsKeyPress(MyKeys.Right) || MyInput.Static.IsGamepadKeyRightPressed()) && (MyGuiManager.TotalTimeInMilliseconds - m_lastTimeArrowPressed > MyGuiConstants.REPEAT_PRESS_DELAY))
                {
                    m_lastTimeArrowPressed = MyGuiManager.TotalTimeInMilliseconds;
                    MoveForward(+MOVEMENT_DELTA_NORMALIZED);
                    ret = this;
                }
            }

            return ret;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            m_railTexture.Draw(
                GetPositionAbsoluteTopLeft(),
                Size - new Vector2(m_labelSpaceWidth, 0f),
                ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                textureScale: DebugScale);
            DrawThumb(transitionAlpha);
            m_label.Draw(transitionAlpha, backgroundTransitionAlpha);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshInternals();
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            RefreshInternals();
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
        }

        private void DrawThumb(float transitionAlpha)
        {
            Vector2 leftTopCorner = GetPositionAbsoluteTopLeft();

            //  Beging and end of horizontal line
            float lineVerticalPosition = leftTopCorner.Y + Size.Y / 2.0f;
            float lineHorizontalPositionStart = GetStart();
            float lineHorizontalPositionEnd = GetEnd();

            //  Horizontal position of silder's marker/selector
            float lineHorizontalPositionSlider = MathHelper.Lerp(lineHorizontalPositionStart, lineHorizontalPositionEnd, ValueNormalized);

            //  Moving Slider
            MyGuiManager.DrawSpriteBatch(m_thumbTexture,
                new Vector2(lineHorizontalPositionSlider, lineVerticalPosition),
                m_styleDef.ThumbTexture.SizeGui * ((DebugScale != 1f) ? DebugScale*0.5f : DebugScale),
                ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }

        private float GetStart()
        {
            return GetPositionAbsoluteTopLeft().X + MyGuiConstants.SLIDER_INSIDE_OFFSET_X;
        }

        private float GetEnd()
        {
            return GetPositionAbsoluteTopLeft().X + (Size.X - (MyGuiConstants.SLIDER_INSIDE_OFFSET_X + m_labelSpaceWidth));
        }

        private void MoveForward(float movementDelta)
        {
            Value = Value + (m_maxValue - m_minValue) * movementDelta;
        }

        private void UpdateNormalizedValue()
        {
            MyDebug.AssertDebug(m_minValue < m_maxValue);
            MyDebug.AssertDebug(m_value >= m_minValue);
            MyDebug.AssertDebug(m_value <= m_maxValue);
            ValueNormalized = (m_value - m_minValue) / (m_maxValue - m_minValue);
        }

        private void UpdateLabel()
        {
            m_label.UpdateFormatParams(MyValueFormatter.GetFormatedFloat(m_value, LabelDecimalPlaces));
            RefreshInternals();
        }

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            if (HasHighlight)
            {
                m_railTexture = m_styleDef.RailHighlightTexture;
                m_thumbTexture = m_styleDef.ThumbTexture.Highlight;
            }
            else
            {
                m_railTexture = m_styleDef.RailTexture;
                m_thumbTexture = m_styleDef.ThumbTexture.Normal;
            }

            MinSize = new Vector2(m_railTexture.MinSizeGui.X + m_labelSpaceWidth, Math.Max(m_railTexture.MinSizeGui.Y, m_label.Size.Y)) * DebugScale;
            MaxSize = new Vector2(m_railTexture.MaxSizeGui.X + m_labelSpaceWidth, Math.Max(m_railTexture.MaxSizeGui.Y, m_label.Size.Y)) * DebugScale;
            m_label.Position = new Vector2(Size.X * 0.5f, 0f);
        }

        public float MinValue
        {
            get { return m_minValue; }
            set { m_minValue = value; UpdateNormalizedValue(); }
        }

        public float MaxValue
        {
            get { return m_maxValue; }
            set { m_maxValue = value; UpdateNormalizedValue(); }
        }
    }
}
