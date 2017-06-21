using System;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public enum MyMessageBoxStyleEnum 
    {
        Error,
        Info,
    }

    //  Type of message box, that means what buttons do we display (only OK, YES and NO, or something else)
    public enum MyMessageBoxButtonsType
    {
        NONE,                   //  No buttons
        OK,                     //  Just OK button
        YES_NO,                 //  YES and NO buttons
        YES_NO_CANCEL,          //  YES, NO and CANCEL buttons
        YES_NO_TIMEOUT,         //  YES and NO buttons; And Timeout so if no pressed YES in selected time, message box ends as if NO was pressed        
        NONE_TIMEOUT,           // No buttons dialog dissapears after Timeout
    }

    public class MyGuiScreenMessageBox : MyGuiScreenBase
    {
        #region Style
        public class Style 
        {
            public MyGuiPaddedTexture BackgroundTexture;
            public string CaptionFont;
            public string TextFont;
            public MyGuiControlButtonStyleEnum ButtonStyle;
        }

        static readonly Style[] m_styles;

        static MyGuiScreenMessageBox() 
        {
            m_styles = new Style[MyUtils.GetMaxValueFromEnum<MyMessageBoxStyleEnum>() + 1];
            m_styles[(int)MyMessageBoxStyleEnum.Error] = new Style()
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_MESSAGEBOX_BACKGROUND_ERROR,
                CaptionFont       = MyFontEnum.ErrorMessageBoxCaption,
                TextFont          = MyFontEnum.ErrorMessageBoxText,
                ButtonStyle       = MyGuiControlButtonStyleEnum.Error,
            };
            m_styles[(int)MyMessageBoxStyleEnum.Info] = new Style()
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_MESSAGEBOX_BACKGROUND_INFO,
                CaptionFont       = MyFontEnum.InfoMessageBoxCaption,
                TextFont          = MyFontEnum.InfoMessageBoxText,
                ButtonStyle       = MyGuiControlButtonStyleEnum.Default,
            };

        }
        #endregion

        public enum ResultEnum
        {
            YES,        //  YES or OK
            NO,         //  NO
            CANCEL,     //  CANCEL or ESC
        }

        public bool CloseBeforeCallback { get; set; }
        public bool InstantClose { get; set; }
        public Action<ResultEnum> ResultCallback;

        private MyStringId m_yesButtonText;
        private MyStringId m_noButtonText;
        private MyStringId m_okButtonText;
        private MyStringId m_cancelButtonText;
        private MyMessageBoxButtonsType m_buttonType;
        private MyMessageBoxStyleEnum m_type;
        private int m_timeoutInMiliseconds;
        private int m_timeoutStartedTimeInMiliseconds;
        private MyGuiControlMultilineText m_messageBoxText;
        private MyGuiControlCheckbox m_showAgainCheckBox;
        private string m_formatText;
        private StringBuilder m_formattedCache;
        private Style m_style;
        private StringBuilder m_messageText;
        private StringBuilder m_messageCaption;
        private ResultEnum m_focusedResult;

        public new bool CanHideOthers
        { 
            get { return base.CanHideOthers; }
            set { base.CanHideOthers = value; }
        }

        public MyGuiScreenMessageBox(
            MyMessageBoxStyleEnum styleEnum,
            MyMessageBoxButtonsType buttonType,
            StringBuilder messageText,
            StringBuilder messageCaption,
            MyStringId okButtonText,
            MyStringId cancelButtonText,
            MyStringId yesButtonText,
            MyStringId noButtonText,
            Action<ResultEnum> callback,
            int timeoutInMiliseconds,
            ResultEnum focusedResult,
            bool canHideOthers,
            Vector2? size
            ):
            base(position: new Vector2(0.5f, 0.5f),
                 backgroundColor: null,
                 size: null,
                 isTopMostScreen: true,
                 backgroundTexture: null)
        {
            InstantClose = true;

            m_style = m_styles[(int)styleEnum];
            m_focusedResult = focusedResult;

            m_backgroundColor   = Vector4.One;
            m_backgroundTexture = m_style.BackgroundTexture.Texture;

            EnabledBackgroundFade = true;

            m_buttonType           = buttonType;
            m_okButtonText         = okButtonText;
            m_cancelButtonText     = cancelButtonText;
            m_yesButtonText        = yesButtonText;
            m_noButtonText         = noButtonText;
            ResultCallback         = callback;
            m_drawEvenWithoutFocus = true;
            CanBeHidden            = false;
            CanHideOthers = canHideOthers;

            // Size of the message box is given by its background.
            if (size.HasValue)
            {
                m_size = size;
            }
            else
            {
                m_size = m_style.BackgroundTexture.SizeGui;
            }

            m_messageText = messageText;
            m_messageCaption = messageCaption ?? new StringBuilder();

            RecreateControls(true);

            if (buttonType == MyMessageBoxButtonsType.YES_NO_TIMEOUT || buttonType == MyMessageBoxButtonsType.NONE_TIMEOUT)
            {
                m_timeoutStartedTimeInMiliseconds = MyGuiManager.TotalTimeInMilliseconds;
                m_timeoutInMiliseconds = timeoutInMiliseconds;
                m_formatText = messageText.ToString();
                m_formattedCache = new StringBuilder(m_formatText.Length);
            }
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            Vector2 buttonSize = MyGuiControlButton.GetVisualStyle(m_style.ButtonStyle).NormalTexture.MinSizeGui;
            Vector2 captionSize = MyGuiManager.MeasureString(m_style.CaptionFont, m_messageCaption, MyGuiConstants.DEFAULT_TEXT_SCALE);

            //  Message box caption
            var padding = m_style.BackgroundTexture.PaddingSizeGui;
            MyGuiControlLabel captionLabel = new MyGuiControlLabel(
                position: new Vector2(0, -0.5f * m_size.Value.Y + padding.Y),
                text: m_messageCaption.ToString(),
                font: m_style.CaptionFont,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            Controls.Add(captionLabel);

            //  Message box text
            m_messageBoxText = new MyGuiControlMultilineText(
                position: Vector2.Zero,
                size: new Vector2(m_size.Value.X - 2 * padding.X, m_size.Value.Y - (2 * padding.Y + captionSize.Y + buttonSize.Y)),
                backgroundColor: Vector4.One,
                contents: m_messageText,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                font: m_style.TextFont);

            Controls.Add(m_messageBoxText);

            //  Buttons
            float buttonY = 0.5f * m_size.Value.Y - padding.Y;
            float buttonOffsetX = 0.05f;
            MyGuiControlBase yesButton = null;
            MyGuiControlBase noButton = null;
            MyGuiControlBase cancelButton = null;
            switch (m_buttonType)
            {
                case MyMessageBoxButtonsType.NONE:
                case MyMessageBoxButtonsType.NONE_TIMEOUT:
                    break;

                case MyMessageBoxButtonsType.OK:
                    Controls.Add(yesButton = MakeButton(new Vector2(0, buttonY), m_style, m_okButtonText, OnYesClick, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM));
                    break;

                case MyMessageBoxButtonsType.YES_NO:
                case MyMessageBoxButtonsType.YES_NO_TIMEOUT:
                    Controls.Add(yesButton = MakeButton(new Vector2(-buttonOffsetX, buttonY), m_style, m_yesButtonText, OnYesClick, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM));
                    Controls.Add(noButton = MakeButton(new Vector2(buttonOffsetX, buttonY), m_style, m_noButtonText, OnNoClick, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM));
                    break;

                case MyMessageBoxButtonsType.YES_NO_CANCEL:
                    Controls.Add(yesButton = MakeButton(new Vector2(-(buttonOffsetX + buttonSize.X * 0.5f), buttonY), m_style, m_yesButtonText, OnYesClick, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM));
                    Controls.Add(noButton = MakeButton(new Vector2(0, buttonY), m_style, m_noButtonText, OnNoClick, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM));
                    Controls.Add(cancelButton = MakeButton(new Vector2((buttonOffsetX + buttonSize.X * 0.5f), buttonY), m_style, m_cancelButtonText, OnCancelClick, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM));
                    break;

                default:
                    throw new InvalidBranchException();
                    break;
            }

            switch (m_focusedResult)
            {
                case ResultEnum.YES:
                    FocusedControl = yesButton;
                    break;
                case ResultEnum.NO:
                    FocusedControl = noButton;
                    break;
                case ResultEnum.CANCEL:
                    FocusedControl = cancelButton;
                    break;
            }
        }

        private MyGuiControlButton MakeButton(Vector2 position, Style config, MyStringId text, Action<MyGuiControlButton> onClick, MyGuiDrawAlignEnum align)
        {
            return new MyGuiControlButton(
                    position: position,
                    text: MyTexts.Get(text),
                    onButtonClick: onClick,
                    visualStyle: config.ButtonStyle,
                    originAlign: align);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMessageBox";
        }

        public void OnYesClick(MyGuiControlButton sender)
        {
            OnClick(ResultEnum.YES);
        }
        
        public void OnNoClick(MyGuiControlButton sender)
        {
            OnClick(ResultEnum.NO);
        }

        public void OnCancelClick(MyGuiControlButton sender)
        {
            OnClick(ResultEnum.CANCEL);
        }

        private void OnClick(ResultEnum result)
        {
            if (CloseBeforeCallback)
            {
                CloseInternal();
                CallResultCallback(result);
            }
            else
            {
                CallResultCallback(result);
                CloseInternal();
            }
        }

        private void CloseInternal()
        {
            if (InstantClose)
                CloseScreenNow();
            else
                CloseScreen();
        }

        void CallResultCallback(ResultEnum val)
        {
            if (ResultCallback != null) ResultCallback(val);
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            if (m_buttonType == MyMessageBoxButtonsType.YES_NO_TIMEOUT || m_buttonType == MyMessageBoxButtonsType.NONE_TIMEOUT)
            {
                //  If timeout passed out, we need to call NO callback
                int deltaTime = MyGuiManager.TotalTimeInMilliseconds - m_timeoutStartedTimeInMiliseconds;
                if (deltaTime >= m_timeoutInMiliseconds)
                {
                    OnNoClick(null);
                }

                //  Update timeout number in message box label
                int timer = (int)MathHelper.Clamp((m_timeoutInMiliseconds - deltaTime) / 1000, 0, m_timeoutInMiliseconds / 1000);
                m_messageBoxText.Text = m_formattedCache.Clear().AppendFormat(m_formatText, timer.ToString());
            }

            return true;
        }

        protected override void Canceling()
        {
            base.Canceling();
            CallResultCallback(ResultEnum.CANCEL);
        }

        
    }
}
