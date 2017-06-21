
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyGuiDetailScreenScriptLocal : MyGuiBlueprintScreenBase
    {
        public bool WasPublished = false;
        protected MyGuiControlMultilineText m_textField;
        protected float m_textScale;
        protected Vector2 m_offset = new Vector2(-0.01f, 0f);
        protected int maxNameLenght = 40;
        private MyGuiBlueprintTextDialog m_dialog;
        MyScriptItemInfo m_selectedItem = null;
        MyGuiIngameScriptsPage m_parent;
        protected MyGuiControlMultilineText m_descriptionField;
        private Action<MyScriptItemInfo> callBack;

        public MyGuiDetailScreenScriptLocal(Action<MyScriptItemInfo> callBack, MyScriptItemInfo selectedItem, MyGuiIngameScriptsPage parent, float textScale) :
            base(new Vector2(0.37f, 0.325f), new Vector2(0.725f, 0.4f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, false)
        {
            WasPublished = false;
            this.callBack = callBack;
            m_parent = parent;
            m_selectedItem = selectedItem;
            m_textScale = textScale;
            m_localBlueprintFolder = Path.Combine(MyFileSystem.UserDataPath, MyGuiIngameScriptsPage.SCRIPTS_DIRECTORY, "local");
            m_workshopBlueprintFolder = Path.Combine(MyFileSystem.UserDataPath, MyGuiIngameScriptsPage.SCRIPTS_DIRECTORY, "workshop");
            RecreateControls(true);
            EnabledBackgroundFade = true;
        }

#if !XB1 // XB1_NOWORKSHOP
        protected void CreateButtons()
        {
            Vector2 buttonPosition = new Vector2(0.15f, -0.173f) + m_offset;
            Vector2 buttonOffset = new Vector2(0.13f, 0.045f);

            float width = 0.13f;

            if (m_selectedItem.SteamItem == null)
            {
                var renameButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonRename), OnRename, textScale: m_textScale);
                renameButton.Position = buttonPosition;

                if (!MyFakes.XB1_PREVIEW)
                {
                    var publishButton = CreateButton(width, MyTexts.Get(MyCommonTexts.LoadScreenButtonPublish), OnPublish, textScale: m_textScale);
                    publishButton.Position = buttonPosition + new Vector2(1f, 0f) * buttonOffset;
                }

                var deleteButton = CreateButton(width, MyTexts.Get(MyCommonTexts.LoadScreenButtonDelete), OnDelete, textScale: m_textScale);
                deleteButton.Position = buttonPosition + new Vector2(0f, 1f) * buttonOffset;

                if (!MyFakes.XB1_PREVIEW)
                {
                    var openWorkshopButton = CreateButton(width, MyTexts.Get(MyCommonTexts.ScreenLoadSubscribedWorldBrowseWorkshop), OnOpenWorkshop, textScale: m_textScale);
                    openWorkshopButton.Position = buttonPosition + new Vector2(1f, 1f) * buttonOffset;
                }
            }
            else
            {
                var openWorkshopButton = CreateButton(width * 2.0f, MyTexts.Get(MyCommonTexts.ScreenLoadSubscribedWorldOpenInWorkshop), OnOpenInWorkshop, textScale: m_textScale);
                openWorkshopButton.Position = new Vector2(0.215f, -0.173f) + m_offset;
            }
            var closeButton = CreateButton(width, MyTexts.Get(MyCommonTexts.Close), OnCloseButton, textScale: m_textScale);
            if (m_selectedItem.SteamItem == null)
            {
                closeButton.Position = buttonPosition + new Vector2(1f, 2f) * buttonOffset;
            }
            else
            {
                closeButton.Position = buttonPosition + new Vector2(1f, 1f) * buttonOffset;
            }
        }
#else // XB1
        protected void CreateButtons()
        {
            Vector2 buttonPosition = new Vector2(0.15f, -0.173f) + m_offset;
            Vector2 buttonOffset = new Vector2(0.13f, 0.045f);

            float width = 0.13f;

            var renameButton = CreateButton(width, MyTexts.Get(MySpaceTexts.ProgrammableBlock_ButtonRename), OnRename, textScale: m_textScale);
            renameButton.Position = buttonPosition;

            var deleteButton = CreateButton(width, MyTexts.Get(MyCommonTexts.LoadScreenButtonDelete), OnDelete, textScale: m_textScale);
            deleteButton.Position = buttonPosition + new Vector2(0f, 1f) * buttonOffset;

            var closeButton = CreateButton(width, MyTexts.Get(MyCommonTexts.Close), OnCloseButton, textScale: m_textScale);

            closeButton.Position = buttonPosition + new Vector2(1f, 2f) * buttonOffset;
        }
#endif // XB1

        protected void CreateDescription()
        {
            var descPosition = new Vector2(-0.325f, 0.015f) + m_offset;
            var descSize = new Vector2(0.67f, 0.15f);
            var padding = new Vector2(0.005f, 0f);

            var descriptionBackgroundPanel = AddCompositePanel(MyGuiConstants.TEXTURE_RECTANGLE_DARK, descPosition, descSize, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            m_descriptionField = AddMultilineText(offset: descPosition + padding, textScale: m_textScale, size: descSize - (padding + new Vector2(0f, 0.005f)), selectable: false);

            RefreshDescriptionField();
        }
        protected void RefreshDescriptionField()
        {
            if (m_descriptionField == null)
            {
                return;
            }
            m_descriptionField.Clear();
            if (m_selectedItem.Description != null)
            {
                m_descriptionField.AppendText(m_selectedItem.Description);
            }
        }

        protected void CreateTextField()
        {
            var textPosition = new Vector2(-0.325f, -0.175f) + m_offset;
            var textSize = new Vector2(0.175f, 0.175f);
            var padding = new Vector2(0.005f, 0f);

            var textBackgroundPanel = AddCompositePanel(MyGuiConstants.TEXTURE_RECTANGLE_DARK, textPosition, textSize, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            m_textField = new MyGuiControlMultilineText();
            m_textField = AddMultilineText(offset: textPosition + padding, textScale: m_textScale, size: textSize - padding);

            RefreshTextField();
        }
        protected void RefreshTextField()
        {
            if (m_textField == null)
            {
                return;
            }
            var displayName = m_selectedItem.ScriptName;
            if (displayName.Length > 25)
            {
                displayName = displayName.Substring(0, 25) + "...";
            }
            m_textField.Clear();
            m_textField.AppendText("Name: " + displayName);
            m_textField.AppendLine();

            m_textField.AppendText("Type: IngameScript");
        }

        public override string GetFriendlyName()
        {
            return "MyDetailScreenScripts";
        }

        void OnRename(MyGuiControlButton button)
        {
            m_dialog = new MyGuiBlueprintTextDialog(
             position: m_position,
             caption: MyTexts.GetString(MySpaceTexts.ProgrammableBlock_NewScriptName),
             defaultName: m_selectedItem.ScriptName,
             maxLenght: maxNameLenght,
             callBack: delegate(string result)
             {
                 if (result != null)
                 {
                     m_parent.ChangeName(result);
                 }
             },
             textBoxWidth: 0.3f
             );

            MyScreenManager.AddScreen(m_dialog);
        }

        void OnDelete(MyGuiControlButton button)
        {
            m_parent.OnDelete(button);
        }

#if !XB1 // XB1_NOWORKSHOP
        void OnPublish(MyGuiControlButton button)
        {
            string modInfoPath = Path.Combine(m_localBlueprintFolder, m_selectedItem.ScriptName, "modinfo.sbmi");
            MyObjectBuilder_ModInfo modInfo;
            if (File.Exists(modInfoPath) && MyObjectBuilderSerializer.DeserializeXML(modInfoPath, out modInfo))
            {
                m_selectedItem.PublishedItemId = modInfo.WorkshopId;
            }

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                styleEnum: MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MyCommonTexts.LoadScreenButtonPublish),
                messageText: MyTexts.Get(MySpaceTexts.ProgrammableBlock_PublishScriptDialogText),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum val)
                {
                    if (val == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        WasPublished = true;
                        string fullPath = Path.Combine(m_localBlueprintFolder, m_selectedItem.ScriptName);
                        MySteamWorkshop.PublishIngameScriptAsync(fullPath, m_selectedItem.ScriptName, m_selectedItem.Description ?? "", m_selectedItem.PublishedItemId, SteamSDK.PublishedFileVisibility.Public,
                            callbackOnFinished: delegate(bool success, Result result, ulong publishedFileId)
                            {
                                if (success)
                                {
                                    MySteamWorkshop.GenerateModInfo(fullPath, publishedFileId, Sync.MyId);
                                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                        styleEnum: MyMessageBoxStyleEnum.Info,
                                        messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextWorldPublished),
                                        messageCaption: MyTexts.Get(MySpaceTexts.ProgrammableBlock_PublishScriptPublished),
                                        callback: (a) =>
                                        {
                                            MySteam.API.OpenOverlayUrl(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", publishedFileId));
                                        }));
                                }
                                else
                                {
                                    MyStringId error;
                                    switch (result)
                                    {
                                        case Result.AccessDenied:
                                            error = MyCommonTexts.MessageBoxTextPublishFailed_AccessDenied;
                                            break;
                                        default:
                                            error = MyCommonTexts.MessageBoxTextWorldPublishFailed;
                                            break;
                                    }

                                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                        messageText: MyTexts.Get(error),
                                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWorldPublishFailed)));
                                }
                            });
                    }
                }));
        }

        void OnOpenWorkshop(MyGuiControlButton button)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_INGAMESCRIPTS, "Steam Workshop");
        }

        void OnOpenInWorkshop(MyGuiControlButton button)
        {
            string url = string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", m_selectedItem.SteamItem.PublishedFileId);
            MyGuiSandbox.OpenUrlWithFallback(url, "Steam Workshop");
        }
#endif // !XB1

        protected void OnCloseButton(MyGuiControlButton button)
        {
            CloseScreen();
        }

        protected void CallResultCallback(MyScriptItemInfo val)
        {
            callBack(val);
        }
        protected override void OnClosed()
        {
            base.OnClosed();
            if (m_dialog != null)
            {
                m_dialog.CloseScreen();
            }
            CallResultCallback(m_selectedItem);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            CreateTextField();
            CreateDescription();
            CreateButtons();
        }
    }
}
