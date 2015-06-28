
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
    class MyGuiScreenEncounterShipSelector : MyGuiScreenBase
    { 
        MyGuiScreenWorldSettings m_parent;
        bool m_isNewGame;
        bool m_isConfirmed;

        MyGuiControlButton m_okButton, m_cancelButton, m_applyFilterButton;
        MyGuiControlSlider m_maxBlocks, m_maxTurrets;
        MyGuiControlCheckbox m_includeLargeShips, m_includeSmallShips, m_includeBases, m_excludeLargeShips, m_excludeSmallShips, m_excludeBases;

        MyGuiControlTable m_ShipsAvailableTable;

        private MyGuiControlTable.Row m_selectedRow;

        public List<MyGuiControlTable.Row> m_shipsAvailableMaster = new List<MyGuiControlTable.Row>();
        public List<MyGuiControlTable.Row> m_shipsAvailableTemporary = new List<MyGuiControlTable.Row>();

        public bool IsConfirmed
        {
            get
            {
                return m_isConfirmed;
            }
        }

        public MyGuiScreenEncounterShipSelector(MyGuiScreenWorldSettings parent, List<MyGuiControlTable.Row> shipsAvailable)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, CalcSize(parent.Checkpoint))
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenEncounterShipSelector.ctor START");

            m_parent = parent;
            EnabledBackgroundFade = true;

            // TODO: May need to review this as I'm not sure I understand the checkpoint system well enough.
            m_isNewGame = (parent.Checkpoint == null);
            m_isConfirmed = false;

            foreach (var row in shipsAvailable)
            {
                var newRow = new MyGuiControlTable.Row(row.UserData);
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                m_shipsAvailableMaster.Add(newRow);
            }

            foreach (var row in m_shipsAvailableMaster)
            {
                var newRow = new MyGuiControlTable.Row(row.UserData);
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                m_shipsAvailableTemporary.Add(newRow);
            }
            
            RecreateControls(true);

            fillShipsAvailableTable();

            MySandboxGame.Log.WriteLine("MyGuiScreenEncounterShipSelector.ctor END");
        }

        private void fillShipsAvailableTable()
        {
            foreach (var row in m_shipsAvailableMaster)
            {
                var newRow = new MyGuiControlTable.Row(row.UserData);
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                m_ShipsAvailableTable.Add(newRow);
            }
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

            AddCaption(MySpaceTexts.ScreenCaptionEncounterShipSelection);

            int numControls = 0;

            // Labels
            float severityComboBoxWidth = 0.2f;
            var maxBlocksLabel = MakeLabel(MySpaceTexts.WorldSettings_MaxBlocksLabel);
            var maxTurretsLabel = MakeLabel(MySpaceTexts.WorldSettings_MaxTurretsLabel);
            var EncounterTypesAllowedLabel = MakeLabel(MySpaceTexts.WorldSettings_EncounterTypesAllowedLabel);
            var LargeShipLabel = MakeLabel(MySpaceTexts.WorldSettings_EncounterLargeShipLabel);
            var SmallShipLabel = MakeLabel(MySpaceTexts.WorldSettings_EncounterSmallShipLabel);
            var BaseShipLabel = MakeLabel(MySpaceTexts.WorldSettings_EncounterBaseShipLabel);
            var IncludeLabel = MakeLabel(MySpaceTexts.WorldSettings_EncounterIncludeLabel);
            var ExcludeLabel = MakeLabel(MySpaceTexts.WorldSettings_EncounterExcludeLabel);
            var FilterLabel = MakeLabel(MySpaceTexts.WorldSettings_EncounterFilterLabel);
            
            // Setup settings controls            
            m_applyFilterButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), visualStyle: MyGuiControlButtonStyleEnum.Small, text: MyTexts.Get(MySpaceTexts.WorldSettings_EncounterApplyButton), onButtonClick: applyFiltersButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

            m_includeLargeShips = new MyGuiControlCheckbox();
            m_includeLargeShips.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));
            m_includeLargeShips.IsCheckedChanged += onIncludeLargeShipsIsCheckedChanged;

            m_includeSmallShips = new MyGuiControlCheckbox();
            m_includeSmallShips.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));
            m_includeSmallShips.IsCheckedChanged += onincludeSmallShipsIsCheckedChanged;

            m_includeBases = new MyGuiControlCheckbox();
            m_includeBases.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));
            m_includeBases.IsCheckedChanged += onincludeBasesIsCheckedChanged;

            m_excludeLargeShips = new MyGuiControlCheckbox();
            m_excludeLargeShips.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));
            m_excludeLargeShips.IsCheckedChanged += onexcludeLargeShipsIsCheckedChanged;

            m_excludeSmallShips = new MyGuiControlCheckbox();
            m_excludeSmallShips.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));
            m_excludeSmallShips.IsCheckedChanged += onexcludeSmallShipsIsCheckedChanged;

            m_excludeBases = new MyGuiControlCheckbox();
            m_excludeBases.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipEncounterSettingsAntennasRangeMaxed));
            m_excludeBases.IsCheckedChanged += onexcludeBasesIsCheckedChanged;

            var maxBlocks = 0;
            var minBlocks = 9999999;

            foreach (var row in m_shipsAvailableMaster)
            {
                var thisShipsBlockCount = int.Parse(row.GetCell(3).Text.ToString());
                if (thisShipsBlockCount > maxBlocks)
                {
                    maxBlocks = thisShipsBlockCount;
                }

                if (thisShipsBlockCount < minBlocks)
                {
                    minBlocks = thisShipsBlockCount;
                }
            }

            m_maxBlocks = new MyGuiControlSlider(
               position: Vector2.Zero - new Vector2(-0.1f, 0.3f),
               width: 0.2f,
               minValue: minBlocks,
               maxValue: maxBlocks,
               labelText: new StringBuilder("{0}").ToString(),
               labelDecimalPlaces: 0,
               labelSpaceWidth: 0.05f,
               intValue: true,
               defaultValue: maxBlocks
               );

            var maxTurrets = 0;
            foreach (var row in m_shipsAvailableMaster)
            {
                var thisShipsTurretCount = int.Parse(row.GetCell(4).Text.ToString());
                if (thisShipsTurretCount > maxTurrets)
                {
                    maxTurrets = thisShipsTurretCount;
                }
            }

            m_maxTurrets = new MyGuiControlSlider(
               position: Vector2.Zero - new Vector2(-0.1f, 0.3f),
               width: 0.2f,
               minValue: 0,
               maxValue: maxTurrets,
               labelText: new StringBuilder("{0}").ToString(),
               labelDecimalPlaces: 0,
               labelSpaceWidth: 0.05f,
               intValue: true,
               defaultValue: maxTurrets
               );

            // Ok-Cancel Buttons
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: CancelButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            
            m_ShipsAvailableTable = new MyGuiControlTable();
            m_ShipsAvailableTable.Position = Vector2.Zero - new Vector2(-0.0f, 0.0f);
            m_ShipsAvailableTable.VisibleRowsCount = 12;            
            m_ShipsAvailableTable.Size = new Vector2(m_size.Value.X * 0.7f, 1.25f);
            m_ShipsAvailableTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_ShipsAvailableTable.ColumnsCount = 5;

            m_ShipsAvailableTable.ItemSelected += OnTableItemSelected;            
            m_ShipsAvailableTable.SetColumnName(0, new StringBuilder("Active"));
            m_ShipsAvailableTable.SetColumnName(1, new StringBuilder("Name"));
            m_ShipsAvailableTable.SetColumnName(2, new StringBuilder("Size"));
            m_ShipsAvailableTable.SetColumnName(3, new StringBuilder("Blocks"));
            m_ShipsAvailableTable.SetColumnName(4, new StringBuilder("Turrets"));

            m_ShipsAvailableTable.SetCustomColumnWidths(new float[] { 0.1f, 0.55f, 0.1f, 0.15f, 0.15f });            
            m_ShipsAvailableTable.SetColumnComparison(0, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            m_ShipsAvailableTable.SetColumnComparison(1, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            m_ShipsAvailableTable.SetColumnComparison(2, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            m_ShipsAvailableTable.SetColumnComparison(3, (a, b) => int.Parse(a.Text.ToString()).CompareTo(int.Parse(b.Text.ToString())));
            m_ShipsAvailableTable.SetColumnComparison(4, (a, b) => int.Parse(a.Text.ToString()).CompareTo(int.Parse(b.Text.ToString())));            

            float labelSize = 0.31f;

            float MARGIN_TOP = 0.05f;

            // Controls that will be automatically positioned
            
            
            parent.Controls.Add(m_includeLargeShips);
            parent.Controls.Add(m_includeSmallShips);
            parent.Controls.Add(m_includeBases);

            parent.Controls.Add(maxBlocksLabel);
            parent.Controls.Add(m_maxBlocks);

            parent.Controls.Add(maxTurretsLabel);
            parent.Controls.Add(m_maxTurrets);

            // Automatic layout - position all controls added up to this point.
            Vector2 originL, originC;
            Vector2 controlsDelta = new Vector2(0f, 0.052f);

            originL = -m_size.Value / 2 + new Vector2(0.16f, MARGIN_TOP);
            originC = originL + new Vector2(labelSize, 0f);
            float rightColumnOffset = originC.X + m_maxTurrets.Size.X - labelSize - 0.017f; 

            foreach (var control in parent.Controls)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                if (control is MyGuiControlLabel)
                    control.Position = originL + controlsDelta * numControls;
                else
                    control.Position = originC + controlsDelta * numControls++;
            }

            m_includeLargeShips.Position = m_includeLargeShips.Position + new Vector2(0.025f, 0.0f);
            m_includeSmallShips.Position = m_includeSmallShips.Position + new Vector2(0.025f, 0.0f);
            m_includeBases.Position = m_includeBases.Position + new Vector2(0.025f, 0.0f);

            EncounterTypesAllowedLabel.Position = originL - new Vector2(0f, 0.034f);
            parent.Controls.Add(EncounterTypesAllowedLabel);

            m_excludeLargeShips.Position = m_includeLargeShips.Position + new Vector2(0.09f, 0.0f);
            m_excludeSmallShips.Position = m_includeSmallShips.Position + new Vector2(0.09f, 0.0f);
            m_excludeBases.Position = m_includeBases.Position + new Vector2(0.09f, 0.0f);

            IncludeLabel.Position = new Vector2(m_includeLargeShips.Position.X - 0.01f, EncounterTypesAllowedLabel.Position.Y);
            parent.Controls.Add(IncludeLabel);
            ExcludeLabel.Position = new Vector2(m_excludeLargeShips.Position.X - 0.03f, EncounterTypesAllowedLabel.Position.Y);
            parent.Controls.Add(ExcludeLabel);

            parent.Controls.Add(m_excludeLargeShips);
            parent.Controls.Add(m_excludeSmallShips);
            parent.Controls.Add(m_excludeBases);

            LargeShipLabel.Position = m_includeLargeShips.Position - new Vector2(0.3f, 0.0f);
            SmallShipLabel.Position = m_includeSmallShips.Position - new Vector2(0.3f, 0.0f);
            BaseShipLabel.Position = m_includeBases.Position - new Vector2(0.3f, 0.0f);

            parent.Controls.Add(LargeShipLabel);
            parent.Controls.Add(SmallShipLabel);
            parent.Controls.Add(BaseShipLabel);

            m_ShipsAvailableTable.Position = originL + controlsDelta * numControls;
            parent.Controls.Add(m_ShipsAvailableTable);

            m_applyFilterButton.Position = m_excludeLargeShips.Position + new Vector2(0.22f, 0.02f);            
            parent.Controls.Add(m_applyFilterButton);

            // The following controls need to be positioned manually.                  
            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);

            Controls.Add(scrollPanel);
            CloseButtonEnabled = true;
        }

        private void onexcludeBasesIsCheckedChanged(MyGuiControlCheckbox obj)
        {
            if (obj.IsChecked)
            {
                m_includeBases.IsChecked = false;
            }
        }

        private void onexcludeSmallShipsIsCheckedChanged(MyGuiControlCheckbox obj)
        {
            if (obj.IsChecked)
            {
                m_includeSmallShips.IsChecked = false;
            }
        }

        private void onexcludeLargeShipsIsCheckedChanged(MyGuiControlCheckbox obj)
        {
            if (obj.IsChecked)
            {
                m_includeLargeShips.IsChecked = false;
            }
        }

        private void onincludeBasesIsCheckedChanged(MyGuiControlCheckbox obj)
        {
            if (obj.IsChecked)
            {
                m_excludeBases.IsChecked = false;
            }
        }

        private void onincludeSmallShipsIsCheckedChanged(MyGuiControlCheckbox obj)
        {
            if (obj.IsChecked)
            {
                m_excludeSmallShips.IsChecked = false;
            }
        }

        private void onIncludeLargeShipsIsCheckedChanged(MyGuiControlCheckbox obj)
        {
            if (obj.IsChecked)
            {
                m_excludeLargeShips.IsChecked = false;
            }
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
        }

        public void GetAvailableShipsSettings(List<MyGuiControlTable.Row> output)
        {
            foreach (var row in m_shipsAvailableMaster)
            {
                var newRow = new MyGuiControlTable.Row(row.UserData);
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                output.Add(newRow);
            }
        }

        public void SetSettings(MyObjectBuilder_SessionSettings settings)
        {               
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenEncounterShipSelector";
        }

        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            m_selectedRow = sender.SelectedRow;

            if (!(m_selectedRow == null))
            {
                if (m_selectedRow.GetCell(0).Text.ToString() == "Yes")
                {
                    m_selectedRow.GetCell(0).Text = new StringBuilder("No");
                }
                else
                {
                    m_selectedRow.GetCell(0).Text = new StringBuilder("Yes");
                }

                foreach (var row in m_shipsAvailableTemporary)
                {
                    if (row.UserData == m_selectedRow.UserData)
                    {
                        row.GetCell(0).Text = m_selectedRow.GetCell(0).Text;
                    }
                }
            }


        }

        private void applyFiltersButtonClicked(object sender)
        {            
           m_ShipsAvailableTable.Clear();

            foreach (var row in m_shipsAvailableTemporary)
            {
                if (row.GetCell(2).Text.ToString() == "Large")
                {
                    if (m_includeLargeShips.IsChecked)
                    {
                        row.GetCell(0).Text = new StringBuilder("Yes");
                    }

                    if (m_excludeLargeShips.IsChecked)
                    {
                        row.GetCell(0).Text = new StringBuilder("No");
                    }
                }

                if (row.GetCell(2).Text.ToString() == "Small")
                {
                    if (m_includeSmallShips.IsChecked)
                    {
                        row.GetCell(0).Text = new StringBuilder("Yes");
                    }

                    if (m_excludeSmallShips.IsChecked)
                    {
                        row.GetCell(0).Text = new StringBuilder("No");
                    }
                }

                if (row.GetCell(2).Text.ToString() == "Base")
                {
                    if (m_includeBases.IsChecked)
                    {
                        row.GetCell(0).Text = new StringBuilder("Yes");
                    }

                    if (m_excludeBases.IsChecked)
                    {
                        row.GetCell(0).Text = new StringBuilder("No");
                    }
                }

                var currentShipBlocks = int.Parse(row.GetCell(3).Text.ToString());

                if (currentShipBlocks > m_maxBlocks.Value)
                {
                    row.GetCell(0).Text = new StringBuilder("No");
                }

                var currentTurrentBlocks = int.Parse(row.GetCell(4).Text.ToString());

                if (currentTurrentBlocks > m_maxTurrets.Value)
                {
                    row.GetCell(0).Text = new StringBuilder("No");
                }

                m_ShipsAvailableTable.Add(row);
            }            
        }

        private void CancelButtonClicked(object sender)
        {
            this.CloseScreen();
        }

        private void OkButtonClicked(object sender)
        {
            var totalYes = 0;

            foreach (var row in m_shipsAvailableTemporary)
            {
                if (row.GetCell(0).Text.ToString() == new StringBuilder("Yes").ToString())
                {
                    totalYes++;
                }
            }

            if (totalYes > 0)
            {
                m_shipsAvailableMaster.Clear();

                foreach (var row in m_shipsAvailableTemporary)
                {
                    var newRow = new MyGuiControlTable.Row(row.UserData);
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(0).Text, toolTip: row.GetCell(0).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(1).Text, toolTip: row.GetCell(1).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(2).Text, toolTip: row.GetCell(2).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(3).Text, toolTip: row.GetCell(3).ToolTip.ToolTips[0].Text.ToString()));
                    newRow.AddCell(new MyGuiControlTable.Cell(text: row.GetCell(4).Text, toolTip: row.GetCell(4).ToolTip.ToolTips[0].Text.ToString()));
                    m_shipsAvailableMaster.Add(newRow);
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
