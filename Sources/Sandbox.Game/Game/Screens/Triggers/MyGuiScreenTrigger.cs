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
    public class MyGuiScreenTrigger : MyGuiScreenBase
    {
        MyGuiControlLabel m_textboxName;
        protected MyGuiControlTextbox m_textboxMessage;
        protected MyGuiControlButton m_okButton, m_cancelButton;
        protected MyTrigger m_trigger;
        protected readonly Vector2 MIDDLE_PART_ORIGIN=new Vector2(0,0.17f);//you can use this to put items into right place in the middle
        protected const float VERTICAL_OFFSET = 0.01f;

        public MyGuiScreenTrigger(MyTrigger trg, Vector2 size)
            : base(size, MyGuiConstants.SCREEN_BACKGROUND_COLOR, size)
        {
            Vector2 m_itemPos=new Vector2();
            m_itemPos.Y = - size.Y / 2 + 0.1f;
            m_textboxName=new MyGuiControlLabel(
                position: m_itemPos,
                text: MyTexts.Get(MySpaceTexts.GuiTriggerMessage).ToString()
                );
            m_itemPos.Y += m_textboxName.Size.Y + VERTICAL_OFFSET;

            m_trigger = trg;
            m_textboxMessage = new MyGuiControlTextbox(
                position: m_itemPos,
                defaultText: trg.Message,
                maxLength: 85);
            m_itemPos.Y += m_textboxMessage.Size.Y + VERTICAL_OFFSET + 0.2f;
            //line to the left of textbox
            m_textboxName.Position = m_textboxName.Position-new Vector2(m_textboxMessage.Size.X / 2 , 0);

            Vector2 buttonOrigin = new Vector2(0f, Size.Value.Y * 0.4f);
            Vector2 buttonOffset = new Vector2(0.01f, 0f); 
            
            m_okButton = new MyGuiControlButton(
                text: MyTexts.Get(MySpaceTexts.Ok),
                onButtonClick: OnOkButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_okButton.Position = buttonOrigin - buttonOffset;

            m_cancelButton = new MyGuiControlButton(
                text: MyTexts.Get(MySpaceTexts.Cancel),
                onButtonClick: OnCancelButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            m_cancelButton.Position = buttonOrigin + buttonOffset;

            Controls.Add(m_textboxName);
            Controls.Add(m_textboxMessage);
            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);
        }

        void OnCancelButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }

        protected virtual void OnOkButtonClick(MyGuiControlButton sender)
        {
            m_trigger.Message = m_textboxMessage.Text;
            CloseScreen();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTrigger";
        }

        protected double? StrToDouble(string str)
        {
            double val;
            try
            {
                val = double.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return null;
            }
            return val;
        }
        protected int? StrToInt(string str)
        {
            int val;
            try
            {
                val = int.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return null;
            }
            return val;
        }
    }
}
