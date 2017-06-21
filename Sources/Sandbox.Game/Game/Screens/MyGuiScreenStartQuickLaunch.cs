
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenStartQuickLaunch : MyGuiScreenBase
    {
        MyQuickLaunchType m_quickLaunchType;
        bool m_childScreenLaunched = false;
        
        //  Using this static public property client-server tells us about login response
        public static MyGuiScreenStartQuickLaunch CurrentScreen = null;    //  This is always filled with reference to actual instance of this scree. If there isn't, it's null.


        public MyGuiScreenStartQuickLaunch(MyQuickLaunchType quickLaunchType, MyStringId progressText) :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            m_quickLaunchType = quickLaunchType;
            CurrentScreen = this;
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenStartQuickLaunch";
        }

        private static MyWorldGenerator.Args CreateBasicQuickstartArgs()
        {
            return new MyWorldGenerator.Args()
            {
                Scenario = MyDefinitionManager.Static.GetScenarioDefinition(new MyDefinitionId(typeof(MyObjectBuilder_ScenarioDefinition), "EasyStart1")),
                AsteroidAmount = 0
            };
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

        public static void QuickstartSandbox(MyObjectBuilder_SessionSettings quickstartSettings, MyWorldGenerator.Args? quickstartArgs)
        {
            MyLog.Default.WriteLine("QuickstartSandbox - START");

            MyScreenManager.RemoveAllScreensExcept(null);

            MySessionLoader.StartLoading(delegate
            {
                var settings = quickstartSettings ?? CreateBasicQuickStartSettings();
                var args = quickstartArgs ?? CreateBasicQuickstartArgs();
                var mods = new List<MyObjectBuilder_Checkpoint.ModItem>(0);
                MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Quickstart);
                MySession.Start("Created " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "", "", settings, mods, args);
            });

            MyLog.Default.WriteLine("QuickstartSandbox - END");
        }

        public override bool Update(bool hasFocus)
        {
            if (!hasFocus)
                return base.Update(hasFocus);

            if (m_childScreenLaunched && hasFocus)
                CloseScreenNow();

            if (m_childScreenLaunched)
                return base.Update(hasFocus);

            if (MyInput.Static.IsKeyPress(MyKeys.Escape))
            {
                MySessionLoader.UnloadAndExitToMenu();
                return base.Update(hasFocus);
            }

            switch (m_quickLaunchType)
            {
                case MyQuickLaunchType.LAST_SANDBOX:
                    {
                        var lastSessionPath = MyLocalCache.GetLastSessionPath();
                        if (lastSessionPath != null && System.IO.Directory.Exists(lastSessionPath))
                        {
                            MySessionLoader.LoadSingleplayerSession(lastSessionPath);
                        }
                        else
                        {
                            MySandboxGame.AfterLogos();
                        }
                        m_childScreenLaunched = true;
                    }
                    break;
                case MyQuickLaunchType.NEW_SANDBOX:
                    {
                        QuickstartSandbox(null, null);
                        m_childScreenLaunched = true;
                    }
                    break;
                default:
                    {
                        throw new InvalidBranchException();
                    }
            }
           
            return base.Update(hasFocus);
        }

    }
}
