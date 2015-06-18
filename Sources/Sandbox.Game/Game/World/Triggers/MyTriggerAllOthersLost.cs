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
    [TriggerType(typeof(MyObjectBuilder_TriggerAllOthersLost))]
    class MyTriggerAllOthersLost : MyTrigger,ICloneable
    {
        public MyTriggerAllOthersLost(){ }

        public MyTriggerAllOthersLost(MyTriggerAllOthersLost trg)
            : base(trg) {}
        public override object Clone()
        {
            return new MyTriggerAllOthersLost(this);
        }

        public override bool RaiseSignal(Signal signal)
        {
            if (signal == Signal.ALL_OTHERS_LOST)
                m_IsTrue = true;
            return IsTrue;
        }

        //GUI
        public override void DisplayGUI()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenTriggerAllOthersLost(this));
        }
        public new static MyStringId GetCaption()
        {
            return MySpaceTexts.GuiTriggerCaptionAllOthersLost;
        }
    }
}
