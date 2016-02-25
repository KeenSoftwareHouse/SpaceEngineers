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
    public abstract class MyGuiScreenTrigger : MyGuiScreenBase
    {
        MyGuiControlLabel m_textboxName;
        protected MyGuiControlTextbox m_textboxMessage;

        MyGuiControlLabel m_wwwLabel;
        protected MyGuiControlTextbox m_wwwTextbox;

        MyGuiControlLabel m_nextMisLabel;
        protected MyGuiControlTextbox m_nextMisTextbox;

        protected MyGuiControlButton m_okButton, m_cancelButton;
        protected MyTrigger m_trigger;
        protected const float VERTICAL_OFFSET = 0.005f;
        protected static readonly Vector2 RESERVED_SIZE = new Vector2(0,0.196f);
        protected static readonly Vector2 MIDDLE_PART_ORIGIN = -RESERVED_SIZE/2+new Vector2(0, 0.17f);//you can use this to put items into right place in the middle

        public MyGuiScreenTrigger(MyTrigger trg, Vector2 size)
            : base(null, MyGuiConstants.SCREEN_BACKGROUND_COLOR, size + RESERVED_SIZE)
        {
            size += RESERVED_SIZE;
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
            m_textboxName.Position = m_textboxName.Position - new Vector2(m_textboxMessage.Size.X / 2, 0);//line to the left of textbox
            Controls.Add(m_textboxName);
            Controls.Add(m_textboxMessage);

            //below middle part, position from bottom:
            m_itemPos.Y = Size.Value.Y * 0.5f - 0.3f;
            m_wwwLabel = new MyGuiControlLabel(
                position: m_itemPos,
                text: MyTexts.Get(MySpaceTexts.GuiTriggerWwwLink).ToString()
                );
            m_itemPos.Y += m_wwwLabel.Size.Y + VERTICAL_OFFSET;
            m_wwwTextbox = new MyGuiControlTextbox(
                position: m_itemPos,
                defaultText: trg.WwwLink,
                maxLength: 300);
            m_itemPos.Y += m_wwwTextbox.Size.Y + VERTICAL_OFFSET;
            m_wwwLabel.Position = m_wwwLabel.Position - new Vector2(m_wwwTextbox.Size.X / 2, 0);//line to the left of textbox
            m_wwwTextbox.TextChanged += OnWwwTextChanged;
            Controls.Add(m_wwwLabel);
            Controls.Add(m_wwwTextbox);

            m_nextMisLabel = new MyGuiControlLabel(
                position: m_itemPos,
                text: MyTexts.Get(MySpaceTexts.GuiTriggerNextMission).ToString()
                );
            m_itemPos.Y += m_wwwLabel.Size.Y + VERTICAL_OFFSET;
            m_nextMisTextbox = new MyGuiControlTextbox(
                position: m_itemPos,
                defaultText: m_trigger.NextMission,
                maxLength: 300);
            m_itemPos.Y += m_wwwTextbox.Size.Y + VERTICAL_OFFSET;
            m_nextMisLabel.Position = m_nextMisLabel.Position - new Vector2(m_nextMisTextbox.Size.X / 2, 0);//line to the left of textbox
            m_nextMisTextbox.SetToolTip(MySpaceTexts.GuiTriggerNextMissionTooltip);
            Controls.Add(m_nextMisLabel);
            Controls.Add(m_nextMisTextbox);

            
            Vector2 buttonOrigin = new Vector2(0f, Size.Value.Y * 0.5f - 0.05f);
            Vector2 buttonOffset = new Vector2(0.01f, 0f); 
            
            m_okButton = new MyGuiControlButton(
                text: MyTexts.Get(MyCommonTexts.Ok),
                onButtonClick: OnOkButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_okButton.Position = buttonOrigin - buttonOffset;

            m_cancelButton = new MyGuiControlButton(
                text: MyTexts.Get(MyCommonTexts.Cancel),
                onButtonClick: OnCancelButtonClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            m_cancelButton.Position = buttonOrigin + buttonOffset;

            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            OnWwwTextChanged(m_wwwTextbox);
        }

        void OnWwwTextChanged(MyGuiControlTextbox source)
        {
            if (source.Text.Length == 0 || MyGuiSandbox.IsUrlWhitelisted(source.Text))
            {
                source.ColorMask = Vector4.One;
                source.SetToolTip((MyToolTips)null);
                m_okButton.Enabled = true;
            }
            else
            {
                m_wwwTextbox.SetToolTip(MySpaceTexts.WwwLinkNotAllowed);
                source.ColorMask = Color.Red.ToVector4();
                m_okButton.Enabled = false;
            }
        }
        void OnCancelButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }

        protected virtual void OnOkButtonClick(MyGuiControlButton sender)
        {
            m_trigger.Message = m_textboxMessage.Text;
            m_trigger.WwwLink = m_wwwTextbox.Text;
            m_trigger.NextMission = m_nextMisTextbox.Text;
            CloseScreen();
        }

        public override bool CloseScreen()
        {
            m_wwwTextbox.TextChanged -= OnWwwTextChanged;
            return base.CloseScreen();
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
