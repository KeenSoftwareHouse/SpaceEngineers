using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;

using VRageMath;
using Sandbox.Engine.Multiplayer;
using VRage;
using VRage.Utils;
using SteamSDK;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenLoadSandbox : MyGuiScreenBase
    {
        enum StateEnum
        {
            ListNeedsReload,
            ListLoading,
            ListLoaded
        }

        private MyGuiControlTable m_sessionsTable;
        private MyGuiControlButton m_continueLastSave, m_loadButton, m_editButton, m_saveButton, m_deleteButton, m_publishButton, m_subscribedWorldsButton;
        private int m_selectedRow;
        private List<Tuple<string, MyWorldInfo>> m_availableSaves = new List<Tuple<string, MyWorldInfo>>();
        private StateEnum m_state;

        // Client has loaded world from server.
        public static event Action BattleWorldLoaded;
        public static event Action ScenarioWorldLoaded;


        public MyGuiScreenLoadSandbox()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.95f, 0.8f))
        {
            EnabledBackgroundFade = true;
            m_state = StateEnum.ListNeedsReload;
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenCaptionLoadWorld);

            var origin = new Vector2(-0.4375f, -0.3f);
            Vector2 buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;

            m_sessionsTable = new MyGuiControlTable();
            m_sessionsTable.Position = origin + new Vector2(buttonSize.X * 1.1f, 0f);
            m_sessionsTable.Size = new Vector2(1075f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 0.15f);
            m_sessionsTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_sessionsTable.ColumnsCount = 2;
            m_sessionsTable.VisibleRowsCount = 17;
            m_sessionsTable.ItemSelected += OnTableItemSelected;
            m_sessionsTable.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            m_sessionsTable.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            m_sessionsTable.SetCustomColumnWidths(new float[] { 0.65f, 0.35f });
            m_sessionsTable.SetColumnComparison(0, (a, b) => ((StringBuilder)a.UserData).CompareToIgnoreCase((StringBuilder)b.UserData));
            m_sessionsTable.SetColumnComparison(1, (a, b) => ((DateTime)a.UserData).CompareTo((DateTime)b.UserData));
            Controls.Add(m_sessionsTable);

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
            m_publishButton = MakeButton(buttonOrigin + buttonDelta * 6, MyCommonTexts.LoadScreenButtonPublish, OnPublishClick);
            if (!MyFakes.XB1_PREVIEW)
            {
                Controls.Add(m_publishButton);
            }

            m_publishButton.SetToolTip(MyTexts.GetString(MyCommonTexts.LoadScreenButtonTooltipPublish));

            m_continueLastSave.Enabled = false;
            m_continueLastSave.DrawCrossTextureWhenDisabled = false;
            m_loadButton.DrawCrossTextureWhenDisabled = false;
            m_editButton.DrawCrossTextureWhenDisabled = false;
            m_deleteButton.DrawCrossTextureWhenDisabled = false;
            m_saveButton.DrawCrossTextureWhenDisabled = false;
            m_publishButton.DrawCrossTextureWhenDisabled = false;

            CloseButtonEnabled = true;

            if (m_state == StateEnum.ListLoaded)
                m_state = StateEnum.ListNeedsReload;
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

        void OnEditClick(MyGuiControlButton sender)
        {
            var row = m_sessionsTable.SelectedRow;
            if (row == null)
                return;
            var save = FindSave(row);
            if (save != null)
            {
                ulong dummySizeInBytes;
                var checkpoint = MyLocalCache.LoadCheckpoint(save.Item1, out dummySizeInBytes);
                MySession.FixIncorrectSettings(checkpoint.Settings);
                var worldSettingsScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.EditWorldSettingsScreen, checkpoint, save.Item1);
                MyGuiSandbox.AddScreen(worldSettingsScreen);
                worldSettingsScreen.Closed += (source) => { m_state = StateEnum.ListNeedsReload; };
            }
        }

        void OnSaveAsClick(MyGuiControlButton sender)
        {
            var row = m_sessionsTable.SelectedRow;
            if (row == null)
                return;

            var save = FindSave(row);
            if (save != null)
            {
                var saveAsScreen = new MyGuiScreenSaveAs(save.Item2, save.Item1, m_availableSaves.Select((x) => x.Item2.SessionName).ToList());
                MyGuiSandbox.AddScreen(saveAsScreen);
                saveAsScreen.Closed += (source) => { m_state = StateEnum.ListNeedsReload; };
            }
        }

        void OnDeleteClick(MyGuiControlButton sender)
        {
            var row = m_sessionsTable.SelectedRow;
            if (row == null)
                return;
            var save = FindSave(row);
            if (save != null)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxTextAreYouSureYouWantToDeleteSave, save.Item2.SessionName),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                    callback: OnDeleteConfirm));
            }
        }

        void OnDeleteConfirm(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                var row = m_sessionsTable.SelectedRow;
                if (row == null)
                    return;
                var save = FindSave(row);
                if (save != null)
                {
                    try
                    {
                        Directory.Delete(save.Item1, true);
                        m_sessionsTable.RemoveSelectedRow();
                        m_sessionsTable.SelectedRowIndex = m_selectedRow;
                        m_availableSaves.Remove(save);
                    }
                    catch
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.OK,
                            messageText: MyTexts.Get(MyCommonTexts.SessionDeleteFailed)));
                    }
                }
            }
        }

        void OnContinueLastGameClick(MyGuiControlButton sender)
        {
            if (m_availableSaves.Count > 0)
            {
                // Loads the first session, which should be the most recent in terms of last saved time
                // but maybe we'll want in terms of last opened time
                LoadSingleplayerSession(m_availableSaves.First().Item1);
            }
        }

        void OnBackClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }

        void OnPublishClick(MyGuiControlButton sender)
        {
#if !XB1 // XB1_NOWORKSHOP
            var row = m_sessionsTable.SelectedRow;
            if (row == null)
                return;
            var save = FindSave(row);
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

        void OnTableItemConfirmedOrDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            LoadSandbox();
        }
        #endregion

        private Tuple<string, MyWorldInfo> FindSave(MyGuiControlTable.Row row)
        {
            string savePath = (string)row.UserData;
            var entry = m_availableSaves.Find((x) => x.Item1 == savePath);
            return entry;
        }

        private void LoadSandbox()
        {
            MyLog.Default.WriteLine("LoadSandbox() - Start");

            var row = m_sessionsTable.SelectedRow;
            if (row != null)
            {
                string savePath = (string)row.UserData;
                LoadSingleplayerSession(savePath);
            }

            MyLog.Default.WriteLine("LoadSandbox() - End");
        }

        public static void LoadMultiplayerSession(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");

            if (!MySteamWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            MySteamWorkshop.DownloadModsAsync(world.Checkpoint.Mods,
                onFinishedCallback: 
                delegate(bool success,string mismatchMods)
                {                       
                    if (success)
                    {
                        CheckMismatchmods(mismatchMods, delegate(VRage.Game.ModAPI.ResultEnum val)
                        {
                            if (val == VRage.Game.ModAPI.ResultEnum.OK)
                            {
                                //Sandbox.Audio.MyAudio.Static.Mute = true;
                                MyScreenManager.CloseAllScreensNowExcept(null);
                                MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

                                // May be called from gameplay, so we must make sure we unload the current game
                                if (MySession.Static != null)
                                {
                                    MySession.Static.Unload();
                                    MySession.Static = null;
                                }

                                MyGuiScreenGamePlay.StartLoading(delegate { MySession.LoadMultiplayer(world, multiplayerSession); });
                            }
                            else
                            {
                                MyGuiScreenMainMenu.UnloadAndExitToMenu();
                            }
                        });
                    }
                    else
                    {
                        if (MyMultiplayer.Static != null)
                        {
                            MyMultiplayer.Static.Dispose();
                        }

                        if (MySteam.IsOnline)
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                                messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                                buttonType: MyMessageBoxButtonsType.OK));
                        }
                        else
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                                          messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                                                          messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline),
                                                          buttonType: MyMessageBoxButtonsType.OK));
                        }
                    }
                    MyLog.Default.WriteLine("LoadSession() - End");
                },
                onCancelledCallback: delegate()
                {
                    multiplayerSession.Dispose();
                });
        }

        public static void CheckMismatchmods(string mismatchMods, Action<VRage.Game.ModAPI.ResultEnum> callback)
        {
            if (String.IsNullOrEmpty(mismatchMods) == false && String.IsNullOrWhiteSpace(mismatchMods) == false)
            {
                MyGuiSandbox.AddScreen(new MyGuiScreenText(
                    windowSize:new Vector2(0.73f, 0.7f),
                    descSize: new Vector2(0.62f,0.44f),
                    missionTitle: MyTexts.GetString(MyCommonTexts.MessageBoxCaptionWarning),
                    currentObjectivePrefix:"",
                    currentObjective: MyTexts.GetString(MyCommonTexts.MessageBoxModsMismatch),
                    description: mismatchMods,
                    resultCallback: callback));
            }
            else if(callback != null)
            {
                callback(VRage.Game.ModAPI.ResultEnum.OK);
            }
        }

        public static void LoadMultiplayerScenarioWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            Debug.Assert(MySession.Static != null);

            MyLog.Default.WriteLine("LoadMultiplayerScenarioWorld() - Start");

            if (world.Checkpoint.BriefingVideo != null && world.Checkpoint.BriefingVideo.Length > 0)
                MyGuiSandbox.OpenUrlWithFallback(world.Checkpoint.BriefingVideo, "Scenario briefing video", true);

            if (!MySteamWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK,
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MyGuiScreenMainMenu.ReturnToMainMenu(); }));
                MyLog.Default.WriteLine("LoadMultiplayerScenarioWorld() - End");
                return;
            }

            MySteamWorkshop.DownloadModsAsync(world.Checkpoint.Mods,
                onFinishedCallback: delegate(bool success,string mismatchMods)
                {
                    if (success)
                    {
                        CheckMismatchmods(mismatchMods, callback: delegate(VRage.Game.ModAPI.ResultEnum val)
                        {
                            MyScreenManager.CloseAllScreensNowExcept(null);
                            MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

                            MyGuiScreenGamePlay.StartLoading(delegate
                            {
                                MySession.Static.LoadMultiplayerWorld(world, multiplayerSession);
                                if (ScenarioWorldLoaded != null)
                                    ScenarioWorldLoaded();
                            });
                        });
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                            buttonType: MyMessageBoxButtonsType.OK,
                            callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MyGuiScreenMainMenu.ReturnToMainMenu(); }));
                    }
                    MyLog.Default.WriteLine("LoadMultiplayerScenarioWorld() - End");
                },
                onCancelledCallback: delegate()
                {
                    MyGuiScreenMainMenu.UnloadAndExitToMenu();
                });
        }

        public static void LoadMultiplayerBattleWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            MyLog.Default.WriteLine("LoadMultiplayerBattleWorld() - Start");

            if (!MySteamWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK,
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MyGuiScreenMainMenu.ReturnToMainMenu(); }));
                MyLog.Default.WriteLine("LoadMultiplayerBattleWorld() - End");
                return;
            }

            MySteamWorkshop.DownloadModsAsync(world.Checkpoint.Mods,
                onFinishedCallback: delegate(bool success,string mismatchMods)
                {
                    if (success)
                    {
                        MyScreenManager.CloseAllScreensNowExcept(null);
                        MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
                        CheckMismatchmods(mismatchMods, callback: delegate(VRage.Game.ModAPI.ResultEnum val)
                        {
                            MyGuiScreenGamePlay.StartLoading(delegate
                            {
                                if (MySession.Static == null)
                                {
                                    MySession.CreateWithEmptyWorld(multiplayerSession);
                                    MySession.Static.Settings.Battle = true;
                                }

                                MySession.Static.LoadMultiplayerWorld(world, multiplayerSession);
                                Debug.Assert(MySession.Static.Battle);
                                if (BattleWorldLoaded != null)
                                    BattleWorldLoaded();
                            });
                        });
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                            buttonType: MyMessageBoxButtonsType.OK,
                            callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MyGuiScreenMainMenu.ReturnToMainMenu(); }));
                    }
                    MyLog.Default.WriteLine("LoadMultiplayerBattleWorld() - End");
                },
                onCancelledCallback: delegate()
                {
                    MyGuiScreenMainMenu.UnloadAndExitToMenu();
                });
        }

        private static void CheckDx11AndLoad(MyObjectBuilder_Checkpoint checkpoint, string sessionPath, ulong checkpointSizeInBytes)
        {
            bool needsDx11 = checkpoint.RequiresDX >= 11;
            if (!needsDx11 || MySandboxGame.IsDirectX11)
            {
                LoadSingleplayerSession(checkpoint, sessionPath, checkpointSizeInBytes);
            }
            else
            {
                MyJoinGameHelper.HandleDx11Needed();
            }
        }

        public static void LoadSingleplayerSession(string sessionPath)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");
            MyLog.Default.WriteLine(sessionPath);

            ulong checkpointSizeInBytes;
            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkpointSizeInBytes);
            CheckDx11AndLoad(checkpoint, sessionPath, checkpointSizeInBytes);
        }
        public static void LoadSingleplayerSession(MyObjectBuilder_Checkpoint checkpoint, string sessionPath, ulong checkpointSizeInBytes)
        {
            if (!MySession.IsCompatibleVersion(checkpoint))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            if (!MySteamWorkshop.CheckLocalModsAllowed(checkpoint.Mods, checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }


            MySteamWorkshop.DownloadModsAsync(checkpoint.Mods, delegate(bool success,string mismatchMods)
            {
                if (success || (checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE) && MySteamWorkshop.CanRunOffline(checkpoint.Mods))
                {
                    //Sandbox.Audio.MyAudio.Static.Mute = true;           
                    MyScreenManager.CloseAllScreensNowExcept(null);
                    MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
                    CheckMismatchmods(mismatchMods, callback: delegate(VRage.Game.ModAPI.ResultEnum val)
                        {
                            if (val == VRage.Game.ModAPI.ResultEnum.OK)
                            {
                                // May be called from gameplay, so we must make sure we unload the current game
                                if (MySession.Static != null)
                                {
                                    MySession.Static.Unload();
                                    MySession.Static = null;
                                }
                                MyGuiScreenGamePlay.StartLoading(delegate
                                {
                                    MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Load);
                                    MySession.Load(sessionPath, checkpoint, checkpointSizeInBytes);
                                });
                            }
                            else
                            {
                                MyGuiScreenMainMenu.ReturnToMainMenu();
                            }
                        });                      
                }
                else
                {
                    MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed).ToString());

                    if (MySteam.IsOnline)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                        messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                        buttonType: MyMessageBoxButtonsType.OK, callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                        {
                            if (MyFakes.QUICK_LAUNCH != null)
                                MyGuiScreenMainMenu.ReturnToMainMenu();
                        }));
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                                      messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                                                      messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline),
                                                      buttonType: MyMessageBoxButtonsType.OK));
                    }
                   
                }
                MyLog.Default.WriteLine("LoadSession() - End");
            });

        }

        public override bool Update(bool hasFocus)
        {
            if (m_state == StateEnum.ListNeedsReload)
                FillList();

            if (m_sessionsTable.SelectedRow != null)
            {
                m_loadButton.Enabled = true;
                m_editButton.Enabled = true;
                m_deleteButton.Enabled = true;
                m_saveButton.Enabled = true;
                m_publishButton.Enabled = true;
            }
            else
            {
                m_loadButton.Enabled = false;
                m_editButton.Enabled = false;
                m_deleteButton.Enabled = false;
                m_saveButton.Enabled = false;
                m_publishButton.Enabled = false;
            }

            return base.Update(hasFocus);
        }

        public override bool Draw()
        {
            // Dont draw screen when the list is about to be reloaded,
            // otherwise it will flick just before opening the loading screen
            if (m_state != StateEnum.ListLoaded)
                return false;
            return base.Draw();
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (m_state == StateEnum.ListNeedsReload)
                FillList();
        }

        #region Async Loading

        void FillList()
        {
            m_state = StateEnum.ListLoading;
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginAction, endAction));
        }

        private void AddHeaders()
        {
            m_sessionsTable.SetColumnName(0, MyTexts.Get(MyCommonTexts.Name));
            m_sessionsTable.SetColumnName(1, MyTexts.Get(MyCommonTexts.Loaded));
        }

        private void RefreshGameList()
        {
            string selectedWorldSavePath = null;
            {
                var selectedRow = m_sessionsTable.SelectedRow;
                if (selectedRow != null)
                    selectedWorldSavePath = (string)selectedRow.UserData;
            }

            m_sessionsTable.Clear();
            AddHeaders();

            for (int index = 0; index < m_availableSaves.Count; index++)
            {
                var checkpoint = m_availableSaves[index].Item2;
                var name = new StringBuilder(checkpoint.SessionName);

                var row = new MyGuiControlTable.Row(m_availableSaves[index].Item1);
                row.AddCell(new MyGuiControlTable.Cell(text: name,
                                                         userData: name));
                row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(checkpoint.LastLoadTime.ToString()),
                                                         userData: checkpoint.LastLoadTime));
                m_sessionsTable.Add(row);

                // Select row with same world ID as we had before refresh.
                if (selectedWorldSavePath != null && m_availableSaves[index].Item1 == selectedWorldSavePath)
                    m_selectedRow = index;
            }

            m_sessionsTable.SelectedRowIndex = m_selectedRow;
            m_sessionsTable.ScrollToSelection();
        }

        public void SelectSteamWorld(string sessionPath)
        {
            int i;
            for (i = 0; i < m_availableSaves.Count; i++)
                if (sessionPath.Equals(m_availableSaves[i].Item1))
                    break;

            Debug.Assert(i >= m_availableSaves.Count, "table row out of range!!!");

            m_sessionsTable.SelectedRowIndex = i;
            m_sessionsTable.ScrollToSelection();
        }

        private IMyAsyncResult beginAction()
        {
            return new MyLoadWorldInfoListResult();
        }

        private void endAction(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;
            m_availableSaves = loadListRes.AvailableSaves;

            if (loadListRes.ContainsCorruptedWorlds)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.SomeWorldFilesCouldNotBeLoaded),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(messageBox);
            }

            if (m_availableSaves.Count != 0)
            {
                RefreshGameList();
                m_continueLastSave.Enabled = true;
            }
            else
            {
                m_continueLastSave.Enabled = false;
                CloseScreenNow(); // close right away to avoid seeing the screen at all
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextNoSavedWorlds)));
            }
            screen.CloseScreen();
            m_state = StateEnum.ListLoaded;
        }

        #endregion
    }
}
