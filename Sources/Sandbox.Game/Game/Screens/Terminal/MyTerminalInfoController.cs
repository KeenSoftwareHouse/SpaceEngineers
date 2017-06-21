
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GUI.HudViewers;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems;
using VRage;
using VRage.Game;
using VRage.Trace;
using VRageMath;
using VRage.Game.Entity;

namespace Sandbox.Game.Gui
{
    class MyTerminalInfoController
    {
        private MyGuiControlTabPage m_infoPage;
        private MyCubeGrid m_grid;
        private List<MyCubeGrid> m_infoGrids = new List<MyCubeGrid>();
        private List<MyPlayer.PlayerId> m_playerIds = new List<MyPlayer.PlayerId>();

        internal void Close()
        {
            foreach (var grid in m_infoGrids)
            {
                grid.OnAuthorshipChanged -= grid_OnAuthorshipChanged;
            }

            if (m_grid == null) return;
            if (m_infoPage == null) return;

            var convertBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("ConvertBtn");
            if (convertBtn != null)
                convertBtn.ButtonClicked -= convertBtn_ButtonClicked;

            var convertToStationBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("ConvertToStationBtn");
            if (convertToStationBtn != null)
                convertToStationBtn.ButtonClicked -= convertToStationBtn_ButtonClicked;

            m_grid.OnBlockAdded -= grid_OnBlockAdded;
            m_grid.OnBlockRemoved -= grid_OnBlockRemoved;
            m_grid.OnPhysicsChanged -= grid_OnPhysicsChanged;
            m_grid.OnBlockOwnershipChanged -= grid_OnBlockOwnershipChanged;

            m_grid = null;
            m_infoPage = null;
        }

        internal void Init(Graphics.GUI.MyGuiControlTabPage infoPage, MyCubeGrid grid)
        {
            m_grid = grid;
            m_infoPage = infoPage;
            Debug.Assert(m_infoPage != null);

            RecreateControls();

            if (grid == null)
                return;
            grid.OnBlockAdded += grid_OnBlockAdded;
            grid.OnBlockRemoved += grid_OnBlockRemoved;
            grid.OnPhysicsChanged += grid_OnPhysicsChanged;
            grid.OnBlockOwnershipChanged += grid_OnBlockOwnershipChanged;

            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
            {
                var renameShipBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("RenameShipButton");
                if (renameShipBtn != null)
                    renameShipBtn.ButtonClicked += renameBtn_ButtonClicked;
            }

            var convertBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("ConvertBtn");
            if (convertBtn != null)
            {
                convertBtn.ButtonClicked += convertBtn_ButtonClicked;
            }

            var convertToStationBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("ConvertToStationBtn");
            if (convertToStationBtn != null)
                convertToStationBtn.ButtonClicked += convertToStationBtn_ButtonClicked;
        }

        private void RecreateControls()
        {
            Debug.Assert(m_infoPage != null, "Terminal page is null");
            if (m_infoPage == null)
                return;
            if (MyFakes.ENABLE_CENTER_OF_MASS)
            {
                var centerBtn = (MyGuiControlCheckbox)m_infoPage.Controls.GetControlByName("CenterBtn");
                centerBtn.IsChecked = MyCubeGrid.ShowCenterOfMass;
                centerBtn.IsCheckedChanged = centerBtn_IsCheckedChanged;

                var pivotBtn = (MyGuiControlCheckbox)m_infoPage.Controls.GetControlByName("PivotBtn");
                pivotBtn.IsChecked = MyCubeGrid.ShowGridPivot;
                pivotBtn.IsCheckedChanged = pivotBtn_IsCheckedChanged;
            }

            var showGravityGizmoBtn = (MyGuiControlCheckbox)m_infoPage.Controls.GetControlByName("ShowGravityGizmo");
            showGravityGizmoBtn.IsChecked = MyCubeGrid.ShowGravityGizmos;
            showGravityGizmoBtn.IsCheckedChanged = showGravityGizmos_IsCheckedChanged;

            var showSenzorGizmoBtn = (MyGuiControlCheckbox)m_infoPage.Controls.GetControlByName("ShowSenzorGizmo");
            showSenzorGizmoBtn.IsChecked = MyCubeGrid.ShowSenzorGizmos;
            showSenzorGizmoBtn.IsCheckedChanged = showSenzorGizmos_IsCheckedChanged;

            var showAntenaGizmoBtn = (MyGuiControlCheckbox)m_infoPage.Controls.GetControlByName("ShowAntenaGizmo");
            showAntenaGizmoBtn.IsChecked = MyCubeGrid.ShowAntennaGizmos;
            showAntenaGizmoBtn.IsCheckedChanged = showAntenaGizmos_IsCheckedChanged;

            var friendAntennaRange = (MyGuiControlSlider)m_infoPage.Controls.GetControlByName("FriendAntennaRange");
            friendAntennaRange.Value = MyHudMarkerRender.FriendAntennaRange;
            friendAntennaRange.ValueChanged += (MyGuiControlSlider s) => { MyHudMarkerRender.FriendAntennaRange = s.Value; };


            var enemyAntennaRange = (MyGuiControlSlider)m_infoPage.Controls.GetControlByName("EnemyAntennaRange");
            enemyAntennaRange.Value = MyHudMarkerRender.EnemyAntennaRange;
            enemyAntennaRange.ValueChanged += (MyGuiControlSlider s) => { MyHudMarkerRender.EnemyAntennaRange = s.Value; };

            var ownedAntennaRange = (MyGuiControlSlider)m_infoPage.Controls.GetControlByName("OwnedAntennaRange");
            ownedAntennaRange.Value = MyHudMarkerRender.OwnerAntennaRange;
            ownedAntennaRange.ValueChanged += (MyGuiControlSlider s) => { MyHudMarkerRender.OwnerAntennaRange = s.Value; };

            if (MyFakes.ENABLE_TERMINAL_PROPERTIES)
            {
                var renameShipLabel = (MyGuiControlLabel)m_infoPage.Controls.GetControlByName("RenameShipLabel");

                var renameShipBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("RenameShipButton");

                var renameShipText = (MyGuiControlTextbox)m_infoPage.Controls.GetControlByName("RenameShipText");
                if(renameShipText != null && m_grid != null)
                    renameShipText.Text = m_grid.DisplayName;

                var showRenameShip = IsPlayerOwner(m_grid);
                renameShipLabel.Visible = showRenameShip;
                renameShipBtn.Visible = showRenameShip;
                renameShipText.Visible = showRenameShip;
            }

            var convertBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("ConvertBtn");
            var convertToStationBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("ConvertToStationBtn");
            MyGuiControlList list = (MyGuiControlList)m_infoPage.Controls.GetControlByName("InfoList");
            list.Controls.Clear();
            var setDestructibleBlocks = (MyGuiControlCheckbox)m_infoPage.Controls.GetControlByName("SetDestructibleBlocks");
            setDestructibleBlocks.Visible = MySession.Static.Settings.ScenarioEditMode || MySession.Static.IsScenario;
            setDestructibleBlocks.Enabled = MySession.Static.Settings.ScenarioEditMode;

            if (m_grid == null || m_grid.Physics == null)
            {
                convertBtn.Enabled = false;
                convertToStationBtn.Enabled = false;

                RecreateServerLimitInfo(list);
                return;
            }

            if (!m_grid.IsStatic)
            {
                convertBtn.Enabled = false;
                convertToStationBtn.Enabled = true;
            }
            else
            {
                convertBtn.Enabled = true;
                convertToStationBtn.Enabled = false;
            }

            if (m_grid.GridSizeEnum == MyCubeSize.Small)
                convertToStationBtn.Enabled = false;

            if (!m_grid.BigOwners.Contains(MySession.Static.LocalPlayerId))
            {
                convertBtn.Enabled = false;
                convertToStationBtn.Enabled = false;
            }

            setDestructibleBlocks.IsChecked = m_grid.DestructibleBlocks;
            setDestructibleBlocks.IsCheckedChanged = setDestructibleBlocks_IsCheckedChanged;

            int gravityCounter = 0;
            if (m_grid.BlocksCounters.ContainsKey(typeof(MyObjectBuilder_GravityGenerator)))
                gravityCounter = m_grid.BlocksCounters[typeof(MyObjectBuilder_GravityGenerator)];
            int massCounter = 0;
            if (m_grid.BlocksCounters.ContainsKey(typeof(MyObjectBuilder_VirtualMass)))
                massCounter = m_grid.BlocksCounters[typeof(MyObjectBuilder_VirtualMass)];
            int lightCounter = 0;
            if (m_grid.BlocksCounters.ContainsKey(typeof(MyObjectBuilder_InteriorLight)))
                lightCounter = m_grid.BlocksCounters[typeof(MyObjectBuilder_InteriorLight)];
            var conveyorCounter = 0;
            foreach (var key in m_grid.BlocksCounters.Keys)
            {
                Type blockType = MyCubeBlockFactory.GetProducedType(key);
                if (typeof(IMyConveyorSegmentBlock).IsAssignableFrom(blockType) || typeof(IMyConveyorEndpointBlock).IsAssignableFrom(blockType))
                    conveyorCounter += m_grid.BlocksCounters[key];
            }
            int polygonCounter = 0;
            foreach (var block in m_grid.GetBlocks())
            {
                if (block.FatBlock != null)
                {
                    polygonCounter += block.FatBlock.Model.GetTrianglesCount();
                }
            }
            foreach (var cell in m_grid.RenderData.Cells.Values)
            {
                foreach (var part in cell.CubeParts)
                {
                    polygonCounter += part.Model.GetTrianglesCount();
                }
            }

	        int thrustCount = 0;
	        var thrustComp = m_grid.Components.Get<MyEntityThrustComponent>();
	        if (thrustComp != null)
		        thrustCount = thrustComp.ThrustCount;
	        MyGuiControlLabel thrustCountLabel = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Thrusters)).AppendInt32(thrustCount).ToString());

            MyGuiControlLabel polygonCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Triangles)).AppendInt32(polygonCounter).ToString());
            polygonCount.SetToolTip(MySpaceTexts.TerminalTab_Info_TrianglesTooltip);
            MyGuiControlLabel cubeCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Blocks)).AppendInt32(m_grid.GetBlocks().Count).ToString());
            cubeCount.SetToolTip(MySpaceTexts.TerminalTab_Info_BlocksTooltip);
            MyGuiControlLabel blockCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_NonArmor)).AppendInt32(m_grid.Hierarchy.Children.Count).ToString());
            MyGuiControlLabel lightCount = new MyGuiControlLabel(text: new StringBuilder().Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Lights)).AppendInt32(lightCounter).ToString());
            MyGuiControlLabel reflectorCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Reflectors)).AppendInt32(m_grid.GridSystems.ReflectorLightSystem.ReflectorCount).ToString());
            //MyGuiControlLabel wheelCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Rotors)).AppendInt32(m_grid.WheelSystem.WheelCount));
            MyGuiControlLabel gravityCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_GravGens)).AppendInt32(gravityCounter).ToString());
            MyGuiControlLabel massCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_VirtualMass)).AppendInt32(massCounter).ToString());
            MyGuiControlLabel conveyorCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Conveyors)).AppendInt32(conveyorCounter).ToString());
			var mainCockpit = m_grid.MainCockpit as MyShipController;
			MyCharacter pilot = null;
			if (mainCockpit != null)
				pilot = mainCockpit.Pilot;
			MyGuiControlLabel gridMass = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_GridMass)).AppendInt32(m_grid.GetCurrentMass(pilot)).ToString());
			list.InitControls(new MyGuiControlBase[] { cubeCount, blockCount, conveyorCount, thrustCountLabel, lightCount, reflectorCount, gravityCount, massCount, polygonCount, gridMass });
        }

        private void setDestructibleBlocks_IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            m_grid.DestructibleBlocks = obj.IsChecked;
        }

        private void RecreateServerLimitInfo(MyGuiControlList list)
        {
            var identity = MySession.Static.Players.TryGetIdentity(MySession.Static.LocalPlayerId);
            int built;

            if (MySession.Static.MaxBlocksPerPlayer > 0 || MySession.Static.BlockTypeLimits.Keys.Count > 0)
            {
                MyGuiControlLabel totalBlocksLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_Overview), textScale: 1.3f);
                list.Controls.Add(totalBlocksLabel);
            }

            if (MySession.Static.MaxBlocksPerPlayer > 0)
            {
                MyGuiControlLabel totalBlocksLabel = new MyGuiControlLabel(text: String.Format("{0} {1}/{2} {3}", MyTexts.Get(MySpaceTexts.TerminalTab_Info_YouBuilt), identity.BlocksBuilt, MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier, MyTexts.Get(MySpaceTexts.TerminalTab_Info_BlocksLower)));
                list.Controls.Add(totalBlocksLabel);
            }
            foreach (var blockType in MySession.Static.BlockTypeLimits)
            {
                identity.BlockTypeBuilt.TryGetValue(blockType.Key, out built);
                var definition = Sandbox.Definitions.MyDefinitionManager.Static.TryGetDefinitionGroup(blockType.Key);
                if (definition == null)
                    continue;
                MyGuiControlLabel blockTypeLabel = new MyGuiControlLabel(text: String.Format("{0} {1}/{2} {3}", MyTexts.Get(MySpaceTexts.TerminalTab_Info_YouBuilt), built, MySession.Static.GetBlockTypeLimit(blockType.Key), definition.Any.DisplayNameText));
                list.Controls.Add(blockTypeLabel);
            }

            foreach (var grid in m_infoGrids)
            {
                grid.OnAuthorshipChanged -= grid_OnAuthorshipChanged;
            }

            m_infoGrids.Clear();
            identity.LockBlocksBuiltByGrid.AcquireExclusive();
            for (int i = 0; i < identity.BlocksBuiltByGrid.Count; i++)
            {
                var grid = identity.BlocksBuiltByGrid.ElementAt(i);
                MyGuiControlParent panel = new MyGuiControlParent();

                if (m_infoGrids.Count == 0)
                {
                    MyGuiControlSeparatorList infoSeparator = new MyGuiControlSeparatorList();
                    infoSeparator.AddHorizontal(new Vector2(-0.2f, -0.052f), 0.4f, width: 0.004f);
                    panel.Controls.Add(infoSeparator);
                }

                MyGuiControlLabel gridNameLabel = new MyGuiControlLabel(text: grid.Key.DisplayName, textScale: 0.9f);
                MyGuiControlLabel gridBlockCountLabel = new MyGuiControlLabel(text: String.Format("{0} {1}", grid.Value, MyTexts.Get(MySpaceTexts.TerminalTab_Info_BlocksLower)), textScale: 0.9f);
                MyGuiControlLabel assignLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.TerminalTab_Info_Assign), originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER, textScale: 0.9f);
                MyGuiControlCombobox assignCombobox = new MyGuiControlCombobox(originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER, size: new Vector2(0.11f, 0.008f));
                MyGuiControlSeparatorList lineSeparator = new MyGuiControlSeparatorList();

                gridNameLabel.Position = new Vector2(-0.15f, -0.025f);
                gridBlockCountLabel.Position = new Vector2(-0.15f, 0.000f);
                assignLabel.Position = new Vector2(0.035f, 0.025f);
                assignCombobox.Position = new Vector2(0.15f, 0.025f);

                assignCombobox.ItemSelected += delegate()
                {
                    assignCombobox_ItemSelected(grid.Key, m_playerIds[(int)assignCombobox.GetSelectedKey()]);
                };

                m_playerIds.Clear();
                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (MySession.Static.LocalHumanPlayer != player)
                    {
                        assignCombobox.AddItem(m_playerIds.Count, player.DisplayName);
                        m_playerIds.Add(player.Id);
                    }
                }
                lineSeparator.AddHorizontal(new Vector2(-0.15f, 0.05f), 0.3f, width: 0.002f);

                panel.Controls.Add(gridNameLabel);
                panel.Controls.Add(gridBlockCountLabel);
                panel.Controls.Add(assignLabel);
                panel.Controls.Add(assignCombobox);
                panel.Controls.Add(lineSeparator);

                if (MySession.Static.EnableRemoteBlockRemoval)
                {
                    MyGuiControlLabel deleteOwnedBlocksLabel = new MyGuiControlLabel(
                        text: MyTexts.GetString(MySpaceTexts.buttonRemove), 
                        originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER, 
                        textScale: 0.9f);
                    MyGuiControlButton deleteOwnedBlocksButton = new MyGuiControlButton(
                        text: new StringBuilder("X"), 
                        onButtonClick: deleteOwnedBlocksButton_ButtonClicked, 
                        buttonIndex: m_infoGrids.Count, 
                        visualStyle: MyGuiControlButtonStyleEnum.SquareSmall, 
                        originAlign: VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
                    deleteOwnedBlocksLabel.Position = new Vector2(0.11f, -0.02f);
                    deleteOwnedBlocksButton.Position = new Vector2(0.15f, -0.02f);
                    panel.Controls.Add(deleteOwnedBlocksLabel);
                    panel.Controls.Add(deleteOwnedBlocksButton);
                }

                grid.Key.OnAuthorshipChanged += grid_OnAuthorshipChanged;

                m_infoGrids.Add(grid.Key);

                panel.Size = new Vector2(panel.Size.X, 0.09f);
                list.Controls.Add(panel);
            }
            identity.LockBlocksBuiltByGrid.ReleaseExclusive();
        }

        //Rule: Count the player who has the most number of FUNCTIONAL blocks: only he can rename the ship
        private bool IsPlayerOwner(MyCubeGrid grid)
        {
            return grid != null && grid.BigOwners.Contains(MySession.Static.LocalPlayerId);            
        }

        void showAntenaGizmos_IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            MyCubeGrid.ShowAntennaGizmos = obj.IsChecked;
        }
        void showSenzorGizmos_IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            MyCubeGrid.ShowSenzorGizmos = obj.IsChecked;
        }
        void showGravityGizmos_IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            MyCubeGrid.ShowGravityGizmos = obj.IsChecked;
        }
        void centerBtn_IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            MyCubeGrid.ShowCenterOfMass = obj.IsChecked;
        }
        void pivotBtn_IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            MyCubeGrid.ShowGridPivot = obj.IsChecked;
        }

        void setDestructibleBlocksBtn_IsCheckedChanged(MyGuiControlCheckbox obj)
        {
            m_grid.DestructibleBlocks = obj.IsChecked;
        }

        void convertBtn_ButtonClicked(MyGuiControlButton obj)
        {
            m_grid.RequestConversionToShip();
        }

        private void convertToStationBtn_ButtonClicked(MyGuiControlButton obj)
        {
            m_grid.RequestConversionToStation();
        }

        void renameBtn_ButtonClicked(MyGuiControlButton obj)
        {
            var textForm = (MyGuiControlTextbox)m_infoPage.Controls.GetControlByName("RenameShipText");
            m_grid.ChangeDisplayNameRequest(textForm.Text);
            
        }

        void deleteOwnedBlocksButton_ButtonClicked(MyGuiControlButton obj)
        {
            var grid = m_infoGrids[obj.Index];
            var messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxTextConfirmDeleteGrid, grid.DisplayName),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                    canHideOthers: false,
                    callback: (result) =>
                    {
                        if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            if (grid != null)
                            {
                                MyMultiplayer.RaiseEvent(grid, x => x.RemoveBlocksBuiltByID, MySession.Static.LocalPlayerId);
                            }
                        }
                    });
            MyGuiSandbox.AddScreen(messageBox);
        }

        void assignCombobox_ItemSelected(MyCubeGrid grid, MyPlayer.PlayerId playerId)
        {
            ulong steamId = playerId.SteamId;
            var identity = MySession.Static.Players.TryGetPlayerIdentity(playerId);
            if (identity == null)
            {
                Debug.Fail("Transfering grid to nonexistent player.");
                return;
            }
            if (grid.IsTransferBlocksBuiltByIDPossible(MySession.Static.LocalPlayerId, identity.IdentityId))
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                        styleEnum: Graphics.GUI.MyMessageBoxStyleEnum.Info,
                        buttonType: MyMessageBoxButtonsType.YES_NO,
                        messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.MessageBoxTextConfirmTransferGrid), new object[] { grid.DisplayName, identity.DisplayName }),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                        canHideOthers: false,
                        callback: (result) =>
                        {
                            if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                            {
                                if (MySession.Static.Players.GetOnlinePlayers().Contains(MySession.Static.Players.GetPlayerById(playerId)))
                                {
                                    MyMultiplayer.RaiseEvent(grid, x => x.SendTransferRequestMessage, MySession.Static.LocalPlayerId, identity.IdentityId, steamId);
                                }
                                else
                                {
                                    ShowPlayerNotOnlineMessage(identity);
                                }
                            }
                        });
                MyGuiSandbox.AddScreen(messageBox);
            }
            else
            {
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                        messageText: new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxTextNotEnoughFreeBlocksForTransfer, identity.DisplayName),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                        canHideOthers: false
                    );
                MyGuiSandbox.AddScreen(messageBox);
            }
        }

        private void ShowPlayerNotOnlineMessage(MyIdentity identity)
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                        messageText: new StringBuilder().AppendFormat(MyCommonTexts.MessageBoxTextPlayerNotOnline, identity.DisplayName),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                        canHideOthers: false,
                        callback: (result) =>
                        {
                            RecreateControls();
                        }
                    );
            MyGuiSandbox.AddScreen(messageBox);
        }


        private void grid_OnBlockRemoved(MySlimBlock obj)
        {
            RecreateControls();
        }

        private void grid_OnBlockAdded(MySlimBlock obj)
        {
            RecreateControls();
        }

        private void grid_OnPhysicsChanged(MyEntity obj)
        {
            RecreateControls();
        }

        private void grid_OnBlockOwnershipChanged(MyEntity obj)
        {
            RecreateControls();
        }

        private void grid_OnAuthorshipChanged(MyEntity obj)
        {
            RecreateControls();
        }
    }
}
