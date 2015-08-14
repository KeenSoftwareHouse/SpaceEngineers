﻿using VRageMath;

using Sandbox.Engine.Utils;
using Sandbox.Game.World;

using Sandbox.Graphics.GUI;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using System;
using Sandbox.Game.Gui;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Collections.Generic;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.Voxels;
using VRage.ObjectBuilders;
using Sandbox.Engine.Networking;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenStartSandbox : MyGuiScreenBase
    {
        bool loaded = false;

        bool m_hasCheckpoint = false;

        int m_additionalButtons = 0;
        
        public MyGuiScreenStartSandbox()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.36f, 0.3f), false, null)
        {
            EnabledBackgroundFade = true;

            if (MyFakes.ENABLE_BATTLE_SYSTEM)
                m_additionalButtons++;
            if (MyPerGameSettings.EnableScenarios)
                m_additionalButtons++;
            if (MyPerGameSettings.EnableTutorials)
                m_additionalButtons++;
            
            Size = new Vector2(0.36f, 0.3f + m_additionalButtons * 0.04f);

            MyDefinitionManager.Static.LoadScenarios();

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MySpaceTexts.ScreenCaptionNewWorld);

            Vector2 menuPositionOrigin = new Vector2(0.0f, -m_size.Value.Y / 2.0f + (0.147f - m_additionalButtons * 0.013f));
            Vector2 buttonDelta = new Vector2(0.15f, 0);

            //MyStringId? otherButtonsForbidden = null;
            //MyStringId newGameText = MySpaceTexts.StartDemo;
            int buttonPositionCounter = 0;

            //  Quickstart
            var quickstartButton = new MyGuiControlButton(
                position: menuPositionOrigin + buttonPositionCounter++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                text: MyTexts.Get(MySpaceTexts.ScreenNewWorldButtonQuickstart),
                toolTip: MyTexts.GetString(MySpaceTexts.ToolTipNewWorldQuickstart),
                onButtonClick: OnQuickstartClick);

            //  Custom Game
            var customGameButton = new MyGuiControlButton(
                position: menuPositionOrigin + buttonPositionCounter++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                text: MyTexts.Get(MySpaceTexts.ScreenNewWorldButtonCustomWorld),
                toolTip: MyTexts.GetString(MySpaceTexts.ToolTipNewWorldCustomWorld),
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

            if (MyPerGameSettings.EnableTutorials)
            {
                // tutorials
                var tutorialButton = new MyGuiControlButton(
                    position: menuPositionOrigin + buttonPositionCounter++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                    text: MyTexts.Get(MySpaceTexts.ScreenCaptionTutorials),
                    //toolTip: MyTexts.GetString(MySpaceTexts.ToolTipNewWorldCustomWorld),
                    onButtonClick: OnTutorialClick);

                Controls.Add(tutorialButton);
            }

            if (MyFakes.ENABLE_BATTLE_SYSTEM)
            {
                var battleButton = new MyGuiControlButton(
                    position: menuPositionOrigin + buttonPositionCounter++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                    text: MyTexts.Get(MySpaceTexts.ScreenButtonBattle),
                    //toolTip: MyTexts.GetString(MySpaceTexts.ToolTipNewWorldCustomWorld),
                    onButtonClick: OnBattleClick);

                Controls.Add(battleButton);

            }

            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenStartSandbox";
        }

        void OnQuickstartClick(MyGuiControlButton sender)
        {
            QuickstartSandbox(GetQuickstartSettings(), GetQuickstartArgs());
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
            settings.EnableStationVoxelSupport = MyPerGameSettings.Game == GameEnum.SE_GAME;
            settings.EnableToolShake = true;
            settings.EnablePlanets = (MyPerGameSettings.Game == GameEnum.SE_GAME) && MyFakes.ENABLE_PLANETS;
            settings.EnableFlora = (MyPerGameSettings.Game == GameEnum.SE_GAME) && MyFakes.ENABLE_PLANETS;
            settings.EnableSunRotation = MyPerGameSettings.Game == GameEnum.SE_GAME;
            settings.VoxelGeneratorVersion = MyVoxelConstants.VOXEL_GENERATOR_VERSION;
            settings.CargoShipsEnabled = !settings.EnablePlanets;
            settings.EnableOxygen = true;
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

        // Start game with some default values
        public static void QuickstartSandbox(MyObjectBuilder_SessionSettings quickstartSettings, MyWorldGenerator.Args? quickstartArgs)
        {
            MyLog.Default.WriteLine("QuickstartSandbox - START");

            MyScreenManager.RemoveAllScreensExcept(null);

            MyGuiScreenGamePlay.StartLoading(delegate
            {
                var settings = (quickstartSettings != null) ? quickstartSettings : CreateBasicQuickStartSettings();
                var args = (quickstartArgs != null) ? quickstartArgs.Value : CreateBasicQuickstartArgs();
                var mods = new List<MyObjectBuilder_Checkpoint.ModItem>(0);
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
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.TutorialScreen));
        }

        public void OnBattleClick(MyGuiControlButton sender)
        {
            if (MySteam.IsOnline)
            {
                if (MyFakes.ENABLE_TUTORIAL_PROMPT && MySandboxGame.Config.NeedShowBattleTutorialQuestion)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextTutorialQuestion),
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionVideoTutorial),
                        callback: delegate(MyGuiScreenMessageBox.ResultEnum val)
                        {
                            if (val == MyGuiScreenMessageBox.ResultEnum.YES)
                                MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_GUIDE_BATTLE, "Steam Guide");
                            else
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.BattleScreen));
                        }));

                    MySandboxGame.Config.NeedShowBattleTutorialQuestion = false;
                    MySandboxGame.Config.Save();
                }
                else
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.BattleScreen));
                }
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.SteamIsOfflinePleaseRestart)
                ));
            }

        }
    }
}