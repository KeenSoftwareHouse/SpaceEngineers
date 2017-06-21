using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Input;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyTerminalInventoryController
    {
        MyGuiControlList m_leftOwnersControl;
        MyGuiControlRadioButton m_leftSuitButton;
        MyGuiControlRadioButton m_leftGridButton;
        MyGuiControlRadioButton m_leftFilterStorageButton;
        MyGuiControlRadioButton m_leftFilterSystemButton;
        MyGuiControlRadioButton m_leftFilterEnergyButton;
        MyGuiControlRadioButton m_leftFilterAllButton;
        MyGuiControlRadioButtonGroup m_leftTypeGroup;
        MyGuiControlRadioButtonGroup m_leftFilterGroup;

        MyGuiControlList m_rightOwnersControl;
        MyGuiControlRadioButton m_rightSuitButton;
        MyGuiControlRadioButton m_rightGridButton;
        MyGuiControlRadioButton m_rightFilterStorageButton;
        MyGuiControlRadioButton m_rightFilterSystemButton;
        MyGuiControlRadioButton m_rightFilterEnergyButton;
        MyGuiControlRadioButton m_rightFilterAllButton;
        MyGuiControlRadioButtonGroup m_rightTypeGroup;
        MyGuiControlRadioButtonGroup m_rightFilterGroup;

        MyGuiControlButton m_throwOutButton;
        MyDragAndDropInfo m_dragAndDropInfo;
        MyGuiControlGridDragAndDrop m_dragAndDrop;

        List<MyGuiControlGrid> m_controlsDisabledWhileDragged;

        MyEntity m_userAsEntity;
        MyEntity m_interactedAsEntity;
        MyEntity m_openInventoryInteractedAsEntity;
        MyEntity m_userAsOwner;
        MyEntity m_interactedAsOwner;
        List<MyEntity> m_interactedGridOwners = new List<MyEntity>();
        List<IMyConveyorEndpoint> m_reachableInventoryOwners = new List<IMyConveyorEndpoint>();

        List<MyGridConveyorSystem> m_registeredConveyorSystems = new List<MyGridConveyorSystem>();

        MyGuiControlInventoryOwner m_focusedOwnerControl;
        MyGuiControlGrid m_focusedGridControl;
        MyPhysicalInventoryItem? m_selectedInventoryItem;
        MyInventory m_selectedInventory;

        bool m_leftShowsGrid;
        bool m_rightShowsGrid;
        MyInventoryOwnerTypeEnum? m_leftFilterType;
        MyInventoryOwnerTypeEnum? m_rightFilterType;

        MyGridColorHelper m_colorHelper;

        private MyGuiControlTextbox m_blockSearchLeft;
        private MyGuiControlButton  m_blockSearchClearLeft;
        private MyGuiControlTextbox m_blockSearchRight;
        private MyGuiControlButton  m_blockSearchClearRight;


        private static readonly Vector2 m_controlListFullSize       = new Vector2(0.44f, 0.65f);  // size of control list without search box
        private static readonly Vector2 m_controlListSizeWithSearch = new Vector2(0.44f, 0.616f); // size of control list with search box

        private static readonly Vector2 m_leftControlListPosition  = new Vector2(-0.465f, -0.295f); // control list position without search box
        private static readonly Vector2 m_rightControlListPosition = new Vector2(0.465f, -0.295f);

        private static readonly Vector2 m_leftControlListPosWithSearch  = new Vector2(-0.465f, -0.26f); // control list position with search box
        private static readonly Vector2 m_rightControlListPosWithSearch = new Vector2(0.465f, -0.26f);

        private MyGuiControlCheckbox m_hideEmptyLeft;
        private MyGuiControlLabel    m_hideEmptyLeftLabel;
        private MyGuiControlCheckbox m_hideEmptyRight;
        private MyGuiControlLabel    m_hideEmptyRightLabel;

        public MyTerminalInventoryController()
        {
            m_leftTypeGroup = new MyGuiControlRadioButtonGroup();
            m_leftFilterGroup = new MyGuiControlRadioButtonGroup();
            m_rightTypeGroup = new MyGuiControlRadioButtonGroup();
            m_rightFilterGroup = new MyGuiControlRadioButtonGroup();
            m_controlsDisabledWhileDragged = new List<MyGuiControlGrid>();
            m_endpointPredicate = this.EndpointPredicate;
        }

        public void Refresh()
        {
            var parentGrid = (m_interactedAsEntity != null) ? m_interactedAsEntity.Parent as MyCubeGrid : null;
            m_interactedGridOwners.Clear();
            if (parentGrid != null)
            {
                var group = MyCubeGridGroups.Static.Logical.GetGroup(parentGrid);
                foreach (var node in group.Nodes)
                {
                    GetGridInventories(node.NodeData, m_interactedGridOwners);
                    node.NodeData.GridSystems.ConveyorSystem.BlockAdded += ConveyorSystem_BlockAdded;
                    node.NodeData.GridSystems.ConveyorSystem.BlockRemoved += ConveyorSystem_BlockRemoved;

                    m_registeredConveyorSystems.Add(node.NodeData.GridSystems.ConveyorSystem);
                }
            }

            m_leftTypeGroup.SelectedIndex = 0;
            m_rightTypeGroup.SelectedIndex = (m_interactedAsEntity is MyCharacter) || (m_interactedAsEntity is MyInventoryBagEntity) ? 0 : 1;
            m_leftFilterGroup.SelectedIndex = 0;
            m_rightFilterGroup.SelectedIndex = 0;

            LeftTypeGroup_SelectedChanged(m_leftTypeGroup);
            RightTypeGroup_SelectedChanged(m_rightTypeGroup);
            SetLeftFilter(m_leftFilterType);
            SetRightFilter(m_rightFilterType);
        }

        public void Init(IMyGuiControlsParent controlsParent, MyEntity thisEntity, MyEntity interactedEntity, MyGridColorHelper colorHelper)
        {
            ProfilerShort.Begin("MyGuiScreenTerminal.ControllerInventory.Init");
            m_userAsEntity = thisEntity;
            m_interactedAsEntity = interactedEntity;
            m_colorHelper = colorHelper;

            m_leftOwnersControl = (MyGuiControlList)controlsParent.Controls.GetControlByName("LeftInventory");
            m_rightOwnersControl = (MyGuiControlList)controlsParent.Controls.GetControlByName("RightInventory");

            m_leftSuitButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("LeftSuitButton");
            m_leftGridButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("LeftGridButton");
            m_leftFilterStorageButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("LeftFilterStorageButton");
            m_leftFilterSystemButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("LeftFilterSystemButton");
            m_leftFilterEnergyButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("LeftFilterEnergyButton");
            m_leftFilterAllButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("LeftFilterAllButton");

            m_rightSuitButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("RightSuitButton");
            m_rightGridButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("RightGridButton");
            m_rightFilterStorageButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("RightFilterStorageButton");
            m_rightFilterSystemButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("RightFilterSystemButton");
            m_rightFilterEnergyButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("RightFilterEnergyButton");
            m_rightFilterAllButton = (MyGuiControlRadioButton)controlsParent.Controls.GetControlByName("RightFilterAllButton");

            m_throwOutButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("ThrowOutButton");

            m_hideEmptyLeft         = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("CheckboxHideEmptyLeft");
            m_hideEmptyLeftLabel    = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("LabelHideEmptyLeft");
            m_hideEmptyRight        = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("CheckboxHideEmptyRight");
            m_hideEmptyRightLabel   = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("LabelHideEmptyRight");
            m_blockSearchLeft       = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("BlockSearchLeft");
            m_blockSearchClearLeft  = (MyGuiControlButton)controlsParent.Controls.GetControlByName("BlockSearchClearLeft");
            m_blockSearchRight      = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("BlockSearchRight");
            m_blockSearchClearRight = (MyGuiControlButton)controlsParent.Controls.GetControlByName("BlockSearchClearRight");

            m_hideEmptyLeft.Visible         = false;
            m_hideEmptyLeftLabel.Visible    = false;
            m_hideEmptyRight.Visible        = true;
            m_hideEmptyRightLabel.Visible   = true;
            m_blockSearchLeft.Visible       = false;
            m_blockSearchClearLeft.Visible  = false;
            m_blockSearchRight.Visible      = true;
            m_blockSearchClearRight.Visible = true;

            m_hideEmptyLeft.IsCheckedChanged      += HideEmptyLeft_Checked;
            m_hideEmptyRight.IsCheckedChanged     += HideEmptyRight_Checked;
            m_blockSearchLeft.TextChanged         += BlockSearchLeft_TextChanged;
            m_blockSearchClearLeft.ButtonClicked  += BlockSearchClearLeft_ButtonClicked;
            m_blockSearchRight.TextChanged        += BlockSearchRight_TextChanged;
            m_blockSearchClearRight.ButtonClicked += BlockSearchClearRight_ButtonClicked;

            m_leftSuitButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_ShowCharacter);
            m_leftGridButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_ShowConnected);
            m_rightSuitButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_ShowInteracted);
            m_rightGridButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_ShowConnected);

            m_leftFilterAllButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterAll);
            m_leftFilterEnergyButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterEnergy);
            m_leftFilterStorageButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterStorage);
            m_leftFilterSystemButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterSystem);

            m_rightFilterAllButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterAll);
            m_rightFilterEnergyButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterEnergy);
            m_rightFilterStorageButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterStorage);
            m_rightFilterSystemButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_FilterSystem);

            m_throwOutButton.SetToolTip(MySpaceTexts.ToolTipTerminalInventory_ThrowOut);
            m_throwOutButton.CueEnum = GuiSounds.None;

            m_leftTypeGroup.Add(m_leftSuitButton);
            m_leftTypeGroup.Add(m_leftGridButton);
            m_rightTypeGroup.Add(m_rightSuitButton);
            m_rightTypeGroup.Add(m_rightGridButton);

            m_leftFilterGroup.Add(m_leftFilterAllButton);
            m_leftFilterGroup.Add(m_leftFilterEnergyButton);
            m_leftFilterGroup.Add(m_leftFilterStorageButton);
            m_leftFilterGroup.Add(m_leftFilterSystemButton);

            m_rightFilterGroup.Add(m_rightFilterAllButton);
            m_rightFilterGroup.Add(m_rightFilterEnergyButton);
            m_rightFilterGroup.Add(m_rightFilterStorageButton);
            m_rightFilterGroup.Add(m_rightFilterSystemButton);

            m_throwOutButton.DrawCrossTextureWhenDisabled = false;
            //m_throwOutButton.Enabled = false;

            // initialize drag and drop
            // maybe this requires screen?
            m_dragAndDrop = new MyGuiControlGridDragAndDrop(MyGuiConstants.DRAG_AND_DROP_BACKGROUND_COLOR,
                                                            MyGuiConstants.DRAG_AND_DROP_TEXT_COLOR,
                                                            0.7f,
                                                            MyGuiConstants.DRAG_AND_DROP_TEXT_OFFSET, true);
            controlsParent.Controls.Add(m_dragAndDrop);

            m_dragAndDrop.DrawBackgroundTexture = false;

            m_throwOutButton.ButtonClicked += throwOutButton_OnButtonClick;
            m_dragAndDrop.ItemDropped += dragDrop_OnItemDropped;

            var thisInventoryOwner = (m_userAsEntity != null && m_userAsEntity.HasInventory) ? m_userAsEntity : null;
            if (thisInventoryOwner != null)
                m_userAsOwner = thisInventoryOwner;

            var targetInventoryOwner = (m_interactedAsEntity != null && m_interactedAsEntity.HasInventory) ? m_interactedAsEntity : null;
            if (targetInventoryOwner != null)
                m_interactedAsOwner = targetInventoryOwner;

            var parentGrid = (m_interactedAsEntity != null) ? m_interactedAsEntity.Parent as MyCubeGrid : null;
            m_interactedGridOwners.Clear();
            if (parentGrid != null)
            {
                var group = MyCubeGridGroups.Static.Logical.GetGroup(parentGrid);
                foreach (var node in group.Nodes)
                {
                    GetGridInventories(node.NodeData, m_interactedGridOwners);
                    node.NodeData.GridSystems.ConveyorSystem.BlockAdded += ConveyorSystem_BlockAdded;
                    node.NodeData.GridSystems.ConveyorSystem.BlockRemoved += ConveyorSystem_BlockRemoved;

                    m_registeredConveyorSystems.Add(node.NodeData.GridSystems.ConveyorSystem);
                }
            }
            
            m_leftTypeGroup.SelectedIndex = 0;
            m_rightTypeGroup.SelectedIndex = (m_interactedAsEntity is MyCharacter) || (m_interactedAsEntity is MyInventoryBagEntity) ? 0 : 1;
            m_leftFilterGroup.SelectedIndex = 0;
            m_rightFilterGroup.SelectedIndex = 0;

            LeftTypeGroup_SelectedChanged(m_leftTypeGroup);
            RightTypeGroup_SelectedChanged(m_rightTypeGroup);
            SetLeftFilter(null);
            SetRightFilter(null);

            m_leftTypeGroup.SelectedChanged += LeftTypeGroup_SelectedChanged;
            m_rightTypeGroup.SelectedChanged += RightTypeGroup_SelectedChanged;

            m_leftFilterAllButton.SelectedChanged += (button) => { if (button.Selected) SetLeftFilter(null); };
            m_leftFilterEnergyButton.SelectedChanged += (button) => { if (button.Selected) SetLeftFilter(MyInventoryOwnerTypeEnum.Energy); };
            m_leftFilterStorageButton.SelectedChanged += (button) => { if (button.Selected) SetLeftFilter(MyInventoryOwnerTypeEnum.Storage); };
            m_leftFilterSystemButton.SelectedChanged += (button) => { if (button.Selected) SetLeftFilter(MyInventoryOwnerTypeEnum.System); };

            m_rightFilterAllButton.SelectedChanged += (button) => { if (button.Selected) SetRightFilter(null); };
            m_rightFilterEnergyButton.SelectedChanged += (button) => { if (button.Selected) SetRightFilter(MyInventoryOwnerTypeEnum.Energy); };
            m_rightFilterStorageButton.SelectedChanged += (button) => { if (button.Selected) SetRightFilter(MyInventoryOwnerTypeEnum.Storage); };
            m_rightFilterSystemButton.SelectedChanged += (button) => { if (button.Selected) SetRightFilter(MyInventoryOwnerTypeEnum.System); };

            if (m_interactedAsEntity == null)
            {
                m_leftGridButton.Enabled = false;
                m_rightGridButton.Enabled = false;
                m_rightTypeGroup.SelectedIndex = 0;
            }

            RefreshSelectedInventoryItem();
            ProfilerShort.End();
        }

        public void Close()
        {
            foreach (var system in m_registeredConveyorSystems)
            {
                system.BlockAdded -= ConveyorSystem_BlockAdded;
                system.BlockRemoved -= ConveyorSystem_BlockRemoved;
            }
            m_registeredConveyorSystems.Clear();
            
            m_leftTypeGroup.Clear();
            m_leftFilterGroup.Clear();
            m_rightTypeGroup.Clear();
            m_rightFilterGroup.Clear();
            m_controlsDisabledWhileDragged.Clear();

            m_leftOwnersControl = null;
            m_leftSuitButton = null;
            m_leftGridButton = null;
            m_leftFilterStorageButton = null;
            m_leftFilterSystemButton = null;
            m_leftFilterEnergyButton = null;
            m_leftFilterAllButton = null;
            m_rightOwnersControl = null;
            m_rightSuitButton = null;
            m_rightGridButton = null;
            m_rightFilterStorageButton = null;
            m_rightFilterSystemButton = null;
            m_rightFilterEnergyButton = null;
            m_rightFilterAllButton = null;
            m_throwOutButton = null;
            m_dragAndDrop = null;
            m_dragAndDropInfo = null;
            m_focusedOwnerControl = null;
            m_focusedGridControl = null;
            m_selectedInventory = null;

            m_hideEmptyLeft.IsCheckedChanged      -= HideEmptyLeft_Checked;
            m_hideEmptyRight.IsCheckedChanged     -= HideEmptyRight_Checked;
            m_blockSearchLeft.TextChanged         -= BlockSearchLeft_TextChanged;
            m_blockSearchClearLeft.ButtonClicked  -= BlockSearchClearLeft_ButtonClicked;
            m_blockSearchRight.TextChanged        -= BlockSearchRight_TextChanged;
            m_blockSearchClearRight.ButtonClicked -= BlockSearchClearRight_ButtonClicked;

            m_hideEmptyLeft         = null;
            m_hideEmptyLeftLabel    = null;
            m_hideEmptyRight        = null;
            m_hideEmptyRightLabel   = null;
            m_blockSearchLeft       = null;
            m_blockSearchClearLeft  = null;
            m_blockSearchRight      = null;
            m_blockSearchClearRight = null;
        }

        private void StartDragging(MyDropHandleType dropHandlingType, MyGuiControlGrid gridControl, ref MyGuiControlGrid.EventArgs args)
        {
            m_dragAndDropInfo = new MyDragAndDropInfo();
            m_dragAndDropInfo.Grid = gridControl;
            m_dragAndDropInfo.ItemIndex = args.ItemIndex;

            DisableInvalidWhileDragging();

            var draggingItem = m_dragAndDropInfo.Grid.GetItemAt(m_dragAndDropInfo.ItemIndex);
            m_dragAndDrop.StartDragging(dropHandlingType, args.Button, draggingItem, m_dragAndDropInfo, includeTooltip: false);
        }

        private void DisableInvalidWhileDragging()
        {
            var draggingItem = m_dragAndDropInfo.Grid.GetItemAt(m_dragAndDropInfo.ItemIndex);
            var invItem = (MyPhysicalInventoryItem)draggingItem.UserData;
            var srcInventory = (MyInventory)m_dragAndDropInfo.Grid.UserData;
            DisableUnacceptingInventoryControls(invItem, m_leftOwnersControl);
            DisableUnacceptingInventoryControls(invItem, m_rightOwnersControl);
            DisableUnreachableInventoryControls(srcInventory, invItem, m_leftOwnersControl);
            DisableUnreachableInventoryControls(srcInventory, invItem, m_rightOwnersControl);
        }

        private void DisableUnacceptingInventoryControls(MyPhysicalInventoryItem item, MyGuiControlList list)
        {
            foreach (var control in list.Controls.GetVisibleControls())
            {
                if (!control.Enabled)
                    continue;

                var ownerControl = (MyGuiControlInventoryOwner)control;
                var owner = ownerControl.InventoryOwner;
                for (int i = 0; i < owner.InventoryCount; ++i)
                {
                    var inventory = owner.GetInventory(i);
                    if (!inventory.CanItemsBeAdded(0, item.Content.GetId()))
                    {
                        ownerControl.ContentGrids[i].Enabled = false;
                        m_controlsDisabledWhileDragged.Add(ownerControl.ContentGrids[i]);
                    }
                }
            }
        }

        private Predicate<IMyConveyorEndpoint> m_endpointPredicate;
        private IMyConveyorEndpointBlock m_interactedEndpointBlock;
        private bool EndpointPredicate(IMyConveyorEndpoint endpoint)
        {
            return (endpoint.CubeBlock != null && endpoint.CubeBlock.HasInventory) || endpoint.CubeBlock == m_interactedEndpointBlock;
        }

        private void DisableUnreachableInventoryControls(MyInventory srcInventory, MyPhysicalInventoryItem item, MyGuiControlList list)
        {
            bool fromUser = srcInventory.Owner == m_userAsOwner;
            bool fromInteracted = srcInventory.Owner == m_interactedAsOwner;

            // srcEndpoint will be the endpoint from which we search the graph
            var srcInventoryOwner = srcInventory.Owner;
            IMyConveyorEndpointBlock srcEndpoint = null;
            // Search the interacted's graph if we want to transfer from the user
            if (fromUser)
            {
                if (m_interactedAsEntity != null)
                    srcEndpoint = m_interactedAsEntity as IMyConveyorEndpointBlock;
            }
            else if (srcInventoryOwner != null)
            {
                srcEndpoint = srcInventoryOwner as IMyConveyorEndpointBlock;
            }

            IMyConveyorEndpointBlock interactedEndpoint = null;
            if (m_interactedAsEntity != null)
            {
                interactedEndpoint = m_interactedAsEntity as IMyConveyorEndpointBlock;
            }

            if (srcEndpoint != null)
            {
                long ownerId = MySession.Static.LocalPlayerId;
                m_interactedEndpointBlock = interactedEndpoint;
                MyGridConveyorSystem.AppendReachableEndpoints(srcEndpoint.ConveyorEndpoint, ownerId, m_reachableInventoryOwners, item, m_endpointPredicate);
            }

            foreach (var control in list.Controls.GetVisibleControls())
            {
                if (!control.Enabled)
                    continue;

                var ownerControl = (MyGuiControlInventoryOwner)control;
                var owner = ownerControl.InventoryOwner;

                IMyConveyorEndpoint endpoint = null;
                var ownerBlock = owner as IMyConveyorEndpointBlock;
                if (ownerBlock != null)
                    endpoint = ownerBlock.ConveyorEndpoint;

                // TODO: Make some of these as functions so we don't have to call it even when not used due to lazy evaluation
                bool transferIsLocal = owner == srcInventoryOwner;
                bool transferIsClose = (fromUser && owner == m_interactedAsOwner) || (fromInteracted && owner == m_userAsOwner);
                bool transferIsFar = !transferIsLocal && !transferIsClose;
                bool endpointUnreachable = !m_reachableInventoryOwners.Contains(endpoint);
                bool interactedReachable = interactedEndpoint != null && m_reachableInventoryOwners.Contains(interactedEndpoint.ConveyorEndpoint);
                // If interacted is reachable but does not have inventory, than you cant take anything out from it.
                // WARNING: no need for check of null on m_interactedAsEntity, because interactedEndpoint is checked above already (that will be null also if the other is)
                bool toOwnerThroughInteracted = owner == m_userAsOwner && interactedReachable && m_interactedAsEntity.HasInventory;

                if (transferIsFar && endpointUnreachable && !toOwnerThroughInteracted)
                {
                    for (int i = 0; i < owner.InventoryCount; ++i)
                    {
                        if (!ownerControl.ContentGrids[i].Enabled)
                            continue;

                        ownerControl.ContentGrids[i].Enabled = false;
                        m_controlsDisabledWhileDragged.Add(ownerControl.ContentGrids[i]);
                    }
                }
            }

            m_reachableInventoryOwners.Clear();
        }

        private void GetGridInventories(MyCubeGrid grid, List<MyEntity> outputInventories)
        {
            Debug.Assert(outputInventories != null);

            //if interacted entity grid is not the same as open inventory interacted entity, then no inventory
            //interacted entity = null if character menu (in which case no inventories should be available)
            //if(m_openInventoryInteractedAsEntity == null || ((m_openInventoryInteractedAsEntity.Parent) as MyCubeGrid).EntityId != grid.EntityId)
            //    return;

            if (grid != null)
            {
                foreach (var block in grid.GridSystems.ConveyorSystem.InventoryBlocks)
                {
                    if ((block is Sandbox.Game.Entities.Cube.MyTerminalBlock) &&
                        !(block as Sandbox.Game.Entities.Cube.MyTerminalBlock).HasLocalPlayerAccess())
                        continue;
                    if (m_interactedAsEntity != block && block is Sandbox.Game.Entities.Cube.MyTerminalBlock && !(block as Sandbox.Game.Entities.Cube.MyTerminalBlock).ShowInInventory)
                        continue;

                    outputInventories.Add(block);
                }
            }
        }

        private void CreateInventoryControlInList(MyEntity owner, MyGuiControlList listControl)
        {
            List<MyEntity> inventories = new List<MyEntity>();
            if (owner != null)
                inventories.Add(owner);

            CreateInventoryControlsInList(inventories, listControl);
        }

        private void CreateInventoryControlsInList(List<MyEntity> owners, MyGuiControlList listControl, MyInventoryOwnerTypeEnum? filterType = null)
        {
            if (listControl.Controls.Contains(m_focusedOwnerControl))
                m_focusedOwnerControl = null;

            List<MyGuiControlBase> inventoryControlList = new List<MyGuiControlBase>();

            foreach (var owner in owners)
            {
                if (!(owner != null && owner.HasInventory))
                    continue;

                if (filterType.HasValue && (owner as MyEntity).InventoryOwnerType() != filterType)
                    continue;

                Vector4 labelColor = Color.White.ToVector4();
                if (owner is MyCubeBlock)
                {
                    labelColor = m_colorHelper.GetGridColor((owner as MyCubeBlock).CubeGrid).ToVector4();
                }

                var ownerControl = new MyGuiControlInventoryOwner(owner, labelColor);
                ownerControl.Size = new Vector2(listControl.Size.X - 0.045f, ownerControl.Size.Y);
                ownerControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                foreach (var grid in ownerControl.ContentGrids)
                {
                    grid.ItemSelected += grid_ItemSelected;
                    grid.ItemDragged += grid_ItemDragged;
                    grid.ItemDoubleClicked += grid_ItemDoubleClicked;
                    grid.ItemClicked += grid_ItemClicked;
                }
                ownerControl.SizeChanged += inventoryControl_SizeChanged;
                ownerControl.InventoryContentsChanged += ownerControl_InventoryContentsChanged;

                if (owner is MyCubeBlock)
                    ownerControl.Enabled = (owner as MyCubeBlock).IsFunctional;

                // Put inventory of interacted block or character on first position.
                if (owner == m_interactedAsOwner ||
                    owner == m_userAsOwner)
                {
                    inventoryControlList.Insert(0, ownerControl);
                }
                else
                {
                    //sort by name (Inventory filters ticket)
                    inventoryControlList.Add(ownerControl);
                }
            
            }
            listControl.InitControls(inventoryControlList);
        }

        private void ShowAmountTransferDialog(MyPhysicalInventoryItem inventoryItem, Action<float> onConfirmed)
        {
            var amount = inventoryItem.Amount;
            var obType = inventoryItem.Content.TypeId;
            int maxDecimalDigits = 0;
            bool asInteger = true;
            if (obType == typeof(MyObjectBuilder_Ore) ||
                obType == typeof(MyObjectBuilder_Ingot))
            {
                maxDecimalDigits = MyInventoryConstants.GUI_DISPLAY_MAX_DECIMALS;
                asInteger = false;
            }
            var dialog = new MyGuiScreenDialogAmount(0, (float)amount, MyCommonTexts.DialogAmount_AddAmountCaption, minMaxDecimalDigits: maxDecimalDigits, parseAsInteger: asInteger);
            dialog.OnConfirmed += onConfirmed;
            MyGuiSandbox.AddScreen(dialog);
        }

        private bool TransferToOppositeFirst(MyPhysicalInventoryItem item)
        {
            var srcControl = m_focusedOwnerControl;
            var otherInventoriesControl = (srcControl.Owner == m_leftOwnersControl) ? m_rightOwnersControl : m_leftOwnersControl;
            var dstControlEnumerator = otherInventoriesControl.Controls.GetEnumerator();
            MyGuiControlInventoryOwner dstControl = null;//()(dstControlEnumerator.MoveNext() ? dstControlEnumerator.Current : null);
            while (dstControlEnumerator.MoveNext())
                if (dstControlEnumerator.Current.Visible)
                {
                    dstControl = dstControlEnumerator.Current as MyGuiControlInventoryOwner;
                    break;
                }
            if (dstControl == null || !dstControl.Enabled)
                return false;

            bool localTransfer = ((srcControl.InventoryOwner == m_userAsOwner || srcControl.InventoryOwner == m_interactedAsOwner) &&
                                  (dstControl.InventoryOwner == m_userAsOwner || dstControl.InventoryOwner == m_interactedAsOwner));

            if (!localTransfer)
            {
                bool fromCharacter = srcControl.InventoryOwner is MyCharacter;
                bool toCharacter = dstControl.InventoryOwner is MyCharacter;

                IMyConveyorEndpointBlock srcEndpoint = srcControl.InventoryOwner == null ? null : (fromCharacter ? m_interactedAsOwner : srcControl.InventoryOwner) as IMyConveyorEndpointBlock;
                IMyConveyorEndpointBlock dstEndpoint = dstControl.InventoryOwner == null ? null : (toCharacter ? m_interactedAsOwner : dstControl.InventoryOwner) as IMyConveyorEndpointBlock;

                if (srcEndpoint == null || dstEndpoint == null)
                    return false;

				try
				{
					MyGridConveyorSystem.AppendReachableEndpoints(srcEndpoint.ConveyorEndpoint, MySession.Static.LocalPlayerId, m_reachableInventoryOwners, item, m_endpointPredicate);

					if (!m_reachableInventoryOwners.Contains(dstEndpoint.ConveyorEndpoint))
						return false;
				}
				finally
				{
					m_reachableInventoryOwners.Clear();
				}

                if (!MyGridConveyorSystem.Reachable(srcEndpoint.ConveyorEndpoint, dstEndpoint.ConveyorEndpoint))
                    return false;
            }

            var dstOwner = dstControl.InventoryOwner;
            var srcOwner = m_focusedOwnerControl.InventoryOwner;
            var srcInventory = (MyInventory)m_focusedGridControl.UserData;

            MyInventory dstInventory = null;
            for (int i = 0; i < dstOwner.InventoryCount; ++i)
            {
                var tmp = dstOwner.GetInventory(i) as MyInventory;
                System.Diagnostics.Debug.Assert(tmp as MyInventory != null, "Null or unexpected inventory type!");

                if (tmp.CheckConstraint(item.Content.GetId()))
                {
                    dstInventory = tmp;
                    break;
                }
            }

            if (dstInventory == null)
                return false;

            MyInventory.TransferByUser(srcInventory, dstInventory, srcInventory.GetItems()[m_focusedGridControl.SelectedIndex.Value].ItemId, amount: item.Amount);
            return true;
        }

        private void SetLeftFilter(MyInventoryOwnerTypeEnum? filterType)
        {
            m_leftFilterType = filterType;
            if (m_leftShowsGrid)
            {
                CreateInventoryControlsInList(m_interactedGridOwners, m_leftOwnersControl, m_leftFilterType);
                m_blockSearchLeft.Text = m_blockSearchLeft.Text;
            }
            RefreshSelectedInventoryItem();
        }

        private void SetRightFilter(MyInventoryOwnerTypeEnum? filterType)
        {
            m_rightFilterType = filterType;
            if (m_rightShowsGrid)
            {
                CreateInventoryControlsInList(m_interactedGridOwners, m_rightOwnersControl, m_rightFilterType);
                m_blockSearchRight.Text = m_blockSearchRight.Text;
            }
            RefreshSelectedInventoryItem();
        }

        private void RefreshSelectedInventoryItem()
        {
            if (m_focusedGridControl != null)
            {
                m_selectedInventory = (MyInventory)m_focusedGridControl.UserData;
                var selectedItem = m_focusedGridControl.SelectedItem;
                m_selectedInventoryItem = (selectedItem != null) ? (MyPhysicalInventoryItem?)selectedItem.UserData
                                                                 : null;
            }
            else
            {
                m_selectedInventory = null;
                m_selectedInventoryItem = null;
            }

            if (m_throwOutButton != null)
            {
                m_throwOutButton.Enabled = m_selectedInventoryItem.HasValue &&
                                           (m_focusedOwnerControl != null && m_focusedOwnerControl.InventoryOwner == m_userAsOwner);
            }
        }

        private MyCubeGrid GetInteractedGrid()
        {
            return (m_interactedAsEntity != null) ? m_interactedAsEntity.Parent as MyCubeGrid : null;
        }

        private void ApplyTypeGroupSelectionChange(
            MyGuiControlRadioButtonGroup obj,
            ref bool showsGrid,
            MyGuiControlList targetControlList,
            MyInventoryOwnerTypeEnum? filterType,
            MyGuiControlRadioButtonGroup filterButtonGroup,
            MyGuiControlCheckbox showEmpty,
            MyGuiControlLabel    showEmptyLabel,
            MyGuiControlTextbox blockSearch,
            MyGuiControlButton  blockSearchClear,
            bool isLeftControllist)
        {
            switch (obj.SelectedButton.VisualStyle)
            {
                case MyGuiControlRadioButtonStyleEnum.FilterCharacter:
                    showsGrid = false;

                    showEmpty.Visible        = false;
                    showEmptyLabel.Visible   = false;
                    blockSearch.Visible      = false;
                    blockSearchClear.Visible = false;

                    targetControlList.Position = (isLeftControllist) ? m_leftControlListPosition : m_rightControlListPosition;
                    targetControlList.Size     = m_controlListFullSize;

                    // hack to allow looting, force user on left and interacted corpse on right
                    if (targetControlList == m_leftOwnersControl)
                        CreateInventoryControlInList(m_userAsOwner, targetControlList);
                    else
                        CreateInventoryControlInList(m_interactedAsOwner, targetControlList);
                    break;

                case MyGuiControlRadioButtonStyleEnum.FilterGrid:
                    showsGrid = true;
                    CreateInventoryControlsInList(m_interactedGridOwners, targetControlList, filterType);

                    showEmpty.Visible        = true;
                    showEmptyLabel.Visible   = true;
                    blockSearch.Visible      = true;
                    blockSearchClear.Visible = true;

                    blockSearch.Text = blockSearch.Text;

                    targetControlList.Position = (isLeftControllist) ? m_leftControlListPosWithSearch : m_rightControlListPosWithSearch;
                    targetControlList.Size     = m_controlListSizeWithSearch;
                    break;

                default:
                    Debug.Assert(false, "Invalid branch!");
                    break;
            }
            foreach (var button in filterButtonGroup)
                button.Visible = button.Enabled = showsGrid;

            RefreshSelectedInventoryItem();
        }

        #region Event handling
        private void ConveyorSystem_BlockAdded(MyCubeBlock obj)
        {
            m_interactedGridOwners.Add(obj);
            if (m_leftShowsGrid) LeftTypeGroup_SelectedChanged(m_leftTypeGroup);
            if (m_rightShowsGrid) RightTypeGroup_SelectedChanged(m_rightTypeGroup);

            if (m_dragAndDropInfo != null)
            {
                ClearDisabledControls();
                DisableInvalidWhileDragging();
            }
        }

        private void ConveyorSystem_BlockRemoved(MyCubeBlock obj)
        {
            m_interactedGridOwners.Remove(obj);
            if (m_leftShowsGrid) LeftTypeGroup_SelectedChanged(m_leftTypeGroup);
            if (m_rightShowsGrid) RightTypeGroup_SelectedChanged(m_rightTypeGroup);

            if (m_dragAndDropInfo != null)
            {
                ClearDisabledControls();
                DisableInvalidWhileDragging();
            }
        }

        private void LeftTypeGroup_SelectedChanged(MyGuiControlRadioButtonGroup obj)
        {
            ApplyTypeGroupSelectionChange(obj, ref m_leftShowsGrid,
                                          m_leftOwnersControl,
                                          m_leftFilterType,
                                          m_leftFilterGroup,
                                          m_hideEmptyLeft,
                                          m_hideEmptyLeftLabel,
                                          m_blockSearchLeft,
                                          m_blockSearchClearLeft, true);
            m_leftOwnersControl.SetScrollBarPage(0);
        }

        private void RightTypeGroup_SelectedChanged(MyGuiControlRadioButtonGroup obj)
        {
            ApplyTypeGroupSelectionChange(obj, ref m_rightShowsGrid,
                                          m_rightOwnersControl,
                                          m_rightFilterType,
                                          m_rightFilterGroup,
                                          m_hideEmptyRight,
                                          m_hideEmptyRightLabel,
                                          m_blockSearchRight,
                                          m_blockSearchClearRight, false);
            m_rightOwnersControl.SetScrollBarPage(0);
        }

        private void throwOutButton_OnButtonClick(MyGuiControlButton sender)
        {
            var owner = m_focusedOwnerControl.InventoryOwner;
            var ownerAsEntity = owner as MyEntity;
            if (m_selectedInventoryItem.HasValue && ownerAsEntity != null)
            {
                // Making copy, since removing this item from inventory will change selection to a different one.
                var thrownItem = m_selectedInventoryItem.Value;
                Debug.Assert(m_focusedGridControl.SelectedIndex.HasValue, "Focused grid has no selected item.");
                if (m_focusedGridControl.SelectedIndex.HasValue)
                {
                    m_selectedInventory.DropItem(m_focusedGridControl.SelectedIndex.Value, thrownItem.Amount);
                }
             

                //var forward = ownerAsEntity.WorldMatrix.Forward;
                //var up = ownerAsEntity.WorldMatrix.Up;

                //MyFloatingObjects.Spawn(thrownItem, ownerAsEntity.GetPosition() + forward + up, forward, up, ownerAsEntity.Physics);
            }

            //MyGuiAudio.PlaySound(MyGuiSounds.PlayDropItem);

            RefreshSelectedInventoryItem();
        }

        private void interactedObjectButton_OnButtonClick(MyGuiControlButton sender)
        {
            CreateInventoryControlInList(m_interactedAsOwner, m_rightOwnersControl);
        }

        private void grid_ItemSelected(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            var sourceGrid = (MyGuiControlGrid)sender;
            if (m_focusedGridControl != null &&
                m_focusedGridControl != sourceGrid)
            {
                m_focusedGridControl.SelectedIndex = null;
            }

            m_focusedGridControl = sourceGrid;
            m_focusedOwnerControl = (MyGuiControlInventoryOwner)sourceGrid.Owner;

            RefreshSelectedInventoryItem();
        }

        private void grid_ItemDragged(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            if (MyInput.Static.IsAnyShiftKeyPressed() ||
                MyInput.Static.IsAnyCtrlKeyPressed())
                return;

            StartDragging(MyDropHandleType.MouseRelease, sender, ref eventArgs);
        }

        private void grid_ItemDoubleClicked(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            if (MyInput.Static.IsAnyShiftKeyPressed() ||
                MyInput.Static.IsAnyCtrlKeyPressed())
                return;

            var item = (MyPhysicalInventoryItem)sender.GetItemAt(eventArgs.ItemIndex).UserData;
            bool transfered = TransferToOppositeFirst(item);
            RefreshSelectedInventoryItem();
            //MyAudio.Static.PlayCue(transfered ? MySoundCuesEnum.HudMouseClick : MySoundCuesEnum.HudUnable);
        }

        private void grid_ItemClicked(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            bool ctrlPressed = MyInput.Static.IsAnyCtrlKeyPressed();
            bool shiftPressed = MyInput.Static.IsAnyShiftKeyPressed();
            if (ctrlPressed || shiftPressed)
            {
                var item = (MyPhysicalInventoryItem)sender.GetItemAt(eventArgs.ItemIndex).UserData;
                item.Amount = MyFixedPoint.Min((shiftPressed ? 100 : 1) * (ctrlPressed ? 10 : 1), item.Amount);
                bool transfered = TransferToOppositeFirst(item);
                RefreshSelectedInventoryItem();
                //MyAudio.Static.PlayCue(transfered ? MySoundCuesEnum.HudMouseClick : MySoundCuesEnum.HudUnable);
            }
        }

        private void dragDrop_OnItemDropped(object sender, MyDragAndDropEventArgs eventArgs)
        {
            if (eventArgs.DropTo != null)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudItem);

                MyPhysicalInventoryItem inventoryItem = (MyPhysicalInventoryItem)eventArgs.Item.UserData;

                var srcGrid = eventArgs.DragFrom.Grid;
                var dstGrid = eventArgs.DropTo.Grid;

                var srcControl = (MyGuiControlInventoryOwner)srcGrid.Owner;
                var dstControl = dstGrid.Owner as MyGuiControlInventoryOwner;
                Debug.Assert(dstControl != null);
                if (dstControl == null)
                    return;
                var srcInventory = (MyInventory)srcGrid.UserData;
                var dstInventory = (MyInventory)dstGrid.UserData;

                if (srcGrid == dstGrid)
                {
                    //GR: Why alter ItemIndex? This caused invalid swapping of items
                    //if (eventArgs.DragFrom.ItemIndex < eventArgs.DropTo.ItemIndex)
                    //    eventArgs.DropTo.ItemIndex++;
                    if (eventArgs.DragButton == MySharedButtonsEnum.Secondary)
                    {
                        ShowAmountTransferDialog(inventoryItem, delegate(float amount)
                        {
                            if (amount == 0)
                                return;
                            if (!srcInventory.IsItemAt(eventArgs.DragFrom.ItemIndex))
                                return;
                            inventoryItem.Amount = (MyFixedPoint)amount;
                            CorrectItemAmount(ref inventoryItem);
                            MyInventory.TransferByUser(srcInventory, srcInventory, inventoryItem.ItemId, eventArgs.DropTo.ItemIndex, inventoryItem.Amount);
                            if (dstGrid.IsValidIndex(eventArgs.DropTo.ItemIndex))
                                dstGrid.SelectedIndex = eventArgs.DropTo.ItemIndex;
                            else
                                dstGrid.SelectLastItem();
                            RefreshSelectedInventoryItem();
                        });
                    }
                    else
                    {
                        MyInventory.TransferByUser(srcInventory, srcInventory, inventoryItem.ItemId, eventArgs.DropTo.ItemIndex);
                        if (dstGrid.IsValidIndex(eventArgs.DropTo.ItemIndex))
                            dstGrid.SelectedIndex = eventArgs.DropTo.ItemIndex;
                        else
                            dstGrid.SelectLastItem();
                        RefreshSelectedInventoryItem();
                    }
                }
                else if (eventArgs.DragButton == MySharedButtonsEnum.Secondary)
                {
                    ShowAmountTransferDialog(inventoryItem, delegate(float amount)
                    {
                        if (amount == 0)
                            return;
                        if (!srcInventory.IsItemAt(eventArgs.DragFrom.ItemIndex))
                            return;
                        inventoryItem.Amount = (MyFixedPoint)amount;
                        CorrectItemAmount(ref inventoryItem);
                        MyInventory.TransferByUser(srcInventory, dstInventory, inventoryItem.ItemId, eventArgs.DropTo.ItemIndex, inventoryItem.Amount);
                        RefreshSelectedInventoryItem();
                    });
                }
                else
                {
                    MyInventory.TransferByUser(srcInventory, dstInventory, inventoryItem.ItemId, eventArgs.DropTo.ItemIndex);
                    RefreshSelectedInventoryItem();
                }
            }

            ClearDisabledControls();

            m_dragAndDropInfo = null;
        }

        private void ClearDisabledControls()
        {
            foreach (var control in m_controlsDisabledWhileDragged)
                control.Enabled = true;
            m_controlsDisabledWhileDragged.Clear();
        }

        private static void CorrectItemAmount(ref MyPhysicalInventoryItem dragItem)
        {
            var obType = dragItem.Content.TypeId;
        }

        private void inventoryControl_SizeChanged(MyGuiControlBase obj)
        {
            ((MyGuiControlList)obj.Owner).Recalculate();
        }

        private void ownerControl_InventoryContentsChanged(MyGuiControlInventoryOwner control)
        {
            if (control == m_focusedOwnerControl)
                RefreshSelectedInventoryItem();

            UpdateDisabledControlsWhileDragging(control);
        }

        private void UpdateDisabledControlsWhileDragging(MyGuiControlInventoryOwner control)
        {
            if (m_controlsDisabledWhileDragged.Count == 0) return;

            var owner = control.InventoryOwner;
            for (int i = 0; i < owner.InventoryCount; ++i)
            {
                var gridControl = control.ContentGrids[i];

                if (m_controlsDisabledWhileDragged.Contains(gridControl))
                {
                    if (gridControl.Enabled)
                    {
                        gridControl.Enabled = false;
                    }
                }
            }
        }

        private void HideEmptyLeft_Checked(MyGuiControlCheckbox obj)
        {
            if (m_leftFilterType == MyInventoryOwnerTypeEnum.Character)
                return;

            SearchInList(m_blockSearchLeft, m_leftOwnersControl, obj.IsChecked);
        }

        private void HideEmptyRight_Checked(MyGuiControlCheckbox obj)
        {
            if (m_rightFilterType == MyInventoryOwnerTypeEnum.Character)
                return;

            SearchInList(m_blockSearchRight, m_rightOwnersControl, obj.IsChecked);
        }

        private void BlockSearchLeft_TextChanged(MyGuiControlTextbox obj)
        {
            if (m_leftFilterType == MyInventoryOwnerTypeEnum.Character)
                return;

            SearchInList(obj, m_leftOwnersControl, m_hideEmptyLeft.IsChecked);
        }

        private void BlockSearchRight_TextChanged(MyGuiControlTextbox obj)
        {
            if (m_rightFilterType == MyInventoryOwnerTypeEnum.Character)
                return;

            SearchInList(obj, m_rightOwnersControl, m_hideEmptyRight.IsChecked);
        }

        private void BlockSearchClearLeft_ButtonClicked(MyGuiControlButton obj)
        {
            m_blockSearchLeft.Text = "";
        }

        private void BlockSearchClearRight_ButtonClicked(MyGuiControlButton obj)
        {
            m_blockSearchRight.Text = "";
        }
        #endregion

        private void SearchInList(MyGuiControlTextbox searchText, MyGuiControlList list, bool hideEmpty)
        {
            if (searchText.Text != "")
            {
                String[] tmpSearch = searchText.Text.ToLower().Split(' ');

                foreach (var item in list.Controls)
                {
                    var owner   = (item as MyGuiControlInventoryOwner).InventoryOwner;
                    var tmp     = (owner as MyEntity).DisplayNameText.ToString().ToLower();
                    var add     = true;
                    var isEmpty = true;

                    foreach (var search in tmpSearch)
                    {
                        if (!tmp.Contains(search))
                        {
                            add = false;
                            break;
                        }
                    }

                    if (!add)
                    {
                        for (int i = 0; i < owner.InventoryCount; i++)
                        {
                            System.Diagnostics.Debug.Assert(owner.GetInventory(i) as MyInventory != null, "Null or other inventory type!");

                            foreach (var inventoryItem in (owner.GetInventory(i) as MyInventory).GetItems())
                            {
                                bool matches = true;
                                string inventoryItemName = MyDefinitionManager.Static.GetPhysicalItemDefinition(inventoryItem.Content).DisplayNameText.ToString().ToLower();
                                foreach (var search in tmpSearch)
                                {
                                    if (!inventoryItemName.Contains(search))
                                    {
                                        matches = false;
                                        break;
                                    }
                                }

                                if (matches)
                                {
                                    add = true;
                                    break;
                                }
                            }
                            if (add)
                            {
                                break;
                            }
                        }
                    }

                    if (add)
                    {
                        for (int i = 0; i < owner.InventoryCount; ++i)
                        {
                            if (owner.GetInventory(i).CurrentMass != 0)
                            {
                                isEmpty = false;
                                break;
                            }
                        }
                        item.Visible = (hideEmpty && isEmpty) ? false : true;
                    }
                    else
                        item.Visible = false;
                }
            }
            else
            {
                foreach (var item in list.Controls)
                {
                    bool isEmpty = true;
                    var owner = (item as MyGuiControlInventoryOwner).InventoryOwner;

                    for (int i = 0; i < owner.InventoryCount; ++i)
                    {
                        if (owner.GetInventory(i).CurrentMass != 0)
                        {
                            isEmpty = false;
                            break;
                        }
                    }

                    if (hideEmpty && isEmpty)
                        item.Visible = false;
                    else
                        item.Visible = true;
                }
            }
            list.SetScrollBarPage();
        }

    }
}
