using System;
using System.Diagnostics;
using System.Globalization;
using VRage.Game;
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

    /**
     * These settings allow the slider to be customized to any use.
     */
    public class MyGuiSliderProperties
    {
        // Convert ratio [0, 1] to slider value
        public Func<float, float> RatioToValue;

        // Convert slider value to ratio [0, 1]
        public Func<float, float> ValueToRatio;

        // Filter for the ratio (for snapping to values) [0, 1] -> [0, 1]
        public Func<float, float> RatioFilter;

        // Formatter for the value
        public Func<float, string> FormatLabel;

        public static MyGuiSliderProperties Default = new MyGuiSliderProperties()
        {
            ValueToRatio = f => f,
            RatioToValue = f => f,
            RatioFilter = f => f,
            FormatLabel = f => f.ToString(CultureInfo.CurrentCulture)
        };
    }

    public class MyGuiSliderPropertiesExponential : MyGuiSliderProperties
    {
        public MyGuiSliderPropertiesExponential(float min, float max, float exponent = 10, bool integer = false)
        {
            max = (float)(Math.Log(10 * 1000) / Math.Log(exponent));
            min = (float)(Math.Log(100) / Math.Log(exponent));


            FormatLabel = x => string.Format("{0:N0}m", x);
            ValueToRatio = x => (float)((Math.Log(x) / Math.Log(exponent) - min) / (max - min));
            RatioToValue = x =>
            {
                var val = Math.Pow(exponent, x * (max - min) + min);
                return integer ? (int)val : (float)val;
            };
            RatioFilter = x => x;
        }
    }

    public class MyGuiControlSliderBase : MyGuiControlBase
    {
        #region Styles
        public class StyleDefinition
        {
            public MyGuiCompositeTexture RailTexture;
            public MyGuiCompositeTexture RailHighlightTexture;
            public MyGuiHighlightTexture ThumbTexture;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlSliderBase()
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

        public Action<MyGuiControlSliderBase> ValueChanged;

        bool m_controlCaptured = false;

        private string m_thumbTexture;
        private MyGuiControlLabel m_label;
        private MyGuiCompositeTexture m_railTexture;
        private float m_labelSpaceWidth;
        private float m_debugScale = 1f;

        public float? DefaultRatio;

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

        private MyGuiSliderProperties m_props;

        /**
         * Control properties of the slider.
         */
        public MyGuiSliderProperties Propeties
        {
            get { return m_props; }
            set
            {
                m_props = value;

                // trigger all arround refresh
                Ratio = m_ratio;
            }
        }

        float m_ratio;

        /// <summary>
        /// This is values selected on slider in original units, e.g. meters, so it can be for example 1000 meters.
        /// </summary>
        public float Ratio
        {
            get { return m_ratio; }
            set
            {
                value = MathHelper.Clamp(value, 0, 1);
                if (m_ratio != value)
                {
                    m_ratio = m_props.RatioFilter(value);
                    UpdateLabel();

                    //  Change callback
                    OnValueChange();
                }
            }
        }

        protected virtual void OnValueChange()
        {
            if (ValueChanged != null)
                ValueChanged(this);
        }

        public Func<MyGuiControlSliderBase, bool> SliderClicked;
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

        public MyGuiControlSliderBase(
            Vector2? position                       = null,
            float width                             = 464f/1600f,
            MyGuiSliderProperties props                  = null,
            float? defaultRatio                     = null,
            Vector4? color                          = null,
            float labelScale                        = MyGuiConstants.DEFAULT_TEXT_SCALE,
            float labelSpaceWidth                   = 0f,
            string labelFont = MyFontEnum.White,
            String toolTip                   = null,
            MyGuiControlSliderStyleEnum visualStyle = MyGuiControlSliderStyleEnum.Default,
            MyGuiDrawAlignEnum originAlign          = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
            : base(
                 position: position,
                 toolTip: toolTip,
                 isActiveControl: true,
                 originAlign: originAlign,
                 canHaveFocus: true)
        {
            // Make sure the default value makes sense
            if (defaultRatio.HasValue)
            {
                Debug.Assert(defaultRatio.Value >= 0 && defaultRatio.Value <= 1);
                defaultRatio = MathHelper.Clamp(defaultRatio.Value, 0, 1);
            }

            if (props == null)
                props = MyGuiSliderProperties.Default;
            m_props = props;

            DefaultRatio = defaultRatio;
            m_ratio = defaultRatio.HasValue ? defaultRatio.Value : 0;
            m_labelSpaceWidth = labelSpaceWidth;

            m_label = new MyGuiControlLabel(
                text: String.Empty,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                textScale: labelScale,
                font: labelFont);
            Elements.Add(m_label);

            VisualStyle = visualStyle;
            Size = new Vector2(width, Size.Y);
            
            UpdateLabel();
        }

        public override void OnRemoving()
        {
            SliderClicked = null;
            ValueChanged = null;
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
                if (!OnSliderClicked())
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

                    Ratio = ((MyGuiManager.MouseCursorPosition.X - lineHorizontalPositionStart) / (lineHorizontalPositionEnd - lineHorizontalPositionStart));
                    ret = this;
                }
                else if (MyInput.Static.IsNewSecondaryButtonPressed() && DefaultRatio.HasValue)
                {
                    Ratio = DefaultRatio.Value;
                    ret = this;
                }
            }

            if (HasFocus)
            {
                const float MOVEMENT_DELTA_NORMALIZED = 0.001f;

                if ((MyInput.Static.IsKeyPress(MyKeys.Left) || MyInput.Static.IsGamepadKeyLeftPressed()) && (MyGuiManager.TotalTimeInMilliseconds - m_lastTimeArrowPressed > MyGuiConstants.REPEAT_PRESS_DELAY))
                {
                    m_lastTimeArrowPressed = MyGuiManager.TotalTimeInMilliseconds;
                    Ratio -= MOVEMENT_DELTA_NORMALIZED;
                    ret = this;
                }

                if ((MyInput.Static.IsKeyPress(MyKeys.Right) || MyInput.Static.IsGamepadKeyRightPressed()) && (MyGuiManager.TotalTimeInMilliseconds - m_lastTimeArrowPressed > MyGuiConstants.REPEAT_PRESS_DELAY))
                {
                    m_lastTimeArrowPressed = MyGuiManager.TotalTimeInMilliseconds;
                    Ratio += MOVEMENT_DELTA_NORMALIZED;
                    ret = this;
                }
            }

            return ret;
        }

        protected virtual bool OnSliderClicked()
        {
            if (SliderClicked != null)
                return SliderClicked(this);
            return false;
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

        private void DrawThumb(float transitionAlpha)
        {
            Vector2 leftTopCorner = GetPositionAbsoluteTopLeft();

            //  Beging and end of horizontal line
            float lineVerticalPosition = leftTopCorner.Y + Size.Y / 2.0f;
            float lineHorizontalPositionStart = GetStart();
            float lineHorizontalPositionEnd = GetEnd();

            //  Horizontal position of silder's marker/selector
            float lineHorizontalPositionSlider = MathHelper.Lerp(lineHorizontalPositionStart, lineHorizontalPositionEnd, m_ratio);

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

        private void UpdateLabel()
        {
            m_label.Text = m_props.FormatLabel(Value);
            RefreshInternals();
        }

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            if (m_styleDef == null)
                m_styleDef = m_styles[(int) MyGuiControlSliderStyleEnum.Default];

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

        public void ApplyStyle(StyleDefinition style)
        {
            if (style != null)
            {
                m_styleDef = style;
                RefreshInternals();
            }
        }

        public float Value
        {
            get { return m_props.RatioToValue(m_ratio); }
            set
            {
                var val = m_props.ValueToRatio(value);
                val = m_props.RatioFilter(val);
                
                m_ratio = MathHelper.Clamp(val, 0, 1);

                UpdateLabel();

                OnValueChange();
            }
        }
    }
}
