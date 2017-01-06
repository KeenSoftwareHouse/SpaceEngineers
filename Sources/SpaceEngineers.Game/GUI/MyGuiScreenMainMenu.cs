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
                int buttonIndex = MyPerGameSettings.MultiplayerEnabled ? 7 : 6;
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA - MyGuiConstants.MENU_BUTTONS_POSITION_DELTA / 2, MyCommonTexts.ScreenMenuButtonContinueGame, OnContinueGameClicked));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonCampaign, OnClickNewGame));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonLoadGame, OnClickLoad));
                if (MyPerGameSettings.MultiplayerEnabled)
                    Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonJoinGame, OnJoinWorld));
                //Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonCustomGame, OnCustomGameClicked));
                --buttonIndex;
                //Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonSubscribedWorlds, OnClickSubscribedWorlds, MyCommonTexts.ToolTipMenuSubscribedWorlds));
                Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonOptions, OnClickOptions));
                //Controls.Add(MakeButton(leftButtonPositionOrigin - (buttonIndex--) * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA, MyCommonTexts.ScreenMenuButtonHelp, OnClickHelp));
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

                if (!Sync.IsServer || MyCampaignManager.Static.IsCampaignRunning)
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

            //  News
            m_newsControl = new MyGuiControlNews()
            {
                Position = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM) - 7f * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                Size = new Vector2(0.4f, 0.28f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
            };
            Controls.Add(m_newsControl);

            // Bottom URL
            var webButton = MakeButton(
                MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, 70),
                MySpaceTexts.Blank, OnClickGameWeb);
            webButton.Text = MyPerGameSettings.GameWebUrl;
            webButton.VisualStyle = MyGuiControlButtonStyleEnum.UrlText;
            Controls.Add(webButton);

            var iconButtonOrigin = m_newsControl.Position;
            var iconButtonSize = new Vector2(50f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            iconButtonOrigin.Y += m_newsControl.Size.Y + MyGuiConstants.GENERIC_BUTTON_SPACING.Y;

            // Help button
            var helpButton = MakeButton(
                iconButtonOrigin, 
                MyStringId.NullOrEmpty, OnClickHelp);

            helpButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            helpButton.VisualStyle = MyGuiControlButtonStyleEnum.Help;
            helpButton.Size = iconButtonSize;
            Controls.Add(helpButton);

            iconButtonOrigin.X -= helpButton.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X * 2;
            // Report button
            var reportButton = MakeButton(
                iconButtonOrigin,
                MyStringId.NullOrEmpty,
                OnClickReportBug);

            reportButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            reportButton.VisualStyle = MyGuiControlButtonStyleEnum.Bug;
            reportButton.Size = iconButtonSize;
            Controls.Add(reportButton);


            iconButtonOrigin.X -= reportButton.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X * 2;
            // Newsletter button
            var newsletterButton = MakeButton(
                iconButtonOrigin,
                MyStringId.NullOrEmpty,
                OnClickNewsletter);

            newsletterButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            newsletterButton.VisualStyle = MyGuiControlButtonStyleEnum.Envelope;
            newsletterButton.Size = iconButtonSize;
            Controls.Add(newsletterButton);

            iconButtonOrigin.X -= newsletterButton.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X * 2;
            // Recommend button
            var button = MakeButton(
                iconButtonOrigin,
                MyStringId.NullOrEmpty,
                OnClickRecommend);

            button.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            button.VisualStyle = MyGuiControlButtonStyleEnum.Like;
            button.Size = iconButtonSize;
            Controls.Add(button);

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

        private void OnClickNewsletter(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenNewsletter>());
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
            if (!Sync.IsServer)
            {
                MySessionLoader.UnloadAndExitToMenu();
                return;
            }

            this.CanBeHidden = false;
            MyGuiScreenMessageBox messageBox;
            if (MyCampaignManager.Static.IsCampaignRunning)
            {
                messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextCampaignBeforeExit),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionExit),
                    callback: OnExitToMainMenuFromCampaignMessageBoxCallback);
            }
            else
            {
                messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO_CANCEL,
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextSaveChangesBeforeExit),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionExit),
                    callback: OnExitToMainMenuMessageBoxCallback);
            }
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

        private void OnExitToMainMenuFromCampaignMessageBoxCallback(MyGuiScreenMessageBox.ResultEnum callbackReturn)
        {
            switch (callbackReturn)
            {
                case MyGuiScreenMessageBox.ResultEnum.YES:
                    MyAudio.Static.Mute = true;
                    MyAudio.Static.StopMusic();
                    MySessionLoader.UnloadAndExitToMenu();
                    break;

                default:
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
