using Sandbox.Game.Localization;
using Sandbox.Game.World.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Triggers
{
    class MyGuiScreenTriggerNoSpawn : MyGuiScreenTriggerTime
    {
        public MyGuiScreenTriggerNoSpawn(MyTrigger trg)
            : base(trg, MySpaceTexts.GuiTriggerNoSpawnTimeLimit)
        {
            AddCaption(MySpaceTexts.GuiTriggerCaptionNoSpawn);
            m_textboxTime.Text = ((MyTriggerNoSpawn)trg).LimitInSeconds.ToString();
        }
        public override bool IsValid(int time)
        {
            return (time >= 15);
        }
        protected override void OnOkButtonClick(MyGuiControlButton sender)
        {
            int? seconds = StrToInt(m_textboxTime.Text);
            Debug.Assert(seconds != null, "incorrect value of time");
            if (seconds != null)
                ((MyTriggerNoSpawn)m_trigger).LimitInSeconds = (int)seconds;
            base.OnOkButtonClick(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTriggerNoSpawn";
        }
    }
}