
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
    class MyGuiScreenEncounterSettings : MyGuiScreenBase
    {
        MyGuiScreenWorldSettings m_parent;
        bool m_isNewGame;

        bool m_isConfirmed;

        MyGuiControlButton m_okButton, m_cancelButton, m_RandomSettingsButton, m_BalancedSettingsButton;
        MyGuiControlSlider m_maxNoShipsPerSpawnGroup, m_maxDamagedShipPercentage, m_maxHostileEncountersPercentage, m_AntennaOnPercentage, m_ReactorsOnPercentage;
        MyGuiControlCombobox m_maxDamagedShipsSeverity;
        MyGuiControlCheckbox m_antennaRangeMaxedOut, m_damageAppliedGlobally;

        internal MyGuiScreenEncounterSettings EncounterConfiguration;        

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

            m_isNewGame = (parent.Checkpoint == null);
            m_isConfirmed = false;

            RecreateControls(true);

            MySandboxGame.Log.WriteLine("MyGuiScreenEncounterSettings.ctor END");
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
        }

        public void BuildControls()
        {
            MyGuiControlParent parent = new MyGuiControlParent(size: new Vector2(Size.Value.X - 0.05f, Size.Value.Y));
            MyGuiControlScrollablePanel scrollPanel = new MyGuiControlScrollablePanel(parent);
            scrollPanel.ScrollbarVEnabled = true;
            scrollPanel.Size = new Vector2(Size.Value.X - 0.05f, 0.8f);

            Vector2 buttonSize = MyGuiConstants.BACK_BUTTON_SIZE;
            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.23f, 0.03f);

            AddCaption(MySpaceTexts.ScreenCaptionEncountersConfiguration);

            int numControls = 0;           

            float width = 0.284375f + 0.025f;

            var maxNoShipsLabel = MakeLabel(MySpaceTexts.WorldSettings_MaxNoShipsPerSpawnGroup);
            var maxDamagedShipPercentageLabel = MakeLabel(MySpaceTexts.WorldSettings_DamagedEncounterLabel);
            var maxDamagedShipsSeverityLabel = MakeLabel(MySpaceTexts.WorldSettings_MaxDamagedShipsSeverity);
            var hostileEncountersLabel = MakeLabel(MySpaceTexts.WorldSettings_HostileEncountersLabel);
            var AntennaActiveLabel = MakeLabel(MySpaceTexts.WorldSettings_AntennaActiveLabel);
            var AntennaRangeMaxedLabel = MakeLabel(MySpaceTexts.WorldSettings_AntennaMaxedLabel);
            var ReactorsOnPercentageLabel = MakeLabel(MySpaceTexts.WorldSettings_ReactorsOnLabel);
            var DamageAppliedGloballyLabel = MakeLabel(MySpaceTexts.WorldSettings_DamageAppliedGloballyLabel);

            maxNoShipsLabel.Position = Vector2.Zero - new Vector2(0.3f, 0.4f);                 

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

            m_maxDamagedShipsSeverity = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));
            m_maxDamagedShipsSeverity.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsDamagedShipSeverity));

            m_maxDamagedShipsSeverity.AddItem(0, MySpaceTexts.WorldSettings_NoDamage);
            m_maxDamagedShipsSeverity.AddItem(1, MySpaceTexts.WorldSettings_Worn);
            m_maxDamagedShipsSeverity.AddItem(2, MySpaceTexts.WorldSettings_LightlyDamaged);
            m_maxDamagedShipsSeverity.AddItem(3, MySpaceTexts.WorldSettings_Damaged);
            m_maxDamagedShipsSeverity.AddItem(4, MySpaceTexts.WorldSettings_HeavilyDamaged);

            m_ReactorsOnPercentage = new MyGuiControlSlider(
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

            m_ReactorsOnPercentage.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsReactorsOn));

            m_AntennaOnPercentage = new MyGuiControlSlider(
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

            m_AntennaOnPercentage.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasOn));

            m_antennaRangeMaxedOut = new MyGuiControlCheckbox();
            m_antennaRangeMaxedOut.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));


            m_damageAppliedGlobally = new MyGuiControlCheckbox();
            m_damageAppliedGlobally.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsDamageAppliedGlobally));
            
            // Ok/Cancel
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: CancelButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

            float labelSize = 0.31f; // 0.21f;

            float MARGIN_TOP = 0.03f;

            parent.Controls.Add(maxNoShipsLabel);
            parent.Controls.Add(m_maxNoShipsPerSpawnGroup);

            parent.Controls.Add(hostileEncountersLabel);
            parent.Controls.Add(m_maxHostileEncountersPercentage);
            
            parent.Controls.Add(maxDamagedShipPercentageLabel);
            parent.Controls.Add(m_maxDamagedShipPercentage);

            parent.Controls.Add(maxDamagedShipsSeverityLabel);
            parent.Controls.Add(m_maxDamagedShipsSeverity);

            parent.Controls.Add(DamageAppliedGloballyLabel);
            parent.Controls.Add(m_damageAppliedGlobally);

            parent.Controls.Add(ReactorsOnPercentageLabel);
            parent.Controls.Add(m_ReactorsOnPercentage);

            parent.Controls.Add(AntennaActiveLabel);
            parent.Controls.Add(m_AntennaOnPercentage);            

            parent.Controls.Add(AntennaRangeMaxedLabel);
            parent.Controls.Add(m_antennaRangeMaxedOut);

            // Automatic layout.
            Vector2 originL, originC;
            Vector2 controlsDelta = new Vector2(0f, 0.052f);

            originL = -m_size.Value / 2 + new Vector2(0.16f, MARGIN_TOP);
            originC = originL + new Vector2(labelSize, 0f);
            float rightColumnOffset = originC.X + maxNoShipsLabel.Size.X - labelSize - 0.017f; // 0.017f;

            foreach (var control in parent.Controls)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                if (control is MyGuiControlLabel)
                    control.Position = originL + controlsDelta * numControls;
                else
                    control.Position = originC + controlsDelta * numControls++;
            }               

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

        public void GetSettings(MyObjectBuilder_SessionSettings output)
        {
            output.MaxShipsInSpawnGroup = (short)m_maxNoShipsPerSpawnGroup.Value;
            output.MaxHostileEncountersPercentage = (int)m_maxHostileEncountersPercentage.Value;
            output.MaxDamagedShipsPercentage = (int)m_maxDamagedShipPercentage.Value;
            output.MaxDamagedShipsSeverity = (int)m_maxDamagedShipsSeverity.GetSelectedKey();
            output.AntennaOnPercentage = (int)m_AntennaOnPercentage.Value;
            output.AntennaRangeMaxedOut = (bool)m_antennaRangeMaxedOut.IsChecked;
            output.ReactorsOnPercentage = (int)m_ReactorsOnPercentage.Value;
            output.DamageAppliedGlobally = (bool)m_damageAppliedGlobally.IsChecked;
        }

        public void SetSettings(MyObjectBuilder_SessionSettings settings)
        {
            m_maxNoShipsPerSpawnGroup.Value = settings.MaxShipsInSpawnGroup;
            m_maxDamagedShipPercentage.Value = settings.MaxDamagedShipsPercentage;
            m_maxDamagedShipsSeverity.SelectItemByIndex(settings.MaxDamagedShipsSeverity);
            m_maxHostileEncountersPercentage.Value = settings.MaxHostileEncountersPercentage;
            m_AntennaOnPercentage.Value = settings.AntennaOnPercentage;
            m_antennaRangeMaxedOut.IsChecked = settings.AntennaRangeMaxedOut;
            m_ReactorsOnPercentage.Value = settings.ReactorsOnPercentage;
            m_damageAppliedGlobally.IsChecked = settings.DamageAppliedGlobally;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenEncounterSettings";
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
        
        public event System.Action OnOkButtonClicked;
    }
}
