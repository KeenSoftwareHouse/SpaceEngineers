
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenAdvancedWorldSettings : MyGuiScreenBase
    {
        public enum MyWorldSizeEnum
        {
            TEN_KM,
            TWENTY_KM,
            FIFTY_KM,
            HUNDRED_KM,
            UNLIMITED,
            CUSTOM,
        }

        public enum MyViewDistanceEnum
        {
            CUSTOM = 0,
            FIVE_KM = 5000,
            SEVEN_KM = 7000, // old default
            TEN_KM = 10000,
            FIFTEEN_KM = 15000,
            TWENTY_KM = 20000, // default
            THIRTY_KM = 30000,
            FORTY_KM = 40000,
            FIFTY_KM = 50000,
        }

		public enum MyFloraDensityEnum
		{
			LOW = 10,
			MEDIUM = 20,
			HIGH = 30,
		}

        public enum MySoundModeEnum
        {
            Arcade,
            Realistic,
        }

        MyGuiScreenWorldSettings m_parent;
        bool m_isNewGame;

        bool m_isConfirmed;
        bool m_showWarningForOxygen;

        MyGuiControlTextbox m_passwordTextbox;
        MyGuiControlCombobox m_onlineMode, m_environment, m_worldSizeCombo, m_soundModeCombo, m_spawnShipTimeCombo, m_viewDistanceCombo, m_physicsOptionsCombo, m_floraDensityCombo;
        MyGuiControlCheckbox m_autoHealing, m_clientCanSave, m_enableCopyPaste, m_weaponsEnabled, m_showPlayerNamesOnHud, m_thrusterDamage, m_cargoShipsEnabled, m_enableSpectator,
                             m_trashRemoval, m_respawnShipDelete, m_resetOwnership, m_permanentDeath, m_destructibleBlocks, m_enableIngameScripts, m_enableToolShake, m_enableOxygen,
                             m_enable3rdPersonCamera, m_enableEncounters, m_disableRespawnShips, m_scenarioEditMode, m_enableFlora, m_stationVoxelSupport, m_enablePlanets,m_enableSunRotation;

        MyGuiControlButton m_okButton, m_cancelButton, m_survivalModeButton, m_creativeModeButton, m_inventory_x1, m_inventory_x3, m_inventory_x10;
        MyGuiControlButton m_assembler_x1, m_assembler_x3, m_assembler_x10,
                           m_refinery_x1, m_refinery_x3, m_refinery_x10,
                           m_welder_half, m_welder_x1, m_welder_x2, m_welder_x5,
                           m_grinder_half, m_grinder_x1, m_grinder_x2, m_grinder_x5;
        MyGuiControlSlider m_maxPlayersSlider,m_sunRotationIntervalSlider;
        MyGuiControlLabel m_enableCopyPasteLabel, m_maxPlayersLabel, m_maxFloatingObjectsLabel, m_sunRotationPeriod, m_sunRotationPeriodValue;
        MyGuiControlSlider m_maxFloatingObjectsSlider;
        StringBuilder m_tempBuilder = new StringBuilder();
        int m_customWorldSize = 0;
        int m_customViewDistance = 20000;

        const int MIN_DAY_TIME_MINUTES = 1;
        const int MAX_DAY_TIME_MINUTES = 60 * 24;

        public string Password
        {
            get
            {
                return m_passwordTextbox.Text;
            }
        }

        public bool IsConfirmed
        {
            get
            {
                return m_isConfirmed;
            }
        }

        public MyGuiScreenAdvancedWorldSettings(MyGuiScreenWorldSettings parent)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, CalcSize(parent.Checkpoint))
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenAdvancedWorldSettings.ctor START");

            m_parent = parent;
            EnabledBackgroundFade = true;

            m_isNewGame = (parent.Checkpoint == null);
            m_isConfirmed = false;

            RecreateControls(true);

            MySandboxGame.Log.WriteLine("MyGuiScreenAdvancedWorldSettings.ctor END");
        }

        public static Vector2 CalcSize(MyObjectBuilder_Checkpoint checkpoint)
        {
            float width = 0.9f;
            float height = 1.24f;

            if (MyFakes.OCTOBER_RELEASE_HIDE_WORLD_PARAMS)
                height -= 0.27f;

            return new Vector2(width, height);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            BuildControls();

            LoadValues();

            if (m_parent.AsteroidAmount == 0)
            {
                m_enablePlanets.IsChecked = false;
                m_enableFlora.IsChecked = false;
                m_enablePlanets.Enabled = false;
                m_floraDensityCombo.Enabled = false;
                m_enableFlora.Enabled = false;
            }
        }

        public void BuildControls()
        {
            MyGuiControlParent parent = new MyGuiControlParent(size: new Vector2(Size.Value.X - 0.05f, Size.Value.Y+0.15f));
            MyGuiControlScrollablePanel scrollPanel = new MyGuiControlScrollablePanel(parent);
            scrollPanel.ScrollbarVEnabled = true;
            scrollPanel.Size = new Vector2(Size.Value.X - 0.05f, 0.8f);

            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.23f, 0.03f);

            AddCaption(MySpaceTexts.ScreenCaptionAdvancedSettings);

            int numControls = 0;

            var passwordLabel = MakeLabel(MySpaceTexts.WorldSettings_Password);
            var onlineModeLabel = MakeLabel(MySpaceTexts.WorldSettings_OnlineMode);
            m_maxPlayersLabel = MakeLabel(MySpaceTexts.MaxPlayers);
            m_maxFloatingObjectsLabel = MakeLabel(MySpaceTexts.MaxFloatingObjects);
            m_sunRotationPeriod = MakeLabel(MySpaceTexts.SunRotationPeriod);
            m_sunRotationPeriodValue = MakeLabel(MySpaceTexts.SunRotationPeriod); 
            var gameTypeLabel = MakeLabel(MySpaceTexts.WorldSettings_GameMode);
            var environmentLabel = MakeLabel(MySpaceTexts.WorldSettings_EnvironmentHostility);
            var gameStyleLabel = MakeLabel(MySpaceTexts.WorldSettings_GameStyle);
            var scenarioLabel = MakeLabel(MySpaceTexts.WorldSettings_Scenario);
            var autoHealingLabel = MakeLabel(MySpaceTexts.WorldSettings_AutoHealing);
            var thrusterDamageLabel = MakeLabel(MySpaceTexts.WorldSettings_ThrusterDamage);
            var enableSpectatorLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableSpectator);
            var resetOwnershipLabel = MakeLabel(MySpaceTexts.WorldSettings_ResetOwnership);
            var permanentDeathLabel = MakeLabel(MySpaceTexts.WorldSettings_PermanentDeath);
            var destructibleBlocksLabel = MakeLabel(MySpaceTexts.WorldSettings_DestructibleBlocks);
            var enableIngameScriptsLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableIngameScripts);
            var enable3rdPersonCameraLabel = MakeLabel(MySpaceTexts.WorldSettings_Enable3rdPersonCamera);
            var enableEncountersLabel = MakeLabel(MySpaceTexts.WorldSettings_Encounters);
            var enableToolShakeLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableToolShake);
            var shipsEnabledLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableCargoShips);
            var soundInSpaceLabel = MakeLabel(MySpaceTexts.WorldSettings_SoundInSpace);
            var friendlyFireLabel = MakeLabel(MySpaceTexts.WorldSettings_FriendlyFire);
            var clientCanSaveLabel = MakeLabel(MySpaceTexts.WorldSettings_ClientCanSave);
            m_enableCopyPasteLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableCopyPaste);
            var enableWeaponsLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableWeapons);
            var showPlayerNamesOnHudLabel = MakeLabel(MySpaceTexts.WorldSettings_ShowPlayerNamesOnHud);
            var inventorySizeLabel = MakeLabel(MySpaceTexts.WorldSettings_InventorySize);
            var refineryEfficiencyLabel = MakeLabel(MySpaceTexts.WorldSettings_RefinerySpeed);
            var assemblerEfficiencyLabel = MakeLabel(MySpaceTexts.WorldSettings_AssemblerEfficiency);
            var trashRemovalLabel = MakeLabel(MySpaceTexts.WorldSettings_RemoveTrash);
            var oxygenLabel = MakeLabel(MySpaceTexts.World_Settings_EnableOxygen);
            var disableRespawnShipsLabel = MakeLabel(MySpaceTexts.WorldSettings_DisableRespawnShips);
            var respawnShipDeleteLabel = MakeLabel(MySpaceTexts.WorldSettings_RespawnShipDelete);
            var worldSizeLabel = MakeLabel(MySpaceTexts.WorldSettings_LimitWorldSize);
            var weldingSpeedLabel = MakeLabel(MySpaceTexts.WorldSettings_WelderSpeed);
            var grindingSpeedLabel = MakeLabel(MySpaceTexts.WorldSettings_GrinderSpeed);
            var soundModeLabel = MakeLabel(MySpaceTexts.WorldSettings_SoundMode);
            var spawnShipTimeLabel = MakeLabel(MySpaceTexts.WorldSettings_RespawnShipCooldown);
            var viewDistanceLabel = MakeLabel(MySpaceTexts.WorldSettings_ViewDistance);
            var physicsOptionLabel = MakeLabel(MySpaceTexts.WorldSettings_Physics);
			var floraDensityLabel = MakeLabel(MySpaceTexts.WorldSettings_FloraDensity);
			var enableFloraLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableFlora);
			var enableStationVoxelLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableStationVoxel);
            var enablePlanetsLabel = MakeLabel(MySpaceTexts.WorldSettings_EnablePlanets);
            var enableSunRotationLabel = MakeLabel(MySpaceTexts.WorldSettings_EnableSunRotation);

            float width = 0.284375f + 0.025f;

            m_passwordTextbox = new MyGuiControlTextbox(maxLength: 256);
            m_onlineMode = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_environment = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_autoHealing = new MyGuiControlCheckbox();
            m_thrusterDamage = new MyGuiControlCheckbox();
            m_cargoShipsEnabled = new MyGuiControlCheckbox();
            m_enableSpectator = new MyGuiControlCheckbox();
            m_resetOwnership = new MyGuiControlCheckbox();
            m_permanentDeath = new MyGuiControlCheckbox();
            m_destructibleBlocks = new MyGuiControlCheckbox();
            m_enableIngameScripts = new MyGuiControlCheckbox();
            m_enable3rdPersonCamera = new MyGuiControlCheckbox();
            m_enableEncounters = new MyGuiControlCheckbox();
            m_disableRespawnShips = new MyGuiControlCheckbox();
            m_enableToolShake = new MyGuiControlCheckbox();
            m_enableOxygen = new MyGuiControlCheckbox();
            m_enableOxygen.IsCheckedChanged = (x) =>
                {
                    if (m_showWarningForOxygen && x.IsChecked)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    buttonType: MyMessageBoxButtonsType.YES_NO,
                                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextAreYouSureEnableOxygen),
                                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                                    callback: (v) =>
                                    {
                                        if (v == MyGuiScreenMessageBox.ResultEnum.NO)
                                        {
                                            x.IsChecked = false;
                                        }
                                    }));
                    }
                };
            m_clientCanSave = new MyGuiControlCheckbox();
            m_enableCopyPaste = new MyGuiControlCheckbox();
            m_weaponsEnabled = new MyGuiControlCheckbox();
            m_showPlayerNamesOnHud = new MyGuiControlCheckbox();
			m_enableFlora = new MyGuiControlCheckbox();
            m_enablePlanets = new MyGuiControlCheckbox();
            m_enableSunRotation = new MyGuiControlCheckbox();

            m_enableSunRotation.IsCheckedChanged = (control) =>
            {
                m_sunRotationIntervalSlider.Enabled = control.IsChecked;
                m_sunRotationPeriodValue.Visible = control.IsChecked;
            };

			m_enableFlora.IsCheckedChanged = (control) => m_floraDensityCombo.Enabled = control.IsChecked;
            m_enablePlanets.IsCheckedChanged = (control) =>
            {
                m_floraDensityCombo.Enabled = control.IsChecked;
                m_enableFlora.Enabled = control.IsChecked;
            };

           
			m_stationVoxelSupport = new MyGuiControlCheckbox();
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
            m_maxFloatingObjectsSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: m_onlineMode.Size.X,
                minValue: 16,
                maxValue: 1024,
                labelText: new StringBuilder("{0}").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true
                );

            m_trashRemoval = new MyGuiControlCheckbox();
            m_respawnShipDelete = new MyGuiControlCheckbox();
            m_worldSizeCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_soundModeCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_spawnShipTimeCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_viewDistanceCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_physicsOptionsCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
			m_floraDensityCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));

            // Ok/Cancel
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: CancelButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

            m_creativeModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_GameModeCreative), onButtonClick: CreativeClicked);
            m_creativeModeButton.SetToolTip(MySpaceTexts.ToolTipWorldSettingsModeCreative);
            m_survivalModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_GameModeSurvival), onButtonClick: SurvivalClicked);
            m_survivalModeButton.SetToolTip(MySpaceTexts.ToolTipWorldSettingsModeSurvival);

            m_inventory_x1 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic), onButtonClick: OnInventoryClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_inventory_x3 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x3), onButtonClick: OnInventoryClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_inventory_x10 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x10), onButtonClick: OnInventoryClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_inventory_x1.UserData = 1.0f;
            m_inventory_x3.UserData = 3.0f;
            m_inventory_x10.UserData = 10.0f;
            m_inventory_x1.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Inventory_x1);
            m_inventory_x3.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Inventory_x3);
            m_inventory_x10.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Inventory_x10);

            m_assembler_x1 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic), onButtonClick: OnAssemblerClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_assembler_x3 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x3), onButtonClick: OnAssemblerClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_assembler_x10 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x10), onButtonClick: OnAssemblerClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_assembler_x1.UserData = 1.0f;
            m_assembler_x3.UserData = 3.0f;
            m_assembler_x10.UserData = 10.0f;
            m_assembler_x1.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Assembler_x1);
            m_assembler_x3.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Assembler_x3);
            m_assembler_x10.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Assembler_x10);

            m_refinery_x1 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic), onButtonClick: OnRefineryClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_refinery_x3 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x3), onButtonClick: OnRefineryClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_refinery_x10 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x10), onButtonClick: OnRefineryClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_refinery_x1.UserData = 1.0f;
            m_refinery_x3.UserData = 3.0f;
            m_refinery_x10.UserData = 10.0f;
            m_refinery_x1.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Refinery_x1);
            m_refinery_x3.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Refinery_x3);
            m_refinery_x10.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Refinery_x10);

            m_welder_half = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_half), onButtonClick: OnWelderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, textScale: MyGuiConstants.HUD_TEXT_SCALE);
            m_welder_x1 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic), onButtonClick: OnWelderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_welder_x2 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x2), onButtonClick: OnWelderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_welder_x5 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x5), onButtonClick: OnWelderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_welder_half.UserData = 0.5f;
            m_welder_x1.UserData = 1.0f;
            m_welder_x2.UserData = 2.0f;
            m_welder_x5.UserData = 5.0f;
            m_welder_half.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Welder_half);
            m_welder_x1.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Welder_x1);
            m_welder_x2.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Welder_x2);
            m_welder_x5.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Welder_x5);

            m_grinder_half = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_half), onButtonClick: OnGrinderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, textScale: MyGuiConstants.HUD_TEXT_SCALE);
            m_grinder_x1 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic), onButtonClick: OnGrinderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_grinder_x2 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x2), onButtonClick: OnGrinderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_grinder_x5 = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Tiny, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE, text: MyTexts.Get(MySpaceTexts.WorldSettings_Realistic_x5), onButtonClick: OnGrinderClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_grinder_half.UserData = 0.5f;
            m_grinder_x1.UserData = 1.0f;
            m_grinder_x2.UserData = 2.0f;
            m_grinder_x5.UserData = 5.0f;
            m_grinder_half.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Grinder_half);
            m_grinder_x1.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Grinder_x1);
            m_grinder_x2.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Grinder_x2);
            m_grinder_x5.SetToolTip(MySpaceTexts.ToolTipWorldSettings_Grinder_x5);

            m_onlineMode.AddItem((int)MyOnlineModeEnum.OFFLINE, MySpaceTexts.WorldSettings_OnlineModeOffline);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PRIVATE, MySpaceTexts.WorldSettings_OnlineModePrivate);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.FRIENDS, MySpaceTexts.WorldSettings_OnlineModeFriends);
            m_onlineMode.AddItem((int)MyOnlineModeEnum.PUBLIC, MySpaceTexts.WorldSettings_OnlineModePublic);

            m_environment.AddItem((int)MyEnvironmentHostilityEnum.SAFE, MySpaceTexts.WorldSettings_EnvironmentHostilitySafe);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.NORMAL, MySpaceTexts.WorldSettings_EnvironmentHostilityNormal);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.CATACLYSM, MySpaceTexts.WorldSettings_EnvironmentHostilityCataclysm);
            m_environment.AddItem((int)MyEnvironmentHostilityEnum.CATACLYSM_UNREAL, MySpaceTexts.WorldSettings_EnvironmentHostilityCataclysmUnreal);

            m_worldSizeCombo.AddItem((int)MyWorldSizeEnum.TEN_KM, MySpaceTexts.WorldSettings_WorldSize10Km);
            m_worldSizeCombo.AddItem((int)MyWorldSizeEnum.TWENTY_KM, MySpaceTexts.WorldSettings_WorldSize20Km);
            m_worldSizeCombo.AddItem((int)MyWorldSizeEnum.FIFTY_KM, MySpaceTexts.WorldSettings_WorldSize50Km);
            m_worldSizeCombo.AddItem((int)MyWorldSizeEnum.HUNDRED_KM, MySpaceTexts.WorldSettings_WorldSize100Km);
            m_worldSizeCombo.AddItem((int)MyWorldSizeEnum.UNLIMITED, MySpaceTexts.WorldSettings_WorldSizeUnlimited);

            m_soundModeCombo.AddItem((int)MySoundModeEnum.Arcade, MySpaceTexts.WorldSettings_ArcadeSound);
            m_soundModeCombo.AddItem((int)MySoundModeEnum.Realistic, MySpaceTexts.WorldSettings_RealisticSound);

            // Keys will be 10x the represented value (to be able to set them as integers)
            m_spawnShipTimeCombo.AddItem((int)0, MySpaceTexts.WorldSettings_RespawnShip_CooldownsDisabled);
            m_spawnShipTimeCombo.AddItem((int)1, MySpaceTexts.WorldSettings_RespawnShip_x01);
            m_spawnShipTimeCombo.AddItem((int)2, MySpaceTexts.WorldSettings_RespawnShip_x02);
            m_spawnShipTimeCombo.AddItem((int)5, MySpaceTexts.WorldSettings_RespawnShip_x05);
            m_spawnShipTimeCombo.AddItem((int)10, MySpaceTexts.WorldSettings_RespawnShip_Default);
            m_spawnShipTimeCombo.AddItem((int)20, MySpaceTexts.WorldSettings_RespawnShip_x2);
            m_spawnShipTimeCombo.AddItem((int)50, MySpaceTexts.WorldSettings_RespawnShip_x5);
            m_spawnShipTimeCombo.AddItem((int)100, MySpaceTexts.WorldSettings_RespawnShip_x10);
            m_spawnShipTimeCombo.AddItem((int)200, MySpaceTexts.WorldSettings_RespawnShip_x20);
            m_spawnShipTimeCombo.AddItem((int)500, MySpaceTexts.WorldSettings_RespawnShip_x50);
            m_spawnShipTimeCombo.AddItem((int)1000, MySpaceTexts.WorldSettings_RespawnShip_x100);

            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.FIVE_KM, MySpaceTexts.WorldSettings_ViewDistance_5_Km);
            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.SEVEN_KM, MySpaceTexts.WorldSettings_ViewDistance_7_Km);
            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.TEN_KM, MySpaceTexts.WorldSettings_ViewDistance_10_Km);
            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.FIFTEEN_KM, MySpaceTexts.WorldSettings_ViewDistance_15_Km);
            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.TWENTY_KM, MySpaceTexts.WorldSettings_ViewDistance_20_Km);
            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.THIRTY_KM, MySpaceTexts.WorldSettings_ViewDistance_30_Km);
            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.FORTY_KM, MySpaceTexts.WorldSettings_ViewDistance_40_Km);
            m_viewDistanceCombo.AddItem((int)MyViewDistanceEnum.FIFTY_KM, MySpaceTexts.WorldSettings_ViewDistance_50_Km);

            m_physicsOptionsCombo.SetToolTip(MySpaceTexts.WorldSettings_Physics_Tooltip);
            m_physicsOptionsCombo.AddItem((int)MyPhysicsPerformanceEnum.Fast, MySpaceTexts.WorldSettings_Physics_Fast);
            m_physicsOptionsCombo.AddItem((int)MyPhysicsPerformanceEnum.Normal, MySpaceTexts.WorldSettings_Physics_Normal);
            m_physicsOptionsCombo.AddItem((int)MyPhysicsPerformanceEnum.Precise, MySpaceTexts.WorldSettings_Physics_Precise);

			m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.LOW, MySpaceTexts.WorldSettings_FloraDensity_Low);
			m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.MEDIUM, MySpaceTexts.WorldSettings_FloraDensity_Medium);
			m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.HIGH, MySpaceTexts.WorldSettings_FloraDensity_High);

            m_autoHealing.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsAutoHealing));
            m_thrusterDamage.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsThrusterDamage));
            m_cargoShipsEnabled.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsEnableCargoShips));
            m_enableSpectator.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsEnableSpectator));
            m_resetOwnership.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsResetOwnership));
            m_permanentDeath.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsPermanentDeath));
            m_destructibleBlocks.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsDestructibleBlocks));
            m_environment.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsEnvironment));
            m_onlineMode.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsOnlineMode));
            m_enableCopyPaste.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsEnableCopyPaste));
            m_showPlayerNamesOnHud.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsShowPlayerNamesOnHud));
            m_maxFloatingObjectsSlider.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsMaxFloatingObjects));
            m_maxPlayersSlider.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsMaxPlayer));
            m_weaponsEnabled.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsWeapons));
            m_trashRemoval.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsRemoveTrash));
            m_worldSizeCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsLimitWorldSize));
            m_viewDistanceCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsViewDistance));
            m_soundModeCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsSoundMode));
            m_respawnShipDelete.SetToolTip(MyTexts.GetString(MySpaceTexts.TooltipWorldSettingsRespawnShipDelete));
            m_enableToolShake.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_ToolShake));
            m_enableOxygen.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_EnableOxygen));
			m_floraDensityCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_FloraDensity));
			m_enableFlora.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_EnableFlora));
            m_enablePlanets.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_EnablePlanets));
			m_stationVoxelSupport.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_EnableStationVoxel));
            m_disableRespawnShips.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_DisableRespawnShips));

            // Add controls in pairs; label first, control second. They will be laid out automatically this way.
            parent.Controls.Add(gameTypeLabel);
            parent.Controls.Add(m_creativeModeButton);

            parent.Controls.Add(inventorySizeLabel);
            parent.Controls.Add(m_inventory_x1);

            parent.Controls.Add(assemblerEfficiencyLabel);
            parent.Controls.Add(m_assembler_x1);

            parent.Controls.Add(refineryEfficiencyLabel);
            parent.Controls.Add(m_refinery_x1);

            parent.Controls.Add(weldingSpeedLabel);
            parent.Controls.Add(m_welder_x1);

            parent.Controls.Add(grindingSpeedLabel);
            parent.Controls.Add(m_grinder_x1);

            parent.Controls.Add(m_maxFloatingObjectsLabel);
            parent.Controls.Add(m_maxFloatingObjectsSlider);

            if (!MyFakes.OCTOBER_RELEASE_HIDE_WORLD_PARAMS)
            {
                parent.Controls.Add(passwordLabel);
                parent.Controls.Add(m_passwordTextbox);
                parent.Controls.Add(gameStyleLabel);
            }

            parent.Controls.Add(worldSizeLabel);
            parent.Controls.Add(m_worldSizeCombo);

            parent.Controls.Add(spawnShipTimeLabel);
            parent.Controls.Add(m_spawnShipTimeCombo);

            parent.Controls.Add(viewDistanceLabel);
            parent.Controls.Add(m_viewDistanceCombo);

            if (MyFakes.ENABLE_NEW_SOUNDS)
            {
                parent.Controls.Add(soundModeLabel);
                parent.Controls.Add(m_soundModeCombo);
            }

            if (MyFakes.ENABLE_PHYSICS_SETTINGS)
            {
                parent.Controls.Add(physicsOptionLabel);
                parent.Controls.Add(m_physicsOptionsCombo);
            }

            if (MyFakes.ENABLE_PLANETS)
            {
                parent.Controls.Add(floraDensityLabel);
                parent.Controls.Add(m_floraDensityCombo);
            }

            parent.Controls.Add(autoHealingLabel);
            parent.Controls.Add(m_autoHealing);

            parent.Controls.Add(m_enableCopyPasteLabel);
            parent.Controls.Add(m_enableCopyPaste);

            if (!MyFakes.OCTOBER_RELEASE_HIDE_WORLD_PARAMS)
            {
                parent.Controls.Add(soundInSpaceLabel);
                parent.Controls.Add(friendlyFireLabel);
            }
            parent.Controls.Add(clientCanSaveLabel);
            parent.Controls.Add(m_clientCanSave);

            parent.Controls.Add(enableWeaponsLabel);
            parent.Controls.Add(m_weaponsEnabled);

            if (MyFakes.ENABLE_TRASH_REMOVAL)
            {
                parent.Controls.Add(trashRemovalLabel);
                parent.Controls.Add(m_trashRemoval);
            }

            parent.Controls.Add(oxygenLabel);
            parent.Controls.Add(m_enableOxygen);
            
            parent.Controls.Add(disableRespawnShipsLabel);
            parent.Controls.Add(m_disableRespawnShips);

            if (MyFakes.ENABLE_PLANETS)
            {
                parent.Controls.Add(enableFloraLabel);
                parent.Controls.Add(m_enableFlora);
            }

            parent.Controls.Add(respawnShipDeleteLabel);
            parent.Controls.Add(m_respawnShipDelete);

            float labelSize = 0.21f;

            float MARGIN_TOP = 0.03f;

            // Automatic layout.
            Vector2 originL, originC;
            Vector2 controlsDelta = new Vector2(0f, 0.052f);

            originL = -m_size.Value / 2 + new Vector2(0.16f, MARGIN_TOP);
            originC = originL + new Vector2(labelSize, 0f);
            float rightColumnOffset = originC.X + m_onlineMode.Size.X - labelSize - 0.017f;

            foreach (var control in parent.Controls)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                if (control is MyGuiControlLabel)
                    control.Position = originL + controlsDelta * numControls;
                else
                    control.Position = originC + controlsDelta * numControls++;
            }


            m_sunRotationIntervalSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: m_onlineMode.Size.X,
                labelSpaceWidth: 0.05f);

            m_sunRotationIntervalSlider.MinValue = 0;
            m_sunRotationIntervalSlider.MaxValue = 1;
            m_sunRotationIntervalSlider.DefaultValue = 0;

            m_sunRotationIntervalSlider.ValueChanged += (MyGuiControlSlider s) =>
            {
                m_tempBuilder.Clear();
                MyValueFormatter.AppendTimeInBestUnit(MathHelper.Clamp(MathHelper.InterpLog(s.Value, MIN_DAY_TIME_MINUTES, MAX_DAY_TIME_MINUTES), MIN_DAY_TIME_MINUTES, MAX_DAY_TIME_MINUTES) * 60, m_tempBuilder);
                m_sunRotationPeriodValue.Text = m_tempBuilder.ToString();
            };

            m_sunRotationIntervalSlider.Position = new Vector2(m_sunRotationIntervalSlider.Position.X + 0.075f, autoHealingLabel.Position.Y);
            m_sunRotationPeriodValue.Position = new Vector2(m_sunRotationIntervalSlider.Position.X +0.12f,m_sunRotationIntervalSlider.Position.Y);
            m_sunRotationPeriod.Position = new Vector2(m_sunRotationIntervalSlider.Position.X - 0.365f, m_sunRotationIntervalSlider.Position.Y);

            parent.Controls.Add(m_sunRotationPeriod);
            parent.Controls.Add(m_sunRotationIntervalSlider);
            parent.Controls.Add(m_sunRotationPeriodValue);

            float buttonsOffset = 0.055f;
            //Left column checkboxes
            autoHealingLabel.Position = new Vector2(autoHealingLabel.Position.X - labelSize / 2, autoHealingLabel.Position.Y + buttonsOffset);
            m_autoHealing.Position = new Vector2(m_autoHealing.Position.X - labelSize / 2, m_autoHealing.Position.Y + buttonsOffset);

            m_enableCopyPasteLabel.Position = new Vector2(m_enableCopyPasteLabel.Position.X - labelSize / 2, m_enableCopyPasteLabel.Position.Y + buttonsOffset);
            m_enableCopyPaste.Position = new Vector2(m_enableCopyPaste.Position.X - labelSize / 2, m_enableCopyPaste.Position.Y + buttonsOffset);

            clientCanSaveLabel.Position = new Vector2(clientCanSaveLabel.Position.X - labelSize / 2, clientCanSaveLabel.Position.Y + buttonsOffset);
            m_clientCanSave.Position = new Vector2(m_clientCanSave.Position.X - labelSize / 2, m_clientCanSave.Position.Y + buttonsOffset);

            enableWeaponsLabel.Position = new Vector2(enableWeaponsLabel.Position.X - labelSize / 2, enableWeaponsLabel.Position.Y + buttonsOffset);
            m_weaponsEnabled.Position = new Vector2(m_weaponsEnabled.Position.X - labelSize / 2, m_weaponsEnabled.Position.Y + buttonsOffset);

            trashRemovalLabel.Position = new Vector2(trashRemovalLabel.Position.X - labelSize / 2, trashRemovalLabel.Position.Y + buttonsOffset);
            m_trashRemoval.Position = new Vector2(m_trashRemoval.Position.X - labelSize / 2, m_trashRemoval.Position.Y + buttonsOffset);

            oxygenLabel.Position = new Vector2(oxygenLabel.Position.X - labelSize / 2, oxygenLabel.Position.Y + buttonsOffset);
            m_enableOxygen.Position = new Vector2(m_enableOxygen.Position.X - labelSize / 2, m_enableOxygen.Position.Y + buttonsOffset);


            respawnShipDeleteLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, m_autoHealing.Position.Y);
            m_respawnShipDelete.Position = new Vector2(rightColumnOffset + labelSize / 2, m_autoHealing.Position.Y);
            m_respawnShipDelete.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

            disableRespawnShipsLabel.Position = new Vector2(disableRespawnShipsLabel.Position.X - labelSize / 2, disableRespawnShipsLabel.Position.Y + buttonsOffset);
            m_disableRespawnShips.Position = new Vector2(m_disableRespawnShips.Position.X - labelSize / 2, m_disableRespawnShips.Position.Y + buttonsOffset);

            enableFloraLabel.Position = new Vector2(enableFloraLabel.Position.X - labelSize / 2, enableFloraLabel.Position.Y + buttonsOffset);
            m_enableFlora.Position = new Vector2(m_enableFlora.Position.X - labelSize / 2, m_enableFlora.Position.Y + buttonsOffset);

            //Middle column checkboxes

            showPlayerNamesOnHudLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, m_enableCopyPasteLabel.Position.Y);
            m_showPlayerNamesOnHud.Position = new Vector2(rightColumnOffset + labelSize / 2, m_enableCopyPasteLabel.Position.Y);

            thrusterDamageLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, clientCanSaveLabel.Position.Y);
            m_thrusterDamage.Position = new Vector2(rightColumnOffset + labelSize / 2, clientCanSaveLabel.Position.Y);

            enableIngameScriptsLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, trashRemovalLabel.Position.Y);
            m_enableIngameScripts.Position = new Vector2(rightColumnOffset + labelSize / 2, m_trashRemoval.Position.Y);

            enable3rdPersonCameraLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, m_enableOxygen.Position.Y);
            m_enable3rdPersonCamera.Position = new Vector2(rightColumnOffset + labelSize / 2, m_enableOxygen.Position.Y);

            enableSunRotationLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, m_disableRespawnShips.Position.Y);
            m_enableSunRotation.Position = new Vector2(rightColumnOffset + labelSize / 2, m_disableRespawnShips.Position.Y);

            enablePlanetsLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, enableFloraLabel.Position.Y);
            m_enablePlanets.Position = new Vector2(rightColumnOffset + labelSize / 2, m_enableFlora.Position.Y);
         
            if (MyFakes.ENABLE_CARGO_SHIPS)
            {
                parent.Controls.Add(shipsEnabledLabel);
                parent.Controls.Add(m_cargoShipsEnabled);
                shipsEnabledLabel.Position = new Vector2(rightColumnOffset - labelSize / 2, enableWeaponsLabel.Position.Y);
                m_cargoShipsEnabled.Position = new Vector2(rightColumnOffset + labelSize / 2, enableWeaponsLabel.Position.Y);
            }

            enableSpectatorLabel.Position = new Vector2(rightColumnOffset + 0.75f * labelSize, m_autoHealing.Position.Y);
            m_enableSpectator.Position = new Vector2(rightColumnOffset + labelSize + 0.75f * labelSize, m_autoHealing.Position.Y);

            resetOwnershipLabel.Position = new Vector2(rightColumnOffset + 0.75f * labelSize, m_enableCopyPasteLabel.Position.Y);
            m_resetOwnership.Position = new Vector2(rightColumnOffset + labelSize + 0.75f * labelSize, m_enableCopyPasteLabel.Position.Y);

            permanentDeathLabel.Position = new Vector2(rightColumnOffset + 0.75f * labelSize, clientCanSaveLabel.Position.Y);
            m_permanentDeath.Position = new Vector2(rightColumnOffset + labelSize + 0.75f * labelSize, clientCanSaveLabel.Position.Y);

            destructibleBlocksLabel.Position = new Vector2(rightColumnOffset + 0.75f * labelSize, m_cargoShipsEnabled.Position.Y);
            m_destructibleBlocks.Position = new Vector2(rightColumnOffset + labelSize + 0.75f * labelSize, m_cargoShipsEnabled.Position.Y);

            enableToolShakeLabel.Position = new Vector2(rightColumnOffset + 0.75f * labelSize, trashRemovalLabel.Position.Y);
            m_enableToolShake.Position = new Vector2(rightColumnOffset + labelSize + 0.75f * labelSize, m_trashRemoval.Position.Y);

            enableEncountersLabel.Position = new Vector2(rightColumnOffset + 0.75f * labelSize, enable3rdPersonCameraLabel.Position.Y);
            m_enableEncounters.Position = new Vector2(rightColumnOffset + labelSize + 0.75f * labelSize, m_enable3rdPersonCamera.Position.Y);

            enableStationVoxelLabel.Position = new Vector2(rightColumnOffset + 0.75f * labelSize, enableSunRotationLabel.Position.Y);
            m_stationVoxelSupport.Position = new Vector2(rightColumnOffset + labelSize + 0.75f * labelSize, m_enableSunRotation.Position.Y);

            parent.Controls.Add(showPlayerNamesOnHudLabel);
            parent.Controls.Add(m_showPlayerNamesOnHud);

            parent.Controls.Add(thrusterDamageLabel);
            parent.Controls.Add(m_thrusterDamage);

            parent.Controls.Add(enableSpectatorLabel);
            parent.Controls.Add(m_enableSpectator);

            parent.Controls.Add(resetOwnershipLabel);
            parent.Controls.Add(m_resetOwnership);

            parent.Controls.Add(permanentDeathLabel);
            parent.Controls.Add(m_permanentDeath);

            parent.Controls.Add(destructibleBlocksLabel);
            parent.Controls.Add(m_destructibleBlocks);

            if (MyFakes.ENABLE_PROGRAMMABLE_BLOCK)
            {
                parent.Controls.Add(enableIngameScriptsLabel);
                parent.Controls.Add(m_enableIngameScripts);
            }
            if (MyFakes.ENABLE_TOOL_SHAKE)
            {
                parent.Controls.Add(enableToolShakeLabel);
                parent.Controls.Add(m_enableToolShake);
            }

            parent.Controls.Add(enableEncountersLabel);
            parent.Controls.Add(m_enableEncounters);
            parent.Controls.Add(enable3rdPersonCameraLabel);
            parent.Controls.Add(m_enable3rdPersonCamera);

			parent.Controls.Add(enableStationVoxelLabel);
			parent.Controls.Add(m_stationVoxelSupport);

            if (MyFakes.ENABLE_PLANETS)
            {
                parent.Controls.Add(enablePlanetsLabel);
                parent.Controls.Add(m_enablePlanets);
            }

            parent.Controls.Add(enableSunRotationLabel);
            parent.Controls.Add(m_enableSunRotation);

            m_survivalModeButton.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
            m_survivalModeButton.Position = m_creativeModeButton.Position + new Vector2(m_onlineMode.Size.X, 0);

            parent.Controls.Add(m_survivalModeButton);

            parent.Controls.Add(m_inventory_x3);
            parent.Controls.Add(m_inventory_x10);
            m_inventory_x3.Position = m_inventory_x1.Position + new Vector2(m_inventory_x1.Size.X + 0.017f, 0);
            m_inventory_x10.Position = m_inventory_x3.Position + new Vector2(m_inventory_x3.Size.X + 0.017f, 0);

            parent.Controls.Add(m_refinery_x3);
            parent.Controls.Add(m_refinery_x10);
            m_refinery_x3.Position = m_refinery_x1.Position + new Vector2(m_refinery_x1.Size.X + 0.017f, 0);
            m_refinery_x10.Position = m_refinery_x3.Position + new Vector2(m_refinery_x3.Size.X + 0.017f, 0);

            parent.Controls.Add(m_assembler_x3);
            parent.Controls.Add(m_assembler_x10);
            m_assembler_x3.Position = m_assembler_x1.Position + new Vector2(m_assembler_x1.Size.X + 0.017f, 0);
            m_assembler_x10.Position = m_assembler_x3.Position + new Vector2(m_assembler_x3.Size.X + 0.017f, 0);

            parent.Controls.Add(m_welder_half);
            parent.Controls.Add(m_welder_x2);
            parent.Controls.Add(m_welder_x5);
            m_welder_half.Position = m_welder_x1.Position + new Vector2(m_welder_x1.Size.X + 0.017f, 0);
            m_welder_x2.Position = m_welder_half.Position + new Vector2(m_welder_half.Size.X + 0.017f, 0);
            m_welder_x5.Position = m_welder_x2.Position + new Vector2(m_welder_x2.Size.X + 0.017f, 0);

            parent.Controls.Add(m_grinder_half);
            parent.Controls.Add(m_grinder_x2);
            parent.Controls.Add(m_grinder_x5);
            m_grinder_half.Position = m_grinder_x1.Position + new Vector2(m_grinder_x1.Size.X + 0.017f, 0);
            m_grinder_x2.Position = m_grinder_half.Position + new Vector2(m_grinder_half.Size.X + 0.017f, 0);
            m_grinder_x5.Position = m_grinder_x2.Position + new Vector2(m_grinder_x2.Size.X + 0.017f, 0);

            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            Controls.Add(scrollPanel);
            CloseButtonEnabled = true;
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        private void LoadValues()
        {
            if (m_isNewGame)
            {
                m_passwordTextbox.Text = "";
            }
            else
            {
                m_passwordTextbox.Text = m_parent.Checkpoint.Password;
            }
            SetSettings(m_parent.Settings);
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

        public void UpdateSurvivalState(bool survivalEnabled)
        {
            m_creativeModeButton.Checked = !survivalEnabled;
            m_survivalModeButton.Checked = survivalEnabled;

            m_inventory_x1.Enabled = m_inventory_x3.Enabled = m_inventory_x10.Enabled = survivalEnabled;
            m_assembler_x1.Enabled = m_assembler_x3.Enabled = m_assembler_x10.Enabled = survivalEnabled;
            m_refinery_x1.Enabled = m_refinery_x3.Enabled = m_refinery_x10.Enabled = survivalEnabled;

            if (survivalEnabled)
            {
                m_enableCopyPaste.IsChecked = false;
            }

            m_enableCopyPaste.Enabled = !survivalEnabled;
            m_enableCopyPasteLabel.Enabled = !survivalEnabled;
        }

        private MyGameModeEnum GetGameMode()
        {
            return m_survivalModeButton.Checked ? MyGameModeEnum.Survival : MyGameModeEnum.Creative;
        }

        // Returns userobject as float from first checked button
        private float GetMultiplier(params MyGuiControlButton[] buttons)
        {
            foreach (var btn in buttons)
            {
                if (btn.Checked && btn.UserData is float)
                    return (float)btn.UserData;
            }
            Debug.Fail("No button is active");
            return 1.0f;
        }

        private float GetInventoryMultiplier()
        {
            return GetMultiplier(m_inventory_x1, m_inventory_x3, m_inventory_x10);
        }

        private float GetRefineryMultiplier()
        {
            return GetMultiplier(m_refinery_x1, m_refinery_x3, m_refinery_x10);
        }

        private float GetAssemblerMultiplier()
        {
            return GetMultiplier(m_assembler_x1, m_assembler_x3, m_assembler_x10);
        }

        private float GetWelderMultiplier()
        {
            return GetMultiplier(m_welder_x1, m_welder_half, m_welder_x2, m_welder_x5);
        }

        private float GetGrinderMultiplier()
        {
            return GetMultiplier(m_grinder_x1, m_grinder_half, m_grinder_x2, m_grinder_x5);
        }

        private float GetSpawnShipTimeMultiplier()
        {
            return (float)m_spawnShipTimeCombo.GetSelectedKey() / 10.0f;
        }

        public int GetWorldSize()
        {
            var asd = m_parent.Settings.WorldSizeKm;
            switch (m_worldSizeCombo.GetSelectedKey())
            {
                case (int)MyWorldSizeEnum.TEN_KM:
                    return 10;
                    break;
                case (int)MyWorldSizeEnum.TWENTY_KM:
                    return 20;
                    break;
                case (int)MyWorldSizeEnum.FIFTY_KM:
                    return 50;
                    break;
                case (int)MyWorldSizeEnum.HUNDRED_KM:
                    return 100;
                    break;
                case (int)MyWorldSizeEnum.UNLIMITED:
                    return 0;
                    break;
                case (int)MyWorldSizeEnum.CUSTOM:
                    return m_customWorldSize;
                    break;
                default:
                    Debug.Assert(false, "Unhandled MyWorldSizeEnum value");
                    return 0;
                    break;
            }
        }

        private MyWorldSizeEnum WorldSizeEnumKey(int worldSize)
        {
            switch (worldSize)
            {
                case 0:
                    return MyWorldSizeEnum.UNLIMITED;
                case 10:
                    return MyWorldSizeEnum.TEN_KM;
                case 20:
                    return MyWorldSizeEnum.TWENTY_KM;
                case 50:
                    return MyWorldSizeEnum.FIFTY_KM;
                case 100:
                    return MyWorldSizeEnum.HUNDRED_KM;
                default:
                    m_worldSizeCombo.AddItem((int)MyWorldSizeEnum.CUSTOM, MySpaceTexts.WorldSettings_WorldSizeCustom);
                    m_customWorldSize = worldSize;
                    //Debug.Assert(false, "non-standard world size");
                    return MyWorldSizeEnum.CUSTOM;
            }
        }

        public int GetViewDistance()
        {
            var key = m_viewDistanceCombo.GetSelectedKey();
            if (key == (int)MyViewDistanceEnum.CUSTOM)
            {
                return m_customViewDistance;
            }
            return (int)key;
        }

        private MyViewDistanceEnum ViewDistanceEnumKey(int viewDistance)
        {
            var value = (MyViewDistanceEnum)viewDistance;
            if (value != MyViewDistanceEnum.CUSTOM && Enum.IsDefined(typeof(MyViewDistanceEnum), value))
            {
                return (MyViewDistanceEnum)viewDistance;
            }
            else
            {
                m_viewDistanceCombo.AddItem((int)MyWorldSizeEnum.CUSTOM, MySpaceTexts.WorldSettings_ViewDistance_Custom);
                m_viewDistanceCombo.SelectItemByKey((int)MyWorldSizeEnum.CUSTOM);
                m_customViewDistance = viewDistance;
                return MyViewDistanceEnum.CUSTOM;
            }
        }

		public int GetFloraDensity()
		{
			return (int)m_floraDensityCombo.GetSelectedKey();
		}

		private MyFloraDensityEnum FloraDensityEnumKey(int floraDensity)
		{
			var value = (MyFloraDensityEnum)floraDensity;
			if (Enum.IsDefined(typeof(MyFloraDensityEnum), value))
			{
				return (MyFloraDensityEnum)floraDensity;
			}
			return MyFloraDensityEnum.LOW;
		}

        public void GetSettings(MyObjectBuilder_SessionSettings output)
        {
            output.OnlineMode = (MyOnlineModeEnum)m_onlineMode.GetSelectedKey();
            output.EnvironmentHostility = (MyEnvironmentHostilityEnum)m_environment.GetSelectedKey();

            output.AutoHealing = m_autoHealing.IsChecked;
            output.ClientCanSave = m_clientCanSave.IsChecked;
            output.CargoShipsEnabled = m_cargoShipsEnabled.IsChecked;
            output.EnableCopyPaste = m_enableCopyPaste.IsChecked;
            output.EnableSpectator = m_enableSpectator.IsChecked;
            output.ResetOwnership = m_resetOwnership.IsChecked;
            output.PermanentDeath = m_permanentDeath.IsChecked;
            output.DestructibleBlocks = m_destructibleBlocks.IsChecked;
            output.EnableIngameScripts = m_enableIngameScripts.IsChecked;
            output.Enable3rdPersonView = m_enable3rdPersonCamera.IsChecked;
            output.EnableEncounters = m_enableEncounters.IsChecked;
            output.EnableToolShake = m_enableToolShake.IsChecked;
            output.ShowPlayerNamesOnHud = m_showPlayerNamesOnHud.IsChecked;
            output.ThrusterDamage = m_thrusterDamage.IsChecked;
            output.WeaponsEnabled = m_weaponsEnabled.IsChecked;
            output.RemoveTrash = m_trashRemoval.IsChecked;
            output.EnableOxygen = m_enableOxygen.IsChecked;
            if (output.EnableOxygen && output.VoxelGeneratorVersion < MyVoxelConstants.VOXEL_GENERATOR_MIN_ICE_VERSION)
            {
                output.VoxelGeneratorVersion = MyVoxelConstants.VOXEL_GENERATOR_MIN_ICE_VERSION;
            }
            output.RespawnShipDelete = m_respawnShipDelete.IsChecked;
			output.EnableFlora = m_enableFlora.IsChecked;
			output.EnableStationVoxelSupport = m_stationVoxelSupport.IsChecked;
            output.DisableRespawnShips = m_disableRespawnShips.IsChecked;
            output.EnablePlanets = m_enablePlanets.IsChecked;
            output.EnableSunRotation = m_enableSunRotation.IsChecked;

            output.MaxPlayers = (short)m_maxPlayersSlider.Value;
            output.MaxFloatingObjects = (short)m_maxFloatingObjectsSlider.Value;
            output.SunRotationIntervalMinutes = MathHelper.Clamp(MathHelper.InterpLog(m_sunRotationIntervalSlider.Value, MIN_DAY_TIME_MINUTES, MAX_DAY_TIME_MINUTES), MIN_DAY_TIME_MINUTES, MAX_DAY_TIME_MINUTES);

            output.AssemblerEfficiencyMultiplier = GetAssemblerMultiplier();
            output.AssemblerSpeedMultiplier = GetAssemblerMultiplier();
            output.InventorySizeMultiplier = GetInventoryMultiplier();
            output.RefinerySpeedMultiplier = GetRefineryMultiplier();
            output.WelderSpeedMultiplier = GetWelderMultiplier();
            output.GrinderSpeedMultiplier = GetGrinderMultiplier();
            output.SpawnShipTimeMultiplier = GetSpawnShipTimeMultiplier();

            output.WorldSizeKm = GetWorldSize();
            output.ViewDistance = GetViewDistance();
			output.FloraDensity = GetFloraDensity();
            output.RealisticSound = ((MySoundModeEnum)m_soundModeCombo.GetSelectedKey() == MySoundModeEnum.Realistic);

            output.PhysicsIterations = (int)m_physicsOptionsCombo.GetSelectedKey();

            output.GameMode = GetGameMode();
        }

        public void SetSettings(MyObjectBuilder_SessionSettings settings)
        {
            m_onlineMode.SelectItemByKey((int)settings.OnlineMode);
            m_environment.SelectItemByKey((int)settings.EnvironmentHostility);
            m_worldSizeCombo.SelectItemByKey((int)WorldSizeEnumKey(settings.WorldSizeKm));
            m_spawnShipTimeCombo.SelectItemByKey((int)(settings.SpawnShipTimeMultiplier * 10));
            m_viewDistanceCombo.SelectItemByKey((int)ViewDistanceEnumKey(settings.ViewDistance));
			m_floraDensityCombo.SelectItemByKey((int)FloraDensityEnumKey(settings.FloraDensity));
            m_soundModeCombo.SelectItemByKey(settings.RealisticSound ? (int)MySoundModeEnum.Realistic : (int)MySoundModeEnum.Arcade);
            if (m_physicsOptionsCombo.TryGetItemByKey(settings.PhysicsIterations) != null)
                m_physicsOptionsCombo.SelectItemByKey(settings.PhysicsIterations);
            else
                m_physicsOptionsCombo.SelectItemByKey((int)MyPhysicsPerformanceEnum.Fast);

            m_autoHealing.IsChecked = settings.AutoHealing;
            m_clientCanSave.IsChecked = settings.ClientCanSave;
            m_cargoShipsEnabled.IsChecked = settings.CargoShipsEnabled;
            m_enableCopyPaste.IsChecked = settings.EnableCopyPaste;
            m_enableSpectator.IsChecked = settings.EnableSpectator;
            m_resetOwnership.IsChecked = settings.ResetOwnership;
            m_permanentDeath.IsChecked = settings.PermanentDeath.Value;
            m_destructibleBlocks.IsChecked = settings.DestructibleBlocks;
            m_enableEncounters.IsChecked = settings.EnableEncounters;
            m_enable3rdPersonCamera.IsChecked = settings.Enable3rdPersonView;
            m_enableIngameScripts.IsChecked = settings.EnableIngameScripts;
            m_enableToolShake.IsChecked = settings.EnableToolShake;
            m_showPlayerNamesOnHud.IsChecked = settings.ShowPlayerNamesOnHud;
            m_thrusterDamage.IsChecked = settings.ThrusterDamage;
            m_weaponsEnabled.IsChecked = settings.WeaponsEnabled;
            m_trashRemoval.IsChecked = settings.RemoveTrash;
            m_enableOxygen.IsChecked = settings.EnableOxygen;
            if (settings.VoxelGeneratorVersion < MyVoxelConstants.VOXEL_GENERATOR_MIN_ICE_VERSION)
            {
                m_showWarningForOxygen = true;
            }
            m_disableRespawnShips.IsChecked = settings.DisableRespawnShips;
            m_respawnShipDelete.IsChecked = settings.RespawnShipDelete;
			m_enableFlora.IsChecked = settings.EnableFlora;
			m_stationVoxelSupport.IsChecked = settings.EnableStationVoxelSupport;
            m_enablePlanets.IsChecked = settings.EnablePlanets;
            m_enableSunRotation.IsChecked = settings.EnableSunRotation;

            m_sunRotationIntervalSlider.Enabled = m_enableSunRotation.IsChecked;
            m_sunRotationPeriodValue.Visible = m_enableSunRotation.IsChecked;

            m_enableFlora.Enabled = m_enablePlanets.IsChecked;
            m_floraDensityCombo.Enabled = m_enablePlanets.IsChecked;

            m_sunRotationIntervalSlider.Value = MathHelper.Clamp(MathHelper.InterpLogInv((float)settings.SunRotationIntervalMinutes, MIN_DAY_TIME_MINUTES, MAX_DAY_TIME_MINUTES), 0, 1);
            m_maxPlayersSlider.Value = settings.MaxPlayers;
            m_maxFloatingObjectsSlider.Value = settings.MaxFloatingObjects;

            CheckButton(settings.AssemblerSpeedMultiplier, m_assembler_x1, m_assembler_x3, m_assembler_x10);
            CheckButton(settings.InventorySizeMultiplier, m_inventory_x1, m_inventory_x3, m_inventory_x10);
            CheckButton(settings.RefinerySpeedMultiplier, m_refinery_x1, m_refinery_x3, m_refinery_x10);
            CheckButton(settings.WelderSpeedMultiplier, m_welder_x1, m_welder_half, m_welder_x2, m_welder_x5);
            CheckButton(settings.GrinderSpeedMultiplier, m_grinder_x1, m_grinder_half, m_grinder_x2, m_grinder_x5);

            UpdateSurvivalState(settings.GameMode == MyGameModeEnum.Survival);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenAdvancedWorldSettings";
        }

        private void CancelButtonClicked(object sender)
        {
            this.CloseScreen();
        }

        private void OkButtonClicked(object sender)
        {
            m_isConfirmed = true;

            if (OnOkButtonClicked != null)
            {
                OnOkButtonClicked();
            }

            this.CloseScreen();
        }

        private void CreativeClicked(object sender)
        {
            UpdateSurvivalState(false);
        }

        private void SurvivalClicked(object sender)
        {
            UpdateSurvivalState(true);
        }

        private void OnInventoryClick(object sender)
        {
            CheckButton((MyGuiControlButton)sender, m_inventory_x1, m_inventory_x3, m_inventory_x10);
            UpdateSurvivalState(m_survivalModeButton.Checked);
        }

        private void OnAssemblerClick(object sender)
        {
            CheckButton((MyGuiControlButton)sender, m_assembler_x1, m_assembler_x3, m_assembler_x10);
            UpdateSurvivalState(m_survivalModeButton.Checked);
        }

        private void OnRefineryClick(object sender)
        {
            CheckButton((MyGuiControlButton)sender, m_refinery_x1, m_refinery_x3, m_refinery_x10);
            UpdateSurvivalState(m_survivalModeButton.Checked);
        }

        private void OnWelderClick(object sender)
        {
            CheckButton((MyGuiControlButton)sender, m_welder_half, m_welder_x1, m_welder_x2, m_welder_x5);
            UpdateSurvivalState(m_survivalModeButton.Checked);
        }

        private void OnGrinderClick(object sender)
        {
            CheckButton((MyGuiControlButton)sender, m_grinder_half, m_grinder_x1, m_grinder_x2, m_grinder_x5);
            UpdateSurvivalState(m_survivalModeButton.Checked);
        }

        public event System.Action OnOkButtonClicked;
    }
}
