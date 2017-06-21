using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Game.ObjectBuilders.Gui;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Gui
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlImageButton))]
    public class MyGuiControlImageButton : MyGuiControlBase
    {
        public struct ButtonIcon
        {
            private string m_normal;
            private string m_active;
            private string m_highlight;
            private string m_activeHighlight;
            private string m_disabled;

            public string Normal
            {
                get { return m_normal; }
                set { m_normal = value; }
            }

            public string Active
            {
                get { return String.IsNullOrEmpty(m_active) ? Highlight : m_active; }
                set { m_active = value; }
            }

            public string Highlight
            {
                get { return String.IsNullOrEmpty(m_highlight) ? m_normal : m_highlight; }
                set { m_highlight = value; }
            }

            public string ActiveHighlight
            {
                get { return String.IsNullOrEmpty(m_activeHighlight) ? Highlight : Active; }
                set { m_activeHighlight = value; }
            }

            public string Disabled
            {
                get { return String.IsNullOrEmpty(m_disabled) ? Normal : m_disabled; }
                set { m_disabled = value; }
            }
        }

        public class StateDefinition
        {
            public MyGuiCompositeTexture Texture;
            public string Font;
            public string CornerTextFont;
            public float CornerTextSize;
        }

        public class StyleDefinition
        {
            public StateDefinition Normal;
            public StateDefinition Active;
            public StateDefinition Highlight;
            public StateDefinition ActiveHighlight;
            public StateDefinition Disabled;
            public MyGuiBorderThickness Padding;
            public Vector4 BackgroundColor = Vector4.One;
        }

        #region Fields
        private StyleDefinition m_styleDefinition;

        private bool m_readyToClick = false;
        private string m_text;
        private MyStringId m_textEnum;
        private float m_textScale;
        private float m_buttonScale = 1.0f;
        private bool m_activateOnMouseRelease = false;

        public event Action<MyGuiControlImageButton> ButtonClicked;

        private StringBuilder m_drawText = new StringBuilder();
        private StringBuilder m_cornerText = new StringBuilder();

        private bool m_drawRedTextureWhenDisabled = true;

        private RectangleF m_internalArea;

        protected GuiSounds m_cueEnum;

        private bool m_checked;
        public bool Checked
        {
            get { return m_checked; }
            set { m_checked = value; RefreshInternals(); }
        }

        public bool ActivateOnMouseRelease
        {
            get { return m_activateOnMouseRelease; }
            set
            {
                m_activateOnMouseRelease = value;
            }
        }

        public bool Selected = false;

        private MyKeys m_boundKey = MyKeys.None;
        private bool m_allowBoundKey = false;

        /// <summary>
        /// The key this button will respond to when pressed. Will act as an OnClick.
        /// MyKeys.None by default.
        /// </summary>
        public MyKeys BoundKey
        {
            get { return m_boundKey; }
            set { m_boundKey = value; }
        }

        /// <summary>
        /// Whether or not this button supports having a key bound to it.
        /// False by default.
        /// </summary>
        public bool AllowBoundKey
        {
            get { return m_allowBoundKey; }
            set { m_allowBoundKey = value; }
        }

        #endregion

        #region Constructors

        public MyGuiControlImageButton() : this(position: null) { }

        public MyGuiControlImageButton(
            string name = "Button",
            Vector2? position = null,
            Vector2? size = null,
            Vector4? colorMask = null,
            MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            String toolTip = null,
            StringBuilder text = null,
            float textScale = MyGuiConstants.DEFAULT_TEXT_SCALE,
            MyGuiDrawAlignEnum textAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            MyGuiControlHighlightType highlightType = MyGuiControlHighlightType.WHEN_ACTIVE,
            Action<MyGuiControlImageButton> onButtonClick = null,
            GuiSounds cueEnum = GuiSounds.MouseClick,
            float buttonScale = 1.0f,
            int? buttonIndex = null,
            bool activateOnMouseRelease = false)
            : base(position: position ?? Vector2.Zero,
                    size: size,
                    colorMask: colorMask ?? MyGuiConstants.BUTTON_BACKGROUND_COLOR,
                    toolTip: toolTip,
                    highlightType: highlightType,
                    originAlign: originAlign,
                    canHaveFocus: true)
        {
            m_styleDefinition = new StyleDefinition()
            {
                Active = new StateDefinition() { Texture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL },
                Disabled = new StateDefinition() { Texture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL },
                Normal = new StateDefinition() { Texture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL },
                Highlight = new StateDefinition() { Texture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_HIGHLIGHT },
                ActiveHighlight = new StateDefinition() { Texture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_HIGHLIGHT },
            };

            Name = name ?? "Button";
            ButtonClicked = onButtonClick;
            Index = buttonIndex ?? 0;
            UpdateText();

            m_drawText.Clear().Append(text);
            TextScale = textScale;
            TextAlignment = textAlignment;

            m_cueEnum = cueEnum;
            m_activateOnMouseRelease = activateOnMouseRelease;

            ButtonScale = buttonScale;

            Size *= ButtonScale;
        }

        #endregion

        //  Method returns true if input was captured by control, so no other controls, nor screen can use input in this update
        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase captureInput = base.HandleInput();

            if (captureInput == null)
            {
                if (!m_activateOnMouseRelease)
                {
                    if (IsMouseOver && MyInput.Static.IsNewPrimaryButtonPressed())
                        m_readyToClick = true;
                    if (!IsMouseOver && MyInput.Static.IsNewPrimaryButtonReleased())
                        m_readyToClick = false;
                }
                else
                    m_readyToClick = true;

                if ((IsMouseOver && (MyInput.Static.IsNewPrimaryButtonReleased()) && m_readyToClick ||
                    (HasFocus && (MyInput.Static.IsNewKeyPressed(MyKeys.Enter) ||
                                  MyInput.Static.IsNewKeyPressed(MyKeys.Space)))))
                {
                    if (Enabled)
                    {
                        MyGuiSoundManager.PlaySound(m_cueEnum);
                        if (ButtonClicked != null)
                            ButtonClicked(this);
                    }

                    captureInput = this;
                    m_readyToClick = false;
                    return captureInput;
                }
                if (IsMouseOver && MyInput.Static.IsPrimaryButtonPressed())
                    captureInput = this;//to be first in queue when button is released

                if (captureInput == null && Enabled && AllowBoundKey && BoundKey != MyKeys.None)
                {
                    if (MyInput.Static.IsNewKeyPressed(BoundKey))
                    {
                        MyGuiSoundManager.PlaySound(m_cueEnum);
                        if (ButtonClicked != null)
                            ButtonClicked(this);

                        captureInput = this;
                        m_readyToClick = false;
                    }
                }
            }
            return captureInput;
        }

        public override string GetMouseCursorTexture()
        {
            return MyGuiConstants.CURSOR_ARROW;
        }

        protected void RaiseButtonClicked()
        {
            if (ButtonClicked != null)
                ButtonClicked(this);
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, transitionAlpha);

            // Draw cross texture 
            if (!Enabled && DrawCrossTextureWhenDisabled)
            {
                MyGuiManager.DrawSpriteBatch(MyGuiConstants.BUTTON_LOCKED, GetPositionAbsolute(), Size * MyGuiConstants.LOCKBUTTON_SIZE_MODIFICATION,
                    MyGuiConstants.DISABLED_BUTTON_COLOR, OriginAlign);
            }

            var topLeft = GetPositionAbsoluteTopLeft();
            var internalTopLeft = topLeft + m_internalArea.Position;

            if (!String.IsNullOrEmpty(Icon.Normal))
            {
                var texture = !Enabled ? Icon.Disabled : HasHighlight && Checked ? Icon.ActiveHighlight : HasHighlight ? Icon.Highlight : Checked ? Icon.Active : Icon.Normal;
                MyGuiManager.DrawSpriteBatch(texture: texture, normalizedCoord: GetPositionAbsoluteCenter(), normalizedSize: Size - m_styleDefinition.Padding.SizeChange, color: ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha), drawAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, waitTillLoaded: false);
            }

            Vector4 textColor = Enabled ? Vector4.One : MyGuiConstants.DISABLED_CONTROL_COLOR_MASK_MULTIPLIER;
            if (m_drawText.Length > 0 && TextScaleWithLanguage > 0)
            {
                Vector2 textPosition = MyUtils.GetCoordAlignedFromTopLeft(internalTopLeft, m_internalArea.Size, TextAlignment);
                MyGuiManager.DrawString(TextFont, m_drawText, textPosition, TextScaleWithLanguage, ApplyColorMaskModifiers(textColor, Enabled, transitionAlpha), TextAlignment);
            }

            if (m_cornerText.Length > 0 && CornerTextSize > 0)
            {
                Vector2 textPosition = MyUtils.GetCoordAlignedFromTopLeft(internalTopLeft, m_internalArea.Size, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
                MyGuiManager.DrawString(CornerTextFont, m_cornerText, textPosition, CornerTextSize, ApplyColorMaskModifiers(textColor, Enabled, transitionAlpha),
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            }

            //DebugDraw();
        }

        private void DebugDraw()
        {
            MyGuiManager.DrawBorders(GetPositionAbsoluteTopLeft() + m_internalArea.Position, m_internalArea.Size, Color.White, 1);
        }

        void UpdateText()
        {
            if (!string.IsNullOrEmpty(m_text))
            {
                m_drawText.Clear();
                m_drawText.Append(m_text);
            }
            else
            {
                m_drawText.Clear();
                m_drawText.Append(MyTexts.GetString(m_textEnum));
            }
        }

        #region Properties

        public int Index { get; private set; }

        public string Text
        {
            get { return m_text; }
            set
            {
                m_text = value;
                UpdateText();
            }
        }

        public MyStringId TextEnum
        {
            get { return m_textEnum; }
            set
            {
                m_textEnum = value;
                UpdateText();
            }
        }

        /// <summary>
        /// Text visible in the bottom left corner.
        /// </summary>
        public string CornerText
        {
            get { return m_cornerText.ToString(); }
            set
            {
                m_cornerText.Clear();
                m_cornerText.Append(value);
            }
        }

        public GuiSounds CueEnum
        {
            get { return m_cueEnum; }
            set { m_cueEnum = value; }
        }

        public float TextScale
        {
            get { return m_textScale; }
            set
            {
                m_textScale = value;
                TextScaleWithLanguage = value * MyGuiManager.LanguageTextScale;
            }
        }

        protected float ButtonScale
        {
            get { return m_buttonScale; }
            set { m_buttonScale = value; }
        }

        private float m_textScaleWithLanguage;

        public float TextScaleWithLanguage
        {
            get { return m_textScaleWithLanguage; }
            private set { m_textScaleWithLanguage = value; }
        }

        public MyGuiDrawAlignEnum TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

        public string TextFont;

        /// <summary>
        /// Corner text font.
        /// </summary>
        public string CornerTextFont;

        /// <summary>
        /// Corner text size where 1.0f is the standard size.
        /// </summary>
        public float CornerTextSize;

        public bool DrawCrossTextureWhenDisabled = false;

        public ButtonIcon Icon;
        #endregion

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var objectBuilder = (MyObjectBuilder_GuiControlImageButton)base.GetObjectBuilder();

            objectBuilder.Text = Text;
            objectBuilder.TextEnum = m_textEnum.ToString();
            objectBuilder.TextScale = TextScale;
            objectBuilder.TextAlignment = (int)TextAlignment;
            objectBuilder.DrawCrossTextureWhenDisabled = DrawCrossTextureWhenDisabled;

            return objectBuilder;
        }

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            var buttonObjectBuilder = (MyObjectBuilder_GuiControlImageButton)objectBuilder;

            Text = buttonObjectBuilder.Text;
            m_textEnum = MyStringId.GetOrCompute(buttonObjectBuilder.TextEnum);
            TextScale = buttonObjectBuilder.TextScale;
            TextAlignment = (MyGuiDrawAlignEnum)buttonObjectBuilder.TextAlignment;
            DrawCrossTextureWhenDisabled = buttonObjectBuilder.DrawCrossTextureWhenDisabled;

            UpdateText();
        }

        protected override bool ShouldHaveHighlight()
        {
            if (HighlightType == MyGuiControlHighlightType.FORCED)
                return Selected;
            else
                return base.ShouldHaveHighlight();
        }

        protected override void OnHasHighlightChanged()
        {
            RefreshInternals();
            base.OnHasHighlightChanged();
        }

        protected override void OnOriginAlignChanged()
        {
            base.OnOriginAlignChanged();
            RefreshInternals();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            ColorMask = m_styleDefinition.BackgroundColor;
            if (!Enabled)
            {
                BackgroundTexture = m_styleDefinition.Disabled.Texture;
                TextFont = m_styleDefinition.Disabled.Font;
                CornerTextFont = m_styleDefinition.Disabled.CornerTextFont;
                CornerTextSize = m_styleDefinition.Disabled.CornerTextSize;
            }
            if (HasHighlight && Checked)
            {
                BackgroundTexture = m_styleDefinition.ActiveHighlight.Texture;
                TextFont = m_styleDefinition.ActiveHighlight.Font;
                CornerTextFont = m_styleDefinition.ActiveHighlight.CornerTextFont;
                CornerTextSize = m_styleDefinition.ActiveHighlight.CornerTextSize;
            }
            else if (HasHighlight)
            {
                BackgroundTexture = m_styleDefinition.Highlight.Texture;
                TextFont = m_styleDefinition.Highlight.Font;
                CornerTextFont = m_styleDefinition.Highlight.CornerTextFont;
                CornerTextSize = m_styleDefinition.Highlight.CornerTextSize;
            }
            else if (Checked)
            {
                BackgroundTexture = m_styleDefinition.Active.Texture;
                TextFont = m_styleDefinition.Active.Font;
                CornerTextFont = m_styleDefinition.Active.CornerTextFont;
                CornerTextSize = m_styleDefinition.Active.CornerTextSize;
            }
            else
            {
                BackgroundTexture = m_styleDefinition.Normal.Texture;
                TextFont = m_styleDefinition.Normal.Font;
                CornerTextFont = m_styleDefinition.Normal.CornerTextFont;
                CornerTextSize = m_styleDefinition.Normal.CornerTextSize;
            }

            var size = Size;
            if (BackgroundTexture != null)
            {
                MinSize = BackgroundTexture.MinSizeGui;
                MaxSize = BackgroundTexture.MaxSizeGui;
            }
            else
            {
                MinSize = Vector2.Zero;
                MaxSize = Vector2.PositiveInfinity;
                size = Vector2.Zero;
            }

            // No size specified, but we have string and font ... probably its a clickable text so let's use that as size.
            if (size == Vector2.Zero && m_drawText != null)
            {
                size = MyGuiManager.MeasureString(TextFont, m_drawText, TextScaleWithLanguage);
            }

            var padding = m_styleDefinition.Padding;
            m_internalArea.Position = padding.TopLeftOffset;
            m_internalArea.Size = Size - padding.SizeChange;
            Size = size;
        }

        public void ApplyStyle(StyleDefinition style)
        {
            m_styleDefinition = style;
            RefreshInternals();
        }
    }
}
