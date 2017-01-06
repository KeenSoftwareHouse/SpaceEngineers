
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Terminal
{
    class MyTerminalPropertiesController
    {
        #region structs and user data
        //sort this in the order you want it to appear in properties screen
        private enum MyCubeGridConnectionStatus
        {
            PhysicallyConnected = 0,
            Connected = 1,
            OutOfBroadcastingRange = 2,
            OutOfReceivingRange = 3,
            Me = 4,
            IsPreviewGrid = 5,
        }

        private enum MyRefuseReason
        {
            NoRemoteControl,
            NoMainRemoteControl,
            NoStableConnection,
            NoOwner,
            NoProblem,
            PlayerBroadcastOff
        }

        struct UserData
        {
            public long GridEntityId;
            public bool IsSelectable;
        };

        private class CubeGridInfo
        {
            public long EntityId;
            public float Distance;
            public string Name;
            public StringBuilder AppendedDistance;
            public MyCubeGridConnectionStatus Status;

            public override bool Equals(object obj)
            {
                if (!(obj is CubeGridInfo))
                    return false;

                var otherObj = obj as CubeGridInfo;
                string name = Name == null ? "" : Name;
                string otherName = otherObj.Name == null ? "" : otherObj.Name;
                return
                    EntityId.Equals(otherObj.EntityId) &&
                    name.Equals(otherName) &&
                    AppendedDistance.Equals(otherObj.AppendedDistance) &&
                    Status == otherObj.Status;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int result = EntityId.GetHashCode();
                    string name = Name == null ? "" : Name;
                    result = (result * 397) ^ name.GetHashCode();
                    result = (result * 397) ^ AppendedDistance.GetHashCode();
                    result = (result * 397) ^ (int) Status;
                    return result;
                }
            }
        }


        #endregion

        #region attributes
        private MyGuiControlCombobox m_shipsInRange;
        private MyGuiControlButton m_button;
        private MyGuiControlTable m_shipsData;

        private MyEntity m_interactedEntityRepresentative, m_openInventoryInteractedEntityRepresentative;
        private int m_columnToSort;
        #endregion

        #region actions
        //Actions to send to GUI Terminal
        public event Action ButtonClicked;

        #endregion

        #region initialize on terminal open
        public void Init(MyGuiControlParent menuParent, MyGuiControlParent panelParent, MyEntity interactedEntity, MyEntity openInventoryInteractedEntity)
        {
            m_interactedEntityRepresentative = GetInteractedEntityRepresentative(interactedEntity);
            m_openInventoryInteractedEntityRepresentative = GetInteractedEntityRepresentative(openInventoryInteractedEntity);

            if(menuParent == null) MySandboxGame.Log.WriteLine("menuParent is null");
            if(panelParent == null) MySandboxGame.Log.WriteLine("panelParent is null");
            if (menuParent == null || panelParent == null)
                return;
            m_shipsInRange = (MyGuiControlCombobox)menuParent.Controls.GetControlByName("ShipsInRange");
            m_button = (MyGuiControlButton)menuParent.Controls.GetControlByName("SelectShip");
            m_shipsData = (MyGuiControlTable)panelParent.Controls.GetControlByName("ShipsData");

            //sort by status by default
            m_columnToSort = 2;

            m_button.ButtonClicked += Menu_ButtonClicked;
            m_shipsData.ColumnClicked += shipsData_ColumnClicked;
            m_shipsInRange.ItemSelected += shipsInRange_ItemSelected;

            Refresh();
        }

        public void Refresh()
        {
            PopulateMutuallyConnectedCubeGrids(MyAntennaSystem.Static.GetMutuallyConnectedGrids(m_openInventoryInteractedEntityRepresentative));
            PopulateOwnedCubeGrids(GetAllCubeGridsInfo());
        }


        private void PopulateMutuallyConnectedCubeGrids(HashSet<MyAntennaSystem.BroadcasterInfo> playerMutualConnection)
        {
            m_shipsInRange.ClearItems();
            bool isCubeGrid = m_openInventoryInteractedEntityRepresentative is MyCubeGrid;

            //You should always be selectable
            m_shipsInRange.AddItem(m_openInventoryInteractedEntityRepresentative.EntityId, new StringBuilder(m_openInventoryInteractedEntityRepresentative.DisplayName));
            
            foreach (var connection in playerMutualConnection)
                if(m_shipsInRange.TryGetItemByKey(connection.EntityId) == null)
                    m_shipsInRange.AddItem(connection.EntityId, new StringBuilder(connection.Name));

            m_shipsInRange.Visible = true;
            m_button.Visible = true;
            m_shipsInRange.SortItemsByValueText();

            //if the interacted entity is not in the combobox, it means we're watching a disconnected ship so it should be red (somehow)
            if (m_shipsInRange.TryGetItemByKey(m_interactedEntityRepresentative.EntityId) == null)
            {
                if(m_interactedEntityRepresentative is MyCubeGrid)
                    m_shipsInRange.AddItem(m_interactedEntityRepresentative.EntityId, new StringBuilder((m_interactedEntityRepresentative as MyCubeGrid).DisplayName));
            }
            m_shipsInRange.SelectItemByKey(m_interactedEntityRepresentative.EntityId);
        }

        private void PopulateOwnedCubeGrids(HashSet <CubeGridInfo> gridInfoList)
        {
            float scrollBarValue = m_shipsData.ScrollBar.Value;
            m_shipsData.Clear();
            foreach (var gridInfo in gridInfoList)
            {
                UserData data;
                MyGuiControlTable.Row row;
                MyGuiControlTable.Cell nameCell, controlCell, distanceCell, statusCell, accessCell;

                data.GridEntityId = gridInfo.EntityId;
                if (gridInfo.Status == MyCubeGridConnectionStatus.Connected || gridInfo.Status == MyCubeGridConnectionStatus.PhysicallyConnected || gridInfo.Status == MyCubeGridConnectionStatus.Me)
                {
                    StringBuilder minDistance = new StringBuilder();
                    if(gridInfo.Status == MyCubeGridConnectionStatus.Connected)
                        minDistance = gridInfo.AppendedDistance.Append(" m");

                    data.IsSelectable = true;
                    nameCell = new MyGuiControlTable.Cell(new StringBuilder(gridInfo.Name), textColor: Color.White);
                    controlCell = CreateControlCell(gridInfo,true);
                    distanceCell = new MyGuiControlTable.Cell(minDistance, userData: gridInfo.Distance, textColor: Color.White);
                    statusCell = CreateStatusIcons(gridInfo, true);
                    accessCell = CreateTerminalCell(gridInfo, true);
                }

                else
                {
                    data.IsSelectable = false;
                    nameCell = new MyGuiControlTable.Cell(new StringBuilder(gridInfo.Name), textColor: Color.Gray);
                    controlCell = CreateControlCell(gridInfo, false);
                    distanceCell = new MyGuiControlTable.Cell(new StringBuilder("Not Available"), userData: float.MaxValue, textColor: Color.Gray);
                    statusCell = CreateStatusIcons(gridInfo, true);
                    accessCell = CreateTerminalCell(gridInfo, false);
                }

                row = new MyGuiControlTable.Row(data);
                row.AddCell(nameCell);
                row.AddCell(controlCell);
                row.AddCell(distanceCell);
                row.AddCell(statusCell);
                row.AddCell(accessCell);
                m_shipsData.Add(row);
                m_shipsData.SortByColumn(m_columnToSort, MyGuiControlTable.SortStateEnum.Ascending, false);
            }
            m_shipsData.ScrollBar.ChangeValue(scrollBarValue);
        }

        private MyGuiControlTable.Cell CreateControlCell(CubeGridInfo gridInfo, bool isActive)
        {
            MyGuiControlTable.Cell cell = new MyGuiControlTable.Cell();
            Vector2 size = new Vector2(0.11f, m_shipsData.RowHeight * 0.9f);

            MyStringId tooltip = MySpaceTexts.BroadcastScreen_TakeControlButton_ToolTip;
            MyRefuseReason reason = CanTakeRemoteControl(gridInfo);
            switch (reason)
            {
                case MyRefuseReason.NoRemoteControl:
                case MyRefuseReason.NoMainRemoteControl:
                case MyRefuseReason.NoOwner:
                    isActive = false;
                    break;
            }

            cell.Control = new MyGuiControlButton(
                text: MyTexts.Get(MySpaceTexts.BroadcastScreen_TakeControlButton),
                visualStyle: VRage.Game.MyGuiControlButtonStyleEnum.Rectangular,
                size: size,
                textScale: 0.9f,
                onButtonClick: OnButtonClicked_TakeControl
                );
            cell.Control.SetToolTip(tooltip);
            cell.Control.Enabled = isActive;
            m_shipsData.Controls.Add(cell.Control);
            return cell;
        }

        private MyGuiControlTable.Cell CreateTerminalCell(CubeGridInfo gridInfo, bool isActive)
        {
            MyGuiControlTable.Cell cell = new MyGuiControlTable.Cell();
            
            Vector2 size = new Vector2(0.1f, m_shipsData.RowHeight * 0.9f);

            MyStringId tooltip = MySpaceTexts.BroadcastScreen_TerminalButton_ToolTip;
            MyRefuseReason reason = CanTakeTerminal(gridInfo);
            switch (reason)
            {
                case MyRefuseReason.NoStableConnection:
                case MyRefuseReason.PlayerBroadcastOff:
                case MyRefuseReason.NoOwner:
                    isActive = false;
                    break;
            }

            cell.Control = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.Terminal),
                visualStyle: VRage.Game.MyGuiControlButtonStyleEnum.Rectangular,
                size: size,
                textScale: 0.9f,
                onButtonClick: OnButtonClicked_OpenTerminal
                );
            cell.Control.SetToolTip(tooltip);
            cell.Control.Enabled = isActive;
            m_shipsData.Controls.Add(cell.Control);
            return cell;
        }

        private MyGuiControlTable.Cell CreateStatusIcons(CubeGridInfo gridInfo, bool isActive)
        {
            MyGuiControlTable.Cell cell = new MyGuiControlTable.Cell();
            float iconSize = m_shipsData.RowHeight * 0.7f;

            bool antIsActive, keyIsActive, remIsActive;
            antIsActive = keyIsActive = remIsActive = isActive;
            MyStringId antTooltip,remTooltip;
            antTooltip = remTooltip = MyStringId.NullOrEmpty;
            StringBuilder keyTooltip = new StringBuilder();

            MyGuiControlParent gr = new MyGuiControlParent();

            MyRefuseReason reasonT = CanTakeTerminal(gridInfo);
            MyRefuseReason reasonRC = CanTakeRemoteControl(gridInfo);

            //Antenna icon
            switch (reasonT)
            {
                case MyRefuseReason.PlayerBroadcastOff:
                    antIsActive = false;
                    antTooltip = MySpaceTexts.BroadcastScreen_TerminalButton_PlayerBroadcastOffToolTip;
                    break;
                case MyRefuseReason.NoStableConnection:
                    antIsActive = false;
                    antTooltip = MySpaceTexts.BroadcastScreen_TerminalButton_NoStableConnectionToolTip;
                    break;
                case MyRefuseReason.NoOwner:
                case MyRefuseReason.NoProblem:
                    antTooltip = MySpaceTexts.BroadcastScreen_TerminalButton_StableConnectionToolTip;
                    break;
            }
            MyGuiControlImage antenna = new MyGuiControlImage(
                position: new Vector2(-2*iconSize,0),
                size: new Vector2(iconSize, iconSize),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                backgroundTexture: (antIsActive ? MyGuiConstants.BS_ANTENNA_ON : MyGuiConstants.BS_ANTENNA_OFF)
                );
            antenna.SetToolTip(antTooltip);
            gr.Controls.Add(antenna);
            
            //Remote Control icon
            switch (reasonRC)
            {
                case MyRefuseReason.NoRemoteControl:
                    remTooltip = MySpaceTexts.BroadcastScreen_TakeControlButton_NoRemoteToolTip;
                    remIsActive = false;
                    break;
                case MyRefuseReason.NoMainRemoteControl:
                    remTooltip = MySpaceTexts.BroadcastScreen_TakeControlButton_NoMainRemoteControl;
                    remIsActive = false;
                    break;
                case MyRefuseReason.NoOwner:
                case MyRefuseReason.NoProblem:
                    remTooltip = MySpaceTexts.BroadcastScreen_TakeControlButton_RemoteToolTip;
                    break;
            }
            MyGuiControlImage remote = new MyGuiControlImage(
                position: new Vector2(-1 * iconSize, 0),
                size: new Vector2( iconSize, iconSize),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                backgroundTexture: (remIsActive ? MyGuiConstants.BS_REMOTE_ON : MyGuiConstants.BS_REMOTE_OFF)
                );
            remote.SetToolTip(remTooltip);
            gr.Controls.Add(remote);

            //Key icon
            if ((reasonT == MyRefuseReason.NoStableConnection||reasonT == MyRefuseReason.PlayerBroadcastOff ) && reasonRC == MyRefuseReason.NoRemoteControl)
            {
                keyTooltip.Append(MyTexts.Get(MySpaceTexts.BroadcastScreen_UnavailableControlButton));
                keyIsActive = false;
            }
            if (keyIsActive && (reasonT == MyRefuseReason.NoOwner || reasonRC == MyRefuseReason.NoOwner || reasonT == MyRefuseReason.NoStableConnection || reasonT == MyRefuseReason.PlayerBroadcastOff))
            {
                keyIsActive = false;
                keyTooltip.Append(MyTexts.Get(MySpaceTexts.BroadcastScreen_NoOwnership));
            }
            if (reasonT == MyRefuseReason.NoOwner)
            {
                keyTooltip.AppendLine();
                keyTooltip.Append(MyTexts.Get(MySpaceTexts.DisplayName_Block_Antenna));
            }
            if (reasonRC == MyRefuseReason.NoOwner)
            {
                keyTooltip.AppendLine();
                keyTooltip.Append(MyTexts.Get(MySpaceTexts.DisplayName_Block_RemoteControl));
            }
            if (keyIsActive)
            {
                keyTooltip.Append(MyTexts.Get(MySpaceTexts.BroadcastScreen_Ownership));
            }
            MyGuiControlImage key = new MyGuiControlImage(
                size: new Vector2(iconSize, iconSize),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                backgroundTexture: (keyIsActive ? MyGuiConstants.BS_KEY_ON : MyGuiConstants.BS_KEY_OFF)
                );
            key.SetToolTip(keyTooltip.ToString());
            gr.Controls.Add(key);


            cell.Control = gr;
            m_shipsData.Controls.Add(gr);
            return cell;
        }

        #endregion

        #region radio getters






        #endregion

        #region helpers

        private HashSet<CubeGridInfo> GetAllCubeGridsInfo()
        {
            HashSet<CubeGridInfo> output = new HashSet<CubeGridInfo>();
            HashSet<long> AddedItems = new HashSet<long>();

            //add your owned grids
            foreach (var gridId in MySession.Static.LocalHumanPlayer.Grids)
            {
                MyCubeGrid grid;
                if (!MyEntities.TryGetEntityById<MyCubeGrid>(gridId, out grid))
                    continue;

                //GR: If grid is preview grid do not take into account (this is needed for project antennas. Another fix would be do disable broadcasting on projected antennas)
                //Currently commented because we take into account (added Preview ship Status that can be seen in the ship table)
                //if(grid.IsPreview)
                //    continue;

                if (!PlayerOwnsShip(grid))
                    continue;

                var representative = MyAntennaSystem.Static.GetLogicalGroupRepresentative(grid);
                if (AddedItems.Contains(representative.EntityId))
                    continue;
                
                AddedItems.Add(representative.EntityId);
                float dist = GetPlayerGridDistance(representative);
                output.Add(new CubeGridInfo()
                {
                    EntityId = representative.EntityId,
                    Distance = dist,
                    AppendedDistance = new StringBuilder().AppendDecimal(dist, 0),
                    Name = representative.DisplayName,
                    Status = GetShipStatus(representative)
                });
            }

            return output;
        }

        private List<MyDataBroadcaster> m_tempBroadcasters = new List<MyDataBroadcaster>();
        private MyCubeGridConnectionStatus GetShipStatus(MyCubeGrid grid)
        {
            if (grid.IsPreview)
                return MyCubeGridConnectionStatus.IsPreviewGrid;

            m_tempBroadcasters.Clear();
            GridBroadcastersFromPlayer(grid, m_tempBroadcasters);
            bool sendingToGrid = m_tempBroadcasters.Count > 0;

            m_tempBroadcasters.Clear();
            PlayerBroadcastersFromGrid(grid, m_tempBroadcasters);
            bool receivingFromGrid = m_tempBroadcasters.Count > 0;

            if (sendingToGrid && receivingFromGrid)
                return MyCubeGridConnectionStatus.Connected;

            else if (receivingFromGrid)
                return MyCubeGridConnectionStatus.OutOfBroadcastingRange;

            return MyCubeGridConnectionStatus.OutOfReceivingRange;
        }

        private bool PlayerOwnsShip(MyCubeGrid grid)
        {
            return grid.SmallOwners.Contains(MySession.Static.LocalPlayerId);
        }

        //Rule: The representative of block is its cube grid, the representative of a character is himself
        private MyEntity GetInteractedEntityRepresentative(MyEntity controlledEntity)
        {
            if (controlledEntity is MyCubeBlock)
                return MyAntennaSystem.Static.GetLogicalGroupRepresentative((controlledEntity as MyCubeBlock).CubeGrid);

            //assumption: it is impossible to open the character control panel when in a ship
            return MySession.Static.LocalCharacter;
        }

        private void GridBroadcastersFromPlayer(MyCubeGrid grid, List<MyDataBroadcaster> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            var gridBroadcasters = MyRadioBroadcaster.GetGridRelayedBroadcasters(grid);
            var controlledGrid = m_openInventoryInteractedEntityRepresentative as MyCubeGrid;
            var controlledObjectGroup = controlledGrid != null ? MyCubeGridGroups.Static.Logical.GetGroup(controlledGrid) : null;
            foreach (var broadcaster in gridBroadcasters)
            {
                var ent = MyAntennaSystem.Static.GetBroadcasterParentEntity(broadcaster);
                var broadcasterGrid = ent as MyCubeGrid;
                if (broadcasterGrid != null && controlledObjectGroup != null &&
                    MyCubeGridGroups.Static.Logical.GetGroup(broadcasterGrid) == controlledObjectGroup)
                {
                    output.Add(broadcaster);
                    continue;
                }
                if (ent == m_openInventoryInteractedEntityRepresentative)
                    output.Add(broadcaster);
            }
        }

        private HashSet<MyDataBroadcaster> m_tempPlayerBroadcasters = new HashSet<MyDataBroadcaster>();
        private void PlayerBroadcastersFromGrid(MyCubeGrid grid, List<MyDataBroadcaster> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            m_tempPlayerBroadcasters.Clear();
            MyAntennaSystem.Static.GetPlayerRelayedBroadcasters(MySession.Static.LocalCharacter, m_openInventoryInteractedEntityRepresentative, m_tempPlayerBroadcasters);
            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(grid);
            foreach (var broadcaster in m_tempPlayerBroadcasters)
            {
                var ent = MyAntennaSystem.Static.GetBroadcasterParentEntity(broadcaster);
                var broadcasterGrid = ent as MyCubeGrid;
                if (broadcasterGrid != null && gridGroup != null &&
                    MyCubeGridGroups.Static.Logical.GetGroup(broadcasterGrid) == gridGroup)
                {
                    output.Add(broadcaster);
                    continue;
                }
                if (ent !=null && ent.EntityId == grid.EntityId)
                    output.Add(broadcaster);
            }
        }

        private float GetPlayerGridDistance(MyCubeGrid grid)
        {
            if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity != null)
            {
                return (float)Vector3D.Distance(MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition(), grid.GetBaseEntity().PositionComp.GetPosition());
            }
            return float.MaxValue;
        }

        private MyRefuseReason CanTakeRemoteControl(CubeGridInfo gridInfo)
        {
            MyEntity openTerminalEntity;
            MyEntities.TryGetEntityById(gridInfo.EntityId, out openTerminalEntity);
            if (openTerminalEntity != null && openTerminalEntity is MyCubeGrid)
            {
                var grid = openTerminalEntity as MyCubeGrid;

                int remoteControlCount = grid.GetFatBlockCount<MyRemoteControl>();
                if(remoteControlCount==0)
                    return MyRefuseReason.NoRemoteControl;
                if(remoteControlCount > 1 && !grid.HasMainRemoteControl())
                    return MyRefuseReason.NoMainRemoteControl;

                if (grid.HasMainRemoteControl()){
                    if (grid.MainRemoteControl.OwnerId != MySession.Static.LocalHumanPlayer.Identity.IdentityId ||
                        (MySession.Static.Factions.GetPlayerFaction(grid.MainRemoteControl.OwnerId) != null &&
                        !MySession.Static.Factions.GetPlayerFaction(grid.MainRemoteControl.OwnerId).IsMember(MySession.Static.LocalHumanPlayer.Identity.IdentityId)))
                        return MyRefuseReason.NoOwner;
                }
                if (remoteControlCount == 1)
                {
                    var fbs = grid.GetFatBlocks<MyRemoteControl>();
                    fbs.MoveNext();
                    if (fbs.Current.OwnerId != MySession.Static.LocalHumanPlayer.Identity.IdentityId || 
                        (MySession.Static.Factions.GetPlayerFaction(fbs.Current.OwnerId)!=null &&
                        !MySession.Static.Factions.GetPlayerFaction(fbs.Current.OwnerId).IsMember(MySession.Static.LocalHumanPlayer.Identity.IdentityId)))
                        return MyRefuseReason.NoOwner;
                }
                
            }
            return MyRefuseReason.NoProblem;
        }

        private MyRefuseReason CanTakeTerminal(CubeGridInfo gridInfo)
        {
             MyEntity openTerminalEntity;
            MyEntities.TryGetEntityById(gridInfo.EntityId, out openTerminalEntity);
            if (openTerminalEntity != null && openTerminalEntity is MyCubeGrid)
            {
                var grid = openTerminalEntity as MyCubeGrid;

                m_tempSendingToGrid.Clear();
                m_tempReceivingFromGrid.Clear();
                GridBroadcastersFromPlayer(grid, m_tempSendingToGrid);
                PlayerBroadcastersFromGrid(grid, m_tempReceivingFromGrid);

                if (m_tempReceivingFromGrid.Count > 0 && m_tempSendingToGrid.Count > 0)
                {
                    bool ownership = false;
                    foreach (var item in m_tempReceivingFromGrid)
                    {
                        var cubeBlock = item.Entity as MyCubeBlock;
                        if (cubeBlock == null)
                            continue;

                        if (cubeBlock.OwnerId == MySession.Static.LocalHumanPlayer.Identity.IdentityId)
                            ownership = true;
                        var blockFaction = MySession.Static.Factions.GetPlayerFaction(cubeBlock.OwnerId);
                        if (blockFaction != null && blockFaction.IsMember(MySession.Static.LocalHumanPlayer.Identity.IdentityId))
                            ownership = true;
                    }
                    foreach (var item in m_tempSendingToGrid)
                    {
                        var cubeBlock = item.Entity as MyCubeBlock;
                        if (cubeBlock == null)
                            continue;

                        if (cubeBlock.OwnerId == MySession.Static.LocalHumanPlayer.Identity.IdentityId)
                            ownership = true;
                        var blockFaction = MySession.Static.Factions.GetPlayerFaction(cubeBlock.OwnerId);
                        if (blockFaction != null && blockFaction.IsMember(MySession.Static.LocalHumanPlayer.Identity.IdentityId))
                            ownership = true;
                    }
                    if (!ownership)
                        return MyRefuseReason.NoOwner;
                }

                if (gridInfo.Status == MyCubeGridConnectionStatus.OutOfReceivingRange && (MySession.Static.ControlledEntity.Entity is MyCharacter) && !(MySession.Static.ControlledEntity.Entity as MyCharacter).RadioBroadcaster.Enabled)
                {
                    return MyRefuseReason.PlayerBroadcastOff;
                }

                if (gridInfo.Status == MyCubeGridConnectionStatus.OutOfBroadcastingRange || gridInfo.Status == MyCubeGridConnectionStatus.OutOfReceivingRange)
                {
                    return MyRefuseReason.NoStableConnection;
                }
            }

            return MyRefuseReason.NoProblem;
        }

        #endregion

        #region eventhandlers

        private void OnButtonClicked_TakeControl(MyGuiControlButton obj)
        {
            //if for some reason player has taken mouse off the table before the event
            if (m_shipsData.SelectedRow == null)
                return;

            var userData = (UserData)m_shipsData.SelectedRow.UserData;
            if (userData.IsSelectable)
                FindRemoteControlAndTakeControl(userData.GridEntityId);
        }

        private void Menu_ButtonClicked(MyGuiControlButton button)
        {
            if (ButtonClicked != null)
                ButtonClicked();
        }

        private void OnButtonClicked_OpenTerminal(MyGuiControlButton obj)
        {
            MyGuiControlTable.EventArgs args;
            args.MouseButton = 0;
            args.RowIndex = -1;
            shipsData_ItemDoubleClicked(null, args);
        }

        private void shipsData_ItemDoubleClicked(MyGuiControlTable sender, MyGuiControlTable.EventArgs args)
        {
            //if for some reason player has taken mouse off the table before the event
            if (m_shipsData.SelectedRow == null)
                return;

            var userData = (UserData)m_shipsData.SelectedRow.UserData;
            if (userData.IsSelectable)
                OpenPropertiesByEntityId(userData.GridEntityId);
        }

        private void shipsData_ColumnClicked(MyGuiControlTable sender, int column)
        {
            m_columnToSort = column;
        }

        private void shipsInRange_ItemSelected()
        {
            if ((!m_shipsInRange.IsMouseOver && !m_shipsInRange.HasFocus) || m_shipsInRange.GetSelectedKey() == m_interactedEntityRepresentative.EntityId)
                return;

            OpenPropertiesByEntityId(m_shipsInRange.GetSelectedKey());
        }

        private List<MyDataBroadcaster> m_tempSendingToGrid = new List<MyDataBroadcaster>();
        private List<MyDataBroadcaster> m_tempReceivingFromGrid = new List<MyDataBroadcaster>();
        private bool OpenPropertiesByEntityId(long entityId)
        {
            MyEntity openTerminalEntity;
            MyEntities.TryGetEntityById(entityId, out openTerminalEntity);

            //rule: When you want to open character terminal you pass null as interactedEntity
            if (openTerminalEntity is MyCharacter)
            {
                MyGuiScreenTerminal.ChangeInteractedEntity(null);
                return true;
            }

            //grid = null -> has been destroyed or something like that
            if (openTerminalEntity != null && openTerminalEntity is MyCubeGrid)
            {
                var grid = openTerminalEntity as MyCubeGrid;

                m_tempSendingToGrid.Clear();
                m_tempReceivingFromGrid.Clear();
                GridBroadcastersFromPlayer(grid, m_tempSendingToGrid);
                PlayerBroadcastersFromGrid(grid, m_tempReceivingFromGrid);

                if ((m_tempSendingToGrid.Count > 0 && m_tempReceivingFromGrid.Count > 0) || m_openInventoryInteractedEntityRepresentative == grid)
                {

                    //This will only happen when you have a grid with no radio connection, in which case the entity itself is the only available option
                    if(m_tempReceivingFromGrid.Count <= 0)
                        MyGuiScreenTerminal.ChangeInteractedEntity(MyGuiScreenTerminal.InteractedEntity);
                    
                    //pick the first antenna from cube grid to switch (could've been any one anyways)                    
                    else
                        MyGuiScreenTerminal.ChangeInteractedEntity(m_tempReceivingFromGrid.ElementAt(0).Entity as MyTerminalBlock);
                    return true;
                }
            }

            //Else throw an alert to the user saying "ship has been disconnected" or something?
            return false;
        }

        private void FindRemoteControlAndTakeControl(long entityId)
        {
            MyEntity openTerminalEntity;
            MyEntities.TryGetEntityById(entityId, out openTerminalEntity);

            if (openTerminalEntity != null && openTerminalEntity is MyCubeGrid)
            {
                var grid = openTerminalEntity as MyCubeGrid;

                m_tempSendingToGrid.Clear();
                m_tempReceivingFromGrid.Clear();
                GridBroadcastersFromPlayer(grid, m_tempSendingToGrid);
                PlayerBroadcastersFromGrid(grid, m_tempReceivingFromGrid);

                if ((m_tempSendingToGrid.Count > 0 && m_tempReceivingFromGrid.Count > 0) || m_openInventoryInteractedEntityRepresentative == grid)
                {
                    //TODO: Find main remote control and take control, dont forget to close terminal and if possible switch to attached camera
                    if (grid.HasMainRemoteControl())
                    {
                        ((MyRemoteControl)grid.MainRemoteControl).RequestControl();
                    }
                    else if (grid.GetFatBlockCount<MyRemoteControl>() == 1)
                    {
                        var fbs = grid.GetFatBlocks<MyRemoteControl>();
                        fbs.MoveNext();
                        fbs.Current.RequestControl();
                    }

                }
            }
        }

        #endregion

        #region public

        //used to test if interacted entity is still connected with open inventory interacted entity
        public bool TestConnection()
        {
            //if in same ship, no need to check radio connection
            if (m_interactedEntityRepresentative.EntityId == m_openInventoryInteractedEntityRepresentative.EntityId)
                return true;

            if (m_interactedEntityRepresentative is MyCubeGrid)
                return GetShipStatus(m_interactedEntityRepresentative as MyCubeGrid) == MyCubeGridConnectionStatus.Connected;
            
            //if the interacted entity is a character, we're sure to be in the terminal, so it's always connected
            return true;
        }

        public void Close()
        {
            if (m_shipsInRange != null)
            {
                m_shipsInRange.ItemSelected -= shipsInRange_ItemSelected;
                m_shipsInRange.ClearItems();
                m_shipsInRange = null;
            }

            if (m_shipsData != null)
            {
                //m_shipsData.ItemDoubleClicked -= shipsData_ItemDoubleClicked;
                m_shipsData.ColumnClicked -= shipsData_ColumnClicked;
                m_shipsData.Clear();
                m_shipsData = null;
            }
            if (m_button != null)
            {
                m_button.ButtonClicked -= Menu_ButtonClicked;
                m_button = null;
            }
        }

        HashSet<MyAntennaSystem.BroadcasterInfo> previousMutualConnectionGrids;
        HashSet<CubeGridInfo> previousShipInfo;
        int cnt = 0;

        public void Update()
        {
            //Hard-coded half-a-second update
            cnt = (++cnt) % 30;
            if (cnt != 0) 
                return;

            if(previousMutualConnectionGrids == null)
                previousMutualConnectionGrids = MyAntennaSystem.Static.GetMutuallyConnectedGrids(m_openInventoryInteractedEntityRepresentative);

            if(previousShipInfo == null)
                previousShipInfo = GetAllCubeGridsInfo();

            var currentMutualConnectionGrids = MyAntennaSystem.Static.GetMutuallyConnectedGrids(m_openInventoryInteractedEntityRepresentative);
            var currentShipInfo = GetAllCubeGridsInfo();

            if (!previousMutualConnectionGrids.SetEquals(currentMutualConnectionGrids))
                PopulateMutuallyConnectedCubeGrids(currentMutualConnectionGrids);

            if (!previousShipInfo.SequenceEqual(currentShipInfo))
                PopulateOwnedCubeGrids(currentShipInfo);
            
            previousMutualConnectionGrids = currentMutualConnectionGrids;
            previousShipInfo = currentShipInfo;
        }

    }
    #endregion
}
