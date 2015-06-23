using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Game.World.Triggers
{
    [TriggerType(typeof(MyObjectBuilder_TriggerLives))]
    class MyTriggerLives : MyTrigger, ICloneable
    {
        //int m_livesLeft;
        public int LivesLeft=1;

        public MyTriggerLives(){ }

        public MyTriggerLives(MyTriggerLives trg)
            : base(trg) 
        {
            LivesLeft = trg.LivesLeft;
        }

        public override object Clone()
        {
            return new MyTriggerLives(this);
        }

        public override bool RaiseSignal(Signal signal)
        {
            if (signal == Signal.PLAYER_DIED)
            {
                LivesLeft--;
                if (LivesLeft<=0)
                    m_IsTrue = true;
            }
            return IsTrue;
        }

        public override void DisplayHints()
        {
            if (MySession.Static.IsScenario)
                MyHud.ScenarioInfo.LivesLeft = LivesLeft;
        }

        //OB:
        public override void Init(MyObjectBuilder_Trigger ob)
        {
            base.Init(ob);
            LivesLeft = ((MyObjectBuilder_TriggerLives)ob).Lives;
        }
        public override MyObjectBuilder_Trigger GetObjectBuilder()
        {
            MyObjectBuilder_TriggerLives ob = (MyObjectBuilder_TriggerLives)base.GetObjectBuilder();
            ob.Lives = LivesLeft;
            return ob;
        }

        //GUI
        public override void DisplayGUI()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenTriggerLives(this));
        }
        public new static MyStringId GetCaption()
        {
            return MySpaceTexts.GuiTriggerCaptionLives;
        }
    }

}
