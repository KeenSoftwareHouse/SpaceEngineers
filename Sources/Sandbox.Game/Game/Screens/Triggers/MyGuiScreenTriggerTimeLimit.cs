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
    class MyGuiScreenTriggerTimeLimit : MyGuiScreenTrigger
    {
        MyGuiControlLabel m_labelMinutesLeft;
        protected MyGuiControlTextbox m_minutesLimit;
        const float WINSIZEX = 0.4f, WINSIZEY=0.37f;
        const float spacingH = 0.01f;
        public MyGuiScreenTriggerTimeLimit(MyTrigger trg)
            : base(trg, new Vector2(WINSIZEX + 0.1f, WINSIZEY))
        {
            float left = m_textboxMessage.Position.X-m_textboxMessage.Size.X/2;
            float top = -WINSIZEY / 2f + MIDDLE_PART_ORIGIN.Y;
            m_labelMinutesLeft = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: new Vector2(0.013f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.GuiTriggerTimeLimit).ToString()
            );
            left += m_labelMinutesLeft.Size.X + spacingH;
            m_minutesLimit = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(0.05f, 0.035f),
                Type = MyGuiControlTextboxType.DigitsOnly,
                Name = "minutes"
            };
            m_minutesLimit.TextChanged += OnTimeChanged;

            AddCaption(MySpaceTexts.GuiTriggerCaptionTimeLimit);
            Controls.Add(m_labelMinutesLeft);
            Controls.Add(m_minutesLimit);

            m_minutesLimit.Text = ((MyTriggerTimeLimit)trg).LimitInMinutes.ToString();
        }

        public void OnTimeChanged(MyGuiControlTextbox sender)
        {
            int? lives = StrToInt(sender.Text);
            if (lives != null && lives>0)
            {
                sender.ColorMask = Vector4.One;
                m_okButton.Enabled = true;
            }
            else
            {
                sender.ColorMask = Color.Red.ToVector4();
                m_okButton.Enabled = false;
            }
        }

        protected override void OnOkButtonClick(MyGuiControlButton sender)
        {
            int? minutes = StrToInt(m_minutesLimit.Text);
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
