using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.IO;
using VRage.FileSystem;
using VRage.Game;
using VRage.Input;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenDialogText: MyGuiScreenBase
    {
        MyGuiControlLabel m_captionLabel;
        MyGuiControlTextbox m_valueTextbox;
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;
        MyStringId m_caption;
        readonly string m_value;

        public event Action<string> OnConfirmed;

        public MyGuiScreenDialogText(string initialValue = null, MyStringId? caption = null)
        {
            m_value = initialValue ?? string.Empty;
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            m_caption = caption ?? MyCommonTexts.DialogAmount_SetValueCaption;
            RecreateControls(true);
        }
        
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDialogText";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            var fileName = MakeScreenFilepath("DialogText");
            var fsPath = Path.Combine(MyFileSystem.ContentPath, fileName);

            MyObjectBuilder_GuiScreen objectBuilder;
            MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_GuiScreen>(fsPath, out objectBuilder);
            Init(objectBuilder);

            m_valueTextbox = (MyGuiControlTextbox)Controls.GetControlByName("ValueTextbox");
            m_confirmButton  = (MyGuiControlButton)Controls.GetControlByName("ConfirmButton");
            m_cancelButton   = (MyGuiControlButton)Controls.GetControlByName("CancelButton");
            m_captionLabel = (MyGuiControlLabel)Controls.GetControlByName("CaptionLabel");
            m_captionLabel.Text = null;
            m_captionLabel.TextEnum = m_caption;

            m_confirmButton.ButtonClicked  += confirmButton_OnButtonClick;
            m_cancelButton.ButtonClicked   += cancelButton_OnButtonClick;

            m_valueTextbox.Text = m_value;
        }
    
        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            base.HandleUnhandledInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
                confirmButton_OnButtonClick(m_confirmButton);
        }

        void confirmButton_OnButtonClick(MyGuiControlButton sender)
        {
            if (OnConfirmed != null)
                OnConfirmed(m_valueTextbox.Text);
            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }
}
