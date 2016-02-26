using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Sandbox.Game.Gui;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Engine.Utils
{

    public class MyTutorialHelper
    {
        private static HashSet<string> m_unlocked = new HashSet<string>();//currently unlocked and available
        private static HashSet<string> m_finished = new HashSet<string>();// finished

        //<key> tutorial can be unlocked by any of <value> tutorials (Content\Data\Tutorials.sbx):
        private static Dictionary<string, List<string>> m_tutorialUnlockedBy = new Dictionary<string, List<string>>();

        #region unlocking from gameplay
        public static void MissionSuccess()
        {
            //as this is called from winning trigger, there is no guarantee that game is in fact tutorial
            if (!IsTutorial(MySession.Static.Name))
                return;//not tutorial
            if (!MySandboxGame.Config.TutorialsFinished.Contains(MySession.Static.Name))
            {
                MySandboxGame.Config.TutorialsFinished.Add(MySession.Static.Name);
                RefreshUnlocked();
                MySandboxGame.Config.Save();
            }
        }
        #endregion

        public static bool IsTutorial(string scenarioName)
        {
            return m_tutorialUnlockedBy.ContainsKey(scenarioName);
        }

        public static bool IsUnlocked(string tutorial)
        {
            return m_unlocked.Contains(tutorial);
        }

        public static void Init()
        {
            if (!MyPerGameSettings.EnableTutorials)
                return;
            var path = Path.Combine(MyFileSystem.ContentPath, Path.Combine("Data", "Tutorials.sbx"));
            if (!MyFileSystem.FileExists(path))
            {
                Debug.Fail("Tutorials.sbx not found");
                return;
            }
            MyDataIntegrityChecker.HashInFile(path);
            MyObjectBuilder_TutorialsHelper objBuilder = null;
            if (!MyObjectBuilderSerializer.DeserializeXML(path, out objBuilder))
                Debug.Fail("Tutorials deserialize fail");

            if (objBuilder == null)
            {
                MyDefinitionErrors.Add(MyModContext.BaseGame, "Tutorials: Cannot load definition file, see log for details", TErrorSeverity.Error);
                return;
            }
            m_tutorialUnlockedBy = new Dictionary<string, List<string>>();

            if (objBuilder.Tutorials != null)
            {
                foreach (var tut in objBuilder.Tutorials)
                {
                    List<string> list;
                    if (!m_tutorialUnlockedBy.TryGetValue(tut.Name, out list))
                    {
                        list = new List<string>();
                        m_tutorialUnlockedBy.Add(tut.Name, list);
                    }

                    if (tut.UnlockedBy != null)
                        foreach (var other in tut.UnlockedBy)
                            list.Add(other);
                }
            }

            RefreshUnlocked();
        }

        private static void RefreshUnlocked()
        {
            m_unlocked.Clear();

            m_finished.Clear();
            m_finished.Add("Default");

            foreach (var finished in MySandboxGame.Config.TutorialsFinished)
                m_finished.Add(finished);

            foreach (var tut in m_tutorialUnlockedBy)
            {
                if (IsUnlockedInternal(tut.Key))
                    m_unlocked.Add(tut.Key);
            }
        }

        private static bool IsUnlockedInternal(string tutorial)
        {
            foreach (var unlocker in m_tutorialUnlockedBy[tutorial])
            {
                if (m_finished.Contains(unlocker)) return true;
            }

            return false;
        }
    }
}
