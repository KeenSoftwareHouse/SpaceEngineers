
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenEncounterSettings : MyGuiScreenBase
    { 
        private enum MyDamageEnum
        {
            NO_DAMAGE,
            ALMOST_NEW,
            LIGHT_DAMAGE,
            DAMAGED,
            HEAVILY_DAMAGED,
        }

        MyGuiScreenWorldSettings m_parent;
        bool m_isNewGame;
        bool m_isConfirmed;
        float m_damageSliderLastValue, m_AntennaOnLastValue;

        MyGuiControlButton m_okButton, m_cancelButton, m_peaceModeButton, m_adventureModeButton, m_warModeButton, m_ruinsModeButton, m_randomModeButton, m_shipSelectorButton;
        MyGuiControlSlider m_maxNoShipsPerSpawnGroup, m_maxDamagedShipPercentage, m_maxHostileEncountersPercentage, m_antennaOnPercentage, m_reactorsOnPercentage, m_smallToLargeShipRatio;
        MyGuiControlCombobox m_maxDamagedShipsSeverity;
        MyGuiControlCheckbox m_antennaRangeMaxedOut, m_damageAppliedGlobally, m_allowArmedLargeShipsOnly;        

        private MyGuiControlTable.Row m_selectedRow;

        internal MyGuiScreenEncounterShipSelector EncounterShipSelection;

        List<MyGuiControlTable.Row> ShipsAvailableMaster = new List<MyGuiControlTable.Row>();
        List<MyGuiControlTable.Row> ShipsAvailableTemporary = new List<MyGuiControlTable.Row>();
        List<MyGuiControlTable.Row> ShipsAvailableHolderForArmedLargeShips = new List<MyGuiControlTable.Row>();

        public bool IsConfirmed
        {
            get
            {
                return m_isConfirmed;
            }
        }

        public MyGuiScreenEncounterSettings(MyGuiScreenWorldSettings parent)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, CalcSize(parent.Checkpoint))
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenEncounterSettings.ctor START");

            m_parent = parent;
            EnabledBackgroundFade = true;

            // TODO: May need to review this as I'm not sure I understand the checkpoint system well enough.
            m_isNewGame = (parent.Checkpoint == null);
            m_isConfirmed = false;

            RecreateControls(true);

            MySandboxGame.Log.WriteLine("MyGuiScreenEncounterSettings.ctor END");
        }

        public static Vector2 CalcSize(MyObjectBuilder_Checkpoint checkpoint)
        {
            float width = 0.9f;
            float height = 0.97f;

            return new Vector2(width, height);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            BuildControls();

            GetAvailableShips();

            LoadValues();            
        }

        public void BuildControls()
        {
            MyGuiControlParent parent = new MyGuiControlParent(size: new Vector2(Size.Value.X - 0.05f, Size.Value.Y));
            MyGuiControlScrollablePanel scrollPanel = new MyGuiControlScrollablePanel(parent);
            scrollPanel.ScrollbarVEnabled = false;
            scrollPanel.Size = new Vector2(Size.Value.X - 0.05f, 0.8f);

            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.23f, 0.03f);

            AddCaption(MySpaceTexts.ScreenCaptionEncountersConfiguration);

            int numControls = 0;

            float severityComboBoxWidth = 0.2f;

            var maxNoShipsLabel = MakeLabel(MySpaceTexts.WorldSettings_MaxNoShipsPerSpawnGroup);
            var maxDamagedShipPercentageLabel = MakeLabel(MySpaceTexts.WorldSettings_DamagedEncounterLabel);
            var maxDamagedShipsSeverityLabel = MakeLabel(MySpaceTexts.WorldSettings_MaxDamagedShipsSeverity);
            var hostileEncountersLabel = MakeLabel(MySpaceTexts.WorldSettings_HostileEncountersLabel);
            var antennaActiveLabel = MakeLabel(MySpaceTexts.WorldSettings_AntennaActiveLabel);
            var antennaRangeMaxedLabel = MakeLabel(MySpaceTexts.WorldSettings_AntennaMaxedLabel);
            var reactorsOnPercentageLabel = MakeLabel(MySpaceTexts.WorldSettings_ReactorsOnLabel);
            var damageAppliedGloballyLabel = MakeLabel(MySpaceTexts.WorldSettings_DamageAppliedGloballyLabel);
            var smallToLargeShipRatioLabel = MakeLabel(MySpaceTexts.WorldSettings_SmallToLargeShipRatioLabel);
            var allowArmedLargeShipsOnlyLabel = MakeLabel(MySpaceTexts.WorldSettings_AllowArmedLargeShipsLabel);
            var presetLabel = MakeLabel(MySpaceTexts.WorldSettings_PresetValuesLabel);                 

            // Setup settings controls
            m_maxNoShipsPerSpawnGroup = new MyGuiControlSlider(
                position: Vector2.Zero - new Vector2(-0.1f, 0.4f),
                width: 0.2f,
                minValue: 1,
                maxValue: 5,
                labelText: new StringBuilder("{0}").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true,
                defaultValue: 2
                );

            m_maxNoShipsPerSpawnGroup.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsNoShips));

            m_maxHostileEncountersPercentage = new MyGuiControlSlider(
                position: Vector2.Zero - new Vector2(-0.1f, 0.4f),
                width: 0.2f,
                minValue: 0,
                maxValue: 100,
                labelText: new StringBuilder("{0}%").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true,
                defaultValue: 0
                );

            m_maxHostileEncountersPercentage.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsHostiles));           

            m_maxDamagedShipPercentage = new MyGuiControlSlider(
                position: Vector2.Zero - new Vector2(-0.1f, 0.3f),
                width: 0.2f,
                minValue: 0,
                maxValue: 100,
                labelText: new StringBuilder("{0}%").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true,
                defaultValue: 0
                );

            m_maxDamagedShipPercentage.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsDamagedShips));
            m_maxDamagedShipPercentage.ValueChanged += onDamagedShipValueChanged;


            m_maxDamagedShipsSeverity = new MyGuiControlCombobox(size: new Vector2(severityComboBoxWidth, 0.04f));
            m_maxDamagedShipsSeverity.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsDamagedShipSeverity));

            m_maxDamagedShipsSeverity.AddItem((int)MyDamageEnum.NO_DAMAGE, MySpaceTexts.WorldSettings_NoDamage);
            m_maxDamagedShipsSeverity.AddItem((int)MyDamageEnum.ALMOST_NEW, MySpaceTexts.WorldSettings_Worn);
            m_maxDamagedShipsSeverity.AddItem((int)MyDamageEnum.LIGHT_DAMAGE, MySpaceTexts.WorldSettings_LightlyDamaged);
            m_maxDamagedShipsSeverity.AddItem((int)MyDamageEnum.DAMAGED, MySpaceTexts.WorldSettings_Damaged);
            m_maxDamagedShipsSeverity.AddItem((int)MyDamageEnum.HEAVILY_DAMAGED, MySpaceTexts.WorldSettings_HeavilyDamaged);

            m_damageAppliedGlobally = new MyGuiControlCheckbox();
            m_damageAppliedGlobally.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsDamageAppliedGlobally));

            m_reactorsOnPercentage = new MyGuiControlSlider(
                position: Vector2.Zero - new Vector2(-0.1f, 0.3f),
                width: 0.2f,
                minValue: 0,
                maxValue: 100,
                labelText: new StringBuilder("{0}%").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true,
                defaultValue: 0
                );

            m_reactorsOnPercentage.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsReactorsOn));

            m_antennaOnPercentage = new MyGuiControlSlider(
                position: Vector2.Zero - new Vector2(-0.1f, 0.3f),
                width: 0.2f,
                minValue: 0,
                maxValue: 100,
                labelText: new StringBuilder("{0}%").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true,
                defaultValue: 0
                );

            m_antennaOnPercentage.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasOn));
            m_antennaOnPercentage.ValueChanged += onAntennaOnValueChanged;

            m_smallToLargeShipRatio = new MyGuiControlSlider(
                position: Vector2.Zero - new Vector2(-0.1f, 0.3f),
                width: 0.2f,
                minValue: 0,
                maxValue: 100,
                labelText: new StringBuilder("{0}%").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true,
                defaultValue: 50
                );

            m_smallToLargeShipRatio.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsSmallToLargeShipRatio));

            m_antennaRangeMaxedOut = new MyGuiControlCheckbox();
            m_antennaRangeMaxedOut.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));

            m_allowArmedLargeShipsOnly = new MyGuiControlCheckbox();
            m_allowArmedLargeShipsOnly.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsArmedLargeShipsOnly));
            m_allowArmedLargeShipsOnly.IsCheckedChanged += onArmedLargeShipsOnlyIsCheckedChanged;

            // Ok-Cancel Buttons
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: CancelButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            
            // Themed Preset Buttons
            m_peaceModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_CURSOR_OVER, text: MyTexts.Get(MySpaceTexts.WorldSettings_PeaceMode), onButtonClick: PeaceButtonClicked);
            m_peaceModeButton.SetToolTip(MySpaceTexts.ToolTipEncounterSettings_PeaceMode);

            m_adventureModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_CURSOR_OVER, text: MyTexts.Get(MySpaceTexts.WorldSettings_AdventureMode), onButtonClick: AdventureButtonClicked);
            m_adventureModeButton.SetToolTip(MySpaceTexts.ToolTipEncounterSettings_AdventureMode);

            m_warModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_CURSOR_OVER, text: MyTexts.Get(MySpaceTexts.WorldSettings_WarMode), onButtonClick: WarButtonClicked);
            m_warModeButton.SetToolTip(MySpaceTexts.ToolTipEncounterSettings_WarMode);

            m_ruinsModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_CURSOR_OVER, text: MyTexts.Get(MySpaceTexts.WorldSettings_RuinsMode), onButtonClick: RuinsButtonClicked);
            m_ruinsModeButton.SetToolTip(MySpaceTexts.ToolTipEncounterSettings_RuinsMode);

            m_randomModeButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.Small, highlightType: MyGuiControlHighlightType.WHEN_CURSOR_OVER, text: MyTexts.Get(MySpaceTexts.WorldSettings_RandomMode), onButtonClick: RandomButtonClicked);
            m_randomModeButton.SetToolTip(MySpaceTexts.ToolTipEncounterSettings_RandomMode);

            m_shipSelectorButton = new MyGuiControlButton(highlightType: MyGuiControlHighlightType.WHEN_CURSOR_OVER, text: MyTexts.Get(MySpaceTexts.WorldSettings_ShipSelector), onButtonClick: ShipSelectorButtonClicked);
            m_shipSelectorButton.SetToolTip(MySpaceTexts.ToolTipEncounterSettings_EncounterSelection);   

            float labelSize = 0.31f;

            float MARGIN_TOP = 0.15f;

            // Controls that will be automatically positioned
            parent.Controls.Add(maxNoShipsLabel);
            parent.Controls.Add(m_maxNoShipsPerSpawnGroup);

            parent.Controls.Add(hostileEncountersLabel);
            parent.Controls.Add(m_maxHostileEncountersPercentage);
            
            parent.Controls.Add(maxDamagedShipPercentageLabel);
            parent.Controls.Add(m_maxDamagedShipPercentage);

            parent.Controls.Add(maxDamagedShipsSeverityLabel);
            parent.Controls.Add(m_maxDamagedShipsSeverity);

            parent.Controls.Add(damageAppliedGloballyLabel);
            parent.Controls.Add(m_damageAppliedGlobally);

            parent.Controls.Add(reactorsOnPercentageLabel);
            parent.Controls.Add(m_reactorsOnPercentage);

            parent.Controls.Add(antennaActiveLabel);
            parent.Controls.Add(m_antennaOnPercentage);            

            parent.Controls.Add(antennaRangeMaxedLabel);
            parent.Controls.Add(m_antennaRangeMaxedOut);

            parent.Controls.Add(smallToLargeShipRatioLabel);
            parent.Controls.Add(m_smallToLargeShipRatio);

            parent.Controls.Add(allowArmedLargeShipsOnlyLabel);
            parent.Controls.Add(m_allowArmedLargeShipsOnly);
            
            // Automatic layout - position all controls added up to this point.
            Vector2 originL, originC;
            Vector2 controlsDelta = new Vector2(0f, 0.052f);

            originL = -m_size.Value / 2 + new Vector2(0.16f, MARGIN_TOP);
            originC = originL + new Vector2(labelSize, 0f);
            float rightColumnOffset = originC.X + maxNoShipsLabel.Size.X - labelSize - 0.017f; 

            foreach (var control in parent.Controls)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                if (control is MyGuiControlLabel)
                    control.Position = originL + controlsDelta * numControls;
                else
                    control.Position = originC + controlsDelta * numControls++;
            }                       
            
            // The following controls need to be positioned manually.
            presetLabel.Position = originL + controlsDelta + new Vector2(0.23f, -0.09f);
            Controls.Add(presetLabel);

            m_peaceModeButton.Position = originL  + controlsDelta + new Vector2(0.02f, -0.03f);
            Controls.Add(m_peaceModeButton);

            m_adventureModeButton.Position = originL + controlsDelta + new Vector2(0.16f, -0.03f);
            Controls.Add(m_adventureModeButton);

            m_warModeButton.Position = originL + controlsDelta + new Vector2(0.30f, -0.03f);
            Controls.Add(m_warModeButton);

            m_ruinsModeButton.Position = originL + controlsDelta + new Vector2(0.44f, -0.03f);
            Controls.Add(m_ruinsModeButton);

            m_randomModeButton.Position = originL + controlsDelta + new Vector2(0.58f, -0.03f);
            Controls.Add(m_randomModeButton);

            m_shipSelectorButton.Position = m_okButton.Position - new Vector2(0.4f, 0.027f);
            Controls.Add(m_shipSelectorButton);

            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            Controls.Add(scrollPanel);
            CloseButtonEnabled = true;
        }

        private void onAntennaOnValueChanged(MyGuiControlSlider obj)
        {
            if (obj.Value > 0.0f && m_AntennaOnLastValue == 0.0f)
            {              
                m_antennaRangeMaxedOut.Enabled = true;                
            }
            else
            {
                if (obj.Value == 0.0f)
                {
                   
                    m_antennaRangeMaxedOut.IsChecked = false;
                    m_antennaRangeMaxedOut.Enabled = false;
                }
            }

            m_AntennaOnLastValue = obj.Value;
        }

        private void onDamagedShipValueChanged(MyGuiControlSlider obj)
        {
            if (obj.Value > 0.0f && m_damageSliderLastValue == 0.0f)
            {
                m_maxDamagedShipsSeverity.SelectItemByIndex(1);
                m_maxDamagedShipsSeverity.Enabled = true;
                m_damageAppliedGlobally.Enabled = true;
                m_damageAppliedGlobally.IsChecked = false;                
            }
            else
            {
                if (obj.Value == 0.0f)
                {
                    m_maxDamagedShipsSeverity.SelectItemByIndex(0);
                    m_damageAppliedGlobally.IsChecked = true;

                    m_maxDamagedShipsSeverity.Enabled = false;
                    m_damageAppliedGlobally.Enabled = false;
                }
            }

            m_damageSliderLastValue = obj.Value;
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        private void LoadValues()
        {
            SetSettings(m_parent.Settings);
        }

        public void GetSettings(MyObjectBuilder_SessionSettings output)
        {
            output.MaxShipsInSpawnGroup = (short)m_maxNoShipsPerSpawnGroup.Value;
            output.MaxHostileEncountersPercentage = (int)m_maxHostileEncountersPercentage.Value;
            output.MaxDamagedShipsPercentage = (int)m_maxDamagedShipPercentage.Value;
            output.MaxDamagedShipsSeverity = (int)m_maxDamagedShipsSeverity.GetSelectedKey();
            output.DamageAppliedGlobally = (bool)m_damageAppliedGlobally.IsChecked;
            output.ReactorsOnPercentage = (int)m_reactorsOnPercentage.Value;
            output.AntennaOnPercentage = (int)m_antennaOnPercentage.Value;
            output.AntennaRangeMaxedOut = (bool)m_antennaRangeMaxedOut.IsChecked;
            output.SmallToLargeShipRatio = (int)m_smallToLargeShipRatio.Value;
            output.AllowArmedLargeShipsOnly = (bool)m_allowArmedLargeShipsOnly.IsChecked;

            output.ShipExcluded.Clear();

            foreach (var row in ShipsAvailableMaster)
            {
                var shipExclude = row.GetCell(0).Text.ToString() == MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_No);
                if (shipExclude)
                {
                    output.ShipExcluded.Add(row.UserData.ToString());
                }
            }
        }

        public void SetSettings(MyObjectBuilder_SessionSettings settings)
        {
            m_maxNoShipsPerSpawnGroup.Value = settings.MaxShipsInSpawnGroup;
            m_maxHostileEncountersPercentage.Value = settings.MaxHostileEncountersPercentage;
            m_maxDamagedShipPercentage.Value = settings.MaxDamagedShipsPercentage;
            m_maxDamagedShipsSeverity.SelectItemByIndex(settings.MaxDamagedShipsSeverity);
            m_damageAppliedGlobally.IsChecked = settings.DamageAppliedGlobally;
            m_reactorsOnPercentage.Value = settings.ReactorsOnPercentage;
            m_antennaOnPercentage.Value = settings.AntennaOnPercentage;
            m_antennaRangeMaxedOut.IsChecked = settings.AntennaRangeMaxedOut;
            m_smallToLargeShipRatio.Value = settings.SmallToLargeShipRatio;
            m_allowArmedLargeShipsOnly.IsChecked = settings.AllowArmedLargeShipsOnly;


            foreach (var excludedShip in settings.ShipExcluded)
            {
                foreach (var row in ShipsAvailableTemporary)
                {
                    if(row.UserData.ToString() == excludedShip.ToString())
                    {
                        row.GetCell(0).Text = new StringBuilder(MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_No));
                    }
                }
            }

            // Setup the controls correctly when damaged ships aren't wanted
            if (m_maxDamagedShipPercentage.Value == 0.0f)
            {
                m_maxDamagedShipsSeverity.SelectItemByIndex(0);
                m_damageAppliedGlobally.IsChecked = true;

                m_maxDamagedShipsSeverity.Enabled = false;
                m_damageAppliedGlobally.Enabled = false;
            }

            m_damageSliderLastValue = m_maxDamagedShipPercentage.Value;

            if (m_AntennaOnLastValue == 0.0f)
            {
                m_antennaRangeMaxedOut.IsChecked = false;
                m_antennaRangeMaxedOut.Enabled = false;
            }

            m_AntennaOnLastValue = m_antennaOnPercentage.Value;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenEncounterSettings";
        }

        private void PeaceButtonClicked(object sender)
        {
            m_maxNoShipsPerSpawnGroup.Value = 2;
            m_maxHostileEncountersPercentage.Value = 0;
            m_maxDamagedShipPercentage.Value = 0;
            m_maxDamagedShipsSeverity.SelectItemByIndex((int)MyDamageEnum.NO_DAMAGE);
            m_damageAppliedGlobally.IsChecked = false;
            m_reactorsOnPercentage.Value = 0;
            m_antennaOnPercentage.Value = 0;
            m_antennaRangeMaxedOut.IsChecked = false;
            m_smallToLargeShipRatio.Value = 50;
            m_allowArmedLargeShipsOnly.IsChecked = false;     
        }

        private void AdventureButtonClicked(object sender)
        {
            m_maxNoShipsPerSpawnGroup.Value = 3;
            m_maxHostileEncountersPercentage.Value = 50;
            m_maxDamagedShipPercentage.Value = 50;
            m_maxDamagedShipsSeverity.SelectItemByIndex((int)MyDamageEnum.DAMAGED);
            m_damageAppliedGlobally.IsChecked = false;
            m_reactorsOnPercentage.Value = 50;
            m_antennaOnPercentage.Value = 50;
            m_antennaRangeMaxedOut.IsChecked = false;
            m_smallToLargeShipRatio.Value = 50;
            m_allowArmedLargeShipsOnly.IsChecked = false;
        }

        private void WarButtonClicked(object sender)
        {
            m_maxNoShipsPerSpawnGroup.Value = 5;
            m_maxHostileEncountersPercentage.Value = 100;
            m_maxDamagedShipPercentage.Value = 90;
            m_maxDamagedShipsSeverity.SelectItemByIndex((int)MyDamageEnum.HEAVILY_DAMAGED);
            m_damageAppliedGlobally.IsChecked = false;
            m_reactorsOnPercentage.Value = 100;
            m_antennaOnPercentage.Value = 100;
            m_antennaRangeMaxedOut.IsChecked = true;
            m_smallToLargeShipRatio.Value = 90;
            m_allowArmedLargeShipsOnly.IsChecked = true;
        }

        private void RuinsButtonClicked(object sender)
        {
            m_maxNoShipsPerSpawnGroup.Value = 2;
            m_maxHostileEncountersPercentage.Value = 80;
            m_maxDamagedShipPercentage.Value = 100;
            m_maxDamagedShipsSeverity.SelectItemByIndex((int)MyDamageEnum.HEAVILY_DAMAGED);
            m_damageAppliedGlobally.IsChecked = true;
            m_reactorsOnPercentage.Value = 80;
            m_antennaOnPercentage.Value = 80;
            m_antennaRangeMaxedOut.IsChecked = false;
            m_smallToLargeShipRatio.Value = 50;
            m_allowArmedLargeShipsOnly.IsChecked = false;
        }

        private void RandomButtonClicked(object sender)
        {
            m_maxNoShipsPerSpawnGroup.Value = MyRandom.Instance.Next(1, 6); 
            m_maxHostileEncountersPercentage.Value = MyRandom.Instance.Next(0, 101);
            m_maxDamagedShipPercentage.Value = MyRandom.Instance.Next(0, 101);
            m_maxDamagedShipsSeverity.SelectItemByIndex(MyRandom.Instance.Next(0, 5));

            m_damageAppliedGlobally.IsChecked = MyRandom.Instance.Next(0, 2) == 1 ? true : false;
            m_reactorsOnPercentage.Value = MyRandom.Instance.Next(0, 101);
            m_antennaOnPercentage.Value = MyRandom.Instance.Next(0, 101);
            m_antennaRangeMaxedOut.IsChecked = MyRandom.Instance.Next(0, 2) == 1 ? true : false;

            m_smallToLargeShipRatio.Value = MyRandom.Instance.Next(0, 101); ;
            m_allowArmedLargeShipsOnly.IsChecked = MyRandom.Instance.Next(0, 2) == 1 ? true : false; 

            OnOkButtonClicked();
            CloseScreen();
        }

        private void ShipSelectorButtonClicked(object sender)
        {
            EncounterShipSelection = new MyGuiScreenEncounterShipSelector(m_parent, ShipsAvailableTemporary);
            EncounterShipSelection.OnOkButtonClicked += EncounterShipSelection_OnOkButtonClicked;

            MyGuiSandbox.AddScreen(EncounterShipSelection);
        }

        private void EncounterShipSelection_OnOkButtonClicked()
        {
            ShipsAvailableTemporary.Clear();

            EncounterShipSelection.GetAvailableShipsSettings(ShipsAvailableTemporary);
        }

        private void onArmedLargeShipsOnlyIsCheckedChanged(MyGuiControlCheckbox sender)
        {
            if (sender.IsChecked)
            {
                ShipsAvailableHolderForArmedLargeShips.Clear();

                foreach (var row in ShipsAvailableTemporary)
                {
                    var newRow = new MyGuiControlTable.Row(row.UserData);
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                    ShipsAvailableHolderForArmedLargeShips.Add(newRow);

                    if ((row.GetCell(2).Text.ToString() == MyTexts.GetString(MySpaceTexts.WorldSettings_GridLargeShipType)) && (int.Parse(row.GetCell(4).Text.ToString()) == 0))
                    {
                        row.GetCell(0).Text = new StringBuilder(MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_No));
                    }
                }
            }
            else
            {
                ShipsAvailableTemporary.Clear();

                foreach (var row in ShipsAvailableHolderForArmedLargeShips)
                {
                    var newRow = new MyGuiControlTable.Row(row.UserData);
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                    ShipsAvailableTemporary.Add(newRow);
                }
            }
        }

        private void GetAvailableShips()
        {
            MyDefinitionManager.Static.UnloadData();

            var mods = new List<MyObjectBuilder_Checkpoint.ModItem>(0);

            MyDefinitionManager.Static.LoadDefinitionsOnly(mods);

            var allSpawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();

            foreach (var spawnGroup in allSpawnGroups)
            {
                var matchesSelectionFilter = true;

                if (spawnGroup.IsEncounter)
                {                   
                        foreach (var prefab in spawnGroup.Prefabs)
                        {
                            var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId);

                            List<MyPrefabProfileDefinition> encounterProfile = MyDefinitionManager.Static.GetEncounterProfiles(prefabDefinition.PrefabPath);                              
                            
                            var firstPrefab = encounterProfile[0];

                            var turrets = 0;

                            var interiorTurrets = 0;
                            var gatlingTurrets = 0;
                            var missileTurrets = 0;

                            var blockToolTip = new StringBuilder();

                            foreach(var blockType in firstPrefab.BlocksTypes)
                            {
                                var blockTypeName = blockType.Key.ToLower();

                                var readableBlockTypeName = blockType.Key.Substring(16, blockType.Key.Length - 16);

                                if (blockTypeName.Contains("turret"))
                                {
                                    turrets += blockType.Value;

                                    if (blockTypeName.Contains("missile"))
                                    {
                                        missileTurrets += blockType.Value;
                                    }

                                    if (blockTypeName.Contains("interior"))
                                    {
                                        interiorTurrets += blockType.Value;
                                    }

                                    if (blockTypeName.Contains("gatling"))
                                    {
                                        gatlingTurrets += blockType.Value;
                                    }
                                }

                                blockToolTip.Append(string.Format("{0}: {1} \n", BreakUpName(readableBlockTypeName), blockType.Value));
                            }

                            var gridSize = "";
                            var gridSizeToolTip = "";

                            if (spawnGroup.Voxels.Count != 0)
                            {
                                gridSize = MyTexts.GetString(MySpaceTexts.WorldSettings_GridBaseType);
                                gridSizeToolTip = MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_AsteroidBaseLabel);
                            }
                            else
                            {
                                if (firstPrefab.GridSize.ToLower() == "large")
                                {
                                    gridSize = MyTexts.GetString(MySpaceTexts.WorldSettings_GridLargeShipType);
                                    gridSizeToolTip = MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_LargeShipLabel);
                                }
                                else
                                {
                                    gridSize = MyTexts.GetString(MySpaceTexts.WorldSettings_GridSmallShipType);
                                    gridSizeToolTip = MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_SmallShipLabel);
                                }
                            }

                            var turretToolTip = new StringBuilder();
                            turretToolTip.Append(string.Format("{0}: {1} \n", MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_InteriorTurretLabel), interiorTurrets));
                            turretToolTip.Append(string.Format("{0}: {1} \n", MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_GatlingTurretLabel), gatlingTurrets));
                            turretToolTip.Append(string.Format("{0}: {1}", MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_MissileTurretLabel), missileTurrets));

                            if (matchesSelectionFilter)
                            {
                                var row = new MyGuiControlTable.Row(prefab.SubtypeId);
                                row.AddCell(new MyGuiControlTable.Cell(text: MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_Yes), toolTip: MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_TableActiveColumn)));
                                row.AddCell(new MyGuiControlTable.Cell(text: prefab.SubtypeId.Replace("_", " "), toolTip: MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_TableNameColumn)));
                                row.AddCell(new MyGuiControlTable.Cell(text: gridSize, toolTip: gridSizeToolTip));
                                row.AddCell(new MyGuiControlTable.Cell(text: firstPrefab.BlocksCount.ToString(), toolTip: blockToolTip.ToString()));
                                row.AddCell(new MyGuiControlTable.Cell(text: turrets.ToString(), toolTip: turretToolTip.ToString()));

                                ShipsAvailableMaster.Add(row);

                                var rowtemp = new MyGuiControlTable.Row(prefab.SubtypeId);
                                row.AddCell(new MyGuiControlTable.Cell(text: MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_Yes), toolTip: MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_TableActiveColumn)));
                                row.AddCell(new MyGuiControlTable.Cell(text: prefab.SubtypeId.Replace("_", " "), toolTip: MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettings_TableNameColumn)));
                                row.AddCell(new MyGuiControlTable.Cell(text: gridSize, toolTip: gridSizeToolTip));
                                row.AddCell(new MyGuiControlTable.Cell(text: firstPrefab.BlocksCount.ToString(), toolTip: blockToolTip.ToString()));
                                row.AddCell(new MyGuiControlTable.Cell(text: turrets.ToString(), toolTip: turretToolTip.ToString()));

                                ShipsAvailableTemporary.Add(row);
                            }
                        }
                }
            }

            MyDefinitionManager.Static.UnloadData();
        }

        private string BreakUpName(string inputString)
        {
            var result = "";
            var firstUpperFound = false;

            foreach(char character in inputString)
            {
                if(char.IsUpper(character))
                {
                    if(firstUpperFound)
                    {
                        result += " ";                        
                    }
                    else
                    {
                        firstUpperFound = true;
                    }

                    result += character;
                }
                else
                {
                    result += character;
                }
            }

            return result;
        }

        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_selectedRow = sender.SelectedRow;

            if (!(m_selectedRow == null))
            {
                if (m_selectedRow.GetCell(0).Text.ToString() == MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_Yes))
                {
                    m_selectedRow.GetCell(0).Text = new StringBuilder(MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_No));
                }
                else
                {
                    m_selectedRow.GetCell(0).Text = new StringBuilder(MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_Yes));
                }
            }
        }

        private void CancelButtonClicked(object sender)
        {
            this.CloseScreen();
        }

        private void OkButtonClicked(object sender)
        {
            var totalYes = 0;

            foreach (var row in ShipsAvailableTemporary)
            {
                if (row.GetCell(0).Text.ToString() == new StringBuilder(MyTexts.GetString(MySpaceTexts.WorldSettings_Encounter_Yes)).ToString())
                {
                    totalYes++;
                }
            }

            if (totalYes > 0)
            {
                ShipsAvailableMaster.Clear();

                foreach (var row in ShipsAvailableTemporary)
                {
                    var newRow = new MyGuiControlTable.Row(row.UserData);
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                    ShipsAvailableMaster.Add(newRow);
                }

                m_isConfirmed = true;

                if (OnOkButtonClicked != null)
                {
                    OnOkButtonClicked();
                }

                this.CloseScreen();
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    buttonType: MyMessageBoxButtonsType.OK,
                                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextNotEnoughEncounterShipsSelected),
                                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionNotEnoughShips)
                                    ));
            }
        }
        
        public event System.Action OnOkButtonClicked;
    }
}
