
using Medieval.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using System;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
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
                MyGuiScreenMainMenu.ReturnToMainMenu();
                return base.Update(hasFocus);
            }

            switch (m_quickLaunchType)
            {
                case MyQuickLaunchType.LAST_SANDBOX:
                    {
                        var lastSessionPath = MyLocalCache.GetLastSessionPath();
                        if (lastSessionPath != null && System.IO.Directory.Exists(lastSessionPath))
                        {
                            MyGuiScreenLoadSandbox.LoadSingleplayerSession(lastSessionPath);
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
                        MyGuiScreenStartSandbox.QuickstartSandbox(null, null);
                        m_childScreenLaunched = true;
                    }
                    break;
                case MyQuickLaunchType.SCENARIO_QUICKSTART:
                    {
                        MyGuiScreenStartSandbox.QuickstartSandbox(new MyObjectBuilder_MedievalSessionSettings() { EnableBarbarians = true, MaximumBots = 10000 }, new World.MyWorldGenerator.Args()
                            {
                                Scenario = (MyScenarioDefinition)MyDefinitionManager.Static.GetScenarioDefinition(new MyDefinitionId(typeof(MyObjectBuilder_ScenarioDefinition), MyFakes.QUICK_LAUNCH_SCENARIO != String.Empty ? MyFakes.QUICK_LAUNCH_SCENARIO : "Quickstart"))
                            });
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
