
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
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
using VRage;
using VRage.Trace;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyTerminalInfoController
    {
        private MyGuiControlTabPage m_infoPage;
        private MyCubeGrid m_grid;

        internal void Close()
        {
            if (m_grid == null) return;
            if (m_infoPage == null) return;

            var convertBtn = (MyGuiControlButton)m_infoPage.Controls.GetControlByName("ConvertBtn");
            if (convertBtn != null)
                convertBtn.ButtonClicked -= convertBtn_ButtonClicked;

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
            MyGuiControlList list = (MyGuiControlList)m_infoPage.Controls.GetControlByName("InfoList");
            list.Controls.Clear();

            if (m_grid == null || m_grid.Physics == null)
            {
                convertBtn.Enabled = false;
                MyGuiControlLabel noShip = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScreenTerminalError_ShipNotConnected), font: Common.MyFontEnum.Red);
                list.Controls.Add(noShip);
                return;
            }

            if (!m_grid.IsStatic || m_grid.MarkedForClose)
                convertBtn.Enabled = false;

            var setDestructibleBlocks = (MyGuiControlCheckbox)m_infoPage.Controls.GetControlByName("SetDestructibleBlocks");
            setDestructibleBlocks.IsChecked = m_grid.DestructibleBlocks;
            setDestructibleBlocks.Visible = MySession.Static.Settings.ScenarioEditMode || MySession.Static.IsScenario;
            setDestructibleBlocks.Enabled = MySession.Static.Settings.ScenarioEditMode;
            setDestructibleBlocks.IsCheckedChanged = setDestructibleBlocksBtn_IsCheckedChanged;

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

            MyGuiControlLabel polygonCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Triangles)).AppendInt32(polygonCounter).ToString());
            polygonCount.SetToolTip(MySpaceTexts.TerminalTab_Info_TrianglesTooltip);
            MyGuiControlLabel cubeCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Blocks)).AppendInt32(m_grid.GetBlocks().Count).ToString());
            cubeCount.SetToolTip(MySpaceTexts.TerminalTab_Info_BlocksTooltip);
            MyGuiControlLabel blockCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_NonArmor)).AppendInt32(m_grid.Hierarchy.Children.Count).ToString());
            MyGuiControlLabel thrustCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Thrusters)).AppendInt32(m_grid.GridSystems.ThrustSystem.ThrustCount).ToString());
            MyGuiControlLabel lightCount = new MyGuiControlLabel(text: new StringBuilder().Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Lights)).AppendInt32(lightCounter).ToString());
            MyGuiControlLabel reflectorCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Reflectors)).AppendInt32(m_grid.GridSystems.ReflectorLightSystem.ReflectorCount).ToString());
            //MyGuiControlLabel wheelCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Rotors)).AppendInt32(m_grid.WheelSystem.WheelCount));
            MyGuiControlLabel gravityCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_GravGens)).AppendInt32(gravityCounter).ToString());
            MyGuiControlLabel massCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_VirtualMass)).AppendInt32(massCounter).ToString());
            MyGuiControlLabel conveyorCount = new MyGuiControlLabel(text: new StringBuilder().AppendStringBuilder(MyTexts.Get(MySpaceTexts.TerminalTab_Info_Conveyors)).AppendInt32(conveyorCounter).ToString());
            list.InitControls(new MyGuiControlBase[] { cubeCount, blockCount, conveyorCount, thrustCount, lightCount, reflectorCount, gravityCount, massCount, polygonCount });


        }

        //Rule: Count the player who has the most number of FUNCTIONAL blocks: only he can rename the ship
        private bool IsPlayerOwner(MyCubeGrid grid)
        {
            return grid != null && grid.BigOwners.Contains(MySession.LocalPlayerId);            
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
            m_grid.SyncObject.SetDestructibleBlocks(obj.IsChecked);
        }

        void convertBtn_ButtonClicked(MyGuiControlButton obj)
        {
            m_grid.SyncObject.RequestConversionToShip();
        }

        void renameBtn_ButtonClicked(MyGuiControlButton obj)
        {
            var textForm = (MyGuiControlTextbox)m_infoPage.Controls.GetControlByName("RenameShipText");
            m_grid.SyncObject.ChangeDisplayNameRequest(m_grid, textForm.Text);
            
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
    }
}
