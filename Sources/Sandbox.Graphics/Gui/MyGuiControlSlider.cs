using System;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlSlider : MyGuiControlSliderBase
    {
        private int m_labelDecimalPlaces;
        public int LabelDecimalPlaces
        {
            get { return m_labelDecimalPlaces; }
            set { m_labelDecimalPlaces = value; }
        }

        private string m_labelFormat = "{0}";

        private float m_minValue;        //  This is min value that can be selected on slider in original units, e.g. meters, so it can be for example 100 meters
        private float m_maxValue;        //  This is max value that can be selected on slider in original units, e.g. meters, so it can be for example 10,000 meters

        private bool m_intValue;

        private float m_range;

        /// <summary>
        /// Normalized value selected on slider (range 0, 1).
        /// </summary>
        public float ValueNormalized
        {
            get { return Ratio; }
        }

        public float? DefaultValue
        {
            get { return DefaultRatio.HasValue ? RatioToValue(DefaultRatio.Value) : default(float?); }
            set
            {
                if (value.HasValue)
                {
                    DefaultRatio = ValueToRatio(value.Value);
                }
                else
                {
                    DefaultRatio = default(float?);
                }
            }
        }

        public MyGuiControlSlider(
            Vector2? position = null,
            float minValue = 0f,
            float maxValue = 1f,
            float width = 464f/1600f,
            float? defaultValue = null,
            Vector4? color = null,
            String labelText = null,
            int labelDecimalPlaces = 1,
            float labelScale = MyGuiConstants.DEFAULT_TEXT_SCALE,
            float labelSpaceWidth = 0f,
            string labelFont = MyFontEnum.White,
            String toolTip = null,
            MyGuiControlSliderStyleEnum visualStyle = MyGuiControlSliderStyleEnum.Default,
            MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            bool intValue = false)
            : base(
                position: position,
                width: width,
                color: color,
                labelScale: labelScale,
                labelSpaceWidth: labelSpaceWidth,
                labelFont: labelFont,
                toolTip: toolTip,
                visualStyle: visualStyle,
                originAlign: originAlign
            )
        {
            m_minValue = minValue;
            m_maxValue = maxValue;
            m_range = m_maxValue - m_minValue;

            MyDebug.AssertDebug(m_maxValue > m_minValue && m_maxValue != m_minValue);

            Propeties = new MyGuiSliderProperties()
            {
                FormatLabel = FormatValue,
                RatioFilter = FilterRatio,
                RatioToValue = RatioToValue,
                ValueToRatio = ValueToRatio
            };

            DefaultRatio = defaultValue.HasValue ? ValueToRatio(defaultValue.Value) : default(float?);
            Ratio = DefaultRatio ?? minValue;
            m_intValue = intValue;

            LabelDecimalPlaces = labelDecimalPlaces;
            
            m_labelFormat = labelText;
        }

        public void SetBounds(float minValue, float maxValue)
        {
            MyDebug.AssertDebug(m_maxValue > m_minValue && m_maxValue != m_minValue);

            m_minValue = minValue;
            m_maxValue = maxValue;
            m_range = maxValue - minValue;
            Refresh();
        }

        // Replace parent call to make things prettyer
        public new Action<MyGuiControlSlider> ValueChanged;

        protected override void OnValueChange()
        {
            if (ValueChanged != null)
                ValueChanged(this);
        }

        public new Func<MyGuiControlSlider, bool> SliderClicked;

        protected override bool OnSliderClicked()
        {
            if (SliderClicked != null)
                return SliderClicked(this);
            return false;
        }

        public float MinValue
        {
            get { return m_minValue; }
            set
            {
                m_minValue = value;
                m_range = m_maxValue - m_minValue;
                Refresh();
            }
        }

        public float MaxValue
        {
            get { return m_maxValue; }
            set
            {
                m_maxValue = value;
                m_range = m_maxValue - m_minValue;
                Refresh();
            }
        }

        private void Refresh()
        {
            Ratio = Ratio;
        }

        public bool IntValue
        {
            get { return m_intValue; }
            set
            {
                m_intValue = value;
                Refresh();
            }
        }

        #region Properties impl

        private float RatioToValue(float ratio)
        {
            if (m_intValue)
                return (float)Math.Round(ratio * m_range + m_minValue);
            return ratio * m_range + m_minValue;
        }

        private float ValueToRatio(float ratio)
        {
            return (ratio - m_minValue) / m_range;
        }

        private float FilterRatio(float ratio)
        {
            ratio = MathHelper.Clamp(ratio, 0, 1);

            // Not the best code but works for now
            if (m_intValue)
                return ValueToRatio((float)Math.Round(RatioToValue(ratio)));
            return ratio;
        }

        private string FormatValue(float value)
        {
            if (m_labelFormat != null)
                return string.Format(m_labelFormat, MyValueFormatter.GetFormatedFloat(value, LabelDecimalPlaces));
            return null;
        }

        #endregion
    }
}
