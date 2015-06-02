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
        public MyGuiScreenScenarioMpClient()
        {
            Debug.Assert(!Sync.IsServer);
            m_startButton.Enabled = false;
        }

        /*protected override void OnStartClicked(MyGuiControlButton sender)
        {
            Debug.Assert(Sync.IsServer);
            MyScenarioSystem.Static.PrepareForStart();
            CloseScreen();
        }*/

    }
}
