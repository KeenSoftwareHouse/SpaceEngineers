using ParallelTasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenScenario : MyGuiScreenScenarioBase
    {
        private int m_listLoadedParts;

        private List<Tuple<string, MyWorldInfo>> m_availableSavesLocal = new List<Tuple<string, MyWorldInfo>>();
        private List<Tuple<string, MyWorldInfo>> m_availableSavesKeens = new List<Tuple<string, MyWorldInfo>>();
        private List<Tuple<string, MyWorldInfo>> m_availableSavesWorkshop = new List<Tuple<string, MyWorldInfo>>();

        string m_sessionPath;

#if !XB1 // XB1_NOWORKSHOP
        private List<MySteamWorkshop.SubscribedItem> m_subscribedScenarios;
#endif // !XB1

        protected MyObjectBuilder_SessionSettings m_settings;
        public MyObjectBuilder_SessionSettings Settings
        {
            get
            {
                //TODO GetSettingsFromControls();
                return m_settings;
            }
        }

        MyObjectBuilder_Checkpoint m_checkpoint;
        public MyObjectBuilder_Checkpoint Checkpoint
        {
            get { return m_checkpoint; }
        }

        MyGuiControlLabel m_difficultyLabel;
        MyGuiControlCombobox m_difficultyCombo;

        MyGuiControlLabel m_onlineModeLabel;
        MyGuiControlCombobox m_onlineMode;

        MyGuiControlLabel m_maxPlayersLabel;
        MyGuiControlSlider m_maxPlayersSlider;

        //BUTTONS:
        MyGuiControlButton m_removeButton, m_publishButton, m_editButton, m_browseWorkshopButton;
        MyGuiControlButton m_refreshButton, m_openInWorkshopButton;  

        MyGuiControlList m_scenarioTypesList;
        MyGuiControlRadioButtonGroup m_scenarioTypesGroup;

        public MyGuiScreenScenario()
            : base()
        {
            RecreateControls(true);
        }

        public override bool CloseScreen()
        {
            //TODO
            return base.CloseScreen();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenScenario";
        }

        protected override void BuildControls()
        {
            base.BuildControls();

            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.65f, 0.1f);

            var difficultyLabel = MakeLabel(MySpaceTexts.Difficulty);
            var onlineModeLabel = MakeLabel(MyCommonTexts.WorldSettings_OnlineMode);
            m_maxPlayersLabel = MakeLabel(MyCommonTexts.MaxPlayers);

            float width = 0.284375f + 0.025f;

            m_difficultyCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_difficultyCombo.Enabled = false;
            m_difficultyCombo.AddItem((int)0, MySpaceTexts.DifficultyEasy);
            m_difficultyCombo.AddItem((int)1, MySpaceTexts.DifficultyNormal);
            m_difficultyCombo.AddItem((int)2, MySpaceTexts.DifficultyHard);

            m_onlineMode = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_onlineMode.Enabled = false;
            m_onlineMode.ItemSelected += OnOnlineModeSelect;
            m_onlineMode.AddItem((int)MyOnlineModeEnum.OFFLINE, MyCommonTexts.WorldSettings_OnlineModeOffline);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PRIVATE, MyCommonTexts.WorldSettings_OnlineModePrivate);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.FRIENDS, MyCommonTexts.WorldSettings_OnlineModeFriends);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PUBLIC, MyCommonTexts.WorldSettings_OnlineModePublic);

            m_maxPlayersSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: m_onlineMode.Size.X,
                minValue: 2,
                maxValue: 16,
                labelText: new StringBuilder("{0}").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true
                );

            m_scenarioTypesList = new MyGuiControlList();

            //BUTTONS
            m_removeButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.buttonRemove), onButtonClick: OnOkButtonClick);
#if !XB1 // XB1_NOWORKSHOP
            m_publishButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.buttonPublish), onButtonClick: OnPublishButtonClick);
#else // XB1
            m_publishButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.buttonPublish), onButtonClick: OnOkButtonClick);
#endif // XB1
            m_editButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.buttonEdit), onButtonClick: OnEditButtonClick);
            m_browseWorkshopButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.buttonBrowseWorkshop), onButtonClick: OnBrowseWorkshopClick);

            m_refreshButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.buttonRefresh), onButtonClick: OnRefreshButtonClick);
            m_openInWorkshopButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.buttonOpenInWorkshop), onButtonClick: OnOkButtonClick);

            m_removeButton.Enabled = false;
            m_publishButton.Enabled = false;
            m_editButton.Enabled = false;
            m_openInWorkshopButton.Enabled = false;
            CloseButtonEnabled = true;

            //m_nameTextbox.TextChanged += m_nameTextbox_TextChanged;

            m_sideMenuLayout.Add(difficultyLabel, MyAlignH.Left, MyAlignV.Top, 2, 0);
            m_sideMenuLayout.Add(m_difficultyCombo, MyAlignH.Left, MyAlignV.Top, 2, 1);
            m_sideMenuLayout.Add(onlineModeLabel, MyAlignH.Left, MyAlignV.Top, 3, 0);
            m_sideMenuLayout.Add(m_onlineMode, MyAlignH.Left, MyAlignV.Top, 3, 1);
            m_sideMenuLayout.Add(m_maxPlayersLabel, MyAlignH.Left, MyAlignV.Top, 4, 0);
            m_sideMenuLayout.Add(m_maxPlayersSlider, MyAlignH.Left, MyAlignV.Top, 4, 1);

            m_buttonsLayout.Add(m_removeButton, MyAlignH.Left, MyAlignV.Top, 0, 0);
            if (!MyFakes.XB1_PREVIEW)
            {
                m_buttonsLayout.Add(m_publishButton, MyAlignH.Left, MyAlignV.Top, 0, 1);
            }
            m_buttonsLayout.Add(m_editButton, MyAlignH.Left, MyAlignV.Top, 0, 2);
            if (!MyFakes.XB1_PREVIEW)
            {
                m_buttonsLayout.Add(m_browseWorkshopButton, MyAlignH.Left, MyAlignV.Top, 0, 3);
            }
            m_buttonsLayout.Add(m_refreshButton, MyAlignH.Left, MyAlignV.Top, 1, 0);
            if (!MyFakes.XB1_PREVIEW)
            {
                m_buttonsLayout.Add(m_openInWorkshopButton, MyAlignH.Left, MyAlignV.Top, 1, 1);
            }
        }

        private void OnOnlineModeSelect()
        {
            m_maxPlayersSlider.Enabled = m_onlineMode.GetSelectedKey() != (int)MyOnlineModeEnum.OFFLINE;
            m_maxPlayersLabel.Enabled = m_onlineMode.GetSelectedKey() != (int)MyOnlineModeEnum.OFFLINE;
        }

        private void OnEditButtonClick(object sender)
        {
            //run as normal save
            var row = m_scenarioTable.SelectedRow;
            if (row != null)
            {
                var save = FindSave(row);
                if (save != null)
                {
                    CloseScreen();
                    MySessionLoader.LoadSingleplayerSession(save.Item1);
                }
                else
                    Debug.Fail("save not found");
            }
            else
                Debug.Fail("row not found");
        }

        protected override void LoadSandboxInternal(Tuple<string, MyWorldInfo> save, bool MP)
        {
            base.LoadSandboxInternal(save, MP);

#if !XB1 // XB1_NOWORKSHOP
            if (save.Item1 == WORKSHOP_PATH_TAG)
            {
                var scenario = FindWorkshopScenario(save.Item2.WorkshopId.Value);
                MySteamWorkshop.CreateWorldInstanceAsync(scenario, MySteamWorkshop.MyWorkshopPathInfo.CreateScenarioInfo(), true, delegate(bool success, string sessionPath)
                {
                    if (success)
                    {
                        //add briefing from workshop description
                        ulong dummy;
                        var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out dummy);
                        checkpoint.Briefing = save.Item2.Briefing;
                        MyLocalCache.SaveCheckpoint(checkpoint, sessionPath);
                        MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Scenario);
                        MyScenarioSystem.LoadMission(sessionPath, /*m_nameTextbox.Text, m_descriptionTextbox.Text,*/ MP, (MyOnlineModeEnum)m_onlineMode.GetSelectedKey(), (short)m_maxPlayersSlider.Value);
                    }
                    else
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextWorkshopDownloadFailed),
                                    messageCaption: MyTexts.Get(MyCommonTexts.ScreenCaptionWorkshop)));
                });
            }
            else
#endif // !XB1
            {
                MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Scenario);
                MyScenarioSystem.LoadMission(save.Item1, /*m_nameTextbox.Text, m_descriptionTextbox.Text,*/ MP, (MyOnlineModeEnum)m_onlineMode.GetSelectedKey(), (short)m_maxPlayersSlider.Value);
            }
        }

#if !XB1 // XB1_NOWORKSHOP
        private MySteamWorkshop.SubscribedItem FindWorkshopScenario(ulong workshopId)
        {
            foreach (var scenario in m_subscribedScenarios)
            {
                if (scenario.PublishedFileId == workshopId)
                {
                    return scenario;
                }
            }

            return null;
        }
#endif // !XB1

        protected override MyGuiHighlightTexture GetIcon(Tuple<string, MyWorldInfo> save)
        {
            if (save.Item1 == WORKSHOP_PATH_TAG)
                return MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP;
            else if (save.Item2.ScenarioEditMode == true)
                return MyGuiConstants.TEXTURE_ICON_MODS_LOCAL;
            else
                return MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL;

        }

        private void OnRefreshButtonClick(object sender)
        {
            m_state = StateEnum.ListNeedsReload;
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            m_difficultyCombo.SelectItemByIndex(1);
            m_onlineMode.SelectItemByIndex(0);
        }

        #region Event handlers

        protected override void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            base.OnTableItemSelected(sender, eventArgs);

            if (eventArgs.RowIndex<2)
            {
                m_publishButton.Enabled = false;
                m_onlineMode.Enabled = false;
                m_onlineMode.SelectItemByIndex(0);
                m_editButton.Enabled = false;
            }
            else
            {
                m_publishButton.Enabled = false;
                m_onlineMode.Enabled = true;

                m_editButton.Enabled = false;
                if (m_scenarioTable.SelectedRow != null)
                {
                    m_publishButton.Enabled = true;
                    Tuple<string, MyWorldInfo> t = FindSave(m_scenarioTable.SelectedRow);
                    if (t.Item1 != WORKSHOP_PATH_TAG)
                        m_editButton.Enabled = true;
                }
            }
        }

        #endregion

        #region Async Loading

        protected override void FillList()
        {
            base.FillList();
            m_listLoadedParts = 0;
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginKeens, endKeens));//from missions
#if !XB1 // XB1_NOWORKSHOP
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginWorkshop, endWorkshop));//workshop items
#endif // !XB1
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginLocal, endLocal));//user's from saves
        }

        private void AfterPartLoaded()
        {
            if (++m_listLoadedParts == 3)
            {
                ClearSaves();
                m_state = StateEnum.ListLoaded;
                //KSH
                AddSaves(m_availableSavesKeens);
                m_availableSavesKeens=null;
                //workshop
                AddSaves(m_availableSavesWorkshop);
                m_availableSavesWorkshop.Clear();
                //Local
                foreach (var save in m_availableSavesLocal)
                {
                    var checkpoint = save.Item2;
                    if (!checkpoint.ScenarioEditMode)
                        continue;
                    AddSave(save);
                }
                m_availableSavesLocal.Clear();

                RefreshGameList();
            }
        }

        private IMyAsyncResult beginKeens()
        {
            return new MyLoadMissionListResult();
        }

        private void endKeens(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;
            m_availableSavesKeens = loadListRes.AvailableSaves;
            m_availableSavesKeens.Sort((x, y) => x.Item2.SessionName.CompareTo(y.Item2.SessionName));

            if (loadListRes.ContainsCorruptedWorlds)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.SomeWorldFilesCouldNotBeLoaded),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(messageBox);
            }
            AfterPartLoaded();
            screen.CloseScreen();
        }

        
        private IMyAsyncResult beginLocal()
        {
            return new MyLoadWorldInfoListResult();
        }

        private void endLocal(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;
            loadListRes.AvailableSaves.Sort((x, y) => x.Item2.SessionName.CompareTo(y.Item2.SessionName));
            m_availableSavesLocal = loadListRes.AvailableSaves;
            AfterPartLoaded();
            screen.CloseScreen();
        }


#if !XB1 // XB1_NOWORKSHOP
        private IMyAsyncResult beginWorkshop()
        {
            return new LoadWorkshopResult();
        }

        private void endWorkshop(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadResult = (LoadWorkshopResult)result;
            m_subscribedScenarios = loadResult.SubscribedScenarios;
            foreach(var item in loadResult.SubscribedScenarios)
            {
                MyWorldInfo wi = new MyWorldInfo();
                wi.SessionName = item.Title;
                wi.Briefing = item.Description;
                wi.WorkshopId = item.PublishedFileId;
                m_availableSavesWorkshop.Add(new Tuple<string, MyWorldInfo>(WORKSHOP_PATH_TAG, wi));
            }
            m_availableSavesWorkshop.Sort((x, y) => x.Item2.SessionName.CompareTo(y.Item2.SessionName));
            AfterPartLoaded();
            screen.CloseScreen();
        }

        class LoadWorkshopResult : IMyAsyncResult
        {
            public bool IsCompleted { get { return this.Task.IsComplete; } }
            public Task Task
            {
                get;
                private set;
            }

            /// <summary>
            /// List of scenarios user is subscribed to, or null if there was an error
            /// during operation.
            /// </summary>
            public List<MySteamWorkshop.SubscribedItem> SubscribedScenarios;

            public LoadWorkshopResult()
            {
                Task = Parallel.Start(() =>
                {
                    SubscribedScenarios = new List<MySteamWorkshop.SubscribedItem>();
                    if (!MySteam.IsOnline)
                        return;
                    if (!MySteamWorkshop.GetSubscribedScenariosBlocking(SubscribedScenarios))
                        return;
                });
            }
        }
#endif // !XB1

        #endregion

        #region steam publish
#if !XB1 // XB1_NOWORKSHOP
        private MySteamWorkshop.SubscribedItem GetSubscribedItem(ulong? publishedFileId)
        {
            foreach (var subcribedItem in m_subscribedScenarios)
                if (subcribedItem.PublishedFileId == publishedFileId)
                    return subcribedItem;
            return null;
        }

        void OnPublishButtonClick(MyGuiControlButton sender)
        {
            var row = m_scenarioTable.SelectedRow;
            if (row == null)
                return;

            if (row.UserData == null)
                return;

            string fullPath = (string)(((Tuple<string, MyWorldInfo>)row.UserData).Item1);
            MyWorldInfo worldInfo = FindSave(m_scenarioTable.SelectedRow).Item2;
            //var mod = (MyObjectBuilder_Checkpoint.ModItem)row.UserData;
            //var nameSB = m_selectedRow.GetCell(1).Text;
            //var name = nameSB.ToString();
            
            MyStringId textQuestion, captionQuestion;
            if (worldInfo.WorkshopId != null)
            {
                textQuestion = MySpaceTexts.MessageBoxTextDoYouWishToUpdateScenario;
                captionQuestion = MySpaceTexts.MessageBoxCaptionDoYouWishToUpdateScenario;
            }
            else
            {
                textQuestion = MySpaceTexts.MessageBoxTextDoYouWishToPublishScenario;
                captionQuestion = MySpaceTexts.MessageBoxCaptionDoYouWishToPublishScenario;
            }

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                styleEnum: MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageText: MyTexts.Get(textQuestion),
                messageCaption: MyTexts.Get(captionQuestion),
                callback: delegate(MyGuiScreenMessageBox.ResultEnum val)
                {
                    if (val == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        string[] inTags = null;
                        var subscribedItem = GetSubscribedItem(worldInfo.WorkshopId);
                        if (subscribedItem != null)
                        {
                            inTags = subscribedItem.Tags;

                            if (subscribedItem.SteamIDOwner != Sync.MyId)
                            {
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextPublishFailed_OwnerMismatchMod),//TODO rename
                                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionModPublishFailed)));
                                return;
                            }
                        }

                        /*MyGuiSandbox.AddScreen(new MyGuiScreenWorkshopTags(MySteamWorkshop.WORKSHOP_SCENARIO_TAG, MySteamWorkshop.ScenarioCategories, inTags, delegate(MyGuiScreenMessageBox.ResultEnum tagsResult, string[] outTags)
                        {
                            if (tagsResult == MyGuiScreenMessageBox.ResultEnum.YES)
                            {*/
                                MySteamWorkshop.PublishScenarioAsync(fullPath, worldInfo.SessionName, worldInfo.Description, worldInfo.WorkshopId, /*outTags,*/ SteamSDK.PublishedFileVisibility.Public, callbackOnFinished: delegate(bool success, Result result, ulong publishedFileId)//TODO public visibility!!
                                {
                                    if (success)
                                    {
                                        ulong dummy;
                                        var checkpoint = MyLocalCache.LoadCheckpoint(fullPath, out dummy);
                                        worldInfo.WorkshopId = publishedFileId;
                                        checkpoint.WorkshopId = publishedFileId;
                                        MyLocalCache.SaveCheckpoint(checkpoint, fullPath);

                                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                            styleEnum: MyMessageBoxStyleEnum.Info,
                                            messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextScenarioPublished),
                                            messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionScenarioPublished),
                                            callback: (a) =>
                                            {
                                                MySteam.API.OpenOverlayUrl(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", publishedFileId));
                                                FillList();
                                            }));
                                    }
                                    else
                                    {
                                        MyStringId error;
                                        switch (result)
                                        {
                                            case Result.AccessDenied:
                                                error = MyCommonTexts.MessageBoxTextPublishFailed_AccessDenied;
                                                break;
                                            default:
                                                error = MySpaceTexts.MessageBoxTextScenarioPublishFailed;
                                                break;
                                        }

                                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                            messageText: MyTexts.Get(error),
                                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionModPublishFailed)));
                                    }
                                });/*
                            }
                        }));*/
                    }
                }));
        }
#endif // !XB1

        #endregion

        private void OnBrowseWorkshopClick(MyGuiControlButton obj)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_SCENARIOS, "Steam Workshop");
        }

        protected override MyStringId ScreenCaption
        {
            get { return MySpaceTexts.ScreenCaptionScenario; }
        }

        protected override bool IsOnlineMode
        {
            get { return m_onlineMode.GetSelectedKey() != (int)MyOnlineModeEnum.OFFLINE; }
        }
    }
}
