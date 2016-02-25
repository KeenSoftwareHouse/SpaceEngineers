using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenTutorial : MyGuiScreenScenarioBase
    {
        enum TrainingLevel
        {
            BASIC = 0,
            INTERMEDIATE,
            ADVANCED,
            PLANETARY
        }

        private MyGuiControlCombobox m_trainingLevel;
        private Dictionary<int, List<Tuple<string, MyWorldInfo>>> m_tutorials;
        private string[] m_trainingNames = new string[] { "Basic, Intermediate", "Advanced" };

        protected override MyStringId ScreenCaption
        {
            get { return MySpaceTexts.ScreenCaptionTutorials; }
        }

        protected override bool IsOnlineMode
        {
            get { return false; }
        }

        public MyGuiScreenTutorial()
            :
            base()
        {
            RecreateControls(true);
        }

        protected override void BuildControls()
        {
            base.BuildControls();

            var trainingLabel = MakeLabel(MySpaceTexts.TrainingLevel);
            trainingLabel.Position = new Vector2(-0.25f, -0.47f + MARGIN_TOP);
            trainingLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;

            m_trainingLevel = new MyGuiControlCombobox(
                position: new Vector2(-0.04f, -0.47f + MARGIN_TOP),
                size: new Vector2(0.2f, trainingLabel.Size.Y),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER
                );
            m_trainingLevel.AddItem((int)TrainingLevel.BASIC, MySpaceTexts.TrainingLevel_Basic);
            //m_trainingLevel.AddItem((int)TrainingLevel.INTERMEDIATE, MySpaceTexts.TrainingLevel_Intermediate);
            //m_trainingLevel.AddItem((int)TrainingLevel.ADVANCED, MySpaceTexts.TrainingLevel_Advanced);
            if (MyFakes.ENABLE_XMAS15_CONTENT)
                m_trainingLevel.AddItem((int)TrainingLevel.PLANETARY, MySpaceTexts.TrainingLevel_Planetary);
            m_trainingLevel.SelectItemByIndex(0);
            m_trainingLevel.ItemSelected += OnTrainingLevelSelected;

            Controls.Add(trainingLabel);
            Controls.Add(m_trainingLevel);
        }

        private void OnTrainingLevelSelected()
        {
            SelectTutorials();
        }

        protected override Graphics.GUI.MyGuiControlTable CreateScenarioTable()
        {
            var scenarioTable = new MyGuiControlTable();
            scenarioTable.Position = new Vector2(-0.42f, -0.435f + MARGIN_TOP);
            scenarioTable.Size = new Vector2(0.38f, 1.8f);
            scenarioTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            scenarioTable.VisibleRowsCount = 18;
            scenarioTable.ColumnsCount = 2;
            scenarioTable.SetCustomColumnWidths(new float[] { 0.085f, 0.905f });
            scenarioTable.SetColumnName(1, MyTexts.Get(MyCommonTexts.Name));
            scenarioTable.ItemSelected += OnTableItemSelected;

            return scenarioTable;
        }

        protected override void FillList()
        {
            base.FillList();

            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, BeginTutorialLoading, EndTutorialLoading));
        }

        private IMyAsyncResult BeginTutorialLoading()
        {
            return new MyLoadTutorialListResult();
        }

        private void EndTutorialLoading(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;

            if (loadListRes.ContainsCorruptedWorlds)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.SomeWorldFilesCouldNotBeLoaded),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(messageBox);
            }

            m_tutorials = new Dictionary<int, List<Tuple<string, MyWorldInfo>>>();

            Dictionary<string, int> trainingLevels = new Dictionary<string, int>();
            trainingLevels["Basic"] = (int)TrainingLevel.BASIC;
            //trainingLevels["Intermediate"] = (int)TrainingLevel.INTERMEDIATE;
            //trainingLevels["Advanced"] = (int)TrainingLevel.ADVANCED;
            trainingLevels["Planetary"] = (int)TrainingLevel.PLANETARY;

            loadListRes.AvailableSaves.Sort((x, y) => x.Item2.SessionName.CompareTo(y.Item2.SessionName));
            foreach (var loadedRes in loadListRes.AvailableSaves)
            {
                var splitted = loadedRes.Item1.Split('\\');
                var trainingLevel = splitted[splitted.Length - 2];
                if (!MyFakes.ENABLE_XMAS15_CONTENT && splitted[splitted.Length - 1].Contains("Tutorial 08 - Climbing on Planets"))
                    continue;
                if (!MyFakes.ENABLE_XMAS15_CONTENT && splitted[splitted.Length - 1].Contains("Tutorial 11 - Mining Planets and Ship Recovery"))
                    continue;
                if (trainingLevels.ContainsKey(trainingLevel))
                {
                    int id = trainingLevels[trainingLevel];
                    if (!m_tutorials.ContainsKey(id))
                        m_tutorials[id] = new List<Tuple<string, MyWorldInfo>>();
                    m_tutorials[id].Add(loadedRes);
                }
            }

            SelectTutorials();

            m_state = StateEnum.ListLoaded;

            screen.CloseScreen();
        }

        public override bool Update(bool hasFocus)
        {
            var ret = base.Update(hasFocus);
            if (m_okButton.Enabled && !MyTutorialHelper.IsUnlocked(((Tuple<string, MyWorldInfo>)m_scenarioTable.SelectedRow.UserData).Item2.SessionName) && !MyFakes.DEVELOPMENT_PRESET)
                m_okButton.Enabled = false;
            return ret;
        }

        private void SelectTutorials()
        {
            ClearSaves();

            var trainingLevel = (int)m_trainingLevel.GetSelectedKey();
            if (m_tutorials.ContainsKey(trainingLevel))
            {
                var loadedTrainingLevels = m_tutorials[trainingLevel];
                AddSaves(loadedTrainingLevels);
            }

            RefreshGameList(true);
        }

        protected override void LoadSandboxInternal(Tuple<string, MyWorldInfo> save, bool MP)
        {
            MyAnalyticsHelper.ReportTutorialStart(save.Item2.SessionName);
            base.LoadSandboxInternal(save, MP);
            MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Tutorial);
            MyScenarioSystem.LoadMission(save.Item1, MP, MyOnlineModeEnum.OFFLINE, 5);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTutorial";
        }
    }
}
