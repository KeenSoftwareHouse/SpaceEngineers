using Sandbox.Common.ObjectBuilders.Definitions;
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
