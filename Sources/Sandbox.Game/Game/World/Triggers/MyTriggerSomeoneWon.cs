using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Triggers;
using Sandbox.Game.SessionComponents;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Library;

namespace Sandbox.Game.World.Triggers
{
    [TriggerType(typeof(MyObjectBuilder_TriggerSomeoneWon))]
    public class MyTriggerSomeoneWon : MyTrigger,ICloneable
    {
        public MyTriggerSomeoneWon(){ }

        public MyTriggerSomeoneWon(MyTriggerSomeoneWon trg)
            : base(trg) {}
        public override object Clone()
        {
            return new MyTriggerSomeoneWon(this);
        }

        public override bool RaiseSignal(Signal signal)
        {
            if (signal == Signal.OTHER_WON)
                m_IsTrue = true;
            return IsTrue;
        }

        private StringBuilder m_progress = new StringBuilder();
        public override StringBuilder GetProgress()
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (players.Count == 1)
                return null;//only me in game
            m_progress.Clear().Append(MyTexts.Get(MySpaceTexts.ScenarioProgressSomeoneWon));
            MyMissionTriggers mtrig;
            foreach (MyPlayer player in players)
            {
                if (player == MySession.Static.LocalHumanPlayer)
                    continue;
                if (!MySessionComponentMissionTriggers.Static.MissionTriggers.TryGetValue(player.Id, out mtrig))
                    continue;
                if (!mtrig.Lost && !mtrig.Won)
                    m_progress.Append(MyEnvironment.NewLine).Append("   ").Append(player.DisplayName);
            }


            return m_progress;
        }
        //GUI
        public override void DisplayGUI()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenTriggerSomeoneWon(this));
        }
        public new static MyStringId GetCaption()
        {
            return MySpaceTexts.GuiTriggerCaptionSomeoneWon;
        }
    }
}
