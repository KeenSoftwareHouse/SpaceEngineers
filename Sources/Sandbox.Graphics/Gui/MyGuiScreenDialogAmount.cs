using Sandbox.Graphics.GUI;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using VRage.FileSystem;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiScreenDialogAmount : MyGuiScreenBase
    {
        private static readonly string PARSING_ERROR = "Given amount is not a valid number.";
        private static readonly string RANGE_ERROR = "Amount should be between {0} and {1}.";

        MyGuiControlTextbox m_amountTextbox;
        MyGuiControlButton m_increaseButton;
        MyGuiControlButton m_decreaseButton;
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;
        MyGuiControlLabel m_errorLabel;
        StringBuilder m_textBuffer;
        MyStringId m_caption;

        bool m_parseAsInteger;
        float m_amountMin;
        float m_amountMax;
        float m_amount;

        public event Action<float> OnConfirmed;
        private MyGuiControlLabel m_captionLabel;

        /// <param name="min">Minimum allowed amount.</param>
        /// <param name="max">Maximum allowed amount.</param>
        /// <param name="minMaxDecimalDigits">Number of digits used from min and max value. Decimal places beyond this value are cut off (no rounding occurs).</param>
        /// <param name="parseAsInteger">True will ensure parsing as integer number (you cannot input decimal values). False will parse as decimal number.</param>
        public MyGuiScreenDialogAmount(float min, float max, MyStringId caption, int minMaxDecimalDigits = 3, bool parseAsInteger = false, float? defaultAmount = null) :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            m_textBuffer = new StringBuilder();
            m_amountMin = min;
            m_amountMax = max;
            m_amount = defaultAmount.HasValue ? defaultAmount.Value : max;
            m_parseAsInteger = parseAsInteger;
            m_caption = caption;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDialogAmount";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            var fileName = MakeScreenFilepath("DialogAmount");
            var fsPath = Path.Combine(MyFileSystem.ContentPath, fileName);

            MyObjectBuilder_GuiScreen objectBuilder;
            MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_GuiScreen>(fsPath, out objectBuilder);
            Init(objectBuilder);

            m_amountTextbox = (MyGuiControlTextbox)Controls.GetControlByName("AmountTextbox");
            m_increaseButton = (MyGuiControlButton)Controls.GetControlByName("IncreaseButton");
            m_decreaseButton = (MyGuiControlButton)Controls.GetControlByName("DecreaseButton");
            m_confirmButton = (MyGuiControlButton)Controls.GetControlByName("ConfirmButton");
            m_cancelButton = (MyGuiControlButton)Controls.GetControlByName("CancelButton");
            m_errorLabel = (MyGuiControlLabel)Controls.GetControlByName("ErrorLabel");
            m_captionLabel = (MyGuiControlLabel)Controls.GetControlByName("CaptionLabel");
            m_captionLabel.Text = null;
            m_captionLabel.TextEnum = m_caption;

            m_errorLabel.Visible = false;

            m_amountTextbox.TextChanged += amountTextbox_TextChanged;
            m_increaseButton.ButtonClicked += increaseButton_OnButtonClick;
            m_decreaseButton.ButtonClicked += decreaseButton_OnButtonClick;
            m_confirmButton.ButtonClicked += confirmButton_OnButtonClick;
            m_cancelButton.ButtonClicked += cancelButton_OnButtonClick;

            m_confirmButton.TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_cancelButton.TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

            RefreshAmountTextbox();
            //GR: in int have all text selected
            m_amountTextbox.SelectAll();
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            base.HandleUnhandledInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
                confirmButton_OnButtonClick(m_confirmButton);
        }

        private void RefreshAmountTextbox()
        {
            m_textBuffer.Clear();
            if (m_parseAsInteger)
                m_textBuffer.AppendInt32((int)m_amount);
            else
                m_textBuffer.AppendDecimalDigit(m_amount, 4);

            m_amountTextbox.TextChanged -= amountTextbox_TextChanged;
            m_amountTextbox.Text = m_textBuffer.ToString();
            m_amountTextbox.TextChanged += amountTextbox_TextChanged;
            m_amountTextbox.ColorMask = Vector4.One;
        }

        private bool TryParseAndStoreAmount(string text)
        {
            float newVal;
            if (MyUtils.TryParseWithSuffix(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out newVal))
            {
                m_amount = m_parseAsInteger ? (float)Math.Floor(newVal) : newVal;
                return true;
            }
            return false;
        }

        #region Event handlers
        void amountTextbox_TextChanged(MyGuiControlTextbox obj)
        {
            m_amountTextbox.ColorMask = Vector4.One;
            m_errorLabel.Visible = false;
        }

        void increaseButton_OnButtonClick(MyGuiControlButton sender)
        {
            if (!TryParseAndStoreAmount(m_amountTextbox.Text))
            {
                m_errorLabel.Text = PARSING_ERROR;
                m_errorLabel.Visible = true;
                m_amountTextbox.ColorMask = Color.Red.ToVector4();
                return;
            }

            ++m_amount;
            m_amount = MathHelper.Clamp(m_amount, m_amountMin, m_amountMax);
            RefreshAmountTextbox();
        }

        void decreaseButton_OnButtonClick(MyGuiControlButton sender)
        {
            if (!TryParseAndStoreAmount(m_amountTextbox.Text))
            {
                m_errorLabel.Text = PARSING_ERROR;
                m_errorLabel.Visible = true;
                m_amountTextbox.ColorMask = Color.Red.ToVector4();
                return;
            }

            --m_amount;
            m_amount = MathHelper.Clamp(m_amount, m_amountMin, m_amountMax);
            RefreshAmountTextbox();
        }

        void confirmButton_OnButtonClick(MyGuiControlButton sender)
        {
            if (!TryParseAndStoreAmount(m_amountTextbox.Text))
            {
                m_errorLabel.Text = PARSING_ERROR;
                m_errorLabel.Visible = true;
                m_amountTextbox.ColorMask = Color.Red.ToVector4();
                return;
            }

            if (m_amount > m_amountMax || m_amount < m_amountMin)
            {
                m_errorLabel.Text = string.Format(RANGE_ERROR, m_amountMin, m_amountMax);
                m_errorLabel.Visible = true;
                m_amountTextbox.ColorMask = Color.Red.ToVector4();
                return;
            }

            if (OnConfirmed != null)
                OnConfirmed(m_amount);
            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
        #endregion

    }
}
