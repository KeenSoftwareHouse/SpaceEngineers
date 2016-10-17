using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GameSystems;
using VRage.Audio;
using VRage.Game;

namespace Sandbox.Game.GUI
{
    public class MyGuiScreenScenarioWaitForPlayers : MyGuiScreenBase
    {
        MyGuiControlLabel m_timeOutLabel;
        MyGuiControlButton m_leaveButton;

        StringBuilder m_tmpStringBuilder = new StringBuilder();


        public MyGuiScreenScenarioWaitForPlayers()
            : base(position: new Vector2(0.5f, 0.5f), backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR)
        {
            Size = new Vector2(800f, 330f) / MyGuiConstants.GUI_OPTIMAL_SIZE;

            CloseButtonEnabled = false;
            m_closeOnEsc = false;

            RecreateControls(true);
            CanHideOthers = false;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyStringId.GetOrCompute("Waiting for other players"));

            var label = new MyGuiControlLabel(text: "Game will start when all players join the world", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_timeOutLabel = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            m_leaveButton = new MyGuiControlButton(text: new StringBuilder("Leave"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnLeaveClicked);

            const float colMargin = 60f;
            const float rowHeight = 65f;
            var layout = new MyLayoutTable(this);
            layout.SetColumnWidths(colMargin, 680, colMargin);
            layout.SetRowHeights(110, rowHeight, rowHeight, rowHeight, rowHeight, rowHeight);

            layout.Add(label, MyAlignH.Center, MyAlignV.Center, 1, 1);
            layout.Add(m_timeOutLabel, MyAlignH.Center, MyAlignV.Center, 2, 1);
            layout.Add(m_leaveButton, MyAlignH.Center, MyAlignV.Center, 3, 1);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenBattleWaitingConnectedPlayers";
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.MainMenu));
            }
        }

        public override bool Update(bool hasFocus)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(0);
            if (MyScenarioSystem.Static.ServerPreparationStartTime != null)
            {
                timeout = DateTime.UtcNow - MyScenarioSystem.Static.ServerPreparationStartTime;
                timeout = TimeSpan.FromSeconds(MyScenarioSystem.LoadTimeout) - timeout;
                if (timeout.TotalMilliseconds < 0)
                    timeout = TimeSpan.FromSeconds(0);
            }

            string strTimeout = timeout.ToString(@"mm\:ss");
            m_tmpStringBuilder.Clear().Append("Timeout: ").Append(strTimeout);

            m_timeOutLabel.Text = m_tmpStringBuilder.ToString();

            return base.Update(hasFocus);
        }

        private void OnLeaveClicked(MyGuiControlButton sender)
        {
            CloseScreen();
            MySessionLoader.UnloadAndExitToMenu();
        }

    }
}
