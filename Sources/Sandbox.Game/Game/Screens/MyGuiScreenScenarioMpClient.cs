using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;

namespace Sandbox.Game.Screens
{
    class MyGuiScreenScenarioMpClient : MyGuiScreenScenarioMpBase
    {
        public void MySyncScenario_InfoAnswer(bool gameAlreadyRunning, bool canJoinGame)
        {
            if (canJoinGame)
            {
                m_startButton.Enabled = gameAlreadyRunning; //when a client joins into a running game, he needs to have start button enabled (with slightly different meaning than server)
            }
            else
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MySpaceTexts.GuiScenarioCannotJoinCaption),
                        messageText: MyTexts.Get(MySpaceTexts.GuiScenarioCannotJoin),
                        buttonType: MyMessageBoxButtonsType.OK,
                        canHideOthers: false,
                        callback:  (v) =>{Canceling();});
                MyScreenManager.AddScreen(messageBox);
                //start button stays disabled
            }
        }
        public MyGuiScreenScenarioMpClient()
        {
            Debug.Assert(!Sync.IsServer);
            m_startButton.Enabled = false;
            MySyncScenario.InfoAnswer += MySyncScenario_InfoAnswer;
            MySyncScenario.AskInfo();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            m_canJoinRunning.Enabled = false;
        }

        protected override void OnStartClicked(MyGuiControlButton sender)
        {
            Debug.Assert(!Sync.IsServer);
            //joining into running game:
            MySyncScenario.OnPrepareScenarioFromLobby(-1);
            CloseScreen();
        }

        protected override void OnClosed()
        {
            MySyncScenario.InfoAnswer -= MySyncScenario_InfoAnswer;
            base.OnClosed();
        }

    }
}
