
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
            m_shipsData.ItemDoubleClicked += shipsData_ItemDoubleClicked;
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
                MyGuiControlTable.Cell nameCell, distanceCell, statusCell;

                data.GridEntityId = gridInfo.EntityId;
                if (gridInfo.Status == MyCubeGridConnectionStatus.Connected || gridInfo.Status == MyCubeGridConnectionStatus.PhysicallyConnected || gridInfo.Status == MyCubeGridConnectionStatus.Me)
                {
                    StringBuilder minDistance = new StringBuilder();
                    if(gridInfo.Status == MyCubeGridConnectionStatus.Connected)
                        minDistance = gridInfo.AppendedDistance.Append(" m");

                    data.IsSelectable = true;
                    nameCell = new MyGuiControlTable.Cell(new StringBuilder(gridInfo.Name), textColor: Color.White);
                    distanceCell = new MyGuiControlTable.Cell(minDistance, userData: gridInfo.Distance, textColor: Color.White);
                    switch (gridInfo.Status)
                    {
                        case MyCubeGridConnectionStatus.PhysicallyConnected:
                            statusCell = new MyGuiControlTable.Cell(MyTexts.Get(MySpaceTexts.BroadcastStatus_PhysicallyConnected), userData: gridInfo.Status, textColor: Color.White);
                            break;
                        case MyCubeGridConnectionStatus.Me:
                            statusCell = new MyGuiControlTable.Cell(MyTexts.Get(MySpaceTexts.BroadcastStatus_Me), userData: gridInfo.Status, textColor: Color.White);
                            break;

                        case MyCubeGridConnectionStatus.Connected:
                        default:
                            statusCell = new MyGuiControlTable.Cell(MyTexts.Get(MySpaceTexts.BroadcastStatus_Connected), userData: gridInfo.Status, textColor: Color.White);
                            break;
                    }
                }

                else
                {
                    data.IsSelectable = false;
                    nameCell = new MyGuiControlTable.Cell(new StringBuilder(gridInfo.Name), textColor: Color.Gray);
                    distanceCell = new MyGuiControlTable.Cell(new StringBuilder(""), userData: float.MaxValue, textColor: Color.Gray);
                    if (gridInfo.Status == MyCubeGridConnectionStatus.OutOfReceivingRange)
                        statusCell = new MyGuiControlTable.Cell(MyTexts.Get(MySpaceTexts.BroadcastStatus_OutOfReceivingRange), userData: gridInfo.Status, textColor: Color.Gray);
                    else if (gridInfo.Status == MyCubeGridConnectionStatus.OutOfBroadcastingRange)
                        statusCell = new MyGuiControlTable.Cell(MyTexts.Get(MySpaceTexts.BroadcastStatus_OutOfBroadcastingRange), userData: gridInfo.Status, textColor: Color.Gray);
                    else
                        statusCell = new MyGuiControlTable.Cell(MyTexts.Get(MySpaceTexts.BroadcastStatus_IsPreviewGrid), userData: gridInfo.Status, textColor: Color.Gray);
                }

                row = new MyGuiControlTable.Row(data);
                row.AddCell(nameCell);
                row.AddCell(distanceCell);
                row.AddCell(statusCell);
                m_shipsData.Add(row);
                m_shipsData.SortByColumn(m_columnToSort, MyGuiControlTable.SortStateEnum.Ascending, false);
            }
            m_shipsData.ScrollBar.ChangeValue(scrollBarValue);
        }
        #endregion

        #region radio getters






        #endregion

        #region helpers

        private HashSet<CubeGridInfo> GetAllCubeGridsInfo()
        {
            HashSet<CubeGridInfo> output = new HashSet<CubeGridInfo>();
            HashSet<long> AddedItems = new HashSet<long>();

            //First, you always add yourself
            AddedItems.Add(m_openInventoryInteractedEntityRepresentative.EntityId);
            output.Add(new CubeGridInfo()
            {
                EntityId = m_openInventoryInteractedEntityRepresentative.EntityId,
                Distance = 0,
                AppendedDistance = new StringBuilder("0"),
                Name = m_openInventoryInteractedEntityRepresentative.DisplayName,
                Status = m_openInventoryInteractedEntityRepresentative == MySession.Static.LocalCharacter ? MyCubeGridConnectionStatus.Me : MyCubeGridConnectionStatus.PhysicallyConnected
            });

            //then you add your owned grids
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
                if (ent.EntityId == grid.EntityId)
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

        #endregion

        #region eventhandlers
        private void Menu_ButtonClicked(MyGuiControlButton button)
        {
            if (ButtonClicked != null)
                ButtonClicked();
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
                m_shipsData.ItemDoubleClicked -= shipsData_ItemDoubleClicked;
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
