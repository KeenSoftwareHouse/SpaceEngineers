using Sandbox.Game.Localization;
using Sandbox.Game.World.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Triggers
{
    public class MyGuiScreenTriggerPositionLeft : MyGuiScreenTriggerPosition
    {
        public MyGuiScreenTriggerPositionLeft(MyTrigger trg)
            : base(trg)
        {
            AddCaption(MySpaceTexts.GuiTriggerCaptionPositionLeft);
            m_xCoord.Text = ((MyTriggerPositionLeft)trg).TargetPos.X.ToString();
            m_yCoord.Text = ((MyTriggerPositionLeft)trg).TargetPos.Y.ToString();
            m_zCoord.Text = ((MyTriggerPositionLeft)trg).TargetPos.Z.ToString();
            m_radius.Text = ((MyTriggerPositionLeft)trg).Radius.ToString();
        }

        protected override void OnOkButtonClick(MyGuiControlButton sender)
        {
            double? radius = StrToDouble(m_radius.Text);
            if (radius != null)
                ((MyTriggerPositionLeft)m_trigger).Radius = (double)radius;
            if (m_coordsChanged)
                ((MyTriggerPositionLeft)m_trigger).TargetPos = m_coords;
            base.OnOkButtonClick(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTriggerPositionLeft";
        }

    }
}

