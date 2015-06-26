
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

        MyGuiControlButton m_okButton, m_cancelButton, m_peaceModeButton, m_adventureModeButton, m_warModeButton, m_ruinsModeButton, m_randomModeButton, m_viewShipsButton;
        MyGuiControlSlider m_maxNoShipsPerSpawnGroup, m_maxDamagedShipPercentage, m_maxHostileEncountersPercentage, m_antennaOnPercentage, m_reactorsOnPercentage;
        MyGuiControlCombobox m_maxDamagedShipsSeverity;
        MyGuiControlCheckbox m_antennaRangeMaxedOut, m_damageAppliedGlobally;
        MyGuiControlTable m_ShipsAvailable;

        private MyGuiControlTable.Row m_selectedRow;

        public List<MyGuiControlTable.Row> m_shipsAvailable;

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

            m_shipsAvailable = shipsAvailable;

            RecreateControls(true);

            fillShipsAvailableTable();

            MySandboxGame.Log.WriteLine("MyGuiScreenEncounterShipSelector.ctor END");
        }

        private void fillShipsAvailableTable()
        {
            foreach (var row in m_shipsAvailable)
            {
                m_ShipsAvailable.Add(row);
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
            var maxNoShipsLabel = MakeLabel(MySpaceTexts.WorldSettings_MaxNoShipsPerSpawnGroup);
            
            // Setup settings controls
                               
            
            // Ok-Cancel Buttons
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: buttonSize, text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: CancelButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            
            m_ShipsAvailable = new MyGuiControlTable();
            m_ShipsAvailable.Position = Vector2.Zero - new Vector2(-0.0f, 0.0f);
            m_ShipsAvailable.VisibleRowsCount = 15;
            // m_ShipsAvailable.Size = new Vector2(m_size.Value.X * 0.4375f, 1.25f);
            m_ShipsAvailable.Size = new Vector2(m_size.Value.X * 0.7f, 1.25f);
            m_ShipsAvailable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_ShipsAvailable.ColumnsCount = 5;

            m_ShipsAvailable.ItemSelected += OnTableItemSelected;
            // m_ShipsAvailable.ItemDoubleClicked += OnTableItemConfirmedOrDoubleClick;
            // m_ShipsAvailable.ItemConfirmed += OnTableItemConfirmedOrDoubleClick;
            m_ShipsAvailable.SetColumnName(0, new StringBuilder("Active"));
            m_ShipsAvailable.SetColumnName(1, new StringBuilder("Name"));
            m_ShipsAvailable.SetColumnName(2, new StringBuilder("Size"));
            m_ShipsAvailable.SetColumnName(3, new StringBuilder("Blocks"));
            m_ShipsAvailable.SetColumnName(4, new StringBuilder("Turrets"));

            m_ShipsAvailable.SetCustomColumnWidths(new float[] { 0.1f, 0.55f, 0.1f, 0.15f, 0.15f });            
            m_ShipsAvailable.SetColumnComparison(0, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            m_ShipsAvailable.SetColumnComparison(1, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            m_ShipsAvailable.SetColumnComparison(2, (a, b) => (a.Text).CompareToIgnoreCase(b.Text));
            m_ShipsAvailable.SetColumnComparison(3, (a, b) => int.Parse(a.Text.ToString()).CompareTo(int.Parse(b.Text.ToString())));
            m_ShipsAvailable.SetColumnComparison(4, (a, b) => int.Parse(a.Text.ToString()).CompareTo(int.Parse(b.Text.ToString())));            

            float labelSize = 0.31f;

            float MARGIN_TOP = 0.15f;

            // Controls that will be automatically positioned
              
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

            m_ShipsAvailable.Position = originL + controlsDelta * numControls;
            parent.Controls.Add(m_ShipsAvailable);

            // The following controls need to be positioned manually.
            //presetLabel.Position = originL + controlsDelta + new Vector2(0.23f, -0.09f);
            //Controls.Add(presetLabel);          

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

        public void GetSettings(MyObjectBuilder_SessionSettings output)
        {
            for (var counter = 0; counter < m_ShipsAvailable.RowsCount; counter++ )
            {
                var shipExclude = m_ShipsAvailable.GetRow(counter).GetCell(0).Text.ToString() == "No";
                if (shipExclude)
                {
                    output.ShipExcluded.Add(m_ShipsAvailable.GetRow(counter).UserData.ToString());
                }
            }
        }

        public void SetSettings(MyObjectBuilder_SessionSettings settings)
        {               
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenEncounterShipSelector";
        }

        private void ViewShipsClicked(object sender)
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
                    //if (spawnGroup.Voxels.Count == 0)
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
                                gridSize = "Base";
                                gridSizeToolTip = "Asteroid Base";
                            }
                            else
                            {
                                if (firstPrefab.GridSize.ToLower() == "large")
                                {
                                    gridSize = "Large";
                                    gridSizeToolTip = "Large Ship";
                                }
                                else
                                {
                                    gridSize = "Small";
                                    gridSizeToolTip = "Small Ship";
                                }
                            }

                            var turretToolTip = new StringBuilder();
                            turretToolTip.Append(string.Format("Interior: {0} \n", interiorTurrets));
                            turretToolTip.Append(string.Format("Gatling: {0} \n", gatlingTurrets));
                            turretToolTip.Append(string.Format("Missile: {0}", missileTurrets));

                            if (matchesSelectionFilter)
                            {
                                var row = new MyGuiControlTable.Row(prefab.SubtypeId);                                
                                row.AddCell(new MyGuiControlTable.Cell(text: "Yes", toolTip: "Will be used in game"));
                                row.AddCell(new MyGuiControlTable.Cell(text: prefab.SubtypeId.Replace("_", " "), toolTip: "The name of the ship or station"));
                                row.AddCell(new MyGuiControlTable.Cell(text: gridSize, toolTip: gridSizeToolTip));
                                row.AddCell(new MyGuiControlTable.Cell(text: firstPrefab.BlocksCount.ToString(), toolTip: blockToolTip.ToString()));
                                row.AddCell(new MyGuiControlTable.Cell(text: turrets.ToString(), toolTip: turretToolTip.ToString()));
                                
                                m_ShipsAvailable.Add(row);
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
                if (m_selectedRow.GetCell(0).Text.ToString() == "Yes")
                {
                    m_selectedRow.GetCell(0).Text = new StringBuilder("No");
                }
                else
                {
                    m_selectedRow.GetCell(0).Text = new StringBuilder("Yes");
                }
            }
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
