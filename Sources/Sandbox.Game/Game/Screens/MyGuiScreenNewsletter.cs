using Sandbox.Engine.Networking;
using Sandbox.Graphics.GUI;
using System;
using System.Linq;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenNewsletter : MyGuiScreenBase
    {
        #region Fields
        MyGuiControlButton m_okBtn;
        MyGuiControlCheckbox m_hideCB;
        MyGuiControlTextbox m_emailTBox;
        #endregion

        #region Constructor
        public MyGuiScreenNewsletter()
            : base(size: new Vector2(0.5f, 0.5f), backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR)
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenNewsletter.ctor START");

            EnabledBackgroundFade = true;
            m_closeOnEsc = true;

            m_drawEvenWithoutFocus = true;
            CanHideOthers = false;
            CanBeHidden = false;
        }
        #endregion

        #region Public Methods
        public override void LoadContent()
        {
            base.LoadContent();

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            BuildControls();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenNewsletter";
        }
        #endregion

        #region Private Methods
        protected void BuildControls()
        {
            AddCaption(MyCommonTexts.ScreenCaptionNewsletter);

            var currentStatus = MySandboxGame.Config.NewsletterCurrentStatus;

            // Description - why do we need email
            var descriptionLbl = new MyGuiControlLabel(
                text: MyTexts.GetString(MyCommonTexts.ScreenNewsletterSubtitle), 
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                position: new Vector2(0, -0.12f));
            descriptionLbl.Autowrap(0.4f);

            // Email label
            var emailLbl = new MyGuiControlLabel(
                text: MyTexts.GetString(MyCommonTexts.ScreenNewsletterEmailLabel),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                position: new Vector2(-0.16f, 0));

            // text that says "do not show again"
            var hideLbl = new MyGuiControlLabel(
                text: MyTexts.GetString(MyCommonTexts.ScreenNewsletterNoInterestCheckbox),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                position: new Vector2(0.05f, 0.1f));


            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.25f, 0.03f);
            // OK button
            m_okBtn = new MyGuiControlButton(
                position: buttonsOrigin - new Vector2(0.01f, 0f), 
                size: buttonSize, 
                text: MyTexts.Get(MyCommonTexts.Ok), 
                onButtonClick: OnOkButtonClick, 
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_okBtn.Enabled = false;

            // Cancel button
            var cancelBtn = new MyGuiControlButton(
                position: buttonsOrigin + new Vector2(0.01f, 0f), 
                size: buttonSize, 
                text: MyTexts.Get(MyCommonTexts.Cancel), 
                onButtonClick: OnCancelButtonClick, 
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);


            // checkbox to hide
            Action<MyGuiControlCheckbox> onCheckboxChanged = (checkbox) => OnCheckedChanged();
            m_hideCB = new MyGuiControlCheckbox(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                position: new Vector2(0.15f, 0.1f));
            m_hideCB.IsChecked = currentStatus == Engine.Utils.MyConfig.NewsletterStatus.NotInterested;
            m_hideCB.IsCheckedChanged = onCheckboxChanged;

            // Email box
            m_emailTBox = new MyGuiControlTextbox(
                maxLength: 50,
                position: new Vector2(0.02f, 0));
            m_emailTBox.Size = new Vector2(0.3f, 0.3f);
            if (currentStatus == Engine.Utils.MyConfig.NewsletterStatus.EmailConfirmed ||
                currentStatus == Engine.Utils.MyConfig.NewsletterStatus.EmailNotConfirmed)
                m_emailTBox.Text = "****************";
            else if (currentStatus == Engine.Utils.MyConfig.NewsletterStatus.NotInterested)
                m_emailTBox.Enabled = false;
            m_emailTBox.TextChanged += emailTBox_TextChanged;


            if (currentStatus == Engine.Utils.MyConfig.NewsletterStatus.EmailNotConfirmed)
            {
                // No confirmation label
                var noConfirmationLbl = new MyGuiControlLabel(
                    text: "* " + MyTexts.GetString(MyCommonTexts.ScreenNewsletterConfirmationMessage),
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                    textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.6f,
                    position: new Vector2(0.01f, 0.03f));
                Elements.Add(noConfirmationLbl);
            }

            Elements.Add(descriptionLbl);
            Elements.Add(emailLbl);
            Elements.Add(m_emailTBox);
            Elements.Add(m_hideCB);
            Elements.Add(hideLbl);
            Elements.Add(m_okBtn);
            Elements.Add(cancelBtn);

            CloseButtonEnabled = true;
        }

        void emailTBox_TextChanged(MyGuiControlTextbox obj)
        {
            m_okBtn.Enabled = IsValidEmail(obj.Text);
        }

        private void OnCheckedChanged()
        {
            if (m_hideCB.IsChecked)
            {
                m_emailTBox.Enabled = false;
                m_okBtn.Enabled = true;
            }
            else
            {
                m_emailTBox.Enabled = true;
                m_okBtn.Enabled = IsValidEmail(m_emailTBox.Text);
            }
        }

        private bool IsValidEmail(string email)
        {
            // Quickly returns false in these cases
            if (!email.Contains('@') ||
                email.Contains('*'))
                return false;

            try
            {
                var correctEmail = new System.Net.Mail.MailAddress(email);
                return email == correctEmail.Address;
            }
            catch
            {
                return false;
            }
        }

        private void OnOkButtonClick(object sender)
        {
            if (m_hideCB.IsChecked)
            {
                MyEShop.SendInfo(string.Empty);
                MySandboxGame.Config.NewsletterCurrentStatus = Engine.Utils.MyConfig.NewsletterStatus.NotInterested;
            }
            else
            {
                MyEShop.SendInfo(m_emailTBox.Text);
                MySandboxGame.Config.NewsletterCurrentStatus = Engine.Utils.MyConfig.NewsletterStatus.EmailNotConfirmed;
            }

            MySandboxGame.Config.Save();
            CloseScreen();
        }

        private void OnCancelButtonClick(object sender)
        {
            CloseScreen();
        }
        #endregion
    }
}
