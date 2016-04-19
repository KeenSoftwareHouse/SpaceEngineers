
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
using Sandbox.Game.Localization;
using VRage.Utils;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenCreateOrEditFaction : MyGuiScreenBase
    {
        protected MyGuiControlTextbox m_shortcut;
        protected MyGuiControlTextbox m_name;
        protected MyGuiControlTextbox m_desc;
        protected MyGuiControlTextbox m_privInfo;

        protected IMyFaction m_editFaction;

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

        public MyGuiScreenCreateOrEditFaction()
            : base(size: new Vector2(0.5f, 0.5f),
                   position: MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER),
                   backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                   backgroundTexture: MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.Texture)
        {
            CanHideOthers = false;
            EnabledBackgroundFade = false;
        }

        public void Init(ref IMyFaction editData)
        {
            m_editFaction = editData;
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
        }

        protected void OnOkClick(MyGuiControlButton sender)
        {
            m_shortcut.Text = m_shortcut.Text.Replace(" ", string.Empty);
            m_name.Text = m_name.Text.Trim();

            if (m_shortcut.Text.Length != 3)
            {
                ShowErrorBox(MyTexts.Get(MyCommonTexts.MessageBoxErrorFactionsTag));
                return;
            }

            if (MySession.Static.Factions.FactionTagExists(m_shortcut.Text, m_editFaction))
            {
                ShowErrorBox(MyTexts.Get(MyCommonTexts.MessageBoxErrorFactionsTagAlreadyExists));
                return;
            }

            if (m_name.Text.Length < 4)
            {
                ShowErrorBox(MyTexts.Get(MyCommonTexts.MessageBoxErrorFactionsNameTooShort));
                return;
            }

            if (MySession.Static.Factions.FactionNameExists(m_name.Text, m_editFaction))
            {
                ShowErrorBox(MyTexts.Get(MyCommonTexts.MessageBoxErrorFactionsNameAlreadyExists));
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
                founderId: MySession.Static.LocalPlayerId,
                tag: m_shortcut.Text,
                name: m_name.Text,
                desc: m_desc.Text,
                privateInfo: m_privInfo.Text);

            CloseScreenNow();
        }

        protected void OnCancelClick(MyGuiControlButton sender)
        {
            CloseScreenNow();
        }

        protected void ShowErrorBox(StringBuilder text)
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.OK,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                messageText: text);
            messageBox.SkipTransition = true;
            messageBox.CloseBeforeCallback = true;
            messageBox.CanHideOthers = false;
            MyGuiSandbox.AddScreen(messageBox);
        }
    }
}
