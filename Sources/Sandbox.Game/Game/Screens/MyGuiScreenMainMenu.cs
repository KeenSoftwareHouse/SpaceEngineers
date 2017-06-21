#region Using

using ParallelTasks;
using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Audio;
using Sandbox.Game.Entities;
#if !XB1
using Sandbox.Game.Gui.DebugInputComponents;
#endif // !XB1
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using VRage;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.News;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using VRageMath;

#endregion


namespace Sandbox.Game.Gui
{
    public class MyGuiScreenMainMenu : MyGuiScreenBase
    {
        private bool m_musicPlayed = false;
        private const int PLAY_MUSIC_AFTER_MENU_LOADED_MS = 1000;
        private const float TEXT_LINE_HEIGHT = 0.024f;
#if !XB1
        private static readonly StringBuilder BUILD_DATE = new StringBuilder("Build: " + MySandboxGame.BuildDateTime.ToString("yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture));
#else // XB1
        private static readonly StringBuilder BUILD_DATE = new StringBuilder("Build: N/A (XB1 TODO?)");
#endif // XB1
        private static readonly StringBuilder APP_VERSION = MyFinalBuildConstants.APP_VERSION_STRING;
        private static readonly StringBuilder STEAM_INACTIVE = new StringBuilder("STEAM NOT AVAILABLE");
        private static readonly StringBuilder NOT_OBFUSCATED = new StringBuilder("NOT OBFUSCATED");
        private static readonly StringBuilder NON_OFFICIAL = new StringBuilder(" NON-OFFICIAL");
#if XB1
        private static readonly StringBuilder PLATFORM = new StringBuilder(" 64-bit");
#else // !XB1
        private static readonly StringBuilder PLATFORM = new StringBuilder(Environment.Is64BitProcess ? " 64-bit" : " 32-bit");
#endif // !XB1
        private static StringBuilder BranchName = new StringBuilder(50);
        private static StringBuilder m_stringCache = new StringBuilder(128);

        //News
        MyGuiControlNews m_newsControl;
        Task m_downloadNewsTask;
        MyNews m_news;
        XmlSerializer m_newsSerializer;
        bool m_downloadedNewsOK = false;
        bool m_downloadedNewsFinished = false;
        bool m_pauseGame = false;
        private static readonly char[] m_trimArray = new char[] { ' ', (char)13, '\r', '\n' };
        private static readonly char[] m_splitArray = new char[] { '\r', '\n' };

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMainMenu";
        }

        //  This is for adding main menu the easy way
        public static void AddMainMenu(bool pauseGame = false)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenMainMenu(pauseGame));
        }

        public MyGuiScreenMainMenu(bool pauseGame)
            : base(Vector2.Zero, null, null)
        {
            if (MyGuiScreenGamePlay.Static == null)
            {
                m_closeOnEsc = false;
            }
            else
            {
                m_pauseGame = pauseGame;
                if(m_pauseGame&&MySandboxGame.IsPaused ==false)
                {
                    MySandboxGame.UserPauseToggle();
                }
            }

            //if (MyGuiScreenGamePlay.Static.GetGameType() == MyGuiScreenGamePlayType.MAIN_MENU) m_closeOnEsc = false;
            //if (MyGuiScreenGamePlay.Static.IsPausable()) MySandboxGame.SwitchPause();

            //Because then it is visible under credits, help, etc..
            m_drawEvenWithoutFocus = false;

            if (MyGuiScreenGamePlay.Static == null)
            {
                //We dont want to load last session if we end up game in main menu
                MyLocalCache.SaveLastSessionInfo(null);
            }

            try
            {
                m_newsSerializer = new XmlSerializer(typeof(MyNews));
            }
            catch { };
        }

        //  Because only main menu's controla depends on fullscreen pixel coordinates (not normalized), after we change
        //  screen resolution we need to recreate controls too. Otherwise they will be still on old/bad positions, and
        //  for example when changing from 1920x1200 to 800x600 they would be out of screen
        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);


            // Enable background fade when we're in game, but in main menu we disable it.
            var buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;
            Vector2 leftButtonPositionOrigin = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM) + new Vector2(buttonSize.X / 2f, 0f);
            Vector2 rightButtonPositionOrigin = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM) + new Vector2(-buttonSize.X / 2f, 0f);

            // In main menu
            if (MyGuiScreenGamePlay.Static == null)
            {
                EnabledBackgroundFade = false;
                // Left main menu part

                var a = MyGuiManager.GetSafeFullscreenRectangle();
                var fullScreenSize = new Vector2(a.Width / (a.Height * (4 / 3f)), 1f);

                // New Game
                // Load world
                // Join world
                // Workshop
                //
                // Options
                // Help
                // Credits
                // Exit to windows
                int buttonIndex = MyPerGameSettings.MultiplayerEnabled ? 8 : 7;
                if (MyFakes.XB1_PREVIEW)
                {
                    buttonIndex = MyPerGameSettings.MultiplayerEnabled ? 7 : 6;
                }
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonNewWorld, OnClickNewWorld));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonLoadWorld, OnClickLoad));
                if (MyPerGameSettings.MultiplayerEnabled)
                    Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonJoinWorld, OnJoinWorld));
                if (!MyFakes.XB1_PREVIEW)
                {
#if !XB1 // XB1_NOWORKSHOP
                    Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonSubscribedWorlds, OnClickSubscribedWorlds, MyCommonTexts.ToolTipMenuSubscribedWorlds));
#endif // !XB1
                }
                --buttonIndex;
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonOptions, OnClickOptions));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonHelp, OnClickHelp));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonCredits, OnClickCredits));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonExitToWindows, OnClickExitToWindows));

                Vector2 textRightTopPosition = MyGuiManager.GetScreenTextRightTopPosition();
                Vector2 position = textRightTopPosition + 8f * MyGuiConstants.CONTROLS_DELTA + new Vector2(-.1f, .06f);
            }
            else // In-game
            {
                MyAnalyticsHelper.ReportActivityStart(null, "show_main_menu", string.Empty, "gui", string.Empty);

                EnabledBackgroundFade = true;
                int buttonRowIndex = Sync.MultiplayerActive ? 6 : 5;

                // Save
                // Load button (only on dev)
                //
                // Options
                // Help
                // Exit to main menu
                var saveButton = MakeButton(leftButtonPositionOrigin - ((float)(--buttonRowIndex)) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonSave, OnClickSaveWorld);
                var saveAsButton = MakeButton(leftButtonPositionOrigin - ((float)(--buttonRowIndex)) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.LoadScreenButtonSaveAs, OnClickSaveAs);

                if (!Sync.IsServer)
                {
                    saveButton.Enabled = false;
                    saveButton.ShowTooltipWhenDisabled = true;
                    saveButton.SetToolTip(MyCommonTexts.NotificationClientCannotSave);

                    saveAsButton.Enabled = false;
                    saveButton.ShowTooltipWhenDisabled = true;
                    saveButton.SetToolTip(MyCommonTexts.NotificationClientCannotSave);
                }

                Controls.Add(saveButton);
                Controls.Add(saveAsButton);

 //               --buttonRowIndex; // empty line
                if (Sync.MultiplayerActive)
                    Controls.Add(MakeButton(leftButtonPositionOrigin - ((float)(--buttonRowIndex)) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonPlayers, OnClickPlayers));
                Controls.Add(MakeButton(leftButtonPositionOrigin - ((float)(--buttonRowIndex)) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonOptions, OnClickOptions));
                Controls.Add(MakeButton(leftButtonPositionOrigin - ((float)(--buttonRowIndex)) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonHelp, OnClickHelp));
                Controls.Add(MakeButton(leftButtonPositionOrigin - ((float)(--buttonRowIndex)) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonExitToMainMenu, OnExitToMainMenuClick));
            }

            var logoPanel = new MyGuiControlPanel(
                position: MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, 54, 84),
                size: MyGuiConstants.TEXTURE_KEEN_LOGO.MinSizeGui,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP
                );
            logoPanel.BackgroundTexture = MyGuiConstants.TEXTURE_KEEN_LOGO;
            Controls.Add(logoPanel);

            // Recommend button
            Vector2 pos = rightButtonPositionOrigin - 8f * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA;
            Controls.Add(MakeButton(pos, MyCommonTexts.ScreenMenuButtonRecommend, OnClickRecommend));
            m_newsControl = new MyGuiControlNews()
            {
                Position = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM) - 7f * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                Size = new Vector2(0.4f, 0.28f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
            };
            Controls.Add(m_newsControl);

            var webButton = MakeButton(
                MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, 70),
                MySpaceTexts.Blank, OnClickGameWeb);
            webButton.Text = MyPerGameSettings.GameWebUrl;
            webButton.VisualStyle = MyGuiControlButtonStyleEnum.UrlText;
            Controls.Add(webButton);

            var reportButton = MakeButton(
                new Vector2(m_newsControl.Position.X , m_newsControl.Position.Y + m_newsControl.Size.Y),
                //MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM, 140,80),
                MyCommonTexts.ReportBug, OnClickReportBug);
            reportButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            reportButton.VisualStyle = MyGuiControlButtonStyleEnum.UrlText;
            Controls.Add(reportButton);

            m_newsControl.State = MyGuiControlNews.StateEnum.Loading;
            DownloadNews();
            CheckLowMemSwitchToLow();
        }

        private void CheckLowMemSwitchToLow()
        {
            if (MySandboxGame.Config.LowMemSwitchToLow==MyConfig.LowMemSwitch.TRIGGERED)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                        {
                            if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                MySandboxGame.Config.LowMemSwitchToLow = MyConfig.LowMemSwitch.ARMED;
                                MySandboxGame.Config.SetToLowQuality();
                                MySandboxGame.Config.Save();
                                ExitGame();
                            }
                            else
                            {
                                MySandboxGame.Config.LowMemSwitchToLow = MyConfig.LowMemSwitch.USER_SAID_NO;
                                MySandboxGame.Config.Save();
                            };
                            
                        },
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.LowMemSwitchToLowQuestion),
                    buttonType: MyMessageBoxButtonsType.YES_NO));
            }
        }

        public static void ExitGame()
        {
            MyAnalyticsTracker.SendGameEnd("Exit to Windows", MySandboxGame.TotalTimeInMilliseconds / 1000);
            MyScreenManager.CloseAllScreensNowExcept(null);
            MySandboxGame.ExitThreadSafe();
        }

        private void OnClickReportBug(MyGuiControlButton obj)
        {
            MyGuiSandbox.OpenUrl(MyPerGameSettings.BugReportUrl, UrlOpenMode.SteamOrExternalWithConfirm, MyTexts.AppendFormat(new StringBuilder(), MyCommonTexts.MessageBoxTextOpenBrowser, "forums.keenswh.com"));
        }

        private void SetNews(MyNews news)
        {
            m_newsControl.Show(news);
        }

        private void DownloadNews()
        {
            m_downloadNewsTask = Parallel.Start(DownloadNewsAsync);
        }

        void DownloadNewsAsync()
        {
            try
            {
                var newsDownloadClient = new WebClient();
                newsDownloadClient.Proxy = null;
                var downloadedNews = newsDownloadClient.DownloadString(new Uri(MyPerGameSettings.ChangeLogUrl));

                using (StringReader stream = new StringReader(downloadedNews))
                {
                    m_news = (MyNews)m_newsSerializer.Deserialize(stream);

                    if(MyFinalBuildConstants.IS_STABLE)
                    {
                        m_news.Entry.RemoveAll(entry => entry.Dev);
                    }

                    StringBuilder text = new StringBuilder();
                    for (int i = 0; i < m_news.Entry.Count; i++)
                    {
                        var newsItem = m_news.Entry[i];

                        string itemText = newsItem.Text.Trim(m_trimArray);
                        string[] lines = itemText.Split(m_splitArray);

                        text.Clear();
                        foreach (string lineItem in lines)
                        {
                            string line = lineItem.Trim();
                            text.AppendLine(line);
                        }

                        m_news.Entry[i] = new MyNewsEntry()
                        {
                            Title = newsItem.Title,
                            Version = newsItem.Version,
                            Date = newsItem.Date,
                            Text = text.ToString(),
                        };
                    }

                    if (MyFakes.TEST_NEWS)
                    {
                        var entry = m_news.Entry[m_news.Entry.Count - 1];
                        entry.Title = "Test";
                        entry.Text = "ASDF\nASDF\n[www.spaceengineersgame.com Space engineers web]\n[[File:Textures\\GUI\\MouseCursor.dds|64x64px]]\n";
                        m_news.Entry.Add(entry);
                    }
                    m_downloadedNewsOK = true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while downloading news: " + e.ToString());
            }
            finally
            {
                m_downloadedNewsFinished = true;
            }
        }

        void DownloadNewsCompleted()
        {
            SetNews(m_news);
            CheckVersion();
        }

        void CheckVersion()
        {
            int latestVersion = 0;
            if (m_news.Entry.Count > 0)
            {
                if (int.TryParse(m_news.Entry[0].Version, out latestVersion))
                {
                    if (latestVersion > MyFinalBuildConstants.APP_VERSION)
                    {
                        if (MySandboxGame.Config.LastCheckedVersion != MyFinalBuildConstants.APP_VERSION)
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageText: MyTexts.Get(MySpaceTexts.NewVersionAvailable),
                                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionInfo),
                                styleEnum: MyMessageBoxStyleEnum.Info));

                            MySandboxGame.Config.LastCheckedVersion = MyFinalBuildConstants.APP_VERSION;
                            MySandboxGame.Config.Save();
                        }
                    }
                }
            }
        }

        private MyGuiControlButton MakeButton(Vector2 position, MyStringId text, Action<MyGuiControlButton> onClick, MyStringId? tooltip = null)
        {
            var button = new MyGuiControlButton(
                position: position,
                text: MyTexts.Get(text),
                textScale: MyGuiConstants.MAIN_MENU_BUTTON_TEXT_SCALE,
                onButtonClick: onClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);

            if (tooltip.HasValue)
                button.SetToolTip(MyTexts.GetString(tooltip.Value));

            return button;
        }

        public static void ReturnToMainMenu()
        {
            UnloadAndExitToMenu();
        }

        public static void UnloadAndExitToMenu()
        {
            MyScreenManager.CloseAllScreensNowExcept(null);
            MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

            if (MySession.Static != null)
            {
                MySession.Static.Unload();
                MySession.Static = null;
            }

            if (MyMusicController.Static != null)
            {
                MyMusicController.Static.Unload();
                MyMusicController.Static = null;
                MyAudio.Static.MusicAllowed = true;
            }

            if(MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.Dispose();
            }

            //  This will quit actual game-play screen and move us to fly-through with main menu on top
            MyGuiSandbox.BackToMainMenu();
        }

        #region Event handlers

        private void OnJoinWorld(MyGuiControlButton sender)
        {
            if (MySteam.IsOnline)
            {
                MyGuiSandbox.AddScreen(new MyGuiScreenJoinGame());
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.SteamIsOfflinePleaseRestart)
                ));
            }
        }

        private void OnClickGameWeb(MyGuiControlButton sender)
        {
            MyGuiSandbox.OpenUrl(MyPerGameSettings.GameWebUrl, UrlOpenMode.SteamOrExternalWithConfirm);
        }

        private void OnClickRecommend(MyGuiControlButton sender)
        {
            if (!MyFakes.XBOX_PREVIEW)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        styleEnum: MyMessageBoxStyleEnum.Info,
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionRecommend),
                        messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextRecommend),
                        callback: new Action<MyGuiScreenMessageBox.ResultEnum>(OnClickRecommendOK)
                        ));
            }
            else
            {
                MyGuiSandbox.Show(MyCommonTexts.MessageBoxTextErrorFeatureNotAvailableYet, MyCommonTexts.MessageBoxCaptionError);
            }
        }

        void OnClickRecommendOK(MyGuiScreenMessageBox.ResultEnum result)
        {
            MyGuiSandbox.OpenUrl(MySteamConstants.URL_RECOMMEND_GAME, UrlOpenMode.SteamOrExternal);
        }

        private void OnClickNewWorld(MyGuiControlButton sender)
        {
            if (MyFakes.ENABLE_TUTORIAL_PROMPT && MySandboxGame.Config.NeedShowTutorialQuestion)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: MyTexts.Get(MyPerGameSettings.EnableTutorials ? MySpaceTexts.MessageBoxTextTutorialQuestion : MyCommonTexts.MessageBoxTextGuideQuestion),
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionTutorial),
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum val)
                    {
                        if (val == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            MyAnalyticsHelper.ReportTutorialScreen("FirstTime");
                            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_GUIDE_DEFAULT, "Steam Guide");
                        }
                        else
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenStartSandbox>());
                    }));
                MySandboxGame.Config.NeedShowTutorialQuestion = false;
                MySandboxGame.Config.Save();
            }
            else
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenStartSandbox>());
        }

        private unsafe void OnClickLoad(MyGuiControlBase sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenLoadSandbox());
        }


        private void OnClickPlayers(MyGuiControlButton obj)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.PlayersScreen));
        }

#if !XB1 // XB1_NOWORKSHOP
        private void OnClickSubscribedWorlds(MyGuiControlButton obj)
        {
            if (!MyFakes.XBOX_PREVIEW)
                MyGuiSandbox.AddScreen(new MyGuiScreenLoadSubscribedWorld());
            else
                MyGuiSandbox.Show(MyCommonTexts.MessageBoxTextErrorFeatureNotAvailableYet, MyCommonTexts.MessageBoxCaptionError);
        }
#endif // !XB1

        private void OnExitToMainMenuClick(MyGuiControlButton sender)
        {
            if (!Sync.IsServer)
            {
                UnloadAndExitToMenu();
                return;
            }

            this.CanBeHidden = false;

            var messageBox = MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO_CANCEL,
                messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextSaveChangesBeforeExit),
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionExit),
                callback: OnExitToMainMenuMessageBoxCallback);
            messageBox.SkipTransition = true;
            messageBox.InstantClose = false;
            MyGuiSandbox.AddScreen(messageBox);
        }

        private void OnExitToMainMenuMessageBoxCallback(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            switch (callbackReturn)
            {
                case MyGuiScreenMessageBox.ResultEnum.YES:
                    MyAudio.Static.Mute = true;
                    MyAudio.Static.StopMusic();
                    MyAsyncSaving.Start(callbackOnFinished: delegate() { MySandboxGame.Static.OnScreenshotTaken += UnloadAndExitAfterScreeshotWasTaken; });
                    break;

                case MyGuiScreenMessageBox.ResultEnum.NO:
                    MyAudio.Static.Mute = true;
                    MyAudio.Static.StopMusic();
                    UnloadAndExitToMenu();
                    break;

                case MyGuiScreenMessageBox.ResultEnum.CANCEL:
                    this.CanBeHidden = true;
                    break;
            }
        }

        private void UnloadAndExitAfterScreeshotWasTaken(object sender, EventArgs e)
        {
            MySandboxGame.Static.OnScreenshotTaken -= UnloadAndExitAfterScreeshotWasTaken;
            UnloadAndExitToMenu();
        }

        private void OnClickCredits(MyGuiControlButton sender)
        {
            //opens dialog screen with list of trailers, where could be selected animation to play
            MyGuiSandbox.AddScreen(new MyGuiScreenGameCredits());
        }

        private void OnClickOptions(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.OptionsScreen));
        }

        private void OnClickHelp(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HelpScreen));
        }

        private void OnClickExitToWindows(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextAreYouSureYouWantToExit),
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionExit),
                callback: OnExitToWindowsMessageBoxCallback));
        }

        private void OnExitToWindowsMessageBoxCallback(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                OnLogoutProgressClosed();
            }
        }

        private void OnLogoutProgressClosed()
        {
            MySandboxGame.Log.WriteLine("Application closed by user");
            MyAnalyticsTracker.SendGameEnd("Exit to Windows", MySandboxGame.TotalTimeInMilliseconds / 1000);
            MyScreenManager.CloseAllScreensNowExcept(null);

            //  Exit application
            MySandboxGame.ExitThreadSafe();
        }

        private void OnClickSaveWorld(MyGuiControlButton sender)
        {
            this.CanBeHidden = false;

            MyGuiScreenMessageBox messageBox;
            if (MyAsyncSaving.InProgress)
            {
                messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextSavingInProgress),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
            }
            else
            {
                messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextDoYouWantToSaveYourProgress),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                    callback: OnSaveWorldMessageBoxCallback);
            }
            messageBox.SkipTransition = true;
            messageBox.InstantClose = false;
            MyGuiSandbox.AddScreen(messageBox);
        }

        private void OnSaveWorldMessageBoxCallback(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                MyAsyncSaving.Start();
            else
                CanBeHidden = true;
        }

        void OnClickSaveAs(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenSaveAs(MySession.Static.Name));
        }

        #endregion

        public override void LoadContent()
        {
            base.LoadContent();

            RecreateControls(true);
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.HELP_SCREEN))
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HelpScreen));

            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.ENABLE_DEVELOPER_KEYS)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Multiply) && MyInput.Static.IsAnyShiftKeyPressed())
                {
                    GC.Collect();
                }
                if (MyInput.Static.IsNewKeyPressed(MyKeys.M))
                {
                    RecreateControls(false);
                }
            }
        }

        public override bool CloseScreen()
        {
            if (m_pauseGame)
            {
                MySandboxGame.UserPauseToggle();
            }

            bool ret = base.CloseScreen();

            m_musicPlayed = false;

            MyAnalyticsHelper.ReportActivityEnd(null, "show_main_menu");

            return ret;
        }

        public override bool Draw()
        {
            if (!base.Draw())
                return false;

            MyGuiSandbox.DrawGameLogo(m_transitionAlpha);
            DrawObfuscationStatus();
            DrawSteamStatus();
            DrawAppVersion();
            DrawBuildDate();

            return true;
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            //MySandboxGame.GraphicsDeviceManager.DbgDumpLoadedResources(true);
            //MyTextureManager.DbgDumpLoadedTextures(true);

            if (m_downloadNewsTask.IsComplete && m_downloadedNewsFinished)
            {
                if (m_downloadedNewsOK)
                {
                    DownloadNewsCompleted();
                    m_newsControl.State = MyGuiControlNews.StateEnum.Entries;
                }
                else
                {
                    m_newsControl.State = MyGuiControlNews.StateEnum.Error;
                    m_newsControl.ErrorText = MyTexts.Get(MyCommonTexts.NewsDownloadingFailed);
                }

                m_downloadedNewsFinished = false;
            }

            if (!m_musicPlayed)
            {
                if (MyGuiScreenGamePlay.Static == null)
                {
                    MyAudio.Static.PlayMusic(MyPerGameSettings.MainMenuTrack);
                }
                m_musicPlayed = true;
            }

#if !XB1
            if (MyReloadTestComponent.Enabled && State == MyGuiScreenState.OPENED)
                MyReloadTestComponent.DoReload();
#endif // !XB1

            return true;
        }

        void emmiter_StoppedPlaying(MyEntity3DSoundEmitter obj)
        {
            obj.StoppedPlaying -= emmiter_StoppedPlaying;
            MyAudio.Static.PlayMusic();
        }

        private void DrawBuildDate()
        {
            Vector2 textRightBottomPosition = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            textRightBottomPosition.Y -= 0 * TEXT_LINE_HEIGHT;
            MyGuiManager.DrawString(MyFontEnum.BuildInfo, BUILD_DATE, textRightBottomPosition, MyGuiConstants.APP_VERSION_TEXT_SCALE,
                new Color(MyGuiConstants.LABEL_TEXT_COLOR * m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
        }

        private void DrawAppVersion()
        {
            Vector2 size;
            Vector2 textRightBottomPosition = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            textRightBottomPosition.Y -= 1 * TEXT_LINE_HEIGHT;

            if (MyFinalBuildConstants.IS_OFFICIAL)
            {
                if (MySteam.BranchName != null)
                {
                    BranchName.Clear();
                    BranchName.Append(" ");
                    BranchName.Append(MySteam.BranchName);

                    size = MyGuiManager.MeasureString(MyFontEnum.BuildInfoHighlight, BranchName, MyGuiConstants.APP_VERSION_TEXT_SCALE);
                    MyGuiManager.DrawString(MyFontEnum.BuildInfoHighlight, BranchName, textRightBottomPosition, MyGuiConstants.APP_VERSION_TEXT_SCALE,
                        new Color(MyGuiConstants.LABEL_TEXT_COLOR * m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

                    textRightBottomPosition.X -= size.X;
                }
            }
            else
            {
                size = MyGuiManager.MeasureString(MyFontEnum.BuildInfoHighlight, NON_OFFICIAL, MyGuiConstants.APP_VERSION_TEXT_SCALE);
                MyGuiManager.DrawString(MyFontEnum.BuildInfoHighlight, NON_OFFICIAL, textRightBottomPosition, MyGuiConstants.APP_VERSION_TEXT_SCALE,
                    new Color(MyGuiConstants.LABEL_TEXT_COLOR * m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

                textRightBottomPosition.X -= size.X;
            }

            size = MyGuiManager.MeasureString(MyFontEnum.BuildInfo, PLATFORM, MyGuiConstants.APP_VERSION_TEXT_SCALE);
            MyGuiManager.DrawString(MyFontEnum.BuildInfo, PLATFORM, textRightBottomPosition, MyGuiConstants.APP_VERSION_TEXT_SCALE,
                new Color(MyGuiConstants.LABEL_TEXT_COLOR * m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

            textRightBottomPosition.X -= size.X;
            
            MyGuiManager.DrawString(MyFontEnum.BuildInfo, APP_VERSION, textRightBottomPosition, MyGuiConstants.APP_VERSION_TEXT_SCALE,
                new Color(MyGuiConstants.LABEL_TEXT_COLOR * m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
        }

        private void DrawSteamStatus()
        {
            if (MySandboxGame.Services == null || MySandboxGame.Services.SteamService == null || !MySteam.IsActive)
            {
                Vector2 textRightBottomPosition = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                textRightBottomPosition.Y -= 2 * TEXT_LINE_HEIGHT;
                MyGuiManager.DrawString(MyFontEnum.BuildInfo, STEAM_INACTIVE, textRightBottomPosition, MyGuiConstants.APP_VERSION_TEXT_SCALE,
                    new Color(MyGuiConstants.LABEL_TEXT_COLOR * m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            }
        }

        private void DrawObfuscationStatus()
        {
            if (!MyPerGameSettings.ShowObfuscationStatus)
            {
                return; // no obfuscation for space anymore !
            }
            
            if (!MyObfuscation.Enabled)
            {
                Vector2 textRightBottomPosition = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                textRightBottomPosition.Y -= 3 * TEXT_LINE_HEIGHT;
                MyGuiManager.DrawString(MyFontEnum.BuildInfoHighlight, NOT_OBFUSCATED, textRightBottomPosition, MyGuiConstants.APP_VERSION_TEXT_SCALE,
                    new Color(MyGuiConstants.LABEL_TEXT_COLOR * m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            }
        }


    }
}