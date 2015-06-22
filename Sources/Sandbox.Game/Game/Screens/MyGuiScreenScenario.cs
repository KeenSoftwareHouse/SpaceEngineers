using ParallelTasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
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
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenScenario : MyGuiScreenBase
    {
        enum StateEnum
        {
            ListNeedsReload,
            ListLoading,
            ListLoaded
        }
        private StateEnum m_state;
        private int m_listLoadedParts;

        private List<Tuple<string, MyWorldInfo>> m_availableSaves = new List<Tuple<string, MyWorldInfo>>();
        private List<Tuple<string, MyWorldInfo>> m_availableSavesLocal = new List<Tuple<string, MyWorldInfo>>();
        private List<Tuple<string, MyWorldInfo>> m_availableSavesKeens = new List<Tuple<string, MyWorldInfo>>();
        private List<Tuple<string, MyWorldInfo>> m_availableSavesWorkshop = new List<Tuple<string, MyWorldInfo>>();

        public static MyGuiScreenScenario Static;
        bool m_nameRewritten;
        string m_sessionPath;
        private int m_selectedRow;

        private List<MySteamWorkshop.SubscribedItem> m_subscribedScenarios;
        const string WORKSHOP_PATH_TAG = "workshop";

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
        //LEFT:
        MyGuiControlTable m_scenarioTable;

        //RIGHT:
        MyGuiControlTextbox m_nameTextbox, m_descriptionTextbox;

        MyGuiControlLabel m_difficultyLabel;
        MyGuiControlCombobox m_difficultyCombo;

        MyGuiControlLabel m_onlineModeLabel;
        MyGuiControlCombobox m_onlineMode;

        MyGuiControlLabel m_maxPlayersLabel;
        MyGuiControlSlider m_maxPlayersSlider;

        MyGuiControlMultilineText m_descriptionBox;

        //BUTTONS:
        MyGuiControlButton m_removeButton, m_publishButton, m_editButton, m_browseWorkshopButton;
        MyGuiControlButton m_refreshButton, m_openInWorkshopButton, m_okButton, m_cancelButton;

        

        MyGuiControlList m_scenarioTypesList;
        MyGuiControlRadioButtonGroup m_scenarioTypesGroup;

        public MyGuiScreenScenario()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, CalcSize(null/*checkpoint*/))
        {
            Static = this;
            RecreateControls(true);
        }

        public static Vector2 CalcSize(MyObjectBuilder_Checkpoint checkpoint)//TODO optimize
        {
            float width = checkpoint == null ? 0.9f : 0.65f;
            float height = checkpoint == null ? 1.24f : 0.97f;
            if (checkpoint != null)
                height -= 0.05f;
            if (MyFakes.OCTOBER_RELEASE_HIDE_WORLD_PARAMS)
                height -= 0.27f;

            return new Vector2(width, height);
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

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            BuildControls();
            SetDefaultValues();
        }

        protected virtual void BuildControls()
        {
            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.65f, 0.1f);

            AddCaption(MySpaceTexts.ScreenCaptionScenario);

            //RIGHT:
            int numControls = 0;

            var nameLabel = MakeLabel(MySpaceTexts.Name);
            var descriptionLabel = MakeLabel(MySpaceTexts.Description);
            var difficultyLabel = MakeLabel(MySpaceTexts.Difficulty);
            var onlineModeLabel = MakeLabel(MySpaceTexts.WorldSettings_OnlineMode);
            m_maxPlayersLabel = MakeLabel(MySpaceTexts.MaxPlayers);

            float width = 0.284375f + 0.025f;

            m_nameTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_NAME_LENGTH);
            m_nameTextbox.Enabled=false;
            m_descriptionTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_DESCRIPTION_LENGTH);
            m_descriptionTextbox.Enabled = false;
            m_difficultyCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_difficultyCombo.AddItem((int)0, MySpaceTexts.DifficultyEasy);
            m_difficultyCombo.AddItem((int)1, MySpaceTexts.DifficultyNormal);
            m_difficultyCombo.AddItem((int)2, MySpaceTexts.DifficultyHard);

            m_onlineMode = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
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
            m_onlineMode.Enabled = false;
            m_scenarioTypesList = new MyGuiControlList();


            //BUTTONS
            m_removeButton = new MyGuiControlButton(position: buttonsOrigin, size: buttonSize, text: MyTexts.Get(MySpaceTexts.buttonRemove), 
                onButtonClick: OnOkButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_publishButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f + m_removeButton.Size.X, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.buttonPublish), 
                onButtonClick: OnPublishButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_editButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(2*(0.01f + m_removeButton.Size.X), 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.buttonEdit), 
                onButtonClick: OnEditButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_browseWorkshopButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(3*(0.01f + m_removeButton.Size.X), 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.buttonBrowseWorkshop),
                onButtonClick: OnBrowseWorkshopClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

            m_refreshButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.0f, m_removeButton.Size.Y+0.01f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.buttonRefresh), 
                onButtonClick: OnRefreshButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_openInWorkshopButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2((0.01f + m_removeButton.Size.X), m_removeButton.Size.Y + 0.01f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.buttonOpenInWorkshop), 
                onButtonClick: OnOkButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_okButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(2 * (0.01f + m_removeButton.Size.X), m_removeButton.Size.Y + 0.01f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Ok), 
                onButtonClick: OnOkButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(3 * (0.01f + m_removeButton.Size.X), m_removeButton.Size.Y + 0.01f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel), 
                onButtonClick: OnCancelButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

            m_onlineMode.ItemSelected += OnOnlineModeSelect;
            m_onlineMode.AddItem((int)MyOnlineModeEnum.OFFLINE, MySpaceTexts.WorldSettings_OnlineModeOffline);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PRIVATE, MySpaceTexts.WorldSettings_OnlineModePrivate);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.FRIENDS, MySpaceTexts.WorldSettings_OnlineModeFriends);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PUBLIC, MySpaceTexts.WorldSettings_OnlineModePublic);


            m_nameTextbox.TextChanged += m_nameTextbox_TextChanged;

            // Add controls in pairs; label first, control second. They will be laid out automatically this way.
            Controls.Add(nameLabel);
            Controls.Add(m_nameTextbox);
            //m_nameTextbox.Enabled = false;
            Controls.Add(descriptionLabel);
            Controls.Add(m_descriptionTextbox);
            //m_descriptionTextbox.Enabled = false;
            Controls.Add(difficultyLabel);
            Controls.Add(m_difficultyCombo);
            m_difficultyCombo.Enabled = false;

            Controls.Add(onlineModeLabel);
            Controls.Add(m_onlineMode);
            //m_onlineMode.Enabled = false;
            Controls.Add(m_maxPlayersLabel);
            Controls.Add(m_maxPlayersSlider);


            float labelSize = 0.12f;

            float MARGIN_TOP = 0.1f;
            float MARGIN_LEFT = 0.42f;// m_isNewGame ? 0.315f : 0.08f;

            // Automatic layout.
            Vector2 originL, originC;
            Vector2 controlsDelta = new Vector2(0f, 0.052f);
            float rightColumnOffset;
            originL = -m_size.Value / 2 + new Vector2(MARGIN_LEFT, MARGIN_TOP);
            originC = originL + new Vector2(labelSize, 0f);
            rightColumnOffset = originC.X + m_onlineMode.Size.X - labelSize - 0.017f;

            foreach (var control in Controls)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                if (control is MyGuiControlLabel)
                    control.Position = originL + controlsDelta * numControls;
                else
                    control.Position = originC + controlsDelta * numControls++;
            }
            //BRIEFING:
            //var textBackgroundPanel = AddCompositePanel(MyGuiConstants.TEXTURE_RECTANGLE_DARK, new Vector2(0f,0f), new Vector2(0.43f, 0.422f), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            //textBackgroundPanel.InnerHeight = 6;
            MyGuiControlParent briefing = new MyGuiControlParent();//new Vector2(0f, 0f), new Vector2(0.43f, 0.422f));

            var briefingScrollableArea = new MyGuiControlScrollablePanel(
                scrolledControl: briefing)
            {
                Name = "BriefingScrollableArea",
                ScrollbarVEnabled = true,
                Position = new Vector2(-0.02f, -0.12f),
                Size = new Vector2(0.43f, 0.422f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ScrolledAreaPadding = new MyGuiBorderThickness(0.005f),
            };
            Controls.Add(briefingScrollableArea);
            //inside scrollable area:
            m_descriptionBox = AddMultilineText(offset: new Vector2(0.0f, 0.0f), size: new Vector2(1f, 1f), selectable: false);
            briefing.Controls.Add(m_descriptionBox);

            //LEFT:
            m_scenarioTable = new MyGuiControlTable();
            m_scenarioTable.Position = new Vector2(-0.42f, -0.5f+MARGIN_TOP);
            m_scenarioTable.Size = new Vector2(0.38f, 1.8f);
            m_scenarioTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_scenarioTable.VisibleRowsCount = 20;
            m_scenarioTable.ColumnsCount = 2;
            m_scenarioTable.SetCustomColumnWidths(new float[] { 0.085f, 0.905f });
            m_scenarioTable.SetColumnName(1, MyTexts.Get(MySpaceTexts.Name));
            m_scenarioTable.ItemSelected += OnTableItemSelected;
            //m_scenarioTable.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            //m_scenarioTable.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            Controls.Add(m_scenarioTable);
            //BUTTONS:
            Controls.Add(m_removeButton);
            m_removeButton.Enabled = false;
            Controls.Add(m_publishButton);
            m_publishButton.Enabled = false;
            Controls.Add(m_editButton);
            m_editButton.Enabled = false;
            Controls.Add(m_browseWorkshopButton);
            Controls.Add(m_refreshButton);
            Controls.Add(m_openInWorkshopButton);
            m_openInWorkshopButton.Enabled = false;
            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            CloseButtonEnabled = true;

            SetDefaultValues();
        }
        protected MyGuiControlMultilineText AddMultilineText(Vector2? size = null, Vector2? offset = null, float textScale = 1.0f, bool selectable = false)
        {
            Vector2 textboxSize = size ?? this.Size ?? new Vector2(0.5f, 0.5f);

            MyGuiControlMultilineText textbox = new MyGuiControlMultilineText(
                position: offset ?? Vector2.Zero,
                size: textboxSize,
                //backgroundColor: m_defaultColor,
                //textScale: this.m_scale * textScale,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                selectable: selectable,
                font: MyFontEnum.Blue);

            //textbox.BackgroundTexture = MyGuiConstants.TEXTURE_NEWS_BACKGROUND;
            //textbox.TextSize = new Vector2(0.2f, 0.2f);
            return textbox;
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }
        void m_nameTextbox_TextChanged(MyGuiControlTextbox obj)
        {
            m_nameRewritten = true;
        }
        private void scenario_SelectedChanged(MyGuiControlRadioButtonGroup group)
        {
            if (!m_nameRewritten)
            {
                var title = ((MyGuiControlScenarioButton)(m_scenarioTypesGroup.SelectedButton)).Title;
                m_nameTextbox.Text = title.ToString() + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                m_nameRewritten = false;
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
                    MyGuiScreenLoadSandbox.LoadSingleplayerSession(save.Item1);
                }
                else
                    Debug.Fail("save not found");
            }
            else
                Debug.Fail("row not found");
        }
        private void OnOkButtonClick(object sender)
        {
            // Validate
            if (m_nameTextbox.Text.Length < MySession.MIN_NAME_LENGTH || m_nameTextbox.Text.Length > MySession.MAX_NAME_LENGTH)
            {
                MyStringId errorType;
                if (m_nameTextbox.Text.Length < MySession.MIN_NAME_LENGTH) errorType = MySpaceTexts.ErrorNameTooShort;
                else errorType = MySpaceTexts.ErrorNameTooLong;
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(errorType),
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError));
                messageBox.SkipTransition = true;
                messageBox.InstantClose = false;
                MyGuiSandbox.AddScreen(messageBox);
                return;
            }

            if (m_descriptionTextbox.Text.Length > MySession.MAX_DESCRIPTION_LENGTH)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MySpaceTexts.ErrorDescriptionTooLong),
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError));
                messageBox.SkipTransition = true;
                messageBox.InstantClose = false;
                MyGuiSandbox.AddScreen(messageBox);
                return;
            }
            CloseScreen();
            LoadSandbox(m_onlineMode.GetSelectedKey() != (int)MyOnlineModeEnum.OFFLINE);
        }
        private void LoadSandbox(bool MP)
        {
            MyLog.Default.WriteLine("LoadSandbox() - Start");
            var row = m_scenarioTable.SelectedRow;
            if (row != null)
            {
                var save = FindSave(row);
                if (save != null)
                {
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

                                LoadMission(sessionPath, m_nameTextbox.Text, m_descriptionTextbox.Text, MP);
                            }
                            else
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                            messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextWorkshopDownloadFailed),
                                            messageCaption: MyTexts.Get(MySpaceTexts.ScreenCaptionWorkshop)));
                        });
                    }
                    else
                        LoadMission(save.Item1, m_nameTextbox.Text, m_descriptionTextbox.Text, MP);
                }
            }

            MyLog.Default.WriteLine("LoadSandbox() - End");
        }

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

        private void OnCancelButtonClick(object sender)
        {
            CloseScreen();
        }

        private void OnRefreshButtonClick(object sender)
        {
            m_state = StateEnum.ListNeedsReload;
        }
        private void LoadValues()
        {
            /*m_nameTextbox.Text = m_checkpoint.SessionName ?? "";
            m_descriptionTextbox.Text = m_checkpoint.Description ?? "";
            m_settings = CopySettings(m_checkpoint.Settings);
            m_mods = m_checkpoint.Mods;
            m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.Normal);
            SetSettingsToControls();
             */
        }

        private void SetDefaultValues()
        {
            m_difficultyCombo.SelectItemByIndex(1);
            m_onlineMode.SelectItemByIndex(0);
            FillRight();
        }
        private void StartNewSandbox()
        {
            MyLog.Default.WriteLine("StartNewSandbox - Start");
/*            MyGuiScreenGamePlay.StartLoading(delegate
            {
                MySession.Start(
                    m_nameTextbox.Text,
                    GetDescription(),
                    GetPassword(),
                    m_settings,
                    m_mods,
                    new MyWorldGenerator.Args()
                    {
                        Scenario = (m_scenarioTypesGroup.SelectedButton as MyGuiControlScenarioButton).Scenario
                    }
                );
            });
        */
        }

        #region Event handlers

        void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_selectedRow = eventArgs.RowIndex;
            FillRight();
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

        /*void OnTableItemConfirmedOrDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            LoadSandbox();
        }*/
        void FillRight()
        {
            if (m_scenarioTable == null || m_scenarioTable.SelectedRow == null)
            {
                m_nameTextbox.SetText(new StringBuilder(""));
                m_descriptionTextbox.SetText(new StringBuilder(""));
            }
            else
            {
                Tuple<string, MyWorldInfo> t = FindSave(m_scenarioTable.SelectedRow);
                m_nameTextbox.SetText(new StringBuilder(t.Item2.SessionName));
                m_descriptionTextbox.SetText(new StringBuilder(t.Item2.Description));
                m_descriptionBox.Text = new StringBuilder(t.Item2.Briefing);
            }

        }
        #endregion

        private Tuple<string, MyWorldInfo> FindSave(MyGuiControlTable.Row row)
        {
            return (Tuple<string, MyWorldInfo>)(row.UserData);
        }

        public void LoadMission(string sessionPath, string name, string description, bool multiplayer)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");
            MyLog.Default.WriteLine(sessionPath);

            ulong checkpointSizeInBytes;
            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkpointSizeInBytes);

            checkpoint.Settings.OnlineMode=(MyOnlineModeEnum)m_onlineMode.GetSelectedKey();
            checkpoint.Settings.MaxPlayers = (short)m_maxPlayersSlider.Value;
            checkpoint.Settings.Scenario = true;
            checkpoint.Settings.GameMode = MyGameModeEnum.Survival;
            checkpoint.Settings.ScenarioEditMode = false;

            if (!MySession.IsCompatibleVersion(checkpoint))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MySpaceTexts.DialogTextIncompatibleWorldVersion).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.DialogTextIncompatibleWorldVersion),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            if (!MySteamWorkshop.CheckLocalModsAllowed(checkpoint.Mods, checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MySpaceTexts.DialogTextLocalModsDisabledInMultiplayer).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }


            MySteamWorkshop.DownloadModsAsync(checkpoint.Mods, delegate(bool success)
            {
                if (success || (checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE) && MySteamWorkshop.CanRunOffline(checkpoint.Mods))
                {
                    //Sandbox.Audio.MyAudio.Static.Mute = true;

                    MyScreenManager.CloseAllScreensNowExcept(null);
                    MyGuiSandbox.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

                    // May be called from gameplay, so we must make sure we unload the current game
                    if (MySession.Static != null)
                    {
                        MySession.Static.Unload();
                        MySession.Static = null;
                    }

                    //seed 0 has special meaning - please randomize at mission start. New seed will be saved and game will run with it ever since.
                    //  if you use this, YOU CANNOT HAVE ANY PROCEDURAL ASTEROIDS ALREADY SAVED
                    if (checkpoint.Settings.ProceduralSeed==0)
                        checkpoint.Settings.ProceduralSeed = MyRandom.Instance.Next();

                    MyGuiScreenGamePlay.StartLoading(delegate
                    {
                        checkpoint.Settings.Scenario = true;
                        MySession.LoadMission(sessionPath, checkpoint, checkpointSizeInBytes, name, description);
                    });
                }
                else
                {
                    MyLog.Default.WriteLine(MyTexts.Get(MySpaceTexts.DialogTextDownloadModsFailed).ToString());
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                        messageText: MyTexts.Get(MySpaceTexts.DialogTextDownloadModsFailed),
                        buttonType: MyMessageBoxButtonsType.OK, callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                        {
                            if (MyFakes.QUICK_LAUNCH != null)
                                MyGuiScreenMainMenu.ReturnToMainMenu();
                        }));
                }
                MyLog.Default.WriteLine("LoadSession() - End");
            });

        }

        public override bool Update(bool hasFocus)
        {
            if (m_state == StateEnum.ListNeedsReload)
                FillList();

            if (m_scenarioTable.SelectedRow != null)
            {
                m_okButton.Enabled = true;
            }
            else
            {
                m_okButton.Enabled = false;
            }

            return base.Update(hasFocus);
        }

        public override bool Draw()
        {
            // Dont draw screen when the list is about to be reloaded,
            // otherwise it will flick just before opening the loading screen
            if (m_state != StateEnum.ListLoaded)
                return false;
            return base.Draw();
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (m_state == StateEnum.ListNeedsReload)
                FillList();
        }

        #region Async Loading

        void FillList()
        {
            m_state = StateEnum.ListLoading;
            m_listLoadedParts = 0;
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MySpaceTexts.LoadingPleaseWait, null, beginKeens, endKeens));//from missions
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MySpaceTexts.LoadingPleaseWait, null, beginWorkshop, endWorkshop));//workshop items
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MySpaceTexts.LoadingPleaseWait, null, beginLocal, endLocal));//user's from saves
        }

        private void AddHeaders()
        {
            m_scenarioTable.SetColumnName(1, MyTexts.Get(MySpaceTexts.Name));
        }

        private void RefreshGameList()
        {
            int selectedIndex = m_scenarioTable.SelectedRowIndex??-1;
            m_scenarioTable.Clear();
            AddHeaders();

            for (int index = 0; index < m_availableSaves.Count; index++)
            {
                var checkpoint = m_availableSaves[index].Item2;
                var name = new StringBuilder(checkpoint.SessionName);

                var row = new MyGuiControlTable.Row(m_availableSaves[index]);
                if (m_availableSaves[index].Item1 == WORKSHOP_PATH_TAG)
                    row.AddCell(new MyGuiControlTable.Cell(text: String.Empty, icon: MyGuiConstants.TEXTURE_ICON_MODS_WORKSHOP));
                else if (m_availableSaves[index].Item2.ScenarioEditMode == true)
                    row.AddCell(new MyGuiControlTable.Cell(text: String.Empty, icon: MyGuiConstants.TEXTURE_ICON_MODS_LOCAL));
                else
                    row.AddCell(new MyGuiControlTable.Cell(text: String.Empty, icon: MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL));

                row.AddCell(new MyGuiControlTable.Cell(text: name, userData: name));
                m_scenarioTable.Add(row);

                // Select row with same world ID as we had before refresh.
                if (index == selectedIndex)
                {
                    m_selectedRow = index;
                    m_scenarioTable.SelectedRow = row;
                }
            }

            m_scenarioTable.SelectedRowIndex = m_selectedRow;
            m_scenarioTable.ScrollToSelection();
            FillRight();
        }
        private void AfterPartLoaded()
        {
            if (++m_listLoadedParts == 3)
            {
                m_state = StateEnum.ListLoaded;
                //KSH
                m_availableSaves = m_availableSavesKeens;
                m_availableSavesKeens=null;
                //workshop
                m_availableSaves.AddList(m_availableSavesWorkshop);
                m_availableSavesWorkshop.Clear();
                //Local
                foreach (var save in m_availableSavesLocal)
                {
                    var checkpoint = save.Item2;
                    if (!checkpoint.ScenarioEditMode)
                        continue;
                    m_availableSaves.Add(save);
                }
                m_availableSavesLocal.Clear();

                RefreshGameList();
            }
        }
        private IMyAsyncResult beginKeens()
        {
            return new MyLoadListResult(true);
        }
        private void endKeens(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;
            m_availableSavesKeens = loadListRes.AvailableSaves;
            m_availableSavesKeens.Sort((x, y) => x.Item2.SessionName.CompareTo(y.Item2.SessionName));

            if (loadListRes.ContainsCorruptedWorlds)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MySpaceTexts.SomeWorldFilesCouldNotBeLoaded),
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(messageBox);
            }
            AfterPartLoaded();
            screen.CloseScreen();
        }

        
        private IMyAsyncResult beginLocal()
        {
            return new MyLoadListResult(false);
        }
        private void endLocal(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;
            loadListRes.AvailableSaves.Sort((x, y) => x.Item2.SessionName.CompareTo(y.Item2.SessionName));
            m_availableSavesLocal = loadListRes.AvailableSaves;
            AfterPartLoaded();
            screen.CloseScreen();
        }


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
        #endregion

        #region steam publish
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

                            if (subscribedItem.SteamIDOwner != MySteam.UserId)
                            {
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextPublishFailed_OwnerMismatchMod),//TODO rename
                                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionModPublishFailed)));
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
                                                error = MySpaceTexts.MessageBoxTextPublishFailed_AccessDenied;
                                                break;
                                            default:
                                                error = MySpaceTexts.MessageBoxTextScenarioPublishFailed;
                                                break;
                                        }

                                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                            messageText: MyTexts.Get(error),
                                            messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionModPublishFailed)));
                                    }
                                });/*
                            }
                        }));*/
                    }
                }));
        }


        #endregion
        private void OnBrowseWorkshopClick(MyGuiControlButton obj)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_BROWSE_WORKSHOP_SCENARIOS, "Steam Workshop");
        }



    }
}
