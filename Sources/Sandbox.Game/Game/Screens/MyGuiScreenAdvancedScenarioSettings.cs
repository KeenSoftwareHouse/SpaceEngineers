using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenAdvancedScenarioSettings : MyGuiScreenBase
    {

        MyGuiScreenMissionTriggers m_parent;
        MyGuiControlButton m_okButton, m_cancelButton;
        MyGuiControlCheckbox m_canJoinRunning;

        public MyGuiScreenAdvancedScenarioSettings(MyGuiScreenMissionTriggers parent)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.9f, 0.9f))
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenAdvancedScenarioSettings.ctor START");

            m_parent = parent;
            EnabledBackgroundFade = true;

            RecreateControls(true);

            MySandboxGame.Log.WriteLine("MyGuiScreenAdvancedScenarioSettings.ctor END");
        }


        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            BuildControls();

        }

        public void BuildControls()
        {
            MyGuiControlParent parent = new MyGuiControlParent(size: new Vector2(Size.Value.X - 0.05f, Size.Value.Y-0.1f));
            MyGuiControlScrollablePanel scrollPanel = new MyGuiControlScrollablePanel(parent);
            scrollPanel.ScrollbarVEnabled = true;
            scrollPanel.Size = new Vector2(Size.Value.X - 0.05f, 0.8f);
            Controls.Add(scrollPanel);

            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.23f, 0.03f);

            //AddCaption(MySpaceTexts.ScreenCaptionAdvancedSettings);
            // Ok/Cancel
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: CancelButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            float buttonsOffset = 0.055f;

            var canJoinrunningLabel = MakeLabel(MySpaceTexts.ScenarioSettings_CanJoinRunning);
            m_canJoinRunning = new MyGuiControlCheckbox();
            m_canJoinRunning.Position = new Vector2(-scrollPanel.Size.X / 2 + buttonsOffset, -scrollPanel.Size.Y / 2 + buttonsOffset);
            canJoinrunningLabel.Position = new Vector2(m_canJoinRunning.Position.X + buttonsOffset, m_canJoinRunning.Position.Y);
            m_canJoinRunning.IsChecked = MySession.Static.Settings.CanJoinRunning;
            parent.Controls.Add(m_canJoinRunning);
            parent.Controls.Add(canJoinrunningLabel);

            CloseButtonEnabled = true;
        }
        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        private void CancelButtonClicked(object sender)
        {
            this.CloseScreen();
        }

        private void OkButtonClicked(object sender)
        {
            MySession.Static.Settings.CanJoinRunning=m_canJoinRunning.IsChecked;
            this.CloseScreen();
        }

        public void SetSettings(MyObjectBuilder_SessionSettings settings)
        {
            //=settings.CanJoinRunning;
        }
        public void GetSettings(MyObjectBuilder_SessionSettings settings)
        {
            //settings.CanJoinRunning=
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenAdvancedScenarioSettings";
        }
        public event System.Action OnOkButtonClicked;
    }
}
