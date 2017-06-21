#region Using

using System.Collections.Generic;
using VRageMath;

using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.World;
using Sandbox.Engine.Networking;
using System;
using VRage;
using Sandbox.Game.Screens.Helpers;
using System.Diagnostics;
using System.IO;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage.FileSystem;
using VRage.Game;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Voxels;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenWorldSettings : MyGuiScreenBase
    {

        public static MyGuiScreenWorldSettings Static;
        internal MyGuiScreenAdvancedWorldSettings Advanced;
        internal MyGuiScreenWorldGeneratorSettings WorldGenerator;
#if !XB1 // XB1_NOWORKSHOP
        internal MyGuiScreenMods ModsScreen;
#endif // !XB1

        bool m_nameRewritten;
        protected bool m_isNewGame;
        string m_sessionPath;
        bool m_isHostilityChanged;

        protected MyObjectBuilder_SessionSettings m_settings;
        public MyObjectBuilder_SessionSettings Settings
        {
            get
            {
                GetSettingsFromControls();
                return m_settings;
            }
        }


        public enum MySoundModeEnum
        {
            Arcade,
            Realistic,
        }

        private List<MyObjectBuilder_Checkpoint.ModItem> m_mods;

        MyObjectBuilder_Checkpoint m_checkpoint;
        public MyObjectBuilder_Checkpoint Checkpoint
        {
            get { return m_checkpoint; }
        }

        MyGuiControlTextbox m_nameTextbox, m_descriptionTextbox;
        MyGuiControlCombobox m_onlineMode, m_environment, m_asteroidAmountCombo, m_soundModeCombo;
        MyGuiControlButton m_okButton, m_cancelButton, m_survivalModeButton, m_creativeModeButton, m_worldGeneratorButton;
        MyGuiControlSlider m_maxPlayersSlider;
        MyGuiControlLabel m_maxPlayersLabel, m_asteroidAmountLabel;
        MyGuiControlCheckbox m_autoSave;
        MyGuiControlCheckbox m_blockLimits;

        MyGuiControlList m_scenarioTypesList;
        MyGuiControlRadioButtonGroup m_scenarioTypesGroup;

        private int? m_asteroidAmount;
        public int AsteroidAmount
        {
            get
            {
                return m_asteroidAmount.HasValue ? m_asteroidAmount.Value : (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralLow;
            }
            set
            {
                m_asteroidAmount = value;
                switch (value)
                {
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.None:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.None);
                        return;
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.Normal:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.Normal);
                        return;
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.More:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.More);
                        return;
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.Many:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.Many);
                        return;
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNone:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNone);
                        return;
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralLow:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralLow);
                        return;
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNormal:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNormal);
                        return;
                    case (int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralHigh:
                        m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralHigh);
                        return;
                    default:
                        Debug.Assert(false, "Unhandled value in AsteroidAmountEnum");
                        return;
                }
            }
        }

        // New game constructor
        public MyGuiScreenWorldSettings()
            : this(null, null)
        {
        }

        // Edit game constructor
        public MyGuiScreenWorldSettings(MyObjectBuilder_Checkpoint checkpoint, string path)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, CalcSize(checkpoint))
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenWorldSettings.ctor START");
            EnabledBackgroundFade = true;
            Static = this;

            m_checkpoint = checkpoint;
            if (checkpoint == null || checkpoint.Mods == null)
                m_mods = new List<MyObjectBuilder_Checkpoint.ModItem>();
            else
                m_mods = checkpoint.Mods;
            m_sessionPath = path;
            m_isNewGame = (checkpoint == null);

            RecreateControls(true);

            // If this screen is loaded from "Saved Games" it should not use the EnvironmentHostility logic 
            m_isHostilityChanged = !m_isNewGame;

            MySandboxGame.Log.WriteLine("MyGuiScreenWorldSettings.ctor END");
        }

        public static Vector2 CalcSize(MyObjectBuilder_Checkpoint checkpoint)
        {
            float width = checkpoint == null ? 0.9f : 0.65f;
            float height = checkpoint == null ? 0.97f : 0.95f;

            return new Vector2(width, height);
        }

        public override bool CloseScreen()
        {
            if (WorldGenerator != null)
            {
                WorldGenerator.CloseScreen();
            }
            WorldGenerator = null;
            if (Advanced != null)
                Advanced.CloseScreen();
            Advanced = null;
#if !XB1 // XB1_NOWORKSHOP
            if (ModsScreen != null)
                ModsScreen.CloseScreen();
            ModsScreen = null;
#endif // !XB1
            Static = null;
            return base.CloseScreen();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenWorldSettings";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            BuildControls();

            if (m_isNewGame)
            {
                SetDefaultValues();
                new MyGuiControlScreenSwitchPanel(this, MyTexts.Get(MyCommonTexts.WorldSettingsScreen_Description));
            }
            else
            {
                LoadValues();
                m_nameTextbox.MoveCarriageToEnd();
                m_descriptionTextbox.MoveCarriageToEnd();
            }
        }

        Vector2 ProjectX(Vector2 vec)
        {
            return new Vector2(vec.X, 0);
        }

        protected virtual void BuildControls()
        {

            if (m_isNewGame)
            {
                //AddCaption(MyCommonTexts.ScreenCaptionCustomWorld);
            }
            else
                AddCaption(MyCommonTexts.ScreenCaptionEditSettings);

            int numControls = 0;


            float MARGIN_TOP = m_isNewGame ? 0.18f : 0.1f;
            float MARGIN_BOTTOM = 0.11f;
            float MARGIN_LEFT = m_isNewGame ? 0.23f : 0.03f;
            float MARGIN_RIGHT = m_isNewGame ? 0.03f : 0.03f;
            float MARGIN_BOTTOM_LISTBOX = 0.015f;

            // Automatic layout.
            Vector2 originL, originC, sizeL, sizeC, sizeControls;
            Vector2 controlsDelta = new Vector2(0f, 0.052f);
            float rightColumnOffset;
            originL = -m_size.Value / 2 + new Vector2(MARGIN_LEFT, MARGIN_TOP) + controlsDelta / 2;
            sizeControls = m_size.Value / 2 - originL;
            sizeControls.X -= MARGIN_RIGHT + 0.005f;
            sizeControls.Y -= MARGIN_BOTTOM;
            sizeL = sizeControls * (m_isNewGame ? 0.44f : 0.395f);

            originC = originL + new Vector2(sizeL.X, 0f);
            sizeC = sizeControls - sizeL;
            //rightColumnOffset = originC.X + m_onlineMode.Size.X - labelSize - 0.017f;

            // Button positioning
            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2;
            buttonsOrigin.X -= MARGIN_RIGHT;
            buttonsOrigin.Y -= 0.03f;

            var nameLabel = MakeLabel(MyCommonTexts.Name);
            var descriptionLabel = MakeLabel(MyCommonTexts.Description);
            var gameModeLabel = MakeLabel(MyCommonTexts.WorldSettings_GameMode);
            var onlineModeLabel = MakeLabel(MyCommonTexts.WorldSettings_OnlineMode);
            m_maxPlayersLabel = MakeLabel(MyCommonTexts.MaxPlayers);
            var environmentLabel = MakeLabel(MySpaceTexts.WorldSettings_EnvironmentHostility);
            var soundModeLabel = MakeLabel(MySpaceTexts.WorldSettings_SoundMode);

            float width = 0.284375f + 0.025f;

            m_nameTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_NAME_LENGTH);
            m_descriptionTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_DESCRIPTION_LENGTH);
            m_onlineMode = new MyGuiControlCombobox(size: new Vector2(sizeC.X, 0.04f));
            m_environment = new MyGuiControlCombobox(size: new Vector2(sizeC.X, 0.04f));
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



            m_asteroidAmountLabel = MakeLabel(MySpaceTexts.Asteroid_Amount);
            m_asteroidAmountCombo = new MyGuiControlCombobox(size: new Vector2(sizeC.X, 0.04f));

            m_asteroidAmountCombo.ItemSelected += m_asteroidAmountCombo_ItemSelected;
            m_soundModeCombo = new MyGuiControlCombobox(size: new Vector2(sizeC.X, 0.04f));

            m_scenarioTypesList = new MyGuiControlList();

            // Ok/Cancel
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin, size: buttonSize, text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            buttonsOrigin.X -= m_cancelButton.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X;
            m_okButton = new MyGuiControlButton(position: buttonsOrigin, size: buttonSize, text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOkButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

            m_creativeModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MyCommonTexts.WorldSettings_GameModeCreative), onButtonClick: OnCreativeClick);
            m_creativeModeButton.SetToolTip(MySpaceTexts.ToolTipWorldSettingsModeCreative);
            m_survivalModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MyCommonTexts.WorldSettings_GameModeSurvival), onButtonClick: OnSurvivalClick);
            m_survivalModeButton.SetToolTip(MySpaceTexts.ToolTipWorldSettingsModeSurvival);

            m_onlineMode.ItemSelected += OnOnlineModeSelect;
            m_onlineMode.AddItem((int)MyOnlineModeEnum.OFFLINE, MyCommonTexts.WorldSettings_OnlineModeOffline);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PRIVATE, MyCommonTexts.WorldSettings_OnlineModePrivate);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.FRIENDS, MyCommonTexts.WorldSettings_OnlineModeFriends);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PUBLIC, MyCommonTexts.WorldSettings_OnlineModePublic);

            m_environment.AddItem((int)MyEnvironmentHostilityEnum.SAFE, MySpaceTexts.WorldSettings_EnvironmentHostilitySafe);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.NORMAL, MySpaceTexts.WorldSettings_EnvironmentHostilityNormal);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.CATACLYSM, MySpaceTexts.WorldSettings_EnvironmentHostilityCataclysm);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.CATACLYSM_UNREAL, MySpaceTexts.WorldSettings_EnvironmentHostilityCataclysmUnreal);
            m_environment.ItemSelected += HostilityChanged;

            m_soundModeCombo.AddItem((int)MySoundModeEnum.Arcade, MySpaceTexts.WorldSettings_ArcadeSound);
            m_soundModeCombo.AddItem((int)MySoundModeEnum.Realistic, MySpaceTexts.WorldSettings_RealisticSound);

            if (m_isNewGame)
            {
                if(MyDefinitionManager.Static.GetScenarioDefinitions().Count == 0)
                    MyDefinitionManager.Static.LoadScenarios();

                m_scenarioTypesGroup = new MyGuiControlRadioButtonGroup();
                m_scenarioTypesGroup.SelectedChanged += scenario_SelectedChanged;

                RefreshCustomWorldsList();
            }

            m_nameTextbox.SetToolTip(string.Format(MyTexts.GetString(MyCommonTexts.ToolTipWorldSettingsName), MySession.MIN_NAME_LENGTH, MySession.MAX_NAME_LENGTH));
            m_descriptionTextbox.SetToolTip(MyTexts.GetString(MyCommonTexts.ToolTipWorldSettingsDescription));
            m_environment.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsEnvironment));
            m_onlineMode.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsOnlineMode));
            m_maxPlayersSlider.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsMaxPlayer));
            m_asteroidAmountCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsAsteroidAmount));
            m_soundModeCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsSoundMode));

            m_nameTextbox.TextChanged += m_nameTextbox_TextChanged;
            m_soundModeCombo.ItemSelected += m_soundModeCombo_ItemSelected;

            var advanced = new MyGuiControlButton(highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Advanced), onButtonClick: OnAdvancedClick);

#if !XB1 // XB1_NOWORKSHOP
            var mods = new MyGuiControlButton(highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MyCommonTexts.WorldSettings_Mods), onButtonClick: OnModsClick);
#endif // !XB1

            m_worldGeneratorButton = new MyGuiControlButton(highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_WorldGenerator), onButtonClick: OnWorldGeneratorClick);

            // Add controls in pairs; label first, control second. They will be laid out automatically this way.
            Controls.Add(nameLabel);
            Controls.Add(m_nameTextbox);
            Controls.Add(descriptionLabel);
            Controls.Add(m_descriptionTextbox);

            Controls.Add(gameModeLabel);
            Controls.Add(m_creativeModeButton);

            if (MyFakes.ENABLE_NEW_SOUNDS)
            {
                Controls.Add(soundModeLabel);
                Controls.Add(m_soundModeCombo);
            }

            Controls.Add(onlineModeLabel);
            Controls.Add(m_onlineMode);
            Controls.Add(m_maxPlayersLabel);
            Controls.Add(m_maxPlayersSlider);

            if (MyFakes.ENABLE_METEOR_SHOWERS)
            {
                Controls.Add(environmentLabel);
                Controls.Add(m_environment);
            }

            if (m_isNewGame && MyFakes.ENABLE_PLANETS == false)
            {
                Controls.Add(m_asteroidAmountLabel);
                Controls.Add(m_asteroidAmountCombo);
            }

            var blockLimitsLabel = MakeLabel(MyCommonTexts.WorldSettings_BlockLimits);
            m_blockLimits = new MyGuiControlCheckbox();
            m_blockLimits.IsCheckedChanged = blockLimits_CheckedChanged;
            m_blockLimits.SetToolTip(MyTexts.GetString(MyCommonTexts.ToolTipWorldSettingsBlockLimits));
            Controls.Add(blockLimitsLabel);
            Controls.Add(m_blockLimits);

            var autoSaveLabel = MakeLabel(MyCommonTexts.WorldSettings_AutoSave);
            m_autoSave = new MyGuiControlCheckbox();
            m_autoSave.SetToolTip(new StringBuilder().AppendFormat(MyCommonTexts.ToolTipWorldSettingsAutoSave, MyObjectBuilder_SessionSettings.DEFAULT_AUTOSAVE_IN_MINUTES).ToString());
            Controls.Add(autoSaveLabel);
            Controls.Add(m_autoSave);

#if !XB1 // XB1_NOWORKSHOP
            if (!MyFakes.XB1_PREVIEW)
            if (MyFakes.ENABLE_WORKSHOP_MODS)
                Controls.Add(mods);
#endif // !XB1

            Controls.Add(advanced);

            // Uncomment to show the World generator button again
            if (m_isNewGame && MyFakes.ENABLE_PLANETS == true)
            {
                Controls.Add(m_worldGeneratorButton);
            }

            foreach (var control in Controls)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                if (control is MyGuiControlLabel)
                    control.Position = originL + controlsDelta * numControls;
                else
                    control.Position = originC + controlsDelta * numControls++;
            }

            Controls.Add(m_survivalModeButton);
            m_survivalModeButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            m_survivalModeButton.Position = m_creativeModeButton.Position + new Vector2(m_onlineMode.Size.X, 0);

            m_nameTextbox.Size = m_onlineMode.Size;
            m_descriptionTextbox.Size = m_nameTextbox.Size;

            if (m_isNewGame)
            {
                Vector2 scenarioPosition = -m_size.Value / 2 + new Vector2(0.015f, MARGIN_TOP);

                m_scenarioTypesList.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                m_scenarioTypesList.Position = scenarioPosition;
                m_scenarioTypesList.Size = new Vector2(MyGuiConstants.LISTBOX_WIDTH, m_size.Value.Y - 0.02f - MARGIN_TOP);
                Controls.Add(m_scenarioTypesList);

                //MyGuiControlSeparatorList m_verticalLine = new MyGuiControlSeparatorList();
                //Vector2 position = nameLabel.Position + new Vector2(-0.025f, -0.02f);
                //m_verticalLine.AddVertical(position, m_size.Value.Y - MARGIN_BOTTOM - MARGIN_TOP + 0.04f);
                //Controls.Add(m_verticalLine);
            }

            var pos2 = advanced.Position;
            //pos2.X = m_isNewGame ? 0.160f : 0.0f;
            pos2.X = Size.HasValue ? Size.Value.X / 2.0f - advanced.Size.X - MARGIN_RIGHT : 0.0f;
            advanced.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            advanced.Position = pos2;

#if !XB1 // XB1_NOWORKSHOP
            mods.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            mods.Position = advanced.Position - new Vector2(advanced.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X, 0);
#endif // !XB1

            m_worldGeneratorButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            m_worldGeneratorButton.Position = advanced.Position - new Vector2(advanced.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X, -0.06f);

            if (MyFakes.XB1_PREVIEW)
            {
                var pos2p = m_worldGeneratorButton.Position;
                pos2p.X = Size.HasValue ? Size.Value.X / 2.0f - m_worldGeneratorButton.Size.X - MARGIN_RIGHT : 0.0f;
                m_worldGeneratorButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
                m_worldGeneratorButton.Position = pos2p;

                advanced.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
                if (m_isNewGame)
                {
                    advanced.Position = m_worldGeneratorButton.Position - new Vector2(m_worldGeneratorButton.Size.X + 0.017f, 0);
                }
                else
                {
                    advanced.Position = m_worldGeneratorButton.Position - new Vector2(m_worldGeneratorButton.Size.X + 0.017f, 0.008f);
                }
            }

            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            CloseButtonEnabled = true;
        }

        void m_soundModeCombo_ItemSelected()
        {
            if(m_soundModeCombo.GetSelectedIndex() == (int)MySoundModeEnum.Realistic)
                m_settings.EnableOxygenPressurization = true;//needed for sound selection - otherwise there would be wrong sounds in stations/ships located in space
        }

        void m_nameTextbox_TextChanged(MyGuiControlTextbox obj)
        {
            m_nameRewritten = true;
        }

        private void scenario_SelectedChanged(MyGuiControlRadioButtonGroup group)
        {
            SetDefaultName();

            // If the scenario selected is "Empty World" it will select Safe as the default environment, but only if this setting wasn't changed before 
            if (!m_isHostilityChanged)
            {
                m_environment.SelectItemByKey(0);
                // It will change with the above code to true
                m_isHostilityChanged = false;
            }

            if (MyFakes.ENABLE_PLANETS)
            {
                m_worldGeneratorButton.Enabled = true;
                if (m_worldGeneratorButton.Enabled)
                {
                    if (WorldGenerator != null)
                    {
                        WorldGenerator.GetSettings(m_settings);
                        AsteroidAmount = WorldGenerator.AsteroidAmount;
                    }
                    else
                    {
                        //GK: If AsteroidAmount==0 it will cause no Asteroids spawned near player in Asteroids Scenario
                        AsteroidAmount = -1;
                    }
                }
                else if (m_settings != null)
                {
                    AsteroidAmount = 0;
                    m_settings.EnableFlora = true;
                }
            }
            else
            {
                UpdateAsteroidAmountEnabled(true);
            }

            var checkpointPath = group.SelectedButton.UserData as string;
            ulong size;
            var checkpoint = MyLocalCache.LoadCheckpoint(checkpointPath, out size);
            if (checkpoint != null)
            {
                m_settings = CopySettings(checkpoint.Settings);
                SetSettingsToControls();
            }
        }

        private void blockLimits_CheckedChanged(MyGuiControlCheckbox checkbox)
        {
            if (!checkbox.IsChecked)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextBlockLimitDisableWarning),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning));
                MyGuiSandbox.AddScreen(messageBox);
                Settings.MaxBlocksPerPlayer = 0;
                Settings.MaxGridSize = 0;
            }
            else
            {
                Settings.MaxBlocksPerPlayer = 100000;
                Settings.MaxGridSize = 50000;
            }
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        private void SetDefaultName()
        {
            if (!m_nameRewritten && m_scenarioTypesGroup.SelectedButton != null)
            {
                var title = ((MyGuiControlContentButton)(m_scenarioTypesGroup.SelectedButton)).Title;
                m_nameTextbox.Text = title.ToString() + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                m_nameRewritten = false;
            }
        }

        private void LoadValues()
        {
            m_nameTextbox.Text = m_checkpoint.SessionName ?? "";
            m_descriptionTextbox.Text = m_checkpoint.Description ?? "";
            m_settings = CopySettings(m_checkpoint.Settings);
            m_mods = m_checkpoint.Mods;
            m_environment.SelectItemByKey((int)(m_checkpoint.Settings.EnvironmentHostility));
            SetSettingsToControls();
        }

        private void SetDefaultValues()
        {
            m_scenarioTypesGroup.SelectByKey(0);
            m_settings = GetDefaultSettings();
            m_settings.EnableToolShake = true;

            m_settings.EnableFlora = (MyPerGameSettings.Game == GameEnum.SE_GAME) && MyFakes.ENABLE_PLANETS;
            m_settings.EnableSunRotation = MyPerGameSettings.Game == GameEnum.SE_GAME;
            m_settings.VoxelGeneratorVersion = MyVoxelConstants.VOXEL_GENERATOR_VERSION;
            m_settings.EnableOxygen = true;
            m_settings.CargoShipsEnabled = true;
            m_mods = new List<MyObjectBuilder_Checkpoint.ModItem>();
            SetSettingsToControls();
            SetDefaultName();
        }

        protected virtual MyObjectBuilder_SessionSettings GetDefaultSettings()
        {
            return MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_SessionSettings>();
        }

        protected virtual MyObjectBuilder_SessionSettings CopySettings(MyObjectBuilder_SessionSettings source)
        {
            return source.Clone() as MyObjectBuilder_SessionSettings;
        }

        private void OnOnlineModeSelect()
        {
            m_maxPlayersSlider.Enabled = m_onlineMode.GetSelectedKey() != (int)MyOnlineModeEnum.OFFLINE;
            m_maxPlayersLabel.Enabled = m_onlineMode.GetSelectedKey() != (int)MyOnlineModeEnum.OFFLINE;
        }

        private void CheckButton(float value, params MyGuiControlButton[] allButtons)
        {
            bool any = false;
            foreach (var btn in allButtons)
            {
                if (btn.UserData is float)
                {
                    if ((float)btn.UserData == value && !btn.Checked)
                    {
                        any = true;
                        btn.Checked = true;
                    }
                    else if ((float)btn.UserData != value && btn.Checked)
                        btn.Checked = false;
                }
            }

            if (!any)
                allButtons[0].Checked = true;
        }

        private void CheckButton(MyGuiControlButton active, params MyGuiControlButton[] allButtons)
        {
            foreach (var btn in allButtons)
            {
                if (btn == active && !btn.Checked)
                    btn.Checked = true;
                else if (btn != active && btn.Checked)
                    btn.Checked = false;
            }
        }

        private void OnCreativeClick(object sender)
        {
            UpdateSurvivalState(false);
        }

        private void OnSurvivalClick(object sender)
        {
            UpdateSurvivalState(true);
        }

        private void OnAdvancedClick(object sender)
        {
            Advanced = new MyGuiScreenAdvancedWorldSettings(this);
            Advanced.UpdateSurvivalState(GetGameMode() == MyGameModeEnum.Survival);
            Advanced.OnOkButtonClicked += Advanced_OnOkButtonClicked;

            MyGuiSandbox.AddScreen(Advanced);
        }

        private void OnWorldGeneratorClick(object sender)
        {
            WorldGenerator = new MyGuiScreenWorldGeneratorSettings(this);
            WorldGenerator.OnOkButtonClicked += WorldGenerator_OnOkButtonClicked;
            MyGuiSandbox.AddScreen(WorldGenerator);
        }

        void WorldGenerator_OnOkButtonClicked()
        {
            WorldGenerator.GetSettings(m_settings);
            AsteroidAmount = WorldGenerator.AsteroidAmount;
            SetSettingsToControls();
        }


#if !XB1 // XB1_NOWORKSHOP
        private void OnModsClick(object sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenMods(m_mods));
        }
#endif // !XB1

        private void UpdateSurvivalState(bool survivalEnabled)
        {
            m_creativeModeButton.Checked = !survivalEnabled;
            m_survivalModeButton.Checked = survivalEnabled;
        }

        void Advanced_OnOkButtonClicked()
        {
            // Some of this screen's settings could depend on advanced settings
            Advanced.GetSettings(m_settings);
            //AsteroidAmount = Advanced.AsteroidAmount;
            SetSettingsToControls();
        }

        private void OnOkButtonClick(object sender)
        {
            // Validate
            if (m_nameTextbox.Text.Length < MySession.MIN_NAME_LENGTH || m_nameTextbox.Text.Length > MySession.MAX_NAME_LENGTH)
            {
                MyStringId errorType;
                if (m_nameTextbox.Text.Length < MySession.MIN_NAME_LENGTH) errorType = MyCommonTexts.ErrorNameTooShort;
                else errorType = MyCommonTexts.ErrorNameTooLong;
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(errorType),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                messageBox.SkipTransition = true;
                messageBox.InstantClose = false;
                MyGuiSandbox.AddScreen(messageBox);
                return;
            }

            if (m_descriptionTextbox.Text.Length > MySession.MAX_DESCRIPTION_LENGTH)
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.ErrorDescriptionTooLong),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                messageBox.SkipTransition = true;
                messageBox.InstantClose = false;
                MyGuiSandbox.AddScreen(messageBox);
                return;
            }

            if (m_isNewGame)
            {
                CheckDx11AndStart();
            }
            else
            {
                OnOkButtonClickQuestions(0);
            }
        }

        private void OnOkButtonClickQuestions(int skipQuestions)
        {
            if (skipQuestions <= 0)
            {
                // Check whether the inventories might be truncated due to harvesting mode change
                bool changingToSurvival = m_checkpoint.Settings.GameMode == MyGameModeEnum.Creative && GetGameMode() == MyGameModeEnum.Survival;
                bool changingToCreative = m_checkpoint.Settings.GameMode == MyGameModeEnum.Survival && GetGameMode() == MyGameModeEnum.Creative;
                if (changingToSurvival || (!changingToCreative && m_checkpoint.Settings.InventorySizeMultiplier > m_settings.InventorySizeMultiplier))
                {
                    var messageBox = MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: MyTexts.Get(MyCommonTexts.HarvestingWarningInventoryMightBeTruncatedAreYouSure),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning),
                        callback: (x) => OnOkButtonClickAnswer(x, 1));
                    messageBox.SkipTransition = true;
                    messageBox.InstantClose = false;
                    MyGuiSandbox.AddScreen(messageBox);
                    return;
                }
            }
            if (skipQuestions <= 1)
            {
                bool loweringWorldSize = (m_checkpoint.Settings.WorldSizeKm == 0 || m_checkpoint.Settings.WorldSizeKm > m_settings.WorldSizeKm) && m_settings.WorldSizeKm != 0;
                if (loweringWorldSize)
                {
                    var messageBox = MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: MyTexts.Get(MySpaceTexts.WorldSettings_WarningChangingWorldSize),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning),
                        callback: (x) => OnOkButtonClickAnswer(x, 2));
                    messageBox.SkipTransition = true;
                    messageBox.InstantClose = false;
                    MyGuiSandbox.AddScreen(messageBox);
                    return;
                }
            }

            ChangeWorldSettings();
        }

        private void OnOkButtonClickAnswer(MyGuiScreenMessageBox.ResultEnum answer, int skipQuestions)
        {
            if (answer == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                OnOkButtonClickQuestions(skipQuestions);
            }
        }

        private MyGameModeEnum GetGameMode()
        {
            return m_survivalModeButton.Checked ? MyGameModeEnum.Survival : MyGameModeEnum.Creative;
        }

        protected virtual void GetSettingsFromControls()
        {
            m_settings.OnlineMode = (MyOnlineModeEnum)m_onlineMode.GetSelectedKey();
            if (m_checkpoint != null)
                m_checkpoint.PreviousEnvironmentHostility = m_settings.EnvironmentHostility;

            m_settings.EnvironmentHostility = (MyEnvironmentHostilityEnum)m_environment.GetSelectedKey();
            m_settings.MaxPlayers = (short)m_maxPlayersSlider.Value;
            m_settings.AutoSaveInMinutes = m_autoSave.IsChecked ? MyObjectBuilder_SessionSettings.DEFAULT_AUTOSAVE_IN_MINUTES : 0;
            m_settings.GameMode = GetGameMode();
            m_settings.RealisticSound = ((MySoundModeEnum)m_soundModeCombo.GetSelectedKey() == MySoundModeEnum.Realistic);
            m_settings.ScenarioEditMode = false;
            m_settings.EnableBlockLimits = m_blockLimits.IsChecked;
        }

        protected virtual void SetSettingsToControls()
        {
            m_onlineMode.SelectItemByKey((int)m_settings.OnlineMode);
            
            m_maxPlayersSlider.Value = m_settings.MaxPlayers;
            m_autoSave.IsChecked = m_settings.AutoSaveInMinutes > 0;

            UpdateSurvivalState(m_settings.GameMode == MyGameModeEnum.Survival);
            m_soundModeCombo.SelectItemByKey(m_settings.RealisticSound ? (int)MySoundModeEnum.Realistic : (int)MySoundModeEnum.Arcade);
            m_blockLimits.IsChecked = m_settings.EnableBlockLimits;
        }

        private string GetPassword()
        {
            if (Advanced != null && Advanced.IsConfirmed)
                return Advanced.Password;

            return m_checkpoint == null ? "" : m_checkpoint.Password;
        }

        private string GetDescription()
        {
            return m_checkpoint == null ? m_descriptionTextbox.Text : m_checkpoint.Description;
        }

        private void ChangeWorldSettings()
        {
            // Confirm dialog?
            // Scenario should not be changed from edit settings
            m_checkpoint.SessionName = m_nameTextbox.Text;
            m_checkpoint.Description = m_descriptionTextbox.Text;
            GetSettingsFromControls();
            m_checkpoint.Settings = m_settings;
            m_checkpoint.Mods = m_mods;

            MyLocalCache.SaveCheckpoint(m_checkpoint, m_sessionPath);

            if (MySession.Static != null && MySession.Static.Name == m_checkpoint.SessionName && m_sessionPath == MySession.Static.CurrentPath)
            {
                var session = MySession.Static;
                session.Password = GetPassword();
                session.Description = GetDescription();
                session.Settings = m_checkpoint.Settings;
                session.Mods = m_checkpoint.Mods;
            }
            CloseScreen();
        }

        private void OnCancelButtonClick(object sender)
        {
            CloseScreen();
        }

        private void CheckDx11AndStart()
        {
            if (MySandboxGame.IsDirectX11)
            {
                StartNewSandbox();
            }
            else if (MyDirectXHelper.IsDx11Supported())
            {
                // Has DX11, ask for switch or selecting different scenario
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    callback: OnSwitchAnswer,
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.QuickstartDX11SwitchQuestion),
                    buttonType: MyMessageBoxButtonsType.YES_NO));
            }
            else
            {
                // No DX11, ask for selecting another scenario
                var text = MyTexts.Get(MySpaceTexts.QuickstartNoDx9SelectDifferent);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(mb);
            }
        }

        void OnSwitchAnswer(MyGuiScreenMessageBox.ResultEnum result)
        {
            if(result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                MySandboxGame.Config.GraphicsRenderer = MySandboxGame.DirectX11RendererKey;
                MySandboxGame.Config.Save();
                MyGuiSandbox.BackToMainMenu();
                var text = MyTexts.Get(MySpaceTexts.QuickstartDX11PleaseRestartGame);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(mb);
            }
            else
            {
                var text = MyTexts.Get(MySpaceTexts.QuickstartSelectDifferent);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(mb);
            }
        }
        private void StartNewSandbox()
        {
            MyLog.Default.WriteLine("StartNewSandbox - Start");

            GetSettingsFromControls();

            // Load the checkpoint
            ulong checkpointSizeInBytes;
            var sesionPath = m_scenarioTypesGroup.SelectedButton.UserData as string;

            var checkpoint = MyLocalCache.LoadCheckpoint(sesionPath, out checkpointSizeInBytes);

            if(checkpoint == null) return;

            GetSettingsFromControls();
            checkpoint.Settings = m_settings;
            checkpoint.SessionName = m_nameTextbox.Text;
            checkpoint.Password = GetPassword();
            checkpoint.Description = GetDescription();
            checkpoint.Mods = m_mods;

            SetupWorldGeneratorSettings(checkpoint);

            MySessionLoader.LoadSingleplayerSession(
                checkpoint,
                sesionPath,
                checkpointSizeInBytes,
                () =>
                {
                    MySession.Static.Save(Path.Combine(MyFileSystem.SavesPath, checkpoint.SessionName.Replace(':','-')));
                });
        }

        private void SetupWorldGeneratorSettings(MyObjectBuilder_Checkpoint checkpoint)
        {
            switch ((MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum)AsteroidAmount)
            {
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralLow:
                    checkpoint.Settings.ProceduralDensity = 0.25f;
                    break;
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNormal:
                    checkpoint.Settings.ProceduralDensity = 0.35f;
                    break;
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralHigh:
                    checkpoint.Settings.ProceduralDensity = 0.50f;
                    break;
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNone:
                    checkpoint.Settings.ProceduralDensity = 0.0f;
                    break;
                default:
                    throw new InvalidBranchException();
                    break;
            }
        }

        public void UpdateAsteroidAmountEnabled(bool enabled)
        {
            m_asteroidAmountCombo.ClearItems();

            if (enabled)
            {
                m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.Normal, MySpaceTexts.WorldSettings_AsteroidAmountNormal);
                m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.More, MySpaceTexts.WorldSettings_AsteroidAmountLarge);
#if XB1
                m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.Many, MySpaceTexts.WorldSettings_AsteroidAmountExtreme);
#else // !XB1
                if (Environment.Is64BitProcess)
                    m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.Many, MySpaceTexts.WorldSettings_AsteroidAmountExtreme);
#endif // !XB1

                if (MyFakes.ENABLE_ASTEROID_FIELDS)
                {
                    m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNone, MySpaceTexts.WorldSettings_AsteroidAmountProceduralNone);
                    m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralLow, MySpaceTexts.WorldSettings_AsteroidAmountProceduralLow);
                    m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNormal, MySpaceTexts.WorldSettings_AsteroidAmountProceduralNormal);
#if XB1
                    m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralHigh, MySpaceTexts.WorldSettings_AsteroidAmountProceduralHigh);
#else // !XB1
                    if (Environment.Is64BitProcess)
                        m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralHigh, MySpaceTexts.WorldSettings_AsteroidAmountProceduralHigh);
#endif // !XB1
                }

            }
            else
            {
                m_asteroidAmountCombo.AddItem((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.None, MySpaceTexts.WorldSettings_AsteroidAmountNone);
            }

            // Try to preserve selection, but if not possible, select the first value
            if (m_asteroidAmountCombo.TryGetItemByKey(AsteroidAmount) == null)
            {
                if (enabled)
                    m_asteroidAmountCombo.SelectItemByKey((int)MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralLow);
                else
                    m_asteroidAmountCombo.SelectItemByIndex(0);
            }
            else
            {
                m_asteroidAmountCombo.SelectItemByKey(AsteroidAmount);
            }

            m_asteroidAmountCombo.Enabled = enabled;
            m_asteroidAmountLabel.Enabled = enabled;
        }

        void m_asteroidAmountCombo_ItemSelected()
        {
            m_asteroidAmount = (int)m_asteroidAmountCombo.GetSelectedKey();
        }

        void HostilityChanged()
        {
            m_isHostilityChanged = true;

            if ((MyEnvironmentHostilityEnum)m_environment.GetSelectedKey() == MyEnvironmentHostilityEnum.SAFE)
            {
                if (m_settings.EnableSpiders.HasValue == false)
                {
                    m_settings.EnableSpiders = false;
                }

                if (m_settings.EnableSpiders.HasValue == false)
                {
                    m_settings.EnableSpiders = false;
                }
            }
        }

        private void RefreshCustomWorldsList()
        {
            // Add loading mini screen
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, StartLoadingWorldInfos, OnLoadingFinished));
        }

        // Starts Async loading.
        private IMyAsyncResult StartLoadingWorldInfos()
        {
            var customWorldsPath = @"CustomWorlds";
            var customWorldsAbsolutePath = Path.Combine(MyFileSystem.ContentPath, customWorldsPath);
            return new MyLoadWorldInfoListResult(customWorldsAbsolutePath);
        }

        // Checks for corrupted worlds and refreshes the table cells.
        private void OnLoadingFinished(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadListRes = (MyLoadListResult)result;

            m_scenarioTypesGroup.Clear();
            m_scenarioTypesList.Clear();

            foreach (var saveTuple in loadListRes.AvailableSaves)
            {
                var button = new MyGuiControlContentButton(saveTuple.Item2.SessionName,
                    Path.Combine(saveTuple.Item1, "thumb.jpg")) {UserData = saveTuple.Item1};

                m_scenarioTypesGroup.Add(button);
                m_scenarioTypesList.Controls.Add(button);
            }

            SetDefaultValues();

            // Close the loading miniscreen
            screen.CloseScreen();
        }
    }
}