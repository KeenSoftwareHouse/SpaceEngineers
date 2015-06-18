using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Gui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlRadioButtonGroup : IEnumerable<MyGuiControlRadioButton>
    {
        public const int INVALID_INDEX = -1;

        private List<MyGuiControlRadioButton> m_radioButtons;

        public MyGuiControlRadioButton SelectedButton
        {
            get { return TryGetButton(SelectedIndex ?? INVALID_INDEX); }
        }

        private int? m_selectedIndex;
        public int? SelectedIndex
        {
            get { return m_selectedIndex; }
            set
            {
                if (m_selectedIndex != value)
                {
                    if (m_selectedIndex.HasValue)
                        m_radioButtons[m_selectedIndex.Value].Selected = false;
                    m_selectedIndex = value;
                    if (m_selectedIndex.HasValue)
                        m_radioButtons[m_selectedIndex.Value].Selected = true;

                    if (SelectedChanged != null)
                        SelectedChanged(this);

                }
            }
        }

        public event Action<MyGuiControlRadioButtonGroup> SelectedChanged;

        public MyGuiControlRadioButtonGroup()
        {
            m_radioButtons  = new List<MyGuiControlRadioButton>();
            m_selectedIndex = null;
        }

        public void Add(MyGuiControlRadioButton radioButton)
        {
            m_radioButtons.Add(radioButton);
            radioButton.SelectedChanged += OnRadioButtonSelected;
        }

        public void Remove(MyGuiControlRadioButton radioButton)
        {
            radioButton.SelectedChanged -= OnRadioButtonSelected;
            m_radioButtons.Remove(radioButton);
        }

        public void Clear()
        {
            foreach (var radioButton in m_radioButtons)
                radioButton.SelectedChanged -= OnRadioButtonSelected;

            m_radioButtons.Clear();
            m_selectedIndex = null;
        }

        public void SelectByKey(int key)
        {
            for (int i = 0; i < m_radioButtons.Count; ++i)
            {
                var button = m_radioButtons[i];
                if (button.Key == key)
                {
                    SelectedIndex = i;
                    button.Selected = true;
                }
                else
                {
                    button.Selected = false;
                }

            }
        }

        private void OnRadioButtonSelected(MyGuiControlRadioButton sender)
        {
            SelectedIndex = m_radioButtons.IndexOf(sender);
        }

        private MyGuiControlRadioButton TryGetButton(int buttonIdx)
        {
            return (buttonIdx < m_radioButtons.Count && buttonIdx >= 0)
                ? m_radioButtons[buttonIdx]
                : null;
        }

        public IEnumerator<MyGuiControlRadioButton> GetEnumerator()
        {
            return m_radioButtons.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlRadioButton))]
    public class MyGuiControlRadioButton : MyGuiControlBase
    {
        #region Styles
        public class StyleDefinition
        {
            public MyGuiCompositeTexture NormalTexture;
            public MyGuiCompositeTexture HighlightTexture;
            public MyFontEnum NormalFont;
            public MyFontEnum HighlightFont;
            public MyGuiBorderThickness Padding;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlRadioButton()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlRadioButtonStyleEnum>() + 1];
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.FilterCharacter] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_FILTER_CHARACTER,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_FILTER_CHARACTER_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.FilterGrid] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_FILTER_GRID,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_FILTER_GRID_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.FilterAll] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_FILTER_ALL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_FILTER_ALL_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.FilterEnergy] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_FILTER_ENERGY,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_FILTER_ENERGY_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.FilterStorage] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_FILTER_STORAGE,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_FILTER_STORAGE_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.FilterSystem] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_FILTER_SYSTEM,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_FILTER_SYSTEM_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.ScenarioButton] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_NULL,
                HighlightTexture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK,
            };
            m_styles[(int)MyGuiControlRadioButtonStyleEnum.Rectangular] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                HighlightTexture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
                Padding          = new MyGuiBorderThickness(6f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                                                            6f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
            };

        }

        public static StyleDefinition GetVisualStyle(MyGuiControlRadioButtonStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion

        private bool m_selected;
        private MyGuiControlRadioButtonStyleEnum m_visualStyle;
        private StyleDefinition m_styleDef;
        private StringBuilder m_text;
        private MyFontEnum m_font;
        private RectangleF m_internalArea;

        public MyGuiControlRadioButtonStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }

        public StringBuilder Text
        {
            get { return m_text; }
            set
            {
                if (value != null)
                {
                    if (m_text == null)
                        m_text = new StringBuilder();
                    m_text.Clear().AppendStringBuilder(value);
                }
                else if (m_text != null)
                {
                    m_text = null;
                }
            }
        }

        public MyGuiDrawAlignEnum TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

        public MyGuiHighlightTexture? Icon;

        public MyGuiDrawAlignEnum IconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

        public event Action<MyGuiControlRadioButton> SelectedChanged;

        public MyGuiControlRadioButton() : this(position: null) { }

        public MyGuiControlRadioButton(
            Vector2? position  = null,
            Vector2? size      = null,
            int key            = 0,
            Vector4? colorMask = null)
            : base( position: position,
                    size: size,
                    colorMask: colorMask,
                    toolTip: null,
                    isActiveControl: true,
                    canHaveFocus: true)
        {
            Name = "RadioButton";
            m_selected = false;
            Key = key;
            VisualStyle = MyGuiControlRadioButtonStyleEnum.Rectangular;
        }

        public override void Init(MyObjectBuilder_GuiControlBase builder)
        {
            base.Init(builder);
            var ob = (MyObjectBuilder_GuiControlRadioButton)builder;
            Key = ob.Key;
            VisualStyle = ob.VisualStyle;
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_GuiControlRadioButton)base.GetObjectBuilder();
            ob.Key = Key;
            ob.VisualStyle = VisualStyle;
            return ob;
        }

        public int Key
        {
            get;
            set;
        }

        public bool Selected
        {
            get { return m_selected; }
            set
            {
                if (m_selected != value)
                {
                    m_selected = value;
                    if (value && SelectedChanged != null)
                        SelectedChanged(this);
                }
            }
        }

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase ret = base.HandleInput();

            if (ret == null && Enabled)
            {
                if ((IsMouseOver && MyInput.Static.IsNewPrimaryButtonReleased()) ||
                    (HasFocus && (MyInput.Static.IsNewKeyPressed(MyKeys.Enter) || MyInput.Static.IsNewKeyPressed(MyKeys.Space) || MyInput.Static.IsJoystickButtonNewPressed(MyJoystickButtonsEnum.J01))))
                {
                    if (!Selected) 
                    {
                        MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                        Selected = true;
                        ret = this;
                    }
                }
            }

            return ret;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            Vector2 topLeft = Vector2.Zero;
            if (Icon.HasValue || (Text != null && Text.Length > 0))
                topLeft = GetPositionAbsoluteTopLeft();
            var internalTopLeft = topLeft + m_internalArea.Position;
            var internalSize = m_internalArea.Size;

            if (Icon.HasValue)
            {
                var iconPosition = MyUtils.GetCoordAlignedFromTopLeft(internalTopLeft, internalSize, IconOriginAlign);
                var icon = Icon.Value;
                var ratios = Vector2.Min(icon.SizeGui, internalSize) / icon.SizeGui;
                float scale = Math.Min(ratios.X, ratios.Y);
                MyGuiManager.DrawSpriteBatch(
                    texture: (HasHighlight) ? icon.Highlight : icon.Normal,
                    normalizedCoord: iconPosition,
                    normalizedSize: icon.SizeGui * scale,
                    color: ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                    drawAlign: IconOriginAlign);
            }

            if (Text != null && Text.Length > 0)
            {
                Vector2 textPosition = MyUtils.GetCoordAlignedFromTopLeft(internalTopLeft, m_internalArea.Size, TextAlignment);
                var textFont = m_font;
                var textScale = MyGuiConstants.DEFAULT_TEXT_SCALE * MyGuiManager.LanguageTextScale;
                MyGuiManager.DrawString(textFont, Text, textPosition, textScale, ApplyColorMaskModifiers(Vector4.One, Enabled, transitionAlpha), TextAlignment);
            }
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            RefreshInternals();
        }

        protected override bool ShouldHaveHighlight()
        {
            return Selected || base.ShouldHaveHighlight();
        }

        protected override void OnSizeChanged()
        {
            RefreshInternalArea();
            base.OnSizeChanged();
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
                m_font            = m_styleDef.HighlightFont;
                BackgroundTexture = m_styleDef.HighlightTexture;
            }
            else
            {
                m_font            = m_styleDef.NormalFont;
                BackgroundTexture = m_styleDef.NormalTexture;
            }
            MinSize = BackgroundTexture.MinSizeGui;
            MaxSize = BackgroundTexture.MaxSizeGui;
            RefreshInternalArea();
        }

        private void RefreshInternalArea()
        {
            m_internalArea.Position = m_styleDef.Padding.TopLeftOffset;
            m_internalArea.Size     = Size - m_styleDef.Padding.SizeChange;
        }
    }
}
