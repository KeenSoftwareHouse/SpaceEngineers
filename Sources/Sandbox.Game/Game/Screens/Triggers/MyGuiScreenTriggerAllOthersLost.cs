using Sandbox.Game.Localization;
using Sandbox.Game.World.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Triggers
{
    public class MyGuiScreenTriggerAllOthersLost : MyGuiScreenTrigger
    {
        public MyGuiScreenTriggerAllOthersLost(MyTrigger trg) : base(trg,new Vector2(0.5f,0.3f))
        {
            AddCaption(MySpaceTexts.GuiTriggerCaptionAllOthersLost);
        }
        public override string GetFriendlyName()
        {
            return "MyGuiScreenTriggerAllOthersLost";
        }
    }
}
