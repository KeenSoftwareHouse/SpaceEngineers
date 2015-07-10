using Sandbox.Game.Localization;
using Sandbox.Game.World.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Triggers
{
    class MyGuiScreenTriggerTimeLimit : MyGuiScreenTriggerTime
    {
        public MyGuiScreenTriggerTimeLimit(MyTrigger trg)
            : base(trg, MySpaceTexts.GuiTriggerTimeLimit)
        {
            AddCaption(MySpaceTexts.GuiTriggerCaptionTimeLimit);
            m_textboxTime.Text = ((MyTriggerTimeLimit)trg).LimitInMinutes.ToString();
        }
        public override bool IsValid(int time)
        {
            return (time > 0);
        }
        protected override void OnOkButtonClick(MyGuiControlButton sender)
        {
            int? minutes = StrToInt(m_textboxTime.Text);
            Debug.Assert(minutes!=null,"incorrect value of time");
            if (minutes != null)
                ((MyTriggerTimeLimit)m_trigger).LimitInMinutes = (int)minutes;
            base.OnOkButtonClick(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTriggerTimeLimit";
        }
    }
}
