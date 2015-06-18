using Sandbox.Engine.Multiplayer;
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
    class MyGuiScreenScenarioMpServer : MyGuiScreenScenarioMpBase
    {
        public MyGuiScreenScenarioMpServer()
        {
            Debug.Assert(Sync.IsServer);
        }
        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            //m_canJoinRunning.Enabled = true;
            //m_canJoinRunning.IsChecked = MySession.Static.Settings.CanJoinRunning;
        }
        protected override void OnStartClicked(MyGuiControlButton sender)
        {
            Debug.Assert(Sync.IsServer);

            MySession.Static.Settings.CanJoinRunning = false;
            if (!MySession.Static.Settings.CanJoinRunning)
                MyMultiplayer.Static.SetLobbyType(SteamSDK.LobbyTypeEnum.Private);
            MyScenarioSystem.Static.PrepareForStart();
            CloseScreen();
        }

    }
}
