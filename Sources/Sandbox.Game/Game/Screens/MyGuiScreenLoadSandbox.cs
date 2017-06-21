using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using VRageMath;
using VRage;
using VRage.Utils;
using SteamSDK;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics;
using VRage.FileSystem;
using VRage.Game;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenLoadSandbox : MyGuiScreenBase
    {
        private MyGuiControlSaveBrowser m_saveBrowser;
        private MyGuiControlButton m_continueLastSave, m_loadButton, m_editButton, m_saveButton, m_deleteButton, m_publishButton, m_subscribedWorldsButton, m_backupsButton;
        private int m_selectedRow;

        // Client has loaded world from server.


        public MyGuiScreenLoadSandbox()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.95f, 0.8f))
        {
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenCaptionLoadWorld);

            var origin = new Vector2(-0.4375f, -0.3f);
            Vector2 buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;

            m_saveBrowser = new MyGuiControlSaveBrowser();
            m_saveBrowser.Position = origin + new Vector2(buttonSize.X * 1.1f, 0f);
            m_saveBrowser.Size = new Vector2(1075f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 0.15f);
            m_saveBrowser.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_saveBrowser.VisibleRowsCount = 17;
            m_saveBrowser.ItemSelected += OnTableItemSelected;
            m_saveBrowser.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            m_saveBrowser.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            Controls.Add(m_saveBrowser);

            Vector2 buttonOrigin = origin + buttonSize * 0.5f;
            Vector2 buttonDelta = MyGuiConstants.MENU_BUTTONS_POSITION_DELTA;

            // Continue last game
            // Load
            // Edit
            // Save
            // Delete
            Controls.Add(m_continueLastSave = MakeButton(buttonOrigin + buttonDelta * 0, MyCommonTexts.LoadScreenButtonContinueLastGame, OnContinueLastGameClick));
            Controls.Add(m_loadButton = MakeButton(buttonOrigin + buttonDelta * 1, MyCommonTexts.LoadScreenButtonLoad, OnLoadClick));
            Controls.Add(m_editButton = MakeButton(buttonOrigin + buttonDelta * 2, MyCommonTexts.LoadScreenButtonEditSettings, OnEditClick));
            Controls.Add(m_saveButton = MakeButton(buttonOrigin + buttonDelta * 3, MyCommonTexts.LoadScreenButtonSaveAs, OnSaveAsClick));
            Controls.Add(m_deleteButton = MakeButton(buttonOrigin + buttonDelta * 4, MyCommonTexts.LoadScreenButtonDelete, OnDeleteClick));
            //Controls.Add(MakeButton(buttonOrigin + buttonDelta * 6, MyCommonTexts.ScreenMenuButtonSubscribedWorlds, OnWorkshopClick));
            m_publishButton = MakeButton(buttonOrigin + buttonDelta * 7, MyCommonTexts.LoadScreenButtonPublish, OnPublishClick);
            if (!MyFakes.XB1_PREVIEW)
            {
                Controls.Add(m_publishButton);
            }
            Controls.Add(m_backupsButton = MakeButton(buttonOrigin + buttonDelta * 8, MyCommonTexts.LoadScreenButtonBackups, OnBackupsButtonClick));

            m_publishButton.SetToolTip(MyTexts.GetString(MyCommonTexts.LoadScreenButtonTooltipPublish));

            m_continueLastSave.DrawCrossTextureWhenDisabled = false;
            m_loadButton.DrawCrossTextureWhenDisabled = false;
            m_editButton.DrawCrossTextureWhenDisabled = false;
            m_deleteButton.DrawCrossTextureWhenDisabled = false;
            m_saveButton.DrawCrossTextureWhenDisabled = false;
            m_publishButton.DrawCrossTextureWhenDisabled = false;

            // Debug saves switch checkbox
            if (MyCompilationSymbols.IsDebugBuild)
            {
                var debugWorldForlderCheckbox = new MyGuiControlCheckbox
                {
                    IsChecked = false,
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                    Position = m_saveBrowser.Position + new Vector2(m_saveBrowser.Size.X + 0.0015f, 0f),
                    IsCheckedChanged = DebugWorldCheckboxIsCheckChanged
                };

                var debugLabel = new MyGuiControlLabel
                {
                    Text = "Debug Worlds: ",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                    Position = debugWorldForlderCheckbox.Position - new Vector2(debugWorldForlderCheckbox.Size.X, debugWorldForlderCheckbox.Size.Y / 2),
                    Font = MyFontEnum.Red
                };

                Controls.Add(debugLabel);
                Controls.Add(debugWorldForlderCheckbox);
            }

            CloseButtonEnabled = true;
        }

        private void DebugWorldCheckboxIsCheckChanged(MyGuiControlCheckbox checkbox)
        {
            // Switch the directory to either Content/Worlds or Saves
            string directoryPath = checkbox.IsChecked ? Path.Combine(MyFileSystem.ContentPath, "Worlds") : MyFileSystem.SavesPath;
            m_saveBrowser.SetTopMostAndCurrentDir(directoryPath);
            m_saveBrowser.Refresh();
        }

        private void OnBackupsButtonClick(MyGuiControlButton myGuiControlButton)
        {
            m_saveBrowser.AccessBackups();
        }

        private void OnTableItemConfirmedOrDoubleClick(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            LoadSandbox();
        }

        private MyGuiControlButton MakeButton(Vector2 position, MyStringId text, Action<MyGuiControlButton> onClick)
        {
            return new MyGuiControlButton(
                            position: position,
                            text: MyTexts.Get(text),
                            onButtonClick: onClick);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenLoadSandbox";
        }

        #region Event handlers
        void OnLoadClick(MyGuiControlButton sender)
        {
            LoadSandbox();
        }

        void OnWorkshopClick(MyGuiControlButton sender)
        {
            MyScreenManager.AddScreen(new MyGuiScreenLoadSubscribedWorld());
        }

        void OnEditClick(MyGuiControlButton sender)
        {
            var row = m_saveBrowser.SelectedRow;
            if (row == null)
                return;
            var save = m_saveBrowser.GetSave(row);
            if (save != null)
            {
                ulong dummySizeInBytes;
                var checkpoint = MyLocalCache.LoadCheckpoint(save.Item1, out dummySizeInBytes);
                MySession.FixIncorrectSettings(checkpoint.Settings);
                var worldSettingsScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.EditWorldSettingsScreen, checkpoint, save.Item1);
                worldSettingsScreen.Closed += source => m_saveBrowser.ForceRefresh();

                MyGuiSandbox.AddScreen(worldSettingsScreen);
            }
        }

        void OnSaveAsClick(MyGuiControlButton sender)
        {
            var row = m_saveBrowser.SelectedRow;
            if (row == null)
                return;

            var save = m_saveBrowser.GetSave(row);
            if (save != null)
            {
                // TODO: EXISTING SESION NAMES
                var saveAsScreen = new MyGuiScreenSaveAs(save.Item2, save.Item1, null);
                saveAsScreen.SaveAsConfirm += OnSaveAsConfirm;
                MyGuiSandbox.AddScreen(saveAsScreen);
            }
        }

        void OnSaveAsConfirm()
        {
            m_saveBrowser.ForceRefresh();
        }

        void OnDeleteClick(MyGuiControlButton sender)
        {
            var row = m_saveBrowser.SelectedRow;
            if (row == null)
                return;
            var save = m_saveBrowser.GetSave(row);
            if (save != null)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxTextAreYouSureYouWantToDeleteSave, save.Item2.SessionName),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                    callback: OnDeleteConfirm));
            } 
            else
            {
                var directory = m_saveBrowser.GetDirectory(row);
                if (directory != null)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxTextAreYouSureYouWantToDeleteSave, directory.Name),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                        callback: OnDeleteConfirm));
                }
            }
        }

        void OnDeleteConfirm(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                var row = m_saveBrowser.SelectedRow;
                if (row == null)
                    return;
                var save = m_saveBrowser.GetSave(row);
                if (save != null)
                {
                    try
                    {
                        Directory.Delete(save.Item1, true);
                        m_saveBrowser.RemoveSelectedRow();
                        m_saveBrowser.SelectedRowIndex = m_selectedRow;
                        m_saveBrowser.Refresh();
                    }
                    catch(Exception e)
                    {
                        Debug.Fail(e.ToString());

                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.OK,
                            messageText: MyTexts.Get(MyCommonTexts.SessionDeleteFailed)));
                    }
                }
                else
                {
                    try
                    {
                        var directory = m_saveBrowser.GetDirectory(row);
                        if (directory != null)
                        {
                            directory.Delete(true);
                            m_saveBrowser.Refresh();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Fail(e.ToString());

                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.OK,
                            messageText: MyTexts.Get(MyCommonTexts.SessionDeleteFailed)));
                    }
                }
            }
        }

        void OnContinueLastGameClick(MyGuiControlButton sender)
        {
            MySessionLoader.LoadLastSession();
            m_continueLastSave.Enabled = false;
        }

        void OnPublishClick(MyGuiControlButton sender)
        {
#if !XB1 // XB1_NOWORKSHOP
            var row = m_saveBrowser.SelectedRow;
            if (row == null)
                return;
            var save = m_saveBrowser.GetSave(row);
            if (save != null)
            {
                Publish(save.Item1, save.Item2);
            }
#else // XB1
            System.Diagnostics.Debug.Assert(false); // TODO?
#endif // XB1
        }

#if !XB1 // XB1_NOWORKSHOP
        public static void Publish(string sessionPath, MyWorldInfo worlInfo)
        {
            if (MyFakes.XBOX_PREVIEW)
            {
                MyGuiSandbox.Show(MyCommonTexts.MessageBoxTextErrorFeatureNotAvailableYet, MyCommonTexts.MessageBoxCaptionError);
                return;
            }

            MyStringId textQuestion, captionQuestion;
            if (worlInfo.WorkshopId.HasValue)
            {
                textQuestion = MyCommonTexts.MessageBoxTextDoYouWishToUpdateWorld;
                captionQuestion = MyCommonTexts.MessageBoxCaptionDoYouWishToUpdateWorld;
            }
            else
            {
                textQuestion = MyCommonTexts.MessageBoxTextDoYouWishToPublishWorld;
                captionQuestion = MyCommonTexts.MessageBoxCaptionDoYouWishToPublishWorld;
            }

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                styleEnum: MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageText: MyTexts.Get(textQuestion),
                messageCaption: MyTexts.Get(captionQuestion),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum val)
                {
                    if (val == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        Action<MyGuiScreenMessageBox.ResultEnum, string[]> onTagsChosen = delegate(MyGuiScreenMessageBox.ResultEnum tagsResult, string[] outTags)
                        {
                            if (tagsResult == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                MySteamWorkshop.PublishWorldAsync(sessionPath, worlInfo.SessionName, worlInfo.Description, worlInfo.WorkshopId, outTags, SteamSDK.PublishedFileVisibility.Public,
                                    callbackOnFinished: delegate(bool success, Result result, ulong publishedFileId)
                                    {
                                        if (success)
                                        {
                                            ulong dummy;
                                            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out dummy);
                                            worlInfo.WorkshopId = publishedFileId;
                                            checkpoint.WorkshopId = publishedFileId;
                                            MyLocalCache.SaveCheckpoint(checkpoint, sessionPath);
                                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                                styleEnum: MyMessageBoxStyleEnum.Info,
                                                messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextWorldPublished),
                                                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWorldPublished),
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
                        };

                        if (MySteamWorkshop.WorldCategories.Length > 0)
                            MyGuiSandbox.AddScreen(new MyGuiScreenWorkshopTags(MySteamWorkshop.WORKSHOP_WORLD_TAG, MySteamWorkshop.WorldCategories, null, onTagsChosen));
                        else
                            onTagsChosen(MyGuiScreenMessageBox.ResultEnum.YES, new string[] { MySteamWorkshop.WORKSHOP_WORLD_TAG });
                    }
                }));

        }
#endif // !XB1

        void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_selectedRow = eventArgs.RowIndex;
        }

        #endregion

        private void LoadSandbox()
        {
            MyLog.Default.WriteLine("LoadSandbox() - Start");

            var row = m_saveBrowser.SelectedRow;
            if (row != null)
            {
                var saveInfo = m_saveBrowser.GetSave(row);
                if (saveInfo == null || saveInfo.Item2.IsCorrupted) return;
                MySessionLoader.LoadSingleplayerSession(saveInfo.Item1);
            }

            MyLog.Default.WriteLine("LoadSandbox() - End");
        }

        public override bool Update(bool hasFocus)
        {
            var save = m_saveBrowser.GetSave(m_saveBrowser.SelectedRow);

            if (save != null)
            {
                m_loadButton.Enabled = true;
                m_editButton.Enabled = true;
                m_saveButton.Enabled = true;
                m_publishButton.Enabled = true;
                m_backupsButton.Enabled = true;
            }
            else
            {
                m_loadButton.Enabled = false;
                m_editButton.Enabled = false;
                m_saveButton.Enabled = false;
                m_publishButton.Enabled = false;
                m_backupsButton.Enabled = false;
            }

            m_deleteButton.Enabled = m_saveBrowser.SelectedRow != null;

            return base.Update(hasFocus);
        }
    }
}
