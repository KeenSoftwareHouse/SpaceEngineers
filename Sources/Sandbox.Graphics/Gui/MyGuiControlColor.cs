using System;
using System.Text;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

//  Label is defined by string builder or by text enum. Only one of them at a time. It's good to use enum whenever 
//  possible, as it easily supports changing languages. Use string builder only if the text isn't and can't be defined 
//  in text resources.
//
//  If enum version is used, then text won't be stored in string builder until you use UpdateParams


namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlColor : MyGuiControlBase
    {
        public event Action<MyGuiControlColor> OnChange;

        const float SLIDER_WIDTH = 0.09f;

        Color m_color;
        MyGuiControlLabel m_textLabel;
        MyGuiControlSlider m_RSlider;
        MyGuiControlSlider m_GSlider;
        MyGuiControlSlider m_BSlider;
        MyGuiControlLabel m_RLabel;
        MyGuiControlLabel m_GLabel;
        MyGuiControlLabel m_BLabel;
        Vector2 m_minSize;
        MyStringId m_caption;

        bool m_canChangeColor = true;
        bool m_placeSlidersVertically;

        public MyGuiControlColor(
            String text,
            float textScale,
            Vector2 position,
            Color color,
            Color defaultColor,
            MyStringId dialogAmountCaption,
            bool placeSlidersVertically = false,
            string font = MyFontEnum.Blue)
            : base(position: position,
                   toolTip: null,
                   isActiveControl: false)
        {
            m_color = color;
            m_placeSlidersVertically = placeSlidersVertically;
            m_textLabel = MakeLabel(textScale, font);
            m_textLabel.Text = text.ToString();
            m_caption = dialogAmountCaption;

            m_RSlider = MakeSlider(font, defaultColor.R);
            m_GSlider = MakeSlider(font, defaultColor.G);
            m_BSlider = MakeSlider(font, defaultColor.B);

            m_RSlider.ValueChanged += delegate(MyGuiControlSlider sender)
            {
                if (m_canChangeColor)
                {
                    m_color.R = (byte)sender.Value;
                    UpdateTexts();
                    if (OnChange != null)
                        OnChange(this);
                }
            };
            m_GSlider.ValueChanged += delegate(MyGuiControlSlider sender)
            {
                if (m_canChangeColor)
                {
                    m_color.G = (byte)sender.Value;
                    UpdateTexts();
                    if (OnChange != null)
                        OnChange(this);
                }
            };
            m_BSlider.ValueChanged += delegate(MyGuiControlSlider sender)
            {
                if (m_canChangeColor)
                {
                    m_color.B = (byte)sender.Value;
                    UpdateTexts();
                    if (OnChange != null)
                        OnChange(this);
                }
            };

            m_RLabel = MakeLabel(textScale, font);
            m_GLabel = MakeLabel(textScale, font);
            m_BLabel = MakeLabel(textScale, font);

            m_RSlider.Value = m_color.R;
            m_GSlider.Value = m_color.G;
            m_BSlider.Value = m_color.B;

            Elements.Add(m_textLabel);

            Elements.Add(m_RSlider);
            Elements.Add(m_GSlider);
            Elements.Add(m_BSlider);

            Elements.Add(m_RLabel);
            Elements.Add(m_GLabel);
            Elements.Add(m_BLabel);

            UpdateTexts();
            RefreshInternals();
            Size = m_minSize;
        }

        MyGuiControlSlider MakeSlider(string font, byte defaultVal)
        {
            MyGuiControlSlider slider=new MyGuiControlSlider(
                position: Vector2.Zero,
                width: 121f/MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                minValue: 0,
                maxValue: 255,
                color: ColorMask,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                labelFont: font,
                defaultValue: (int)defaultVal);
            slider.SliderClicked = OnSliderClicked;
            return slider;
        }

        private bool OnSliderClicked(MyGuiControlSlider who)
        {
            if (MyInput.Static.IsAnyCtrlKeyPressed())
            {
                float min = 0;
                float max = 255;
                float val = who.Value;

                MyGuiScreenDialogAmount dialog = new MyGuiScreenDialogAmount(min, max, m_caption, parseAsInteger: true, defaultAmount: val);
                dialog.OnConfirmed += (v) => { who.Value = v; };
                MyScreenManager.AddScreen(dialog);
                return true;
            }
            return false;

        }

        MyGuiControlLabel MakeLabel(float scale, string font)
        {
            return new MyGuiControlLabel(
                text: String.Empty,
                colorMask: ColorMask,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * scale,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                font: font);
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            var position = GetPositionAbsoluteTopRight();
            var size = new Vector2(m_BSlider.Size.X, m_textLabel.Size.Y);
            MyGuiManager.DrawSpriteBatch(
                MyGuiConstants.BLANK_TEXTURE,
                position,
                size,
                ApplyColorMaskModifiers(m_color.ToVector4(), true, transitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            position.X -= size.X;
            MyGuiManager.DrawBorders(position, size, Color.White, BorderSize);
        }

        public override MyGuiControlBase HandleInput()
        {
            var handled = base.HandleInput();
            if (handled == null)
                handled = base.HandleInputElements();
            return handled;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            // Start placing things from top left.
            var posTopLeft = -0.5f * Size;
            var pos        = posTopLeft;

            const float COLOR_LABELS_WIDTH = 0.06f;

            var m_minSize = Vector2.Zero;
            if (m_placeSlidersVertically)
            {
                m_minSize.X = Math.Max(m_textLabel.Size.X, m_GSlider.MinSize.X + COLOR_LABELS_WIDTH);
                m_minSize.Y = (m_textLabel.Size.Y * 1.1f) + 3 * Math.Max(m_GSlider.Size.Y, m_GLabel.Size.Y);
            }
            else
            {
                m_minSize.X = MathHelper.Max(m_textLabel.Size.X, 3 * (m_GSlider.MinSize.X + COLOR_LABELS_WIDTH));
                m_minSize.Y = (m_textLabel.Size.Y * 1.1f) + m_RSlider.Size.Y + m_RLabel.Size.Y;
            }

            if (Size.X < m_minSize.X || Size.Y < m_minSize.Y)
            {
                Size = Vector2.Max(Size, m_minSize);
                return;
            }

            m_textLabel.Position = pos;
            pos.Y += m_textLabel.Size.Y * 1.1f;

            if (m_placeSlidersVertically)
            {
                Vector2 sliderSize = new Vector2(Size.X - COLOR_LABELS_WIDTH, m_RSlider.MinSize.Y);

                float rowHeight = Math.Max(m_RLabel.Size.Y, m_RSlider.Size.Y);

                m_RSlider.Size = m_GSlider.Size = m_BSlider.Size = sliderSize;

                m_RLabel.Position     = pos + new Vector2(0f, 0.5f) * rowHeight;
                m_RSlider.Position    = new Vector2(pos.X + Size.X, pos.Y + 0.5f * rowHeight);
                m_RLabel.OriginAlign  = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                m_RSlider.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
                pos.Y += rowHeight;

                m_GLabel.Position     = pos + new Vector2(0f, 0.5f) * rowHeight;
                m_GSlider.Position    = new Vector2(pos.X + Size.X, pos.Y + 0.5f * rowHeight);
                m_GLabel.OriginAlign  = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                m_GSlider.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
                pos.Y += rowHeight;

                m_BLabel.Position     = pos + new Vector2(0f, 0.5f) * rowHeight;
                m_BSlider.Position    = new Vector2(pos.X + Size.X, pos.Y + 0.5f * rowHeight);
                m_BLabel.OriginAlign  = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                m_BSlider.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
                pos.Y += rowHeight;
            }
            else
            {
                float colWidth = MathHelper.Max(m_RLabel.Size.X, m_RSlider.MinSize.X, Size.X / 3);
                Vector2 sliderSize = new Vector2(colWidth, m_RSlider.Size.Y);
                m_RSlider.Size = m_GSlider.Size = m_BSlider.Size = sliderSize;

                var sliderPos = pos;
                m_RSlider.Position = sliderPos; sliderPos.X += colWidth;
                m_GSlider.Position = sliderPos; sliderPos.X += colWidth;
                m_BSlider.Position = sliderPos;

                pos.Y += m_RSlider.Size.Y;

                m_RLabel.Position = pos; pos.X += colWidth;
                m_GLabel.Position = pos; pos.X += colWidth;
                m_BLabel.Position = pos; pos.X += colWidth;

                m_RLabel.OriginAlign = m_GLabel.OriginAlign = m_BLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                m_RSlider.OriginAlign = m_GSlider.OriginAlign = m_BSlider.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            }
        }

        private void UpdateSliders()
        {
            m_canChangeColor = false;
            m_RSlider.Value = m_color.R;
            m_GSlider.Value = m_color.G;
            m_BSlider.Value = m_color.B;

            UpdateTexts();

            m_canChangeColor = true;
        }

        private void UpdateTexts()
        {
            m_RLabel.Text = string.Format("R: {0}", m_color.R);
            m_GLabel.Text = string.Format("G: {0}", m_color.G);
            m_BLabel.Text = string.Format("B: {0}", m_color.B);
        }

        public void SetColor(Vector3 color)
        {
            SetColor(new Color(color));
        }

        public void SetColor(Vector4 color)
        {
            SetColor(new Color(color));
        }

        public void SetColor(Color color)
        {
            bool changed = m_color != color;
            m_color = color;
            
            UpdateSliders();
            if (changed && OnChange != null)
                OnChange(this);
        }

        public Color GetColor()
        {
            return m_color;
        }

        public Color Color
        {
            get { return m_color; }
        }
    }
}
