using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;

namespace Sandbox.Game.World.Triggers
{
    [TriggerType(typeof(MyObjectBuilder_TriggerTimeLimit))]
    class MyTriggerTimeLimit : MyTrigger, ICloneable
    {
        private int m_limitInMinutes=30;
        private TimeSpan m_limit = new TimeSpan(0, 30, 0);
        public int LimitInMinutes
        {
            get
            {
                return m_limitInMinutes;
            }
            set
            {
                m_limitInMinutes=value;
                m_limit = new TimeSpan(0, value, 0);
            }
        }
        public MyTriggerTimeLimit(){ }

        public MyTriggerTimeLimit(MyTriggerTimeLimit trg)
            : base(trg) 
        {
            LimitInMinutes = trg.LimitInMinutes;
        }

        public override object Clone()
        {
            return new MyTriggerTimeLimit(this);
        }

        private int m_lastSeconds;
        public override void DisplayHints(MyPlayer player, MyEntity me)
        {
            if (!MySession.Static.IsScenario || MyScenarioSystem.Static.ServerStartGameTime==DateTime.MaxValue)
                return;
            TimeSpan difference = m_limit - (DateTime.UtcNow - MyScenarioSystem.Static.ServerStartGameTime);
            var seconds = difference.Seconds;
            if (m_lastSeconds!=seconds)
            {
                m_lastSeconds=seconds;
                MyHud.ScenarioInfo.TimeLeftMin = (int)difference.TotalMinutes;
                MyHud.ScenarioInfo.TimeLeftSec = seconds;
            }
        }

        private StringBuilder m_progress = new StringBuilder();
        public override StringBuilder GetProgress()
        {
            m_progress.Clear().AppendFormat(MyTexts.GetString(MySpaceTexts.ScenarioProgressTimeLimit), LimitInMinutes);
            return m_progress;
        }

        public override bool Update(MyPlayer player, MyEntity me)
        {
            if (MySession.Static.IsScenario)
                if (m_limit <= DateTime.UtcNow - MyScenarioSystem.Static.ServerStartGameTime)
                    m_IsTrue = true;
            return IsTrue;
        }
        //OB:
        public override void Init(MyObjectBuilder_Trigger ob)
        {
            base.Init(ob);
            LimitInMinutes = ((MyObjectBuilder_TriggerTimeLimit)ob).Limit;
        }
        public override MyObjectBuilder_Trigger GetObjectBuilder()
        {
            MyObjectBuilder_TriggerTimeLimit ob = (MyObjectBuilder_TriggerTimeLimit)base.GetObjectBuilder();
            ob.Limit = LimitInMinutes;
            return ob;
        }

        //GUI
        public override void DisplayGUI()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenTriggerTimeLimit(this));
        }
        public new static MyStringId GetCaption()
        {
            return MySpaceTexts.GuiTriggerCaptionTimeLimit;
        }
    }

}
