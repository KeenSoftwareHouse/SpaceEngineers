using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
#if !XB1
using System.Windows.Forms;
#endif
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

//  Textbox GUI control. Supports MaxLength.
//  Supports horisontaly scrollable texts - if text is longer than textbox width, the text will scroll from left to right.
//  This is accomplished by having sliding window and using stencil buffer to cut out invisible text - although it won't be optimal for extremely long texts.
//  Vertical scroling is not yet supported. Also password asterisks are supported. Selection, copy-paste is not supported yet.

namespace Sandbox.Graphics.GUI
{
    public enum MyGuiControlTextboxType : byte
    {
        Normal,
        Password,
        DigitsOnly,
    }

    public enum MyGuiControlTextboxStyleEnum
    {
        Default,
        Debug,
        Custom
    }

    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlTextbox))]
    public class MyGuiControlTextbox : MyGuiControlBase
    {
        #region Styles and static data
        public class StyleDefinition
        {
            public string NormalFont;
            public string HighlightFont;
            public MyGuiCompositeTexture NormalTexture;
            public MyGuiCompositeTexture HighlightTexture;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlTextbox()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlTextboxStyleEnum>() + 1];
            m_styles[(int)MyGuiControlTextboxStyleEnum.Default] = new StyleDefinition()
            {
                NormalTexture = MyGuiConstants.TEXTURE_TEXTBOX,
                HighlightTexture = MyGuiConstants.TEXTURE_TEXTBOX_HIGHLIGHT,
                NormalFont = MyFontEnum.Blue,
                HighlightFont = MyFontEnum.White,
            };

            m_styles[(int)MyGuiControlTextboxStyleEnum.Debug] = new StyleDefinition()
            {
                NormalTexture = MyGuiConstants.TEXTURE_TEXTBOX,
                HighlightTexture = MyGuiConstants.TEXTURE_TEXTBOX_HIGHLIGHT,
                NormalFont = MyFontEnum.Debug,
                HighlightFont = MyFontEnum.Debug
            };

            m_keyThrottler = new MyKeyThrottler();
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlTextboxStyleEnum style)
        {
            return m_styles[(int)style];
        }

        #endregion

        #region Private fields
        private int m_carriageBlinkerTimer;
        private int m_carriagePositionIndex;
        private bool m_drawBackground;
        private bool m_formattedAlready;
        private int m_maxLength;
        private List<MyKeys> m_pressedKeys = new List<MyKeys>(10);
        private Vector4 m_textColor;
        private float m_textScale;
        private float m_textScaleWithLanguage;
        private bool m_hadFocusLastTime;
        private float m_slidingWindowOffset;
        private MyRectangle2D m_textAreaRelative;
        private MyGuiCompositeTexture m_compositeBackground;
        private StringBuilder m_text = new StringBuilder();
        private MyGuiControlTextboxSelection m_selection = new MyGuiControlTextboxSelection();
        
        #endregion

        #region Properties
        public int MaxLength
        {
            get { return m_maxLength; }
            set
            {
                m_maxLength = value;
                if (m_text.Length > m_maxLength)
                    m_text.Remove(m_maxLength, m_text.Length - m_maxLength);
            }
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

        public float TextScaleWithLanguage
        {
            get { return m_textScaleWithLanguage; }
            private set { m_textScaleWithLanguage = value; }
        }

        /// <summary>
        /// When setting text to textbox, make sure you won't set it to unsuported charact
        /// </summary>
        [Obsolete("Do not use this, it allocates! Use SetText instead!")]
        public string Text
        {
            get { return m_text.ToString(); }

            set
            {
                //  Fix text so it will contain only allowed characters and reduce it if it's too long
                m_text.Clear().Append(value);
                if (CarriagePositionIndex >= m_text.Length)
                {
                    CarriagePositionIndex = m_text.Length;
                }
                OnTextChanged();
            }
        }

        public bool TextEquals(StringBuilder text)
        {
            return m_text.CompareTo(text) == 0;
        }

        public void GetText(StringBuilder result)
        {
            result.AppendStringBuilder(m_text);
        }

        /// <summary>
        /// Copies string from source to internal string builder
        /// </summary>
        public void SetText(StringBuilder source)
        {
            m_text.Clear().AppendStringBuilder(source);
            if (CarriagePositionIndex >= m_text.Length)
            {
                CarriagePositionIndex = m_text.Length;
            }
            OnTextChanged();
        }

        public MyGuiControlTextboxType Type;

        public MyGuiControlTextboxStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlTextboxStyleEnum m_visualStyle;
        private StyleDefinition m_styleDef;
        private StyleDefinition m_customStyle;
        private bool m_useCustomStyle;
        private static MyKeyThrottler m_keyThrottler;

        protected int CarriagePositionIndex
        {
            get { return m_carriagePositionIndex; }
            private set
            {
                m_carriagePositionIndex = MathHelper.Clamp(value, 0, Text.Length);
            }
        }
        #endregion

        private void RefreshVisualStyle()
        {
            if (m_useCustomStyle)
            {
                m_styleDef = m_customStyle;
            }
            else
            {
                m_styleDef = GetVisualStyle(VisualStyle);
            }
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            if (HasHighlight)
            {
                m_compositeBackground = m_styleDef.HighlightTexture;
                MinSize = m_compositeBackground.MinSizeGui;
                MaxSize = m_compositeBackground.MaxSizeGui;
                TextFont = m_styleDef.HighlightFont;
            }
            else
            {
                m_compositeBackground = m_styleDef.NormalTexture;
                MinSize = m_compositeBackground.MinSizeGui;
                MaxSize = m_compositeBackground.MaxSizeGui;
                TextFont = m_styleDef.NormalFont;
            }
            RefreshTextArea();
        }

        public struct MySkipCombination
        {
            public bool Alt;
            public bool Ctrl;
            public bool Shift;
            public MyKeys[] Keys;
        }

        public MySkipCombination[] SkipCombinations
        {
            get;
            set;
        }

        #region Events
        public event Action<MyGuiControlTextbox> TextChanged;
        public event Action<MyGuiControlTextbox> EnterPressed;
        #endregion

        #region Construction and serialization
        public MyGuiControlTextbox() : this(position: null) { }

        public MyGuiControlTextbox(
            Vector2? position            = null,
            string defaultText           = null,
            int maxLength                = 512,
            Vector4? textColor           = null,
            float textScale              = MyGuiConstants.DEFAULT_TEXT_SCALE,
            MyGuiControlTextboxType type = MyGuiControlTextboxType.Normal,
            MyGuiControlTextboxStyleEnum visualStyle = MyGuiControlTextboxStyleEnum.Default)
            : base(position: position,
                   canHaveFocus: true,
                   size: new Vector2(512f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
        {
            Name                    = "Textbox";
            Type                    = type;
            m_carriagePositionIndex = 0;
            m_carriageBlinkerTimer  = 0;
            m_textColor             = textColor ?? Vector4.One;
            TextScale               = textScale;
            m_maxLength             = maxLength;
            Text                    = defaultText ?? "";
            m_visualStyle           = visualStyle;
            RefreshVisualStyle();
            m_slidingWindowOffset = 0f;
        }

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            m_slidingWindowOffset = 0f;
            m_carriagePositionIndex = 0;
        }

        #endregion

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            if (!Visible)
                return;

            m_compositeBackground.Draw(GetPositionAbsoluteTopLeft(), Size, ApplyColorMaskModifiers(ColorMask, Enabled, backgroundTransitionAlpha));

            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            var textAreaRelative = m_textAreaRelative;
            var textArea = textAreaRelative;
            textArea.LeftTop += GetPositionAbsoluteTopLeft();
            float carriageOffset = GetCarriageOffset(CarriagePositionIndex);

            var scissor = new RectangleF(textArea.LeftTop, textArea.Size);
            using (MyGuiManager.UsingScissorRectangle(ref scissor))
            {
                RefreshSlidingWindow();

                //Draws selection background, if any
                if (m_selection.Length > 0)
                {
                    float normalizedWidth = GetCarriageOffset(m_selection.End) - GetCarriageOffset(m_selection.Start);
                    MyGuiManager.DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE,
                        new Vector2(textArea.LeftTop.X + GetCarriageOffset(m_selection.Start), textArea.LeftTop.Y),
                        new Vector2(normalizedWidth, textArea.Size.Y),
                        ApplyColorMaskModifiers(new Vector4(1, 1, 1, 0.5f), Enabled, transitionAlpha),
                        MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
                   );
                }

                //  Draw text in textbox
                MyGuiManager.DrawString(TextFont,
                    new StringBuilder(GetModifiedText()),
                    new Vector2(textArea.LeftTop.X + m_slidingWindowOffset, textArea.LeftTop.Y),
                    TextScaleWithLanguage,
                    ApplyColorMaskModifiers(m_textColor, Enabled, transitionAlpha),
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

                //  Draw carriage line
                //  Carriage blinker time is solved here in Draw because I want to be sure it will be drawn even in low FPS
                if (HasFocus)
                {
                    //  This condition controls "blinking", so most often is carrier visible and blinks very fast
                    //  It also depends on FPS, but as we have max FPS set to 60, it won't go faster, nor will it omit a "blink".
                    int carriageInterval = m_carriageBlinkerTimer % 20;
                    if ((carriageInterval >= 0) && (carriageInterval <= 15))
                    {
                        MyGuiManager.DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE,
                            new Vector2(textArea.LeftTop.X + carriageOffset, textArea.LeftTop.Y),
                            1,
                            textArea.Size.Y,
                            ApplyColorMaskModifiers(Vector4.One, Enabled, transitionAlpha),
                            MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                    }
                }
                m_carriageBlinkerTimer++;
            }

            //DebugDraw();
        }

        private void DebugDraw()
        {
            var textArea = m_textAreaRelative;
            textArea.LeftTop += GetPositionAbsoluteTopLeft();
            MyGuiManager.DrawBorders(textArea.LeftTop, textArea.Size, Color.White, 1);

        }

        /// <summary>
        /// gets the position of the first space to the left of the carriage or 0 if there isn't any
        /// </summary>
        /// <returns></returns>
        private int GetPreviousSpace()
        {
            if (CarriagePositionIndex == 0)
                return 0;

            int output = m_text.ToString().Substring(0, CarriagePositionIndex).LastIndexOf(" ");
            if (output == -1)
                return 0;

            return output;
        }

        /// <summary>
        /// gets the position of the first space to the right of the carriage or the text length if there isn't any
        /// </summary>
        /// <returns></returns>
        private int GetNextSpace()
        {
            if (CarriagePositionIndex == m_text.Length)
                return m_text.Length;

            int output = m_text.ToString().Substring(CarriagePositionIndex+1).IndexOf(" ");
            if (output == -1)
                return m_text.Length;

            return CarriagePositionIndex + output+1;
        }



        /// <summary>
        /// Method returns true if input was captured by control, so no other controls, nor screen can use input in this update
        /// </summary>
        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase ret = base.HandleInput();

            try
            {
                if (ret == null && Enabled)
                {
                    if (MyInput.Static.IsNewLeftMousePressed())
                    {
                        if (IsMouseOver)
                        {
                            m_selection.Dragging = true;
                            CarriagePositionIndex = GetCarriagePositionFromMouseCursor();
                            if (MyInput.Static.IsAnyShiftKeyPressed())
                                m_selection.SetEnd(this);
                            else
                                m_selection.Reset(this);
                            ret = this;
                        }
                        else
                            m_selection.Reset(this);
                    }
                    else if (MyInput.Static.IsNewLeftMouseReleased())
                    {
                        m_selection.Dragging = false;
                    }
                    else if (m_selection.Dragging)
                    {
                        CarriagePositionIndex = GetCarriagePositionFromMouseCursor();
                        m_selection.SetEnd(this);
                        ret = this;
                    }

                    if (HasFocus)
                    {
                        if(!MyInput.Static.IsAnyCtrlKeyPressed())
                            HandleTextInputBuffered(ref ret);

                        //  Move left
                        if (m_keyThrottler.GetKeyStatus(MyKeys.Left) == ThrottledKeyStatus.PRESSED_AND_READY)
                        {
                            if (MyInput.Static.IsAnyCtrlKeyPressed())
                                CarriagePositionIndex = GetPreviousSpace();
                            else
                                CarriagePositionIndex--;

                            if (MyInput.Static.IsAnyShiftKeyPressed())
                                m_selection.SetEnd(this);
                            else
                                m_selection.Reset(this);

                            ret = this;
                        }

                        //  Move right
                        if (m_keyThrottler.GetKeyStatus(MyKeys.Right) == ThrottledKeyStatus.PRESSED_AND_READY)
                        {
                            if (MyInput.Static.IsAnyCtrlKeyPressed())
                                CarriagePositionIndex = GetNextSpace();
                            else
                                CarriagePositionIndex++;

                            if (MyInput.Static.IsAnyShiftKeyPressed())
                                m_selection.SetEnd(this);
                            else
                                m_selection.Reset(this);

                            ret = this;
                        }

                        //  Move home
                        if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.Home))
                        {
                            CarriagePositionIndex = 0;

                            if (MyInput.Static.IsAnyShiftKeyPressed())
                                m_selection.SetEnd(this);
                            else
                                m_selection.Reset(this);

                            ret = this;
                        }

                        //  Move end
                        if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.End))
                        {
                            CarriagePositionIndex = m_text.Length;
                            
                            if (MyInput.Static.IsAnyShiftKeyPressed())
                                m_selection.SetEnd(this);
                            else
                                m_selection.Reset(this);
                            
                            ret = this;
                        }

                        //Cut selected text
                        if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.X) && MyInput.Static.IsAnyCtrlKeyPressed())
                        {
                            m_selection.CutText(this);
                        }

                        //Copy
                        if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.C) && MyInput.Static.IsAnyCtrlKeyPressed())
                        {
                            m_selection.CopyText(this);
                        }

                        //Paste
                        if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.V) && MyInput.Static.IsAnyCtrlKeyPressed())
                        {
                            m_selection.PasteText(this);
                        }

                        //Select All
                        if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.A) && MyInput.Static.IsAnyCtrlKeyPressed())
                        {
                            m_selection.SelectAll(this);
                        }

                        if (MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
                        {
                            if (EnterPressed != null)
                            {
                                EnterPressed(this);
                            }
                        }

                        m_formattedAlready = false;
                    }
                    else
                    {
                        if (Type == MyGuiControlTextboxType.DigitsOnly && m_formattedAlready == false && m_text.Length != 0)
                        {
                            var number = MyValueFormatter.GetDecimalFromString(Text, 1);
                            int decimalDigits = (number - Decimal.Truncate(number) > 0) ? 1 : 0;
                            m_text.Clear().Append(MyValueFormatter.GetFormatedFloat((float)number, decimalDigits, ""));
                            CarriagePositionIndex = m_text.Length;
                            m_formattedAlready = true;
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException) // mkTODO: Why? Handle correctly
            {
            }

            m_hadFocusLastTime = HasFocus;
            return ret;
        }
        public bool IsSkipCharacter(MyKeys character)
        {
            if (SkipCombinations != null)
                foreach (var skipCombination in SkipCombinations)
                {
                    if (skipCombination.Alt == MyInput.Static.IsAnyAltKeyPressed() &&
                        skipCombination.Ctrl == MyInput.Static.IsAnyCtrlKeyPressed() &&
                        skipCombination.Shift == MyInput.Static.IsAnyShiftKeyPressed() &&
                        (skipCombination.Keys == null ||
                        skipCombination.Keys.Contains((MyKeys)character)))
                    {
                        return true;
                    }
                }
            return false;
        }
        private void HandleTextInputBuffered(ref MyGuiControlBase ret)
        {
            const char BACKSPACE = '\b';
            bool textChanged = false;
            foreach (var character in MyInput.Static.TextInput)
            {
                if (IsSkipCharacter((MyKeys)character))
                    continue;
                if (Char.IsControl(character))
                {
                    if (character == BACKSPACE)
                    {
                        if (m_selection.Length == 0)
                            ApplyBackspace();
                        else
                            m_selection.EraseText(this);
                        
                        textChanged = true;
                    }
                }
                else
                {
                    if (m_selection.Length > 0)
                        m_selection.EraseText(this);

                    InsertChar(character);
                    textChanged = true;
                }
            }

            // Unbuffered Delete because it's not delivered as a message through Win32 message loop.
            if (m_keyThrottler.GetKeyStatus(MyKeys.Delete) == ThrottledKeyStatus.PRESSED_AND_READY)
            {
                if (m_selection.Length == 0)
                    ApplyDelete();
                else
                    m_selection.EraseText(this);

                textChanged = true;
            }

            if (textChanged)
            {
                OnTextChanged();
                ret = this;
            }
        }

        private void InsertChar(char character)
        {
            if (m_text.Length >= m_maxLength)
                return;

            m_text.Insert(CarriagePositionIndex, character);
            ++CarriagePositionIndex;
        }

        private void ApplyBackspace()
        {
            if (CarriagePositionIndex > 0)
            {
                --CarriagePositionIndex;
                m_text.Remove(CarriagePositionIndex, 1);
            }
        }

        private void ApplyDelete()
        {
            if (CarriagePositionIndex < m_text.Length)
            {
                m_text.Remove(CarriagePositionIndex, 1);
            }
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            RefreshInternals();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshTextArea();
            RefreshSlidingWindow();
        }

        internal override void OnFocusChanged(bool focus)
        {
            if (focus)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Tab))     // If we got here by Tab key
                {
                    // Highlight the text in the text box when tabbed to
                    MoveCarriageToEnd();
                    m_selection.SelectAll(this);
                }
            }
            else
            {
                // Clear the selection when leaving
                m_selection.Reset(this);
            }

            base.OnFocusChanged(focus);
        }

        public void MoveCarriageToEnd()
        {
            CarriagePositionIndex = m_text.Length;
        }

        #region Private helpers

        /// <summary>
        /// Converts carriage (or just char) position to normalized coordinates
        /// </summary>
        private float GetCarriageOffset(int index)
        {
            string leftFromCarriageText = GetModifiedText().Substring(0, index);
            Vector2 leftFromCarriageSize = MyGuiManager.MeasureString(MyFontEnum.Blue, new StringBuilder(leftFromCarriageText), TextScaleWithLanguage);
            return leftFromCarriageSize.X + m_slidingWindowOffset;
        }

        /// <summary>
        /// After user clicks on textbox, we will try to set carriage position where the cursor is
        /// </summary>
        private int GetCarriagePositionFromMouseCursor()
        {
            RefreshSlidingWindow();
            float mouseRelativeX = MyGuiManager.MouseCursorPosition.X - GetPositionAbsoluteTopLeft().X;

            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            for (int i = 0; i <= m_text.Length; i++)
            {
                float charPositionX = GetCarriageOffset(i);
                float charDistance = Math.Abs(mouseRelativeX - charPositionX);
                if (charDistance < closestDistance)
                {
                    closestDistance = charDistance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private void RefreshTextArea()
        {
            m_textAreaRelative = new MyRectangle2D(MyGuiConstants.TEXTBOX_TEXT_OFFSET, Size - 2 * MyGuiConstants.TEXTBOX_TEXT_OFFSET);
        }

        /// <summary>
        /// If type of textbox is password, this method returns asterisk. Otherwise original text.
        /// </summary>
        private string GetModifiedText()
        {
            switch (Type)
            {
                case MyGuiControlTextboxType.Normal:
                case MyGuiControlTextboxType.DigitsOnly:
                    return Text;

                case MyGuiControlTextboxType.Password:
                    return new String('*', m_text.Length);

                default:
                    Debug.Fail("Invalid branch reached.");
                    return Text;
            }
        }

        private void OnTextChanged()
        {
            if (TextChanged != null)
                TextChanged(this);

            RefreshSlidingWindow();
            m_selection.Reset(this);
        }

        /// <summary>
        /// If carriage is in current sliding window, then we don't change it. If it's over its left or right borders, we move sliding window.
        /// Of course on on X axis, Y is ignored at all.
        /// This method could be called from Update() or Draw() - it doesn't matter now
        /// </summary>
        private void RefreshSlidingWindow()
        {
            float carriageOffset = GetCarriageOffset(CarriagePositionIndex);
            var textArea = m_textAreaRelative;

            if (carriageOffset < 0)
            {
                m_slidingWindowOffset -= carriageOffset;
            }
            else if (carriageOffset > textArea.Size.X)
            {
                m_slidingWindowOffset -= carriageOffset - textArea.Size.X;
            }
        }

        #endregion

        private class MyGuiControlTextboxSelection
        {
            private int m_startIndex, m_endIndex;
            private string ClipboardText;
            private bool m_dragging = false;

            public bool Dragging
            {
                get { return m_dragging; }
                set { m_dragging = value; }
            }

            public MyGuiControlTextboxSelection()
            {
                m_startIndex = 0;
                m_endIndex = 0;
            }

            public int Start
            {
                get { return Math.Min(m_startIndex, m_endIndex); }
            }

            public int End
            {
                get { return Math.Max(m_startIndex, m_endIndex); }
            }

            public int Length
            {
                get { return End - Start;  }
            }

            public void SetEnd(MyGuiControlTextbox sender)
            {
                m_endIndex = MathHelper.Clamp(sender.CarriagePositionIndex, 0, sender.Text.Length);
            }


            public void Reset(MyGuiControlTextbox sender)
            {
                m_startIndex = m_endIndex = MathHelper.Clamp(sender.CarriagePositionIndex, 0, sender.Text.Length);
            }

            public void SelectAll(MyGuiControlTextbox sender)
            {
                m_startIndex = 0;
                m_endIndex = sender.Text.Length;
                sender.CarriagePositionIndex = sender.Text.Length;
            }

            public void EraseText(MyGuiControlTextbox sender)
            {
                if (Start == End)
                    return; 
                StringBuilder prefix = new StringBuilder(sender.Text.Substring(0, Start));
                StringBuilder suffix = new StringBuilder(sender.Text.Substring(End));
                sender.CarriagePositionIndex = Start;
                sender.Text = prefix.Append(suffix).ToString();
            }

            public void CutText(MyGuiControlTextbox sender)
            {
                //First off, we have to copy
                CopyText(sender);

                //Then we cut the text away from the form
                EraseText(sender);
            }

            public void CopyText(MyGuiControlTextbox sender)
            {
#if !XB1
                ClipboardText = sender.Text.Substring(Start, Length);

                if (!string.IsNullOrEmpty(ClipboardText))
                {
                    Thread thread = new Thread(() => System.Windows.Forms.Clipboard.SetText(ClipboardText));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                }
#else // XB1
                System.Diagnostics.Debug.Assert(false, "Not Clipboard support on XB1!");
#endif // XB1
            }

            void CopyToClipboard()
            {
#if !XB1
                if(ClipboardText != "")
                    Clipboard.SetText(ClipboardText);
#else
                Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
            }

            public void PasteText(MyGuiControlTextbox sender)
            {
#if !XB1
                //First we erase the selection
                EraseText(sender);
                
                var prefix = sender.Text.Substring(0, sender.CarriagePositionIndex);
                var suffix = sender.Text.Substring(sender.CarriagePositionIndex);
                
                Thread myth = new Thread(new System.Threading.ThreadStart(PasteFromClipboard));
                myth.ApartmentState = ApartmentState.STA;
                myth.Start();
                
                //We have to wait for the thread to end to make sure we got the text
                myth.Join();

                sender.Text = new StringBuilder(prefix).Append(ClipboardText).Append(suffix).ToString();
                sender.CarriagePositionIndex = prefix.Length + ClipboardText.Length;
                Reset(sender);
#else
                Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
            }

            void PasteFromClipboard()
            {
#if !XB1
                ClipboardText = Clipboard.GetText();
#else
                Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
            }
        }

        public string TextFont
        {
            get;
            private set;
        }

        /// <summary>
        /// GR: Use this to select all text outside for current textbox.
        /// </summary>
        public void SelectAll()
        {
            if (m_selection != null)
            {
                m_selection.SelectAll(this);
            }
        }

        public void ApplyStyle(StyleDefinition style)
        {
            m_useCustomStyle = true;
            m_customStyle = style;
            RefreshVisualStyle();
        }
    }
}
