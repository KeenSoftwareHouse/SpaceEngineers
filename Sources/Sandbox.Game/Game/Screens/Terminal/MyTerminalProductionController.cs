using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyTerminalProductionController
    {
        enum AssemblerMode
        {
            Assembling,
            Disassembling
        }

        public static readonly int BLUEPRINT_GRID_ROWS = 8;
        public static readonly int QUEUE_GRID_ROWS = 2;
        public static readonly int INVENTORY_GRID_ROWS = 3;
        private static readonly Vector4 ERROR_ICON_COLOR_MASK = new Vector4(1f, 0.5f, 0.5f, 1f); // light red

        private static StringBuilder m_textCache = new StringBuilder();
        private static Dictionary<MyDefinitionId, MyFixedPoint> m_requiredCountCache = new Dictionary<MyDefinitionId, MyFixedPoint>(MyDefinitionId.Comparer);
        private static List<MyBlueprintDefinitionBase.ProductionInfo> m_blueprintCache = new List<MyBlueprintDefinitionBase.ProductionInfo>();

        private IMyGuiControlsParent m_controlsParent;
        private MyGridTerminalSystem m_terminalSystem;
        private Dictionary<int, MyAssembler> m_assemblersByKey = new Dictionary<int, MyAssembler>();
        private int m_assemblerKeyCounter;

        private MyGuiControlCombobox m_comboboxAssemblers;
        private MyGuiControlGrid m_blueprintsGrid;
        private MyAssembler m_selectedAssembler;
        private MyGuiControlRadioButtonGroup m_blueprintButtonGroup = new MyGuiControlRadioButtonGroup();
        private MyGuiControlRadioButtonGroup m_modeButtonGroup = new MyGuiControlRadioButtonGroup();
        private MyGuiControlGrid m_queueGrid;
        private MyGuiControlGrid m_inventoryGrid;
        private MyGuiControlComponentList m_materialsList;
        private MyGuiControlScrollablePanel m_blueprintsArea;
        private MyGuiControlScrollablePanel m_queueArea;
        private MyGuiControlScrollablePanel m_inventoryArea;
        private MyGuiControlBase m_blueprintsBgPanel;
        private MyGuiControlBase m_blueprintsLabel;
        private MyGuiControlCheckbox m_repeatCheckbox;
        private MyGuiControlCheckbox m_slaveCheckbox;
        private MyGuiControlButton m_disassembleAllButton;
        private MyGuiControlButton m_controlPanelButton;
        private MyGuiControlButton m_inventoryButton;

        MyDragAndDropInfo m_dragAndDropInfo;
        MyGuiControlGridDragAndDrop m_dragAndDrop;
        StringBuilder m_incompleteAssemblerName = new StringBuilder();

        private AssemblerMode CurrentAssemblerMode
        {
            get { return (AssemblerMode)m_modeButtonGroup.SelectedButton.Key; }
        }

        public void Init(IMyGuiControlsParent controlsParent, MyCubeGrid grid)
        {
            if (grid == null)
            {
                ShowError(MySpaceTexts.ScreenTerminalError_ShipNotConnected, controlsParent);
                return;
            }

            grid.RaiseGridChanged();
            m_assemblerKeyCounter = 0;
            m_assemblersByKey.Clear();
            foreach (var block in grid.GridSystems.TerminalSystem.Blocks)
            {
                var assembler = block as MyAssembler;
                if (assembler == null) continue;
                if (!assembler.HasLocalPlayerAccess()) continue;

                m_assemblersByKey.Add(m_assemblerKeyCounter++, assembler);
            }

            m_controlsParent = controlsParent;
            m_terminalSystem = grid.GridSystems.TerminalSystem;

            m_blueprintsArea = (MyGuiControlScrollablePanel)controlsParent.Controls.GetControlByName("BlueprintsScrollableArea");
            m_queueArea = (MyGuiControlScrollablePanel)controlsParent.Controls.GetControlByName("QueueScrollableArea");
            m_inventoryArea = (MyGuiControlScrollablePanel)controlsParent.Controls.GetControlByName("InventoryScrollableArea");
            m_blueprintsBgPanel = controlsParent.Controls.GetControlByName("BlueprintsBackgroundPanel");
            m_blueprintsLabel = controlsParent.Controls.GetControlByName("BlueprintsLabel");
            m_comboboxAssemblers = (MyGuiControlCombobox)controlsParent.Controls.GetControlByName("AssemblersCombobox");
            m_blueprintsGrid = (MyGuiControlGrid)m_blueprintsArea.ScrolledControl;
            m_queueGrid = (MyGuiControlGrid)m_queueArea.ScrolledControl;
            m_inventoryGrid = (MyGuiControlGrid)m_inventoryArea.ScrolledControl;
            m_materialsList = (MyGuiControlComponentList)controlsParent.Controls.GetControlByName("MaterialsList");
            m_repeatCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("RepeatCheckbox");
            m_slaveCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("SlaveCheckbox");
            m_disassembleAllButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("DisassembleAllButton");
            m_controlPanelButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("ControlPanelButton");
            m_inventoryButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("InventoryButton");

            {
                var assemblingButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("AssemblingButton");
                var disassemblingButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("DisassemblingButton");
                assemblingButton.Key = (int)AssemblerMode.Assembling;
                disassemblingButton.Key = (int)AssemblerMode.Disassembling;
                m_modeButtonGroup.Add(assemblingButton);
                m_modeButtonGroup.Add(disassemblingButton);
            }

            foreach (var entry in m_assemblersByKey)
            {
                if (entry.Value.IsFunctional == false)
                {
                    m_incompleteAssemblerName.Clear();
                    m_incompleteAssemblerName.AppendStringBuilder(entry.Value.CustomName);
                    m_incompleteAssemblerName.AppendStringBuilder(MyTexts.Get(MySpaceTexts.Terminal_BlockIncomplete));
                    m_comboboxAssemblers.AddItem(entry.Key, m_incompleteAssemblerName);
                }
                else
                {
                    m_comboboxAssemblers.AddItem(entry.Key, entry.Value.CustomName);
                }
            }
            m_comboboxAssemblers.ItemSelected += Assemblers_ItemSelected;

            m_comboboxAssemblers.SelectItemByIndex(0);

            m_dragAndDrop = new MyGuiControlGridDragAndDrop(MyGuiConstants.DRAG_AND_DROP_BACKGROUND_COLOR,
                                                            MyGuiConstants.DRAG_AND_DROP_TEXT_COLOR,
                                                            0.7f,
                                                            MyGuiConstants.DRAG_AND_DROP_TEXT_OFFSET, true);
            controlsParent.Controls.Add(m_dragAndDrop);
            m_dragAndDrop.DrawBackgroundTexture = false;
            m_dragAndDrop.ItemDropped += dragDrop_OnItemDropped;

            RefreshBlueprints();
            Assemblers_ItemSelected();

            RegisterEvents();

            if (m_assemblersByKey.Count == 0)
                ShowError(MySpaceTexts.ScreenTerminalError_NoAssemblers, controlsParent);
        }

        private void UpdateBlueprintClassGui()
        {
            foreach (var classButton in m_blueprintButtonGroup)
            {
                m_controlsParent.Controls.Remove(classButton);
            }
            m_blueprintButtonGroup.Clear();

            float posX = 0.0f;

            if (!(m_selectedAssembler.BlockDefinition is MyProductionBlockDefinition))
            {
                Debug.Assert(false, "Selected block was not an assembler in MyTerminalProductionController");
                return;
            }
            var blueprintClasses = (m_selectedAssembler.BlockDefinition as MyProductionBlockDefinition).BlueprintClasses;
            for (int i = 0; i < blueprintClasses.Count; ++i)
            {
                bool selectedState = i == 0 || blueprintClasses[i].Id.SubtypeName == "Components";
                AddBlueprintClassButton(blueprintClasses[i], ref posX, selected: selectedState);
            }
        }

        private void AddBlueprintClassButton(MyBlueprintClassDefinition classDef, ref float xOffset, bool selected = false)
        {
            Debug.Assert(classDef != null);
            if (classDef == null) return;

            var test = new MyGuiControlRadioButton(
                position: m_blueprintsLabel.Position + new Vector2(xOffset, m_blueprintsLabel.Size.Y),
                size: new Vector2(46f, 46f) / MyGuiConstants.GUI_OPTIMAL_SIZE
            );
            xOffset += test.Size.X;
            test.Icon = new MyGuiHighlightTexture() { Normal = classDef.Icon, Highlight = classDef.HighlightIcon, SizePx = new Vector2(46f, 46f) };
            test.UserData = classDef;
            test.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            if (classDef.DisplayNameEnum.HasValue)
                test.SetToolTip(classDef.DisplayNameEnum.Value);
            else
                test.SetToolTip(classDef.DisplayNameString);
            m_blueprintButtonGroup.Add(test);
            m_controlsParent.Controls.Add(test);
            test.Selected = selected;
        }

        private static void ShowError(MyStringId errorText, IMyGuiControlsParent controlsParent)
        {
            foreach (var control in controlsParent.Controls)
                control.Visible = false;

            var label = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("ErrorMessage");
            if (label == null)
                label = MyGuiScreenTerminal.CreateErrorLabel(errorText, "ErrorMessage");
            label.TextEnum = errorText;
            if (!controlsParent.Controls.Contains(label))
                controlsParent.Controls.Add(label);
        }

        private static void HideError(IMyGuiControlsParent controlsParent)
        {
            controlsParent.Controls.RemoveControlByName("ErrorMessage");

            foreach (var control in controlsParent.Controls)
                control.Visible = true;
        }

        private void RegisterEvents()
        {
            foreach (var entry in m_assemblersByKey)
                entry.Value.CustomNameChanged += assembler_CustomNameChanged;


            m_terminalSystem.BlockAdded += TerminalSystem_BlockAdded;
            m_terminalSystem.BlockRemoved += TerminalSystem_BlockRemoved;
            m_blueprintButtonGroup.SelectedChanged += blueprintButtonGroup_SelectedChanged;
            m_modeButtonGroup.SelectedChanged += modeButtonGroup_SelectedChanged;
            m_blueprintsGrid.ItemClicked += blueprintsGrid_ItemClicked;
            m_blueprintsGrid.MouseOverIndexChanged += blueprintsGrid_MouseOverIndexChanged;
            m_inventoryGrid.ItemClicked += inventoryGrid_ItemClicked;
            m_inventoryGrid.MouseOverIndexChanged += inventoryGrid_MouseOverIndexChanged;
            m_repeatCheckbox.IsCheckedChanged = repeatCheckbox_IsCheckedChanged;
            m_slaveCheckbox.IsCheckedChanged = slaveCheckbox_IsCheckedChanged;
            m_queueGrid.ItemClicked += queueGrid_ItemClicked;
            m_queueGrid.ItemDragged += queueGrid_ItemDragged;
            m_queueGrid.MouseOverIndexChanged += queueGrid_MouseOverIndexChanged;
            m_controlPanelButton.ButtonClicked += controlPanelButton_ButtonClicked;
            m_inventoryButton.ButtonClicked += inventoryButton_ButtonClicked;
            m_disassembleAllButton.ButtonClicked += disassembleAllButton_ButtonClicked;
        }

        private void UnregisterEvents()
        {
            // showing error message instead of controls.
            if (m_controlsParent == null)
                return;

            foreach (var entry in m_assemblersByKey)
                entry.Value.CustomNameChanged -= assembler_CustomNameChanged;

            if (m_terminalSystem != null)
            {
                m_terminalSystem.BlockAdded -= TerminalSystem_BlockAdded;
                m_terminalSystem.BlockRemoved -= TerminalSystem_BlockRemoved;
            }
            m_blueprintButtonGroup.SelectedChanged -= blueprintButtonGroup_SelectedChanged;
            m_modeButtonGroup.SelectedChanged -= modeButtonGroup_SelectedChanged;
            m_blueprintsGrid.ItemClicked -= blueprintsGrid_ItemClicked;
            m_blueprintsGrid.MouseOverIndexChanged -= blueprintsGrid_MouseOverIndexChanged;
            m_inventoryGrid.ItemClicked -= inventoryGrid_ItemClicked;
            m_inventoryGrid.MouseOverIndexChanged -= inventoryGrid_MouseOverIndexChanged;
            m_repeatCheckbox.IsCheckedChanged = null;
            m_slaveCheckbox.IsCheckedChanged = null;
            m_queueGrid.ItemClicked -= queueGrid_ItemClicked;
            m_queueGrid.ItemDragged -= queueGrid_ItemDragged;
            m_queueGrid.MouseOverIndexChanged -= queueGrid_MouseOverIndexChanged;
            m_controlPanelButton.ButtonClicked -= controlPanelButton_ButtonClicked;
            m_inventoryButton.ButtonClicked -= inventoryButton_ButtonClicked;
            m_disassembleAllButton.ButtonClicked -= disassembleAllButton_ButtonClicked;
        }

        private void RegisterAssemblerEvents(MyAssembler assembler)
        {
            if (assembler == null)
                return;

            assembler.CurrentModeChanged += assembler_CurrentModeChanged;
            assembler.QueueChanged += assembler_QueueChanged;
            assembler.CurrentProgressChanged += assembler_CurrentProgressChanged;
            assembler.CurrentStateChanged += assembler_CurrentStateChanged;
            assembler.InputInventory.ContentsChanged += InputInventory_ContentsChanged;
            assembler.OutputInventory.ContentsChanged += OutputInventory_ContentsChanged;
        }

        private void UnregisterAssemblerEvents(MyAssembler assembler)
        {
            if (assembler == null)
                return;

            m_selectedAssembler.CurrentModeChanged -= assembler_CurrentModeChanged;
            m_selectedAssembler.QueueChanged -= assembler_QueueChanged;
            m_selectedAssembler.CurrentProgressChanged -= assembler_CurrentProgressChanged;
            m_selectedAssembler.CurrentStateChanged -= assembler_CurrentStateChanged;
            assembler.InputInventory.ContentsChanged -= InputInventory_ContentsChanged;
            m_selectedAssembler.OutputInventory.ContentsChanged -= OutputInventory_ContentsChanged;
        }

        internal void Close()
        {
            UnregisterEvents();
            UnregisterAssemblerEvents(m_selectedAssembler);

            m_assemblersByKey.Clear();
            m_blueprintButtonGroup.Clear();
            m_modeButtonGroup.Clear();

            m_selectedAssembler = null;
            m_controlsParent = null;
            m_terminalSystem = null;
            m_comboboxAssemblers = null;

            m_dragAndDrop = null;
            m_dragAndDropInfo = null;
        }

        private void SelectAndShowAssembler(MyAssembler assembler)
        {
            UnregisterAssemblerEvents(m_selectedAssembler);
            m_selectedAssembler = assembler;
            RegisterAssemblerEvents(assembler);

            RefreshRepeatMode(assembler.RepeatEnabled);
            RefreshSlaveMode(assembler.IsSlave);
            SelectModeButton(assembler.DisassembleEnabled);
            UpdateBlueprintClassGui();
            RefreshQueue();
            RefreshInventory();
            RefreshProgress();
            RefreshAssemblerModeView();
        }

        private void RefreshInventory()
        {
            m_inventoryGrid.Clear();
            foreach (var item in m_selectedAssembler.OutputInventory.GetItems())
            {
                m_inventoryGrid.Add(MyGuiControlInventoryOwner.CreateInventoryGridItem(item));
            }
            int itemCount = m_selectedAssembler.OutputInventory.GetItems().Count;
            m_inventoryGrid.RowsCount = Math.Max(1 + (itemCount / m_inventoryGrid.ColumnsCount), INVENTORY_GRID_ROWS);
        }

        private void RefreshQueue()
        {
            m_queueGrid.Clear();
            int i = 0;
            foreach (var queueItem in m_selectedAssembler.Queue)
            {
                m_textCache.Clear()
                    .Append((int)queueItem.Amount)
                    .Append('x');

                var item = new MyGuiControlGrid.Item(
                    icon: queueItem.Blueprint.Icon,
                    toolTip: queueItem.Blueprint.DisplayNameText,
                    userData: queueItem);
                item.AddText(text: m_textCache, textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

                if (MyFakes.SHOW_PRODUCTION_QUEUE_ITEM_IDS)
                {
                    m_textCache.Clear()
                        .Append((int)queueItem.ItemId);

                    item.AddText(text: m_textCache, textAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                }

                m_queueGrid.Add(item);
                ++i;
            }

            m_queueGrid.RowsCount = Math.Max(1 + (i / m_queueGrid.ColumnsCount), QUEUE_GRID_ROWS);
            RefreshProgress();
        }

        private void RefreshBlueprints()
        {
            if (m_blueprintButtonGroup.SelectedButton == null) return;

            var blueprintClass = m_blueprintButtonGroup.SelectedButton.UserData as MyBlueprintClassDefinition;
            Debug.Assert(blueprintClass != null);
            if (blueprintClass == null) return;

            m_blueprintsGrid.Clear();
            int i = 0;
            foreach (var blueprint in blueprintClass)
            {
                var item = new MyGuiControlGrid.Item(
                    icon: blueprint.Icon,
                    toolTip: blueprint.DisplayNameText,
                    userData: blueprint);
                m_blueprintsGrid.Add(item);
                ++i;
            }

            m_blueprintsGrid.RowsCount = Math.Max(1 + (i / m_blueprintsGrid.ColumnsCount), BLUEPRINT_GRID_ROWS);
            RefreshBlueprintGridColors();
        }

        private void RefreshBlueprintGridColors()
        {
            m_selectedAssembler.InventoryOwnersDirty = true;
            for (int i = 0; i < m_blueprintsGrid.RowsCount; ++i)
                for (int j = 0; j < m_blueprintsGrid.ColumnsCount; ++j)
                {
                    var item = m_blueprintsGrid.TryGetItemAt(i, j);
                    if (item == null) continue;
                    var blueprint = item.UserData as MyBlueprintDefinitionBase;
                    if (blueprint == null) continue;
                    item.IconColorMask = Vector4.One;
                    if (m_selectedAssembler == null /*|| CurrentAssemblerMode == AssemblerMode.Disassembling*/)
                        continue;

                    AddComponentPrerequisites(blueprint, 1, m_requiredCountCache);

                    if(CurrentAssemblerMode == AssemblerMode.Assembling)
                    {
                        foreach (var entry in m_requiredCountCache)
                        {
                            if (!m_selectedAssembler.CheckConveyorResources(entry.Value, entry.Key))
                            {
                                item.IconColorMask = ERROR_ICON_COLOR_MASK;
                                break;
                            }
                        }
                    }
                    else if (CurrentAssemblerMode == AssemblerMode.Disassembling)
                    {
                        if (!m_selectedAssembler.CheckConveyorResources(null, blueprint.Results[0].Id))
                        {
                            item.IconColorMask = ERROR_ICON_COLOR_MASK;
                        }
                    }
                    m_requiredCountCache.Clear();
                }
        }

        private void RefreshProgress()
        {
            var queueGridItem = m_queueGrid.TryGetItemAt(0);
            if (queueGridItem == null)
                return;

            var queueItem = (MyProductionBlock.QueueItem)queueGridItem.UserData;

            queueGridItem.OverlayPercent = MathHelper.Clamp(m_selectedAssembler.CurrentProgress, 0f, 1f);
            queueGridItem.ToolTip.ToolTips.Clear();

            m_textCache.Clear().AppendFormat("{0}: {1}%", queueItem.Blueprint.DisplayNameText, (int)(m_selectedAssembler.CurrentProgress * 100f));
            queueGridItem.ToolTip.AddToolTip(m_textCache.ToString());

            if (m_selectedAssembler.CurrentState == MyAssembler.StateEnum.Ok)
            {
                queueGridItem.IconColorMask = Color.White.ToVector4();
                queueGridItem.OverlayColorMask = Color.White.ToVector4();
            }
            else
            {
                queueGridItem.IconColorMask = ERROR_ICON_COLOR_MASK;
                queueGridItem.OverlayColorMask = Color.Red.ToVector4();
                queueGridItem.ToolTip.AddToolTip(GetAssemblerStateText(m_selectedAssembler.CurrentState), font: MyFontEnum.Red);
            }
        }

        private void RefreshAssemblerModeView()
        {
            bool assembling = CurrentAssemblerMode == AssemblerMode.Assembling;
            bool repeat = m_selectedAssembler.RepeatEnabled;
            //m_blueprintsArea.Enabled = assembling;
            //m_blueprintsBgPanel.Enabled = assembling;
            //m_blueprintsLabel.Enabled = assembling;
            //foreach (var button in m_blueprintButtonGroup)
            //    button.Enabled = assembling;

            m_blueprintsArea.Enabled = true;
            m_blueprintsBgPanel.Enabled = true;
            m_blueprintsLabel.Enabled = true;
            foreach (var button in m_blueprintButtonGroup)
                button.Enabled = true;
            
            m_materialsList.ValuesText = (assembling)
                ? MyTexts.Get(MySpaceTexts.ScreenTerminalProduction_RequiredAndAvailable)
                : MyTexts.Get(MySpaceTexts.ScreenTerminalProduction_GainedAndAvailable);

            // disabled when repeat disassembling is turned on
            m_queueGrid.Enabled = assembling || !repeat;

            m_disassembleAllButton.Visible = !assembling && !repeat;
            RefreshBlueprintGridColors();
        }

        private void RefreshRepeatMode(bool repeatModeEnabled)
        {
            if (m_selectedAssembler.IsSlave && repeatModeEnabled)
            {
                RefreshSlaveMode(false);
            }
            m_selectedAssembler.CurrentModeChanged -= assembler_CurrentModeChanged;
            m_selectedAssembler.RequestRepeatEnabled(repeatModeEnabled);
            m_selectedAssembler.CurrentModeChanged += assembler_CurrentModeChanged;

            m_repeatCheckbox.IsCheckedChanged = null;
            m_repeatCheckbox.IsChecked = m_selectedAssembler.RepeatEnabled;
            m_repeatCheckbox.IsCheckedChanged = repeatCheckbox_IsCheckedChanged;
        }

        private void RefreshSlaveMode(bool slaveModeEnabled)
        {
            if (m_selectedAssembler.RepeatEnabled && slaveModeEnabled)
            {
                RefreshRepeatMode(false);
            }
            if (m_selectedAssembler.DisassembleEnabled)
            {
                m_slaveCheckbox.Enabled = false;
                m_slaveCheckbox.Visible = false;
            }
            if (!m_selectedAssembler.DisassembleEnabled)
            {
                m_slaveCheckbox.Enabled = true;
                m_slaveCheckbox.Visible = true;
            }
            m_selectedAssembler.CurrentModeChanged -= assembler_CurrentModeChanged;
            m_selectedAssembler.RequestSlaveEnabled(slaveModeEnabled);
            m_selectedAssembler.CurrentModeChanged += assembler_CurrentModeChanged;

            m_slaveCheckbox.IsCheckedChanged = null;
            m_slaveCheckbox.IsChecked = m_selectedAssembler.IsSlave;
            m_slaveCheckbox.IsCheckedChanged = slaveCheckbox_IsCheckedChanged;
        }

        private void EnqueueBlueprint(MyBlueprintDefinitionBase blueprint, MyFixedPoint amount)
        {
            m_blueprintCache.Clear();
            blueprint.GetBlueprints(m_blueprintCache);
            foreach (var entry in m_blueprintCache)
                m_selectedAssembler.InsertQueueItemRequest(-1, entry.Blueprint, entry.Amount * amount);
            m_blueprintCache.Clear();
        }

        private void ShowBlueprintComponents(MyBlueprintDefinitionBase blueprint, MyFixedPoint amount)
        {
            m_materialsList.Clear();
            if (blueprint == null)
                return;

            AddComponentPrerequisites(blueprint, amount, m_requiredCountCache);
            FillMaterialList(m_requiredCountCache);
            m_requiredCountCache.Clear();
        }

        private void FillMaterialList(Dictionary<MyDefinitionId, MyFixedPoint> materials)
        {
            bool disassembling = CurrentAssemblerMode == AssemblerMode.Disassembling;
            foreach (var entry in materials)
            {
                MyFixedPoint inventoryCount = m_selectedAssembler.InputInventory.GetItemAmount(entry.Key);
                var font = (disassembling || entry.Value <= inventoryCount) ? MyFontEnum.White : MyFontEnum.Red;
                m_materialsList.Add(entry.Key, (double)entry.Value, (double)inventoryCount, font);
            }
        }

        private static void AddComponentPrerequisites(MyBlueprintDefinitionBase blueprint, MyFixedPoint multiplier, Dictionary<MyDefinitionId, MyFixedPoint> outputAmounts)
        {
            foreach (var prerequisite in blueprint.Prerequisites)
            {
                if (!outputAmounts.ContainsKey(prerequisite.Id))
                    outputAmounts[prerequisite.Id] = 0;

                var amountMult = (MyFixedPoint)(1.0f / MySession.Static.AssemblerEfficiencyMultiplier);
                outputAmounts[prerequisite.Id] += prerequisite.Amount * multiplier * amountMult;
            }
        }

        private void StartDragging(MyDropHandleType dropHandlingType, MyGuiControlGrid gridControl, ref MyGuiControlGrid.EventArgs args)
        {
            m_dragAndDropInfo = new MyDragAndDropInfo();
            m_dragAndDropInfo.Grid = gridControl;
            m_dragAndDropInfo.ItemIndex = args.ItemIndex;

            var draggingItem = m_dragAndDropInfo.Grid.GetItemAt(m_dragAndDropInfo.ItemIndex);
            m_dragAndDrop.StartDragging(dropHandlingType, args.Button, draggingItem, m_dragAndDropInfo, includeTooltip: false);
        }

        private void SelectModeButton(bool disassembling)
        {
            m_modeButtonGroup.SelectByKey((int)(disassembling ? AssemblerMode.Disassembling : AssemblerMode.Assembling));
           
        }

        private void RefreshMaterialsPreview()
        {
            m_materialsList.Clear();
            m_requiredCountCache.Clear();

            if (m_blueprintsGrid.MouseOverItem != null /*&& CurrentAssemblerMode == AssemblerMode.Assembling*/)
            {
                ShowBlueprintComponents((MyBlueprintDefinitionBase)m_blueprintsGrid.MouseOverItem.UserData, 1);
            }
            else if (m_inventoryGrid.MouseOverItem != null && CurrentAssemblerMode == AssemblerMode.Disassembling)
            {
                var item = (MyPhysicalInventoryItem)m_inventoryGrid.MouseOverItem.UserData;
                if (MyDefinitionManager.Static.HasBlueprint(item.Content.GetId()))
                {
                    ShowBlueprintComponents(MyDefinitionManager.Static.GetBlueprintDefinition(item.Content.GetId()), 1);
                }
            }
            else if (m_queueGrid.MouseOverItem != null)
            {
                var item = (MyProductionBlock.QueueItem)m_queueGrid.MouseOverItem.UserData;
                ShowBlueprintComponents(item.Blueprint, item.Amount);
            }
            else if (m_selectedAssembler != null)
            {
                foreach (var queueItem in m_selectedAssembler.Queue)
                {
                    AddComponentPrerequisites(queueItem.Blueprint, queueItem.Amount, m_requiredCountCache);
                }
                FillMaterialList(m_requiredCountCache);
            }

        }

        private static String GetAssemblerStateText(MyAssembler.StateEnum state)
        {
            MyStringId enumVal = MySpaceTexts.Blank;
            switch (state)
            {
                case MyAssembler.StateEnum.Ok:
                    enumVal = MySpaceTexts.Blank;
                    break;

                case MyAssembler.StateEnum.Disabled:
                    enumVal = MySpaceTexts.AssemblerState_Disabled;
                    break;

                case MyAssembler.StateEnum.NotWorking:
                    enumVal = MySpaceTexts.AssemblerState_NotWorking;
                    break;

                case MyAssembler.StateEnum.MissingItems:
                    enumVal = MySpaceTexts.AssemblerState_MissingItems;
                    break;

                case MyAssembler.StateEnum.NotEnoughPower:
                    enumVal = MySpaceTexts.AssemblerState_NotEnoughPower;
                    break;

                case MyAssembler.StateEnum.InventoryFull:
                    enumVal = MySpaceTexts.AssemblerState_InventoryFull;
                    break;

                default:
                    Debug.Assert(false, "Invalid branch reached.");
                    break;
            }

            return MyTexts.GetString(enumVal);
        }

        #region Event handlers

        void blueprintButtonGroup_SelectedChanged(MyGuiControlRadioButtonGroup obj)
        {
            RefreshBlueprints();
        }

        void Assemblers_ItemSelected()
        {
            if (m_assemblersByKey.Count > 0)
            {
                System.Diagnostics.Debug.Assert(m_assemblersByKey.ContainsKey((int)m_comboboxAssemblers.GetSelectedKey()));

                if (m_assemblersByKey.ContainsKey((int)m_comboboxAssemblers.GetSelectedKey()))
                    SelectAndShowAssembler(m_assemblersByKey[(int)m_comboboxAssemblers.GetSelectedKey()]);
            }
        }

        void assembler_CurrentModeChanged(MyAssembler assembler)
        {
            Debug.Assert(m_selectedAssembler == assembler);
            SelectModeButton(assembler.DisassembleEnabled);
            RefreshRepeatMode(assembler.RepeatEnabled);
            RefreshSlaveMode(assembler.IsSlave);
            RefreshProgress();
            RefreshAssemblerModeView();
            RefreshMaterialsPreview();
        }

        void assembler_QueueChanged(MyProductionBlock block)
        {
            Debug.Assert(m_selectedAssembler == block);
            RefreshQueue();
            RefreshMaterialsPreview();
        }

        void assembler_CurrentProgressChanged(MyAssembler assembler)
        {
            Debug.Assert(assembler == m_selectedAssembler);
            RefreshProgress();
        }

        void assembler_CurrentStateChanged(MyAssembler obj)
        {
            Debug.Assert(obj == m_selectedAssembler);
            RefreshProgress();
        }

        void InputInventory_ContentsChanged(MyInventory obj)
        {
            if (CurrentAssemblerMode == AssemblerMode.Assembling)
                RefreshBlueprintGridColors();
            RefreshMaterialsPreview();
        }

        void OutputInventory_ContentsChanged(MyInventory obj)
        {
            RefreshInventory();
            RefreshMaterialsPreview();
        }

        void blueprintsGrid_ItemClicked(MyGuiControlGrid control, MyGuiControlGrid.EventArgs args)
        {
            Debug.Assert(control == m_blueprintsGrid);

            //if(CurrentAssemblerMode == AssemblerMode.Assembling)
            {
                var item = control.GetItemAt(args.ItemIndex);
                if (item == null)
                    return;

                var blueprint = (MyBlueprintDefinitionBase)item.UserData;
                var amount = MyInput.Static.IsAnyShiftKeyPressed() ? 100 :
                             MyInput.Static.IsAnyCtrlKeyPressed() ? 10 : 1;
                EnqueueBlueprint(blueprint, amount);
            }
        }

        void inventoryGrid_ItemClicked(MyGuiControlGrid control, MyGuiControlGrid.EventArgs args)
        {
            Debug.Assert(control == m_inventoryGrid);

            if (CurrentAssemblerMode == AssemblerMode.Assembling)
                return;

            var item = (MyPhysicalInventoryItem)control.GetItemAt(args.ItemIndex).UserData;
            var blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(item.Content.GetId());
            if (blueprint != null)
            {
                var amount = MyInput.Static.IsAnyShiftKeyPressed() ? 100 :
                             MyInput.Static.IsAnyCtrlKeyPressed() ? 10 : 1;
                EnqueueBlueprint(blueprint, amount);
            }
        }

        void queueGrid_ItemClicked(MyGuiControlGrid control, MyGuiControlGrid.EventArgs args)
        {
            Debug.Assert(control == m_queueGrid);

            // Changing queue in auto-disassembling mode is forbidden.
            if (CurrentAssemblerMode == AssemblerMode.Disassembling && m_selectedAssembler.RepeatEnabled)
                return;

            if (args.Button == MySharedButtonsEnum.Secondary)
                m_selectedAssembler.RemoveQueueItemRequest(args.ItemIndex);
        }

        void queueGrid_ItemDragged(MyGuiControlGrid control, MyGuiControlGrid.EventArgs args)
        {
            StartDragging(MyDropHandleType.MouseRelease, control, ref args);
        }

        private void dragDrop_OnItemDropped(object sender, MyDragAndDropEventArgs eventArgs)
        {
            if (m_selectedAssembler != null && eventArgs.DropTo != null)
            {
                var queueItem = (MyProductionBlock.QueueItem)eventArgs.Item.UserData;

                m_selectedAssembler.MoveQueueItemRequest(queueItem.ItemId, eventArgs.DropTo.ItemIndex);
            }

            m_dragAndDropInfo = null;
        }

        void blueprintsGrid_MouseOverIndexChanged(MyGuiControlGrid control, MyGuiControlGrid.EventArgs args)
        {
            Debug.Assert(control == m_blueprintsGrid);
            RefreshMaterialsPreview();
        }

        void inventoryGrid_MouseOverIndexChanged(MyGuiControlGrid control, MyGuiControlGrid.EventArgs args)
        {
            Debug.Assert(control == m_inventoryGrid);

            if (CurrentAssemblerMode == AssemblerMode.Assembling)
                return;

            RefreshMaterialsPreview();
        }

        void queueGrid_MouseOverIndexChanged(MyGuiControlGrid control, MyGuiControlGrid.EventArgs args)
        {
            Debug.Assert(control == m_queueGrid);
            RefreshMaterialsPreview();
        }

        void modeButtonGroup_SelectedChanged(MyGuiControlRadioButtonGroup obj)
        {
            m_selectedAssembler.CurrentModeChanged -= assembler_CurrentModeChanged;

            var disassembling = (AssemblerMode)obj.SelectedButton.Key == AssemblerMode.Disassembling;
            m_selectedAssembler.RequestDisassembleEnabled(disassembling);
            if (disassembling)
            {
                m_slaveCheckbox.Enabled = false;
                m_slaveCheckbox.Visible = false;
            }

            if (!disassembling)
            {
                m_slaveCheckbox.Enabled = true;
                m_slaveCheckbox.Visible = true;
            }

            m_selectedAssembler.CurrentModeChanged += assembler_CurrentModeChanged;

            m_repeatCheckbox.IsCheckedChanged = null;
            m_repeatCheckbox.IsChecked = m_selectedAssembler.RepeatEnabled;
            m_repeatCheckbox.IsCheckedChanged = repeatCheckbox_IsCheckedChanged;

            m_slaveCheckbox.IsCheckedChanged = null;
            m_slaveCheckbox.IsChecked = m_selectedAssembler.IsSlave;
            m_slaveCheckbox.IsCheckedChanged = slaveCheckbox_IsCheckedChanged;

            RefreshProgress();
            RefreshAssemblerModeView();
        }

        void repeatCheckbox_IsCheckedChanged(MyGuiControlCheckbox control)
        {
            Debug.Assert(control == m_repeatCheckbox);
            RefreshRepeatMode(control.IsChecked);
            RefreshAssemblerModeView();
        }

        void slaveCheckbox_IsCheckedChanged(MyGuiControlCheckbox control)
        {
            Debug.Assert(control == m_slaveCheckbox);
            RefreshSlaveMode(control.IsChecked);
            RefreshAssemblerModeView();
        }

        void controlPanelButton_ButtonClicked(MyGuiControlButton control)
        {
            Debug.Assert(control == m_controlPanelButton);
            MyGuiScreenTerminal.SwitchToControlPanelBlock(m_selectedAssembler);
        }

        void inventoryButton_ButtonClicked(MyGuiControlButton control)
        {
            MyGuiScreenTerminal.SwitchToInventory();
        }

        void TerminalSystem_BlockAdded(MyTerminalBlock obj)
        {
            var assembler = obj as MyAssembler;
            if (assembler != null)
            {
                if (m_assemblersByKey.Count == 0)
                {
                    HideError(m_controlsParent);
                }
                var key = m_assemblerKeyCounter++;
                m_assemblersByKey.Add(key, assembler);
                m_comboboxAssemblers.AddItem(key, assembler.CustomName);
                if (m_assemblersByKey.Count == 1)
                    m_comboboxAssemblers.SelectItemByIndex(0);
                assembler.CustomNameChanged += assembler_CustomNameChanged;
            }
        }

        void TerminalSystem_BlockRemoved(MyTerminalBlock obj)
        {
            var removedAssembler = obj as MyAssembler;
            if (removedAssembler != null)
            {
                removedAssembler.CustomNameChanged -= assembler_CustomNameChanged;
                int? key = null;
                foreach (var entry in m_assemblersByKey)
                {
                    if (entry.Value == removedAssembler)
                    {
                        key = entry.Key;
                        break;
                    }
                }
                if (key.HasValue)
                {
                    m_assemblersByKey.Remove(key.Value);
                    m_comboboxAssemblers.RemoveItem(key.Value);
                }

                if (removedAssembler == m_selectedAssembler)
                {
                    if (m_assemblersByKey.Count > 0)
                        m_comboboxAssemblers.SelectItemByIndex(0);
                    else
                        ShowError(MySpaceTexts.ScreenTerminalError_NoAssemblers, m_controlsParent);
                }
            }
        }

        void assembler_CustomNameChanged(MyTerminalBlock block)
        {
            Debug.Assert(block is MyAssembler && m_assemblersByKey.ContainsValue(block as MyAssembler));

            foreach (var entry in m_assemblersByKey)
            {
                if (entry.Value == block)
                {
                    var comboItem = m_comboboxAssemblers.TryGetItemByKey(entry.Key);
                    comboItem.Value.Clear().AppendStringBuilder(block.CustomName);
                }
            }
        }

        void disassembleAllButton_ButtonClicked(MyGuiControlButton obj)
        {
            if (CurrentAssemblerMode == AssemblerMode.Disassembling && !m_selectedAssembler.RepeatEnabled)
                m_selectedAssembler.RequestDisassembleAll();
            else
                Debug.Fail("Invalid branch.");
        }

        #endregion
    }
}
