#region Using

using System.Collections.Generic;
using VRageMath;

using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using Sandbox.Common.ObjectBuilders;
using System.Text;
using Sandbox.Definitions;

using Sandbox.Common.ObjectBuilders.Gui;

using Sandbox.Game.World;
using Sandbox.Engine.Networking;
using Sandbox.Game.Gui;
using System;
using Sandbox.Common;
using VRage;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using System.Diagnostics;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage.Voxels;
using VRage.Library.Utils;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenWorldSettings : MyGuiScreenBase
    {
        public enum AsteroidAmountEnum
        {
            None = 0,
            Normal = 4,
            More = 7,
            Many = 16,
            ProceduralLow = -1,
            ProceduralNormal = -2,
            ProceduralHigh = -3,
        }

        public static MyGuiScreenWorldSettings Static;
        internal MyGuiScreenAdvancedWorldSettings Advanced;
        internal MyGuiScreenMods ModsScreen;

        bool m_nameRewritten;
        protected bool m_isNewGame;
        string m_sessionPath;

        protected MyObjectBuilder_SessionSettings m_settings;
        public MyObjectBuilder_SessionSettings Settings
        {
            get
            {
                GetSettingsFromControls();
                return m_settings;
            }
        }

        MyGuiControlCheckbox m_scenarioEditMode;

        private List<MyObjectBuilder_Checkpoint.ModItem> m_mods;

        /// Saved values of advanced screen that cannot be saved to MySessionSettings
        /// CH:TODO: If there are more such settings later, consider creting a separate struct for them

        /*private int AsteroidAmount
        {
            get
            {
                return m_asteroidAmount.HasValue ? m_asteroidAmount.Value : 4;
            }
            set
            {
                m_asteroidAmount = value;
            }
        }*/

        private int? m_asteroidAmount;
        public int AsteroidAmount
        {
            get
            {
                return m_asteroidAmount.HasValue ? m_asteroidAmount.Value : (int)AsteroidAmountEnum.ProceduralLow;
            }
            set
            {
                m_asteroidAmount = value;
                switch (value)
                {
                    case (int)AsteroidAmountEnum.None:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.None);
                        return;
                    case (int)AsteroidAmountEnum.Normal:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.Normal);
                        return;
                    case (int)AsteroidAmountEnum.More:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.More);
                        return;
                    case (int)AsteroidAmountEnum.Many:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.Many);
                        return;
                    case (int)AsteroidAmountEnum.ProceduralLow:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.ProceduralLow);
                        return;
                    case (int)AsteroidAmountEnum.ProceduralNormal:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.ProceduralNormal);
                        return;
                    case (int)AsteroidAmountEnum.ProceduralHigh:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.ProceduralHigh);
                        return;
                    default:
                        Debug.Assert(false, "Unhandled value in AsteroidAmountEnum");
                        return;
                }
            }
        }

        MyObjectBuilder_Checkpoint m_checkpoint;
        public MyObjectBuilder_Checkpoint Checkpoint
        {
            get { return m_checkpoint; }
        }

        MyGuiControlTextbox m_nameTextbox, m_descriptionTextbox;
        MyGuiControlCombobox m_onlineMode, m_environment, m_asteroidAmountCombo;
        MyGuiControlButton m_okButton, m_cancelButton, m_survivalModeButton, m_creativeModeButton;
        MyGuiControlSlider m_maxPlayersSlider;
        MyGuiControlLabel m_maxPlayersLabel, m_asteroidAmountLabel;
        MyGuiControlCheckbox m_autoSave;

        MyGuiControlList m_scenarioTypesList;
        MyGuiControlRadioButtonGroup m_scenarioTypesGroup;

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

            MySandboxGame.Log.WriteLine("MyGuiScreenWorldSettings.ctor END");
        }

        public static Vector2 CalcSize(MyObjectBuilder_Checkpoint checkpoint)
        {
            float width = checkpoint == null ? 0.9f : 0.65f;
            float height = checkpoint == null ? 1.24f : 1.00f;
            if (checkpoint != null)
                height -= 0.05f;
            if (MyFakes.OCTOBER_RELEASE_HIDE_WORLD_PARAMS)
                height -= 0.27f;

            return new Vector2(width, height);
        }

        public override bool CloseScreen()
        {
            if (Advanced != null)
                Advanced.CloseScreen();
            Advanced = null;
            if (ModsScreen != null)
                ModsScreen.CloseScreen();
            ModsScreen = null;
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
                SetDefaultValues();
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
            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.23f, 0.03f);

            if (m_isNewGame)
                AddCaption(MySpaceTexts.ScreenCaptionCustomWorld);
            else
                AddCaption(MySpaceTexts.ScreenCaptionEditSettings);

            int numControls = 0;

            var nameLabel = MakeLabel(MySpaceTexts.Name);
            var descriptionLabel = MakeLabel(MySpaceTexts.Description);
            var gameModeLabel = MakeLabel(MySpaceTexts.WorldSettings_GameMode);
            var onlineModeLabel = MakeLabel(MySpaceTexts.WorldSettings_OnlineMode);
            m_maxPlayersLabel = MakeLabel(MySpaceTexts.MaxPlayers);
            var environmentLabel = MakeLabel(MySpaceTexts.WorldSettings_EnvironmentHostility);
            var scenarioLabel = MakeLabel(MySpaceTexts.WorldSettings_Scenario);

            float width = 0.284375f + 0.025f;

            m_nameTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_NAME_LENGTH);
            m_descriptionTextbox = new MyGuiControlTextbox(maxLength: MySession.MAX_DESCRIPTION_LENGTH);
            m_onlineMode = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_environment = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
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

            m_asteroidAmountLabel = MakeLabel(MySpaceTexts.Asteroid_Amount);
            m_asteroidAmountCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));

            m_asteroidAmountCombo.ItemSelected += m_asteroidAmountCombo_ItemSelected;

            // Ok/Cancel
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OnOkButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: OnCancelButtonClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

            m_creativeModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_GameModeCreative), onButtonClick: OnCreativeClick);
            m_creativeModeButton.SetToolTip(MySpaceTexts.ToolTipWorldSettingsModeCreative);
            m_survivalModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_GameModeSurvival), onButtonClick: OnSurvivalClick);
            m_survivalModeButton.SetToolTip(MySpaceTexts.ToolTipWorldSettingsModeSurvival);

            m_onlineMode.ItemSelected += OnOnlineModeSelect;
            m_onlineMode.AddItem((int)MyOnlineModeEnum.OFFLINE, MySpaceTexts.WorldSettings_OnlineModeOffline);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PRIVATE, MySpaceTexts.WorldSettings_OnlineModePrivate);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.FRIENDS, MySpaceTexts.WorldSettings_OnlineModeFriends);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PUBLIC, MySpaceTexts.WorldSettings_OnlineModePublic);

            m_environment.AddItem((int)MyEnvironmentHostilityEnum.SAFE, MySpaceTexts.WorldSettings_EnvironmentHostilitySafe);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.NORMAL, MySpaceTexts.WorldSettings_EnvironmentHostilityNormal);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.CATACLYSM, MySpaceTexts.WorldSettings_EnvironmentHostilityCataclysm);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.CATACLYSM_UNREAL, MySpaceTexts.WorldSettings_EnvironmentHostilityCataclysmUnreal);

            if (m_isNewGame)
            {
                m_scenarioTypesGroup = new MyGuiControlRadioButtonGroup();
                m_scenarioTypesGroup.SelectedChanged += scenario_SelectedChanged;
                foreach (var scenario in MyDefinitionManager.Static.GetScenarioDefinitions())
                {
                    if (!scenario.Public && !MyFakes.ENABLE_NON_PUBLIC_BLOCKS)
                        continue;

                    var button = new MyGuiControlScenarioButton(scenario);
                    m_scenarioTypesGroup.Add(button);
                    m_scenarioTypesList.Controls.Add(button);
                }
            }

            m_nameTextbox.SetToolTip(string.Format(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsName), MySession.MIN_NAME_LENGTH, MySession.MAX_NAME_LENGTH));
            m_descriptionTextbox.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsDescription));
            m_environment.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsEnvironment));
            m_onlineMode.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsOnlineMode));
            m_maxPlayersSlider.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsMaxPlayer));
            m_asteroidAmountCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsAsteroidAmount));

            m_nameTextbox.TextChanged += m_nameTextbox_TextChanged;

            var advanced = new MyGuiControlButton(highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Advanced), onButtonClick: OnAdvancedClick);

            var mods = new MyGuiControlButton(highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Mods), onButtonClick: OnModsClick);

            // Add controls in pairs; label first, control second. They will be laid out automatically this way.
            Controls.Add(nameLabel);
            Controls.Add(m_nameTextbox);
            Controls.Add(descriptionLabel);
            Controls.Add(m_descriptionTextbox);

            Controls.Add(gameModeLabel);
            Controls.Add(m_creativeModeButton);

            Controls.Add(onlineModeLabel);
            Controls.Add(m_onlineMode);
            Controls.Add(m_maxPlayersLabel);
            Controls.Add(m_maxPlayersSlider);

            if (MyFakes.ENABLE_METEOR_SHOWERS)
            {
                Controls.Add(environmentLabel);
                Controls.Add(m_environment);
            }

            if (m_isNewGame)
            {
                Controls.Add(m_asteroidAmountLabel);
                Controls.Add(m_asteroidAmountCombo);
            }

            var autoSaveLabel = MakeLabel(MySpaceTexts.WorldSettings_AutoSave);
            m_autoSave = new MyGuiControlCheckbox();
            m_autoSave.SetToolTip(new StringBuilder().AppendFormat(MySpaceTexts.ToolTipWorldSettingsAutoSave, MyObjectBuilder_SessionSettings.DEFAULT_AUTOSAVE_IN_MINUTES).ToString());
            Controls.Add(autoSaveLabel);
            Controls.Add(m_autoSave);

            var scenarioEditModeLabel = MakeLabel(MySpaceTexts.WorldSettings_ScenarioEditMode);
            m_scenarioEditMode = new MyGuiControlCheckbox();
            m_scenarioEditMode.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_ScenarioEditMode));
            Controls.Add(scenarioEditModeLabel);
            Controls.Add(m_scenarioEditMode);

            if (MyFakes.ENABLE_WORKSHOP_MODS)
                Controls.Add(mods);

            Controls.Add(advanced);

            float labelSize = 0.20f;

            float MARGIN_TOP = 0.12f;
            float MARGIN_BOTTOM = 0.12f;
            float MARGIN_LEFT = m_isNewGame ? 0.315f : 0.08f;
            float MARGIN_RIGHT = m_isNewGame ? 0.075f : 0.045f;

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
            
            Controls.Add(m_survivalModeButton);
            m_survivalModeButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            m_survivalModeButton.Position = m_creativeModeButton.Position + new Vector2(m_onlineMode.Size.X, 0);

            if (m_isNewGame)
            {
                Vector2 scenarioPosition = new Vector2(-0.375f, nameLabel.Position.Y);

                m_nameTextbox.Size = m_onlineMode.Size;
                m_descriptionTextbox.Size = m_nameTextbox.Size;

                scenarioLabel.Position = scenarioPosition;

                m_scenarioTypesList.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                m_scenarioTypesList.Position = scenarioLabel.Position + new Vector2(0, 0.02f);
                m_scenarioTypesList.Size = new Vector2(0.19f, m_size.Value.Y - MARGIN_BOTTOM - MARGIN_TOP);
                Controls.Add(scenarioLabel);
                Controls.Add(m_scenarioTypesList);

                MyGuiControlSeparatorList m_verticalLine = new MyGuiControlSeparatorList();
                Vector2 position = nameLabel.Position + new Vector2(-0.025f, -0.02f);
                m_verticalLine.AddVertical(position, m_size.Value.Y - MARGIN_BOTTOM - MARGIN_TOP + 0.04f);
                Controls.Add(m_verticalLine);
            }

            var pos2 = advanced.Position;
            //pos2.X = m_isNewGame ? 0.160f : 0.0f;
            pos2.X = Size.HasValue ? Size.Value.X / 2.0f - advanced.Size.X - MARGIN_RIGHT : 0.0f;
            advanced.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            advanced.Position = pos2;

            mods.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            mods.Position = advanced.Position - new Vector2(advanced.Size.X + 0.017f, 0);

            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            CloseButtonEnabled = true;
        }

        void m_nameTextbox_TextChanged(MyGuiControlTextbox obj)
        {
            m_nameRewritten = true;
        }

        private void scenario_SelectedChanged(MyGuiControlRadioButtonGroup group)
        {
            SetDefaultName();
            UpdateAsteroidAmountEnabled((m_scenarioTypesGroup.SelectedButton as MyGuiControlScenarioButton).Scenario.AsteroidClustersEnabled);
        }

        void m_asteroidAmountCombo_ItemSelected()
        {
            m_asteroidAmount = (int)m_asteroidAmountCombo.GetSelectedKey();
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        private void SetDefaultName()
        {
            if (!m_nameRewritten)
            {
                var title = ((MyGuiControlScenarioButton)(m_scenarioTypesGroup.SelectedButton)).Title;
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
            m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.Normal);
            SetSettingsToControls();
        }

        private void SetDefaultValues()
        {
            m_scenarioTypesGroup.SelectByKey(0);
            m_settings = GetDefaultSettings();
            m_settings.EnableToolShake = true;
            m_settings.EnablePlanets = MyFakes.ENABLE_PLANETS;
            m_settings.EnableStationVoxelSupport = true;
            m_settings.EnableSunRotation = true;
            m_settings.VoxelGeneratorVersion = MyVoxelConstants.VOXEL_GENERATOR_VERSION;
            m_settings.EnableOxygen = true;
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

        private void OnModsClick(object sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenMods(m_mods));
        }

        private void UpdateSurvivalState(bool survivalEnabled)
        {
            m_creativeModeButton.Checked = !survivalEnabled;
            m_survivalModeButton.Checked = survivalEnabled;
        }

        public void UpdateAsteroidAmountEnabled(bool enabled)
        {
            m_asteroidAmountCombo.ClearItems();

            if (enabled)
            {
                m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.Normal, MySpaceTexts.WorldSettings_AsteroidAmountNormal);
                m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.More, MySpaceTexts.WorldSettings_AsteroidAmountLarge);
                if (Environment.Is64BitProcess)
                    m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.Many, MySpaceTexts.WorldSettings_AsteroidAmountExtreme);

                if (MyFakes.ENABLE_ASTEROID_FIELDS)
                {
                    m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.ProceduralLow, MySpaceTexts.WorldSettings_AsteroidAmountProceduralLow);
                    m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.ProceduralNormal, MySpaceTexts.WorldSettings_AsteroidAmountProceduralNormal);
                    if (Environment.Is64BitProcess)
                        m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.ProceduralHigh, MySpaceTexts.WorldSettings_AsteroidAmountProceduralHigh);
                }
            }
            else
            {
                m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.None, MySpaceTexts.WorldSettings_AsteroidAmountNone);
            }

            // Try to preserve selection, but if not possible, select the first value
            if (m_asteroidAmountCombo.TryGetItemByKey(AsteroidAmount) == null)
            {
                if (enabled)
                    m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.ProceduralLow);
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

            if (m_isNewGame)
                StartNewSandbox();
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
                        messageText: MyTexts.Get(MySpaceTexts.HarvestingWarningInventoryMightBeTruncatedAreYouSure),
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionWarning),
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
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionWarning),
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
            m_settings.GameMode   = GetGameMode();
            m_settings.ScenarioEditMode = m_scenarioEditMode.IsChecked;
        }

        protected virtual void SetSettingsToControls()
        {
            m_onlineMode.SelectItemByKey((int)m_settings.OnlineMode);
            m_environment.SelectItemByKey((int)m_settings.EnvironmentHostility);

            m_maxPlayersSlider.Value = m_settings.MaxPlayers;
            m_autoSave.IsChecked     = m_settings.AutoSaveInMinutes > 0;

            UpdateSurvivalState(m_settings.GameMode == MyGameModeEnum.Survival);
            m_scenarioEditMode.IsChecked = m_settings.ScenarioEditMode;
        }

        private string GetPassword()
        {
            if (Advanced != null && Advanced.IsConfirmed)
                return Advanced.Password;

            return m_checkpoint == null ? "" : m_checkpoint.Password;
        }

        private string GetDescription()
        {
            return m_checkpoint == null ? "" : m_checkpoint.Description;
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

        private void StartNewSandbox()
        {
            MyLog.Default.WriteLine("StartNewSandbox - Start");
            
            GetSettingsFromControls();
            if (!MySteamWorkshop.CheckLocalModsAllowed(m_mods, m_settings.OnlineMode == MyOnlineModeEnum.OFFLINE))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            MySteamWorkshop.DownloadModsAsync(m_mods, delegate(bool success)
            {
                if (success || (m_settings.OnlineMode == MyOnlineModeEnum.OFFLINE) && MySteamWorkshop.CanRunOffline(m_mods))
                {
                    MyScreenManager.RemoveAllScreensExcept(null);

                    if (AsteroidAmount < 0)
                    {
                        MyWorldGenerator.SetProceduralSettings(m_asteroidAmount, m_settings);
                        m_asteroidAmount = 0;
                    }

                    MyGuiScreenGamePlay.StartLoading(delegate
                    {
                        MySession.Start(
                            m_nameTextbox.Text,
                            GetDescription(),
                            GetPassword(),
                            m_settings,
                            m_mods,
                            new MyWorldGenerator.Args()
                            {
                                AsteroidAmount = this.AsteroidAmount,
                                Scenario = (m_scenarioTypesGroup.SelectedButton as MyGuiControlScenarioButton).Scenario
                            }
                        );
                    });
                }
                else
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                        messageText: MyTexts.Get(MySpaceTexts.DialogTextDownloadModsFailed),
                        buttonType: MyMessageBoxButtonsType.OK));
                }
                MyLog.Default.WriteLine("StartNewSandbox - End");
            });
        }
    }
}