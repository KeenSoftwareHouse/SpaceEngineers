﻿using Sandbox.Game.Localization;
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
    public abstract class MyGuiScreenTriggerTime : MyGuiScreenTrigger
    {
        MyGuiControlLabel m_labelTime;
        protected MyGuiControlTextbox m_textboxTime;
        const float WINSIZEX = 0.4f, WINSIZEY=0.37f;
        const float spacingH = 0.01f;
        public MyGuiScreenTriggerTime(MyTrigger trg, MyStringId labelText)
            : base(trg, new Vector2(WINSIZEX + 0.1f, WINSIZEY))
        {
            float left = m_textboxMessage.Position.X-m_textboxMessage.Size.X/2;
            float top = -WINSIZEY / 2f + MIDDLE_PART_ORIGIN.Y;
            m_labelTime = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: new Vector2(0.013f, 0.035f),
                text: MyTexts.Get(labelText).ToString()//text: MyTexts.Get(MySpaceTexts.GuiTriggerTimeLimit).ToString()
            );
            left += m_labelTime.Size.X + spacingH;
            m_textboxTime = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(0.05f, 0.035f),
                Type = MyGuiControlTextboxType.DigitsOnly,
                Name = "time"
            };
            m_textboxTime.TextChanged += OnTimeChanged;

            Controls.Add(m_labelTime);
            Controls.Add(m_textboxTime);

        }

        public void OnTimeChanged(MyGuiControlTextbox sender)
        {
            int? time = StrToInt(sender.Text);
            if (time != null && IsValid((int)time))
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
        public virtual bool IsValid(int time)
        {
            return true;
        }
    }
}
