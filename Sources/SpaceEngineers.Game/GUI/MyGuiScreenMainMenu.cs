using System;
using System.Text;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Audio;
using VRage.FileSystem;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.GUI
{
    public class MyGuiScreenMainMenu : MyGuiScreenMainMenuBase
    {
        MyGuiControlNews m_newsControl;

        public MyGuiScreenMainMenu() : this(false)
        {
        }

        public MyGuiScreenMainMenu(bool pauseGame) 
            : base(pauseGame)
        {    
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
                int buttonIndex = MyPerGameSettings.MultiplayerEnabled ? 9 : 8;
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA - MyGuiConstants.MENU_BUTTONS_POSITION_DELTA / 2, MyCommonTexts.ScreenMenuButtonContinueGame, OnContinueGameClicked));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonCampaign, OnClickNewGame));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonLoadGame, OnClickLoad));
                if (MyPerGameSettings.MultiplayerEnabled)
                    Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonJoinGame, OnJoinWorld));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonCustomGame, OnCustomGameClicked));
                --buttonIndex;
                //Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonSubscribedWorlds, OnClickSubscribedWorlds, MyCommonTexts.ToolTipMenuSubscribedWorlds));
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

                if (!Sync.IsServer || (MySession.Static.Battle))
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
                new Vector2(m_newsControl.Position.X, m_newsControl.Position.Y + m_newsControl.Size.Y),
                //MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM, 140,80),
                MyCommonTexts.ReportBug, OnClickReportBug);
            reportButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            reportButton.VisualStyle = MyGuiControlButtonStyleEnum.UrlText;
            Controls.Add(reportButton);

            CheckLowMemSwitchToLow();
        }

        private void OnContinueGameClicked(MyGuiControlButton myGuiControlButton)
        {
            MySessionLoader.LoadLastSession();
        }

        private void OnCustomGameClicked(MyGuiControlButton myGuiControlButton)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenWorldSettings>());
        }

        private void OnClickReportBug(MyGuiControlButton obj)
        {
            MyGuiSandbox.OpenUrl(MyPerGameSettings.BugReportUrl, UrlOpenMode.SteamOrExternalWithConfirm, MyTexts.AppendFormat(new StringBuilder(), MyCommonTexts.MessageBoxTextOpenBrowser, "forums.keenswh.com"));
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

        private void OnClickNewGame(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenNewGame>());
        }

        private unsafe void OnClickLoad(MyGuiControlBase sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenLoadSandbox());
        }


        private void OnClickPlayers(MyGuiControlButton obj)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenPlayers>());
        }

        private void OnExitToMainMenuClick(MyGuiControlButton sender)
        {
            if (!Sync.IsServer || MySession.Static.Battle)
            {
                MySessionLoader.UnloadAndExitToMenu();
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
                    MySessionLoader.UnloadAndExitToMenu();
                    break;

                case MyGuiScreenMessageBox.ResultEnum.CANCEL:
                    this.CanBeHidden = true;
                    break;
            }
        }

        private void UnloadAndExitAfterScreeshotWasTaken(object sender, EventArgs e)
        {
            MySandboxGame.Static.OnScreenshotTaken -= UnloadAndExitAfterScreeshotWasTaken;
            MySessionLoader.UnloadAndExitToMenu();
        }

        private void OnClickCredits(MyGuiControlButton sender)
        {
            //opens dialog screen with list of trailers, where could be selected animation to play
            MyGuiSandbox.AddScreen(new MyGuiScreenGameCredits());
        }

        private void OnClickOptions(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenOptionsSpace>());
        }

        private void OnClickHelp(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenHelpSpace>());
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
    }
}
