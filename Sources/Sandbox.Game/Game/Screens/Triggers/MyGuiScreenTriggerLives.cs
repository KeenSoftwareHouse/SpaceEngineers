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
    class MyGuiScreenTriggerLives : MyGuiScreenTrigger
    {
        MyGuiControlLabel m_labelLives;
        protected MyGuiControlTextbox m_lives;
        const float WINSIZEX = 0.4f, WINSIZEY=0.37f;
        const float spacingH = 0.01f;
        public MyGuiScreenTriggerLives(MyTrigger trg)
            : base(trg, new Vector2(WINSIZEX + 0.1f, WINSIZEY))
        {
            float left = m_textboxMessage.Position.X-m_textboxMessage.Size.X/2;
            float top = -WINSIZEY / 2f + MIDDLE_PART_ORIGIN.Y;
            m_labelLives = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                position: new Vector2(left, top),
                size: new Vector2(0.01f, 0.035f),
                text: MyTexts.Get(MySpaceTexts.GuiTriggersLives).ToString()
            );
            left += m_labelLives.Size.X + spacingH;
            m_lives = new MyGuiControlTextbox()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2((WINSIZEX - spacingH) / 3 - 2 * spacingH - m_labelLives.Size.X, 0.035f),
                Type = MyGuiControlTextboxType.DigitsOnly,
                Name = "lives"
            };
            m_lives.TextChanged += OnLivesChanged;

            AddCaption(MySpaceTexts.GuiTriggerCaptionLives);
            Controls.Add(m_labelLives);
            Controls.Add(m_lives);

            m_lives.Text = ((MyTriggerLives)trg).LivesLeft.ToString();
        }

        public void OnLivesChanged(MyGuiControlTextbox sender)
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
            int? lives = StrToInt(m_lives.Text);
            Debug.Assert(lives!=null,"incorrect value of lives");
            if (lives != null)
                ((MyTriggerLives)m_trigger).LivesLeft = (int)lives;
            base.OnOkButtonClick(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTriggerLives";
        }
    }
}
