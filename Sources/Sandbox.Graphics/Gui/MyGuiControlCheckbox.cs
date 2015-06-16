using Sandbox.Common.ObjectBuilders.Gui;
using System;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlCheckbox))]
    public class MyGuiControlCheckbox : MyGuiControlBase
    {
        #region Styles
        public class StyleDefinition
        {
            public MyGuiCompositeTexture NormalCheckedTexture;
            public MyGuiCompositeTexture NormalUncheckedTexture;
            public MyGuiCompositeTexture HighlightCheckedTexture;
            public MyGuiCompositeTexture HighlightUncheckedTexture;
            public MyGuiHighlightTexture CheckedIcon;
            public MyGuiHighlightTexture UncheckedIcon;
            public Vector2? SizeOverride;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlCheckbox()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlCheckboxStyleEnum>() + 1];
            m_styles[(int)MyGuiControlCheckboxStyleEnum.Default] = new StyleDefinition()
            {
                NormalCheckedTexture      = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_NORMAL_CHECKED,
                NormalUncheckedTexture    = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_NORMAL_UNCHECKED,
                HighlightCheckedTexture   = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_HIGHLIGHT_CHECKED,
                HighlightUncheckedTexture = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_HIGHLIGHT_UNCHECKED,
            };
            m_styles[(int)MyGuiControlCheckboxStyleEnum.Debug] = new StyleDefinition()
            {
                NormalCheckedTexture      = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_NORMAL_CHECKED.LeftTop },
                NormalUncheckedTexture    = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_NORMAL_UNCHECKED.LeftTop },
                HighlightCheckedTexture   = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_HIGHLIGHT_CHECKED.LeftTop },
                HighlightUncheckedTexture = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_HIGHLIGHT_UNCHECKED.LeftTop },
                SizeOverride              = MyGuiConstants.TEXTURE_CHECKBOX_DEFAULT_NORMAL_UNCHECKED.MinSizeGui * 0.65f,
            };
            m_styles[(int)MyGuiControlCheckboxStyleEnum.SwitchOnOffLeft] = new StyleDefinition()
            {
                NormalCheckedTexture      = MyGuiConstants.TEXTURE_SWITCHONOFF_LEFT_HIGHLIGHT,
                NormalUncheckedTexture    = MyGuiConstants.TEXTURE_SWITCHONOFF_LEFT_NORMAL,
                HighlightCheckedTexture   = MyGuiConstants.TEXTURE_SWITCHONOFF_LEFT_HIGHLIGHT,
                HighlightUncheckedTexture = MyGuiConstants.TEXTURE_SWITCHONOFF_LEFT_NORMAL,
            };
            m_styles[(int)MyGuiControlCheckboxStyleEnum.SwitchOnOffRight] = new StyleDefinition()
            {
                NormalCheckedTexture      = MyGuiConstants.TEXTURE_SWITCHONOFF_RIGHT_HIGHLIGHT,
                NormalUncheckedTexture    = MyGuiConstants.TEXTURE_SWITCHONOFF_RIGHT_NORMAL,
                HighlightCheckedTexture   = MyGuiConstants.TEXTURE_SWITCHONOFF_RIGHT_HIGHLIGHT,
                HighlightUncheckedTexture = MyGuiConstants.TEXTURE_SWITCHONOFF_RIGHT_NORMAL,
            };
            m_styles[(int)MyGuiControlCheckboxStyleEnum.Repeat] = new StyleDefinition()
            {
                NormalCheckedTexture      = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                NormalUncheckedTexture    = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                HighlightCheckedTexture   = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                HighlightUncheckedTexture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                CheckedIcon               = MyGuiConstants.TEXTURE_BUTTON_ICON_REPEAT,
                SizeOverride              = MyGuiConstants.TEXTURE_BUTTON_ICON_REPEAT.SizeGui * 1.4f,
            };
            m_styles[(int)MyGuiControlCheckboxStyleEnum.Slave] = new StyleDefinition()
            {
                NormalCheckedTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                NormalUncheckedTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                HighlightCheckedTexture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                HighlightUncheckedTexture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                CheckedIcon = MyGuiConstants.TEXTURE_BUTTON_ICON_SLAVE,
                SizeOverride = MyGuiConstants.TEXTURE_BUTTON_ICON_SLAVE.SizeGui * 1.4f,
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlCheckboxStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion

        public Action<MyGuiControlCheckbox> IsCheckedChanged;

        public bool IsChecked
        {
            get { return m_isChecked; }

            set
            {
                if (m_isChecked != value)
                {
                    m_isChecked = value;
                    RefreshInternals();
                    if (IsCheckedChanged != null)
                        IsCheckedChanged(this);
                }
            }
        }

        bool m_isChecked;

        public MyGuiControlCheckboxStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlCheckboxStyleEnum m_visualStyle;
        private StyleDefinition m_styleDef;

        private MyGuiHighlightTexture m_icon;

        public MyGuiControlCheckbox(
            Vector2? position                         = null,
            Vector4? color                            = null,
            String toolTip                     = null,
            bool isChecked                            = false,
            MyGuiControlCheckboxStyleEnum visualStyle = MyGuiControlCheckboxStyleEnum.Default,
            MyGuiDrawAlignEnum originAlign            = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER):
            base(
                 position:        position ?? Vector2.Zero,
                 toolTip:         toolTip,
                 colorMask: color,
                 isActiveControl: true,
                 originAlign: originAlign,
                 canHaveFocus: true)
        {
            Name = "CheckBox";
            m_isChecked = isChecked;
            VisualStyle = visualStyle;
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var objectBuilder = (MyObjectBuilder_GuiControlCheckbox)base.GetObjectBuilder();

            objectBuilder.IsChecked = m_isChecked;
            objectBuilder.VisualStyle = VisualStyle;
            return objectBuilder;
        }

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            var controlBuilder = (MyObjectBuilder_GuiControlCheckbox)objectBuilder;
            m_isChecked = controlBuilder.IsChecked;
            VisualStyle = controlBuilder.VisualStyle;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, transitionAlpha);
            var iconTexture = (HasHighlight) ? m_icon.Highlight : m_icon.Normal;
            if (!string.IsNullOrEmpty(iconTexture))
            {
                MyGuiManager.DrawSpriteBatch(
                    texture: iconTexture,
                    normalizedCoord: GetPositionAbsoluteCenter(),
                    normalizedSize: m_icon.SizeGui,
                    color: ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                    drawAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }
        }

        //  Method returns true if input was captured by control, so no other controls, nor screen can use input in this update
        public override MyGuiControlBase HandleInput()
        {
            if (!Enabled)
                return null;

            MyGuiControlBase ret = base.HandleInput();

            if (ret == null && Owner.HandleMouse)
            {
                if ( (IsMouseOver && MyInput.Static.IsNewPrimaryButtonPressed()) ||
                     (HasFocus && (MyInput.Static.IsNewKeyPressed(MyKeys.Enter) ||
                                   MyInput.Static.IsNewKeyPressed(MyKeys.Space)))
                    )
                {
                    UserCheck();
                    ret = this;
                }
            }

            return ret;
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            RefreshInternals();
        }

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            if (IsChecked)
            {
                if (HasHighlight)
                    BackgroundTexture = m_styleDef.HighlightCheckedTexture;
                else
                    BackgroundTexture = m_styleDef.NormalCheckedTexture;
                m_icon = m_styleDef.CheckedIcon;
                Size = m_styleDef.SizeOverride ?? BackgroundTexture.MinSizeGui;
            }
            else
            {
                if (HasHighlight)
                    BackgroundTexture = m_styleDef.HighlightUncheckedTexture;
                else
                    BackgroundTexture = m_styleDef.NormalUncheckedTexture;
                m_icon = m_styleDef.UncheckedIcon;
                Size = m_styleDef.SizeOverride ?? BackgroundTexture.MinSizeGui;
            }
            MinSize = BackgroundTexture.MinSizeGui;
            MaxSize = BackgroundTexture.MaxSizeGui;
        }

        private void UserCheck()
        {
            MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
            IsChecked = !IsChecked;
        }

    }
}
