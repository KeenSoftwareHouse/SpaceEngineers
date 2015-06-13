using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens
{
    class MyGuiScreenScenarioMpClient : MyGuiScreenScenarioMpBase
    {
        public void MySyncScenario_InfoAnswer(bool gameAlreadyRunning)
        {
            m_startButton.Enabled = gameAlreadyRunning; //when a client joins into a running game, he needs to have start button enabled (with slightly different meaning than server)
        }
        public MyGuiScreenScenarioMpClient()
        {
            Debug.Assert(!Sync.IsServer);
            m_startButton.Enabled = false;
            MySyncScenario.InfoAnswer += MySyncScenario_InfoAnswer;
            MySyncScenario.AskInfo();
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
