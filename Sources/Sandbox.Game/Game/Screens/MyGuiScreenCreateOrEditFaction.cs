
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using Sandbox.ModAPI;
using VRage;
using Sandbox.Game.Localization;
using VRage.Utils;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenCreateOrEditFaction : MyGuiScreenBase
    {
        MyGuiControlTextbox m_shortcut;
        MyGuiControlTextbox m_name;
        MyGuiControlTextbox m_desc;
        MyGuiControlTextbox m_privInfo;

        IMyFaction m_editFaction;

        public MyGuiScreenCreateOrEditFaction(ref IMyFaction editData)
            : base(size: new Vector2(0.5f, 0.5f),
                   position: MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER),
                   backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                   backgroundTexture: MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.Texture)
        {
            CanHideOthers         = false;
            EnabledBackgroundFade = false;
            m_editFaction         = editData;

            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenCreateOrEditFaction";
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            var left       = -0.23f;
            var top        = -0.23f;
            var spacingV   =  0.045f;
            var buttonSize = new Vector2(0.29f, 0.052f);

            var composite = new MyGuiControlCompositePanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(0.46f, 0.46f)
            };
            left += 0.005f;
            top  += 0.007f;

            var panel = new MyGuiControlPanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = new Vector2(left, top),
                Size = new Vector2(0.451f, 0.038f),
                BackgroundTexture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK
            };
            left += 0.005f;
            top  += 0.003f;

            // LABELS
            var headerText = MyTexts.GetString((m_editFaction == null) ? MySpaceTexts.TerminalTab_Factions_CreateFaction : MySpaceTexts.TerminalTab_Factions_EditFaction);
            var headerLabel = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, size: buttonSize, position: new Vector2(left, top), text: headerText);
            top += 0.01f;
            var shortcutLabel = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, size: buttonSize, position: new Vector2(left, top + spacingV), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_CreateFactionTag));
            var nameLabel = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, size: buttonSize, position: new Vector2(left, top + 2f * spacingV), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_CreateFactionName));
            var descLabel = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, size: buttonSize, position: new Vector2(left, top + 3f * spacingV), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_CreateFactionDescription));
            var secretLabel = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, size: buttonSize, position: new Vector2(left, top + 4f * spacingV), text: MyTexts.GetString(MySpaceTexts.TerminalTab_Factions_CreateFactionPrivateInfo));

            shortcutLabel.SetToolTip(MySpaceTexts.TerminalTab_Factions_CreateFactionTagToolTip);
            secretLabel.SetToolTip(MySpaceTexts.TerminalTab_Factions_CreateFactionPrivateInfoToolTip);

            Controls.Add(composite);
            Controls.Add(panel);
            Controls.Add(headerLabel);
            Controls.Add(shortcutLabel);
            Controls.Add(nameLabel);
            Controls.Add(descLabel);
            Controls.Add(secretLabel);

            // INPUTS
            var iLeft =  0.06f;
            var iTop  = -0.15f;

            m_shortcut = new MyGuiControlTextbox(position: new Vector2(iLeft, iTop),                 maxLength: 3,   defaultText: (m_editFaction != null) ? m_editFaction.Tag : "");
            m_name     = new MyGuiControlTextbox(position: new Vector2(iLeft, iTop + spacingV),      maxLength: 64,  defaultText: (m_editFaction != null) ? m_editFaction.Name : "");
            m_desc     = new MyGuiControlTextbox(position: new Vector2(iLeft, iTop + 2f * spacingV), maxLength: 512, defaultText: (m_editFaction != null) ? m_editFaction.Description : "");
            m_privInfo = new MyGuiControlTextbox(position: new Vector2(iLeft, iTop + 3f * spacingV), maxLength: 512, defaultText: (m_editFaction != null) ? m_editFaction.PrivateInfo : "");

            m_shortcut.SetToolTip(MySpaceTexts.TerminalTab_Factions_CreateFactionTagToolTip);
            m_privInfo.SetToolTip(MySpaceTexts.TerminalTab_Factions_CreateFactionPrivateInfoToolTip);

            Controls.Add(m_shortcut);
            Controls.Add(m_name);
            Controls.Add(m_desc);
            Controls.Add(m_privInfo);

            // BUTTONS
            top  -= 0.003f;

            Controls.Add(new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM, size: buttonSize, position: new Vector2(left, -top), text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OnOkClick));
            Controls.Add(new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM, size: buttonSize, position: new Vector2(-left, -top), text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: OnCancelClick));
        }

        private void OnOkClick(MyGuiControlButton sender)
        {
            m_shortcut.Text = m_shortcut.Text.Replace(" ", string.Empty);
            m_name.Text = m_name.Text.Trim();

            if (m_shortcut.Text.Length != 3)
            {
                ShowErrorBox(MyTexts.Get(MySpaceTexts.MessageBoxErrorFactionsTag));
                return;
            }

            if (MySession.Static.Factions.FactionTagExists(m_shortcut.Text, m_editFaction))
            {
                ShowErrorBox(MyTexts.Get(MySpaceTexts.MessageBoxErrorFactionsTagAlreadyExists));
                return;
            }

            if (m_name.Text.Length < 4)
            {
                ShowErrorBox(MyTexts.Get(MySpaceTexts.MessageBoxErrorFactionsNameTooShort));
                return;
            }

            if (MySession.Static.Factions.FactionNameExists(m_name.Text, m_editFaction))
            {
                ShowErrorBox(MyTexts.Get(MySpaceTexts.MessageBoxErrorFactionsNameAlreadyExists));
                return;
            }

            if (m_editFaction != null)
            {
                MySession.Static.Factions.EditFaction(
                    factionId: m_editFaction.FactionId,
                    tag: m_shortcut.Text,
                    name: m_name.Text,
                    desc: m_desc.Text,
                    privateInfo: m_privInfo.Text);

                CloseScreenNow();
                return;
            }

            MySession.Static.Factions.CreateFaction(
                founderId: MySession.LocalPlayerId,
                tag: m_shortcut.Text,
                name: m_name.Text,
                desc: m_desc.Text,
                privateInfo: m_privInfo.Text);

            CloseScreenNow();
        }

        private void OnCancelClick(MyGuiControlButton sender)
        {
            CloseScreenNow();
        }

        private void ShowErrorBox(StringBuilder text)
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.OK,
                messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                messageText: text);
            messageBox.SkipTransition = true;
            messageBox.CloseBeforeCallback = true;
            messageBox.CanHideOthers = false;
            MyGuiSandbox.AddScreen(messageBox);
        }
    }
}
