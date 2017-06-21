using VRageMath;

using Sandbox.Engine.Utils;
using Sandbox.Game.World;

using Sandbox.Graphics.GUI;
using System;
using Sandbox.Definitions;
using System.Collections.Generic;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.ObjectBuilders;
using Sandbox.Engine.Networking;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Voxels;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenStartSandbox : MyGuiScreenBase
    {
        bool loaded = false;

        bool m_hasCheckpoint = false;

        int m_additionalButtons = 0;
        private MyGuiControlButton m_aiSchoolButton;

        public MyGuiScreenStartSandbox()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.36f, 0.3f), false, null)
        {
            EnabledBackgroundFade = true;

            if (MyPerGameSettings.EnableScenarios)
                m_additionalButtons++;

            Size = new Vector2(0.36f, 0.3f + m_additionalButtons * 0.04f);

            MyDefinitionManager.Static.LoadScenarios();

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenCaptionNewWorld);

            Vector2 menuPositionOrigin = new Vector2(0.0f, -m_size.Value.Y / 2.0f + (0.147f - m_additionalButtons * 0.013f));
            Vector2 buttonDelta = new Vector2(0.15f, 0);

            //MyStringId? otherButtonsForbidden = null;
            //MyStringId newGameText = MySpaceTexts.StartDemo;
            int buttonPositionCounter = 0;

            //  Quickstart
            var quickstartButton = new MyGuiControlButton(
                position: menuPositionOrigin + buttonPositionCounter++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                text: MyTexts.Get(MyCommonTexts.ScreenNewWorldButtonQuickstart),
                toolTip: MyTexts.GetString(MySpaceTexts.ToolTipNewWorldQuickstart),
                onButtonClick: OnQuickstartClick);

            //  Custom Game
            var customGameButton = new MyGuiControlButton(
                position: menuPositionOrigin + buttonPositionCounter++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                text: MyTexts.Get(MyCommonTexts.ScreenNewWorldButtonCustomWorld),
                toolTip: MyTexts.GetString(MyCommonTexts.ToolTipNewWorldCustomWorld),
                onButtonClick: OnCustomGameClick);
            Controls.Add(quickstartButton);
            Controls.Add(customGameButton);

            if (MyPerGameSettings.EnableScenarios)
            {
                //  scenarios
                var scenarioButton = new MyGuiControlButton(
                    position: menuPositionOrigin + buttonPositionCounter++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                    text: MyTexts.Get(MySpaceTexts.ScreenCaptionScenario),
                    //toolTip: MyTexts.GetString(MySpaceTexts.ToolTipNewWorldCustomWorld),
                    onButtonClick: OnScenarioGameClick);

                Controls.Add(scenarioButton);
            }

            CloseButtonEnabled = true;
        }

        public override bool Update(bool hasFocus)
        {
            return base.Update(hasFocus);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenStartSandbox";
        }

        void OnQuickstartClick(MyGuiControlButton sender)
        {
            StartQuickstart();
        }

        protected virtual void StartQuickstart()
        {
            // TODO: Move to derived screen in SpaceEngineers.Game
            if (MySandboxGame.IsDirectX11) // Running DirectX11, start planet quickstart
            {
                QuickstartSandbox(GetQuickstartSettings(), CreatePlanetQuickstartArgs());
            }
            else if (MyDirectXHelper.IsDx11Supported()) // DirectX11 not enabled, messagebox
            {
                MyScreenManager.RemoveAllScreensExcept(null);
                var text = MyTexts.Get(MySpaceTexts.QuickstartDX11SwitchQuestion);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), buttonType: MyMessageBoxButtonsType.YES_NO, callback: MessageBoxSwitchCallback);
                MyGuiSandbox.AddScreen(mb);
            }
            else // DirectX11 not supported, show message, start easy start 1
            {
                MyScreenManager.RemoveAllScreensExcept(null);
                var text = MyTexts.Get(MySpaceTexts.QuickstartDX11NotAvailable);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), callback: StartNoPlanetsOK);
                MyGuiSandbox.AddScreen(mb);
            }
        }

        void MessageBoxSwitchCallback(MyGuiScreenMessageBox.ResultEnum result)
        {
            if (result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                MySandboxGame.Config.GraphicsRenderer = MySandboxGame.DirectX11RendererKey;
                MySandboxGame.Config.Save();
                var text = MyTexts.Get(MySpaceTexts.QuickstartDX11PleaseRestartGame);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), callback: OnPleaseRestart);
                MyGuiSandbox.AddScreen(mb);
            }
            else
            {
                var text = MyTexts.Get(MySpaceTexts.QuickstartNoPlanets);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), callback: StartNoPlanetsOK);
                MyGuiSandbox.AddScreen(mb);
            }
        }

        void OnPleaseRestart(MyGuiScreenMessageBox.ResultEnum result)
        {
            MyGuiSandbox.BackToMainMenu();
        }

        void StartNoPlanetsOK(MyGuiScreenMessageBox.ResultEnum result)
        {
            QuickstartSandbox(GetQuickstartSettings(), CreateBasicQuickstartArgs());
        }

        protected virtual MyObjectBuilder_SessionSettings GetQuickstartSettings()
        {
            return CreateBasicQuickStartSettings();
        }

        protected virtual MyWorldGenerator.Args GetQuickstartArgs()
        {
            return CreateBasicQuickstartArgs();
        }

        private static MyObjectBuilder_SessionSettings CreateBasicQuickStartSettings()
        {
            var settings = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_SessionSettings>();
            settings.GameMode = MyGameModeEnum.Creative;
            settings.EnableToolShake = true;
            settings.EnableFlora = (MyPerGameSettings.Game == GameEnum.SE_GAME) && MyFakes.ENABLE_PLANETS;
            settings.EnableSunRotation = MyPerGameSettings.Game == GameEnum.SE_GAME;
            settings.VoxelGeneratorVersion = MyVoxelConstants.VOXEL_GENERATOR_VERSION;
            settings.CargoShipsEnabled = true;
            settings.EnableOxygen = true;
            settings.EnableSpiders = false;
            settings.EnableWolfs = false;
            MyWorldGenerator.SetProceduralSettings(-1, settings);
            return settings;
        }

        private static MyWorldGenerator.Args CreateBasicQuickstartArgs()
        {
            return new MyWorldGenerator.Args()
            {
                Scenario = MyDefinitionManager.Static.GetScenarioDefinition(new MyDefinitionId(typeof(MyObjectBuilder_ScenarioDefinition), "EasyStart1")),
                AsteroidAmount = 0
            };
        }

        private static MyWorldGenerator.Args CreatePlanetQuickstartArgs()
        {
            return new MyWorldGenerator.Args()
            {
                Scenario = MyDefinitionManager.Static.GetScenarioDefinition(new MyDefinitionId(typeof(MyObjectBuilder_ScenarioDefinition), "EarthEasyStart")),
                AsteroidAmount = 0
            };
        }

        // Start game with some default values
        public static void QuickstartSandbox(MyObjectBuilder_SessionSettings quickstartSettings, MyWorldGenerator.Args? quickstartArgs)
        {
            MyLog.Default.WriteLine("QuickstartSandbox - START");

            MyScreenManager.RemoveAllScreensExcept(null);

            MySessionLoader.StartLoading(delegate
            {
                var settings = (quickstartSettings != null) ? quickstartSettings : CreateBasicQuickStartSettings();
                var args = (quickstartArgs != null) ? quickstartArgs.Value : CreateBasicQuickstartArgs();
                var mods = new List<MyObjectBuilder_Checkpoint.ModItem>(0);
                MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Quickstart);
                MySession.Start("Created " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "", "", settings, mods, args);
            });

            MyLog.Default.WriteLine("QuickstartSandbox - END");
        }

        public void OnCustomGameClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.CustomWorldScreen));
        }

        public void OnScenarioGameClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ScenarioScreen));
        }

        public void OnTutorialClick(MyGuiControlButton sender)
        {
            MyAnalyticsHelper.ReportTutorialScreen("TutorialsButtonClicked");
//            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.TutorialScreen));
        }
    }
}