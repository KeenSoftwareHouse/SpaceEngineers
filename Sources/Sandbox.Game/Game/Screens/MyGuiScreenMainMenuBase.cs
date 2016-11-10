using System;
using System.Globalization;
using System.Text;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Gui.DebugInputComponents;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenMainMenuBase : MyGuiScreenBase
    {
        protected const float TEXT_LINE_HEIGHT = 0.024f;

        protected bool m_pauseGame;
        protected bool m_musicPlayed;

        #region Build Information
        #if !XB1
        private static readonly StringBuilder BUILD_DATE =
            new StringBuilder("Build: " +
                              MySandboxGame.BuildDateTime.ToString("yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture));
        #else // XB1
        private static readonly StringBuilder BUILD_DATE = new StringBuilder("Build: N/A (XB1 TODO?)");
        #endif // XB1

        private static readonly StringBuilder APP_VERSION = MyFinalBuildConstants.APP_VERSION_STRING;
        private static readonly StringBuilder STEAM_INACTIVE = new StringBuilder("STEAM NOT AVAILABLE");
        private static readonly StringBuilder NOT_OBFUSCATED = new StringBuilder("NOT OBFUSCATED");
        private static readonly StringBuilder NON_OFFICIAL = new StringBuilder(" NON-OFFICIAL");

        #if XB1
        private static readonly StringBuilder PLATFORM = new StringBuilder(" 64-bit");
        #else //XB1
        private static readonly StringBuilder PLATFORM =
            new StringBuilder(Environment.Is64BitProcess ? " 64-bit" : " 32-bit");
        #endif // XB1

        private static StringBuilder BranchName = new StringBuilder(50);

        #endregion

        public bool DrawBuildInformation { get; set; }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMainMenu";
        }

        public MyGuiScreenMainMenuBase(bool pauseGame = false)
            : base(Vector2.Zero, null, null)
        {
            // If the session is currently running
            if(MyScreenManager.IsScreenOfTypeOpen(typeof(MyGuiScreenGamePlay)))
            {
                m_pauseGame = pauseGame;

                // Pause if not paused and should be
                if(m_pauseGame)
                {
                    MySandboxGame.PausePush();
                }
            }
            else
            {
                m_closeOnEsc = false;
            }

            //Because then it is visible under credits, help, etc..
            m_drawEvenWithoutFocus = false;

            DrawBuildInformation = true;
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            if (!m_musicPlayed)// && MySandboxGame.TotalTimeInMilliseconds - m_timeFromMenuLoadedMS >= PLAY_MUSIC_AFTER_MENU_LOADED_MS)
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
            #endif

            return true;
        }

        public override bool Draw()
        {
            if (!base.Draw())
                return false;

            MyGuiSandbox.DrawGameLogo(m_transitionAlpha);

            if(DrawBuildInformation)
            {
                DrawObfuscationStatus();
                DrawSteamStatus();
                DrawAppVersion();
                DrawBuildDate();
            }

            return true;
        }

        public override bool CloseScreen()
        {
            if (m_pauseGame)
            {
                MySandboxGame.PausePop();
            }

            bool ret = base.CloseScreen();

            m_musicPlayed = false;

            MyAnalyticsHelper.ReportActivityEnd(null, "show_main_menu");

            return ret;
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

        public override void LoadContent()
        {
            base.LoadContent();

            RecreateControls(true);
        }

        #region Draw Build Information

        private void DrawBuildDate()
        {
            Vector2 textRightBottomPosition =
                MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            textRightBottomPosition.Y -= 0*TEXT_LINE_HEIGHT;
            MyGuiManager.DrawString(MyFontEnum.BuildInfo, BUILD_DATE, textRightBottomPosition,
                MyGuiConstants.APP_VERSION_TEXT_SCALE,
                new Color(MyGuiConstants.LABEL_TEXT_COLOR*m_transitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
        }

        private void DrawAppVersion()
        {
            Vector2 size;
            Vector2 textRightBottomPosition =
                MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            textRightBottomPosition.Y -= 1*TEXT_LINE_HEIGHT;

            if (MyFinalBuildConstants.IS_OFFICIAL)
            {
                if (MySteam.BranchName != null)
                {
                    BranchName.Clear();
                    BranchName.Append(" ");
                    BranchName.Append(MySteam.BranchName);

                    size = MyGuiManager.MeasureString(MyFontEnum.BuildInfoHighlight, BranchName,
                        MyGuiConstants.APP_VERSION_TEXT_SCALE);
                    MyGuiManager.DrawString(MyFontEnum.BuildInfoHighlight, BranchName, textRightBottomPosition,
                        MyGuiConstants.APP_VERSION_TEXT_SCALE,
                        new Color(MyGuiConstants.LABEL_TEXT_COLOR*m_transitionAlpha),
                        MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

                    textRightBottomPosition.X -= size.X;
                }
            }
            else
            {
                size = MyGuiManager.MeasureString(MyFontEnum.BuildInfoHighlight, NON_OFFICIAL,
                    MyGuiConstants.APP_VERSION_TEXT_SCALE);
                MyGuiManager.DrawString(MyFontEnum.BuildInfoHighlight, NON_OFFICIAL, textRightBottomPosition,
                    MyGuiConstants.APP_VERSION_TEXT_SCALE,
                    new Color(MyGuiConstants.LABEL_TEXT_COLOR*m_transitionAlpha),
                    MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

                textRightBottomPosition.X -= size.X;
            }

            size = MyGuiManager.MeasureString(MyFontEnum.BuildInfo, PLATFORM, MyGuiConstants.APP_VERSION_TEXT_SCALE);
            MyGuiManager.DrawString(MyFontEnum.BuildInfo, PLATFORM, textRightBottomPosition,
                MyGuiConstants.APP_VERSION_TEXT_SCALE,
                new Color(MyGuiConstants.LABEL_TEXT_COLOR*m_transitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

            textRightBottomPosition.X -= size.X;

            MyGuiManager.DrawString(MyFontEnum.BuildInfo, APP_VERSION, textRightBottomPosition,
                MyGuiConstants.APP_VERSION_TEXT_SCALE,
                new Color(MyGuiConstants.LABEL_TEXT_COLOR*m_transitionAlpha),
                MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
        }

        private void DrawSteamStatus()
        {
            if (MySandboxGame.Services == null || MySandboxGame.Services.SteamService == null || !MySteam.IsActive)
            {
                Vector2 textRightBottomPosition =
                    MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                textRightBottomPosition.Y -= 2*TEXT_LINE_HEIGHT;
                MyGuiManager.DrawString(MyFontEnum.BuildInfo, STEAM_INACTIVE, textRightBottomPosition,
                    MyGuiConstants.APP_VERSION_TEXT_SCALE,
                    new Color(MyGuiConstants.LABEL_TEXT_COLOR*m_transitionAlpha),
                    MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
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
                Vector2 textRightBottomPosition =
                    MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                textRightBottomPosition.Y -= 3*TEXT_LINE_HEIGHT;
                MyGuiManager.DrawString(MyFontEnum.BuildInfoHighlight, NOT_OBFUSCATED, textRightBottomPosition,
                    MyGuiConstants.APP_VERSION_TEXT_SCALE,
                    new Color(MyGuiConstants.LABEL_TEXT_COLOR*m_transitionAlpha),
                    MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            }
        }

        #endregion

        #region Helpers

        protected MyGuiControlButton MakeButton(Vector2 position, MyStringId text, Action<MyGuiControlButton> onClick,
            MyStringId? tooltip = null)
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

        protected void CheckLowMemSwitchToLow()
        {
            if (MySandboxGame.Config.LowMemSwitchToLow == MyConfig.LowMemSwitch.TRIGGERED)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                    {
                        if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            MySandboxGame.Config.LowMemSwitchToLow = MyConfig.LowMemSwitch.ARMED;
                            MySandboxGame.Config.SetToLowQuality();
                            MySandboxGame.Config.Save();
                            // Exit game
                            {
                                MyAnalyticsTracker.SendGameEnd("Exit to Windows", MySandboxGame.TotalTimeInMilliseconds / 1000);
                                MyScreenManager.CloseAllScreensNowExcept(null);
                                MySandboxGame.ExitThreadSafe();
                            }
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

        #endregion

    }
}
