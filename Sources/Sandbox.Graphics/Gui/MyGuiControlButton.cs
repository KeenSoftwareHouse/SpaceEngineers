#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Gui;
using System;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Graphics.GUI
{
    public enum MyGuiControlButtonTextAlignment
    {
        Centered,       //  Text is in the button's center
        Left            //  Text is moved to the left side
    }

    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlButton))]
    public class MyGuiControlButton : MyGuiControlBase
    {
        #region Styles
        public class StyleDefinition
        {
            public MyFontEnum NormalFont;
            public MyFontEnum HighlightFont;
            public MyGuiCompositeTexture NormalTexture;
            public MyGuiCompositeTexture HighlightTexture;
            public Vector2? SizeOverride;
            public MyGuiBorderThickness Padding;
            public Vector4 BackgroundColor = Vector4.One;
            public string Underline = null;
            public string UnderlineHighlight = null;
            public string MouseOverCursor = MyGuiConstants.CURSOR_ARROW;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlButton()
        {
            var defaultPadding = new MyGuiBorderThickness()
            {
                Left   = 7f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                Right  = 5f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                Top    = 6f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                Bottom = 10f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
            };
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlButtonStyleEnum>() + 1];
            m_styles[(int)MyGuiControlButtonStyleEnum.Default] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_HIGHLIGHT,
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
                Padding          = defaultPadding,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Small] = new StyleDefinition()
            {
                NormalTexture    = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL.LeftTop },
                HighlightTexture = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_HIGHLIGHT.LeftTop },
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
                SizeOverride     = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL.MinSizeGui * 0.75f,
                Padding          = defaultPadding,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Tiny] = new StyleDefinition()
            {
                NormalTexture = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_SWITCHONOFF_LEFT_NORMAL.LeftTop },
                HighlightTexture = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_SWITCHONOFF_LEFT_HIGHLIGHT.LeftTop },
                NormalFont = MyFontEnum.Blue,
                HighlightFont = MyFontEnum.White,
                SizeOverride = MyGuiConstants.TEXTURE_SWITCHONOFF_LEFT_NORMAL.MinSizeGui * 0.75f,
                Padding = defaultPadding,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.ClickableText] = new StyleDefinition()
            {
                NormalFont    = MyFontEnum.Blue,
                HighlightFont = MyFontEnum.White,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.UrlText] = new StyleDefinition()
            {
                NormalFont = MyFontEnum.UrlNormal,
                HighlightFont = MyFontEnum.UrlHighlight,
                Underline = @"Textures\GUI\Underline.dds",
                UnderlineHighlight = @"Textures\GUI\UnderlineHighlight.dds",
                MouseOverCursor = MyGuiConstants.CURSOR_HAND,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Red] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_RED_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_RED_HIGHLIGHT,
                NormalFont       = MyFontEnum.Red,
                HighlightFont    = MyFontEnum.White,
                Padding          = defaultPadding,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Close] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_CLOSE_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_CLOSE_HIGHLIGHT,
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Info] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_INFO_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_INFO_HIGHLIGHT,
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.InventoryTrash] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_INVENTORY_TRASH_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_INVENTORY_TRASH_HIGHLIGHT,
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Debug] = new StyleDefinition()
            {
                NormalTexture    = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL.LeftTop },
                HighlightTexture = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_HIGHLIGHT.LeftTop },
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
                SizeOverride     = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL.MinSizeGui * new Vector2(0.55f, 0.65f),
                Padding          = defaultPadding,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.ControlSetting] = new StyleDefinition()
            {
                NormalTexture    = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL.LeftTop },
                HighlightTexture = new MyGuiCompositeTexture() { Center = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_HIGHLIGHT.LeftTop },
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
                SizeOverride     = MyGuiConstants.TEXTURE_BUTTON_DEFAULT_NORMAL.MinSizeGui * new Vector2(0.5f, 0.8f),
                Padding          = defaultPadding,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Increase] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_INCREASE,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_INCREASE,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Decrease] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_DECREASE,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_DECREASE,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Rectangular] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
                HighlightTexture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                NormalFont       = MyFontEnum.Blue,
                HighlightFont    = MyFontEnum.White,
                Padding          = new MyGuiBorderThickness(5f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                                                            5f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.ArrowLeft] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_ARROW_LEFT,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_ARROW_LEFT_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.ArrowRight] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_ARROW_RIGHT,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_ARROW_RIGHT_HIGHLIGHT,
            };
            m_styles[(int)MyGuiControlButtonStyleEnum.Square] = new StyleDefinition()
            {
                NormalTexture = MyGuiConstants.TEXTURE_BUTTON_SQUARE_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_SQUARE_HIGHLIGHT,
            };

            m_styles[(int)MyGuiControlButtonStyleEnum.SquareSmall] = new StyleDefinition()
            {
                NormalTexture = MyGuiConstants.TEXTURE_BUTTON_SQUARE_SMALL_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_SQUARE_SMALL_HIGHLIGHT,
            };

            m_styles[(int)MyGuiControlButtonStyleEnum.Error] = new StyleDefinition()
            {
                NormalTexture    = MyGuiConstants.TEXTURE_BUTTON_RED_NORMAL,
                HighlightTexture = MyGuiConstants.TEXTURE_BUTTON_RED_HIGHLIGHT,
                NormalFont       = MyFontEnum.ErrorMessageBoxText,
                HighlightFont    = MyFontEnum.White,
                Padding          = defaultPadding,
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlButtonStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion

        #region Fields

        private bool m_readyToClick = false;
        private string m_text;
        private MyStringId m_textEnum;
        private Vector2 m_textOffset;
        private float m_textScale;
        private float m_buttonScale = 1.0f;
        private bool m_activateOnMouseRelease = false;

        private float m_iconRotation;
        public float IconRotation
        {
            get { return m_iconRotation; }
            set { m_iconRotation = value; RefreshInternals(); }
        }

        public event Action<MyGuiControlButton> ButtonClicked;

        protected bool m_implementedFeature;

        private StringBuilder m_drawText = new StringBuilder();

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

        #endregion

        #region Constructors

        public MyGuiControlButton() : this(position: null) { }

        public MyGuiControlButton(
            Vector2? position                         = null,
            MyGuiControlButtonStyleEnum visualStyle   = MyGuiControlButtonStyleEnum.Default,
            Vector2? size                             = null,
            Vector4? colorMask                        = null,
            MyGuiDrawAlignEnum originAlign            = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            String toolTip                     = null,
            StringBuilder text                        = null,
            float textScale                           = MyGuiConstants.DEFAULT_TEXT_SCALE,
            MyGuiDrawAlignEnum textAlignment          = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            MyGuiControlHighlightType highlightType   = MyGuiControlHighlightType.WHEN_ACTIVE,
            bool implementedFeature                   = true,
            Action<MyGuiControlButton> onButtonClick  = null,
            GuiSounds cueEnum                   = GuiSounds.MouseClick,
            float buttonScale                         = 1.0f,
            int? buttonIndex = null,
            bool activateOnMouseRelease               = false)
            : base( position:        position ?? Vector2.Zero,
                    size:            size,
                    colorMask:       colorMask ?? MyGuiConstants.BUTTON_BACKGROUND_COLOR,
                    toolTip:         toolTip,
                    highlightType:   highlightType,
                    originAlign:     originAlign,
                    canHaveFocus:    implementedFeature)
        {
            Name                             = "Button";
            ButtonClicked                    = onButtonClick;
            Index                            = buttonIndex ?? 0;
            m_implementedFeature             = implementedFeature;
            UpdateText();

            m_drawText.Clear().Append(text);
            TextScale = textScale;
            TextAlignment = textAlignment;

            VisualStyle = visualStyle;
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
                    (HasFocus &&  (MyInput.Static.IsNewKeyPressed(MyKeys.Enter) ||
                                  MyInput.Static.IsNewKeyPressed(MyKeys.Space)))))
                {
                    if (m_implementedFeature == false)
                    {
                        //MyAudio.Static.PlayCue(m_cueEnum);
                        //MyGuiManager2.AddScreen(new MyGuiScreenMessageBox(
                        //    messageText: MyTextsWrapper.Get(MyStringId.FeatureNotYetImplemented),
                        //    messageCaption: MyTextsWrapper.Get(MyStringId.MessageBoxCaptionFeatureDisabled)));
                    }
                    else if (Enabled)
                    {
                        MyGuiSoundManager.PlaySound(m_cueEnum);
                        if (ButtonClicked != null)
                            ButtonClicked(this);
                    }

                    captureInput = this;
                    m_readyToClick = false;
                }
            }

            return captureInput;
        }

        public override string GetMouseCursorTexture()
        {
            if (IsMouseOver)
            {
                return m_styleDef.MouseOverCursor;
            }
            else
            {
                return MyGuiConstants.CURSOR_ARROW;
            }
        }

        protected void RaiseButtonClicked()
        {
            if (ButtonClicked != null)
                ButtonClicked(this);
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, transitionAlpha);

            bool isNotImplementedForbidenOrDisabled = !m_implementedFeature || !Enabled;
            Vector4 backgroundColor, textColor;
            backgroundColor = isNotImplementedForbidenOrDisabled ? ColorMask * MyGuiConstants.DISABLED_CONTROL_COLOR_MASK_MULTIPLIER
                                                                 : ColorMask;

            textColor = isNotImplementedForbidenOrDisabled ? MyGuiConstants.DISABLED_CONTROL_COLOR_MASK_MULTIPLIER
                                                           : Vector4.One;

            // Draw cross texture 
            if (isNotImplementedForbidenOrDisabled && DrawCrossTextureWhenDisabled)
            {
                MyGuiManager.DrawSpriteBatch(MyGuiConstants.BUTTON_LOCKED, GetPositionAbsolute(), Size * MyGuiConstants.LOCKBUTTON_SIZE_MODIFICATION,
                    MyGuiConstants.DISABLED_BUTTON_COLOR, OriginAlign);
            }

            var topLeft = GetPositionAbsoluteTopLeft();
            var internalTopLeft = topLeft + m_internalArea.Position;
            var internalSize = m_internalArea.Size;
            if (Icon.HasValue)
            {
                var iconPosition = GetPositionAbsoluteCenter();
                var icon = Icon.Value;
                var ratios = Vector2.Min(icon.SizeGui, internalSize) / icon.SizeGui;
                float scale = Math.Min(ratios.X, ratios.Y);
                MyGuiManager.DrawSpriteBatch((HasHighlight) ? icon.Highlight : icon.Normal, iconPosition, icon.SizeGui * scale, Color.White, IconOriginAlign, IconRotation);
            }

            if (m_drawText.Length > 0 && TextScaleWithLanguage > 0)
            {
                Vector2 textPosition = MyUtils.GetCoordAlignedFromTopLeft(internalTopLeft, m_internalArea.Size, TextAlignment);
                MyGuiManager.DrawString(TextFont, m_drawText, textPosition, TextScaleWithLanguage, ApplyColorMaskModifiers(textColor, Enabled, transitionAlpha), TextAlignment);
            }

            if (m_styleDef.Underline != null)
            {
                var underlinePos = topLeft;
                underlinePos.Y += Size.Y;
                var underlineSize = new Vector2(Size.X, MyGuiManager.GetNormalizedSizeFromScreenSize(new Vector2(0, 2)).Y);
                MyGuiManager.DrawSpriteBatch(HasHighlight ? m_styleDef.UnderlineHighlight : m_styleDef.Underline, underlinePos, underlineSize, Color.White, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
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

        public int Index
        {
            get;
            private set;
        }

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
            set
            {
                m_buttonScale = value;
            }
        }

        private float m_textScaleWithLanguage;
        public float TextScaleWithLanguage
        {
            get { return m_textScaleWithLanguage; }
            private set { m_textScaleWithLanguage = value; }
        }

        public MyGuiDrawAlignEnum TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

        public MyFontEnum TextFont;

        public bool DrawCrossTextureWhenDisabled = false;

        public MyGuiControlButtonStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlButtonStyleEnum m_visualStyle;
        private StyleDefinition m_styleDef;

        public MyGuiHighlightTexture? Icon;

        public MyGuiDrawAlignEnum IconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

        #endregion

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var objectBuilder = (MyObjectBuilder_GuiControlButton)base.GetObjectBuilder();

            objectBuilder.Text                         = Text;
            objectBuilder.TextEnum                     = m_textEnum.ToString();
            objectBuilder.TextScale                    = TextScale;
            objectBuilder.TextAlignment                = (int)TextAlignment;
            objectBuilder.DrawCrossTextureWhenDisabled = DrawCrossTextureWhenDisabled;
            objectBuilder.VisualStyle                  = VisualStyle;

            return objectBuilder;
        }

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            var buttonObjectBuilder = (MyObjectBuilder_GuiControlButton)objectBuilder;

            Text                         = buttonObjectBuilder.Text;
            m_textEnum                   = MyStringId.GetOrCompute(buttonObjectBuilder.TextEnum);
            TextScale                    = buttonObjectBuilder.TextScale;
            TextAlignment                = (MyGuiDrawAlignEnum)buttonObjectBuilder.TextAlignment;
            DrawCrossTextureWhenDisabled = buttonObjectBuilder.DrawCrossTextureWhenDisabled;
            VisualStyle                  = buttonObjectBuilder.VisualStyle;

            UpdateText();
        }

        protected override bool ShouldHaveHighlight()
        {
            if(HighlightType == MyGuiControlHighlightType.FORCED)
                return Selected;
            else
                return base.ShouldHaveHighlight();
        }

        protected override void OnHasHighlightChanged()
        {
            RefreshVisualStyle();
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

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            ColorMask = m_styleDef.BackgroundColor;
            if (HasHighlight || Checked)
            {
                BackgroundTexture = m_styleDef.HighlightTexture;
                TextFont          = m_styleDef.HighlightFont;
            }
            else
            {
                BackgroundTexture = m_styleDef.NormalTexture;
                TextFont          = m_styleDef.NormalFont;
            }
            var size = Size;
            if (BackgroundTexture != null)
            {
                MinSize = BackgroundTexture.MinSizeGui;
                MaxSize = BackgroundTexture.MaxSizeGui;
                if (ButtonScale == 1.0f)
                    size = m_styleDef.SizeOverride ?? size;
            }
            else
            {
                MinSize = Vector2.Zero;
                MaxSize = Vector2.PositiveInfinity;
                size = m_styleDef.SizeOverride ?? Vector2.Zero;
            }

            // No size specified, but we have string and font ... probably its a clickable text so let's use that as size.
            if (size == Vector2.Zero && m_drawText != null)
            {
                size = MyGuiManager.MeasureString(TextFont, m_drawText, TextScaleWithLanguage);
            }

            var padding = m_styleDef.Padding;
            m_internalArea.Position = padding.TopLeftOffset;
            m_internalArea.Size = Size - padding.SizeChange;
            Size = size;
        }
    }
}
