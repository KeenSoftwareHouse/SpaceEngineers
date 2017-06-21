using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage;
using VRageMath;
using Sandbox.Graphics;
using System.Collections.Generic;
using Sandbox.Common;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Profiler;
using VRage.Utils;

namespace Sandbox.Game.Gui
{
    class MyTerminalControlPanel
    {
        private static readonly MyTerminalComparer m_nameComparer = new MyTerminalComparer();

        private IMyGuiControlsParent m_controlsParent;
        private MyGuiControlListbox m_blockListbox;
        private MyGuiControlLabel m_blockNameLabel;
        private MyGuiControlBase m_blockControl;

        private MyGridTerminalSystem m_terminalSystem;
        private List<MyTerminalBlock> CurrentBlocks { get { return m_tmpGroup.Blocks; } }
        private List<MyBlockGroup> m_currentGroups = new List<MyBlockGroup>();
        private MyBlockGroup m_tmpGroup;
        private MyGuiControlTextbox m_blockSearch;
        private MyGuiControlTextbox m_groupName;
        private MyGuiControlButton m_groupSave;
        private MyGuiControlButton m_showAll;
        private MyGuiControlButton m_groupDelete;
        private MyGuiControlButton m_blockSearchClear;
        private List<MyBlockGroup> m_oldGroups = new List<MyBlockGroup>();

        private static bool m_showAllTerminalBlocks = false;

        private MyGridColorHelper m_colorHelper;

        private MyPlayer m_controller;

        public MyGridTerminalSystem TerminalSystem { get { return m_terminalSystem; } }

        public void Init(IMyGuiControlsParent controlsParent, MyPlayer controller, MyCubeGrid grid, MyTerminalBlock currentBlock, MyGridColorHelper colorHelper)
        {
            m_controlsParent = controlsParent;
            m_controller = controller;
            m_colorHelper = colorHelper;

            if (grid == null)
            {
                foreach (var control in controlsParent.Controls)
                    control.Visible = false;

                var label = MyGuiScreenTerminal.CreateErrorLabel(MySpaceTexts.ScreenTerminalError_ShipNotConnected, "ErrorMessage");
                controlsParent.Controls.Add(label);
                return;
            }

            m_terminalSystem = grid.GridSystems.TerminalSystem;
            m_tmpGroup = new MyBlockGroup(grid);

            m_blockSearch = (MyGuiControlTextbox)m_controlsParent.Controls.GetControlByName("FunctionalBlockSearch");
            m_blockSearch.TextChanged += blockSearch_TextChanged;
            m_blockSearchClear = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("FunctionalBlockSearchClear");
            m_blockSearchClear.ButtonClicked += blockSearchClear_ButtonClicked;
            m_blockListbox = (MyGuiControlListbox)m_controlsParent.Controls.GetControlByName("FunctionalBlockListbox");
            m_blockNameLabel = (MyGuiControlLabel)m_controlsParent.Controls.GetControlByName("BlockNameLabel");
            m_blockNameLabel.Text = "";
            m_groupName = (MyGuiControlTextbox)m_controlsParent.Controls.GetControlByName("GroupName");
            m_groupName.TextChanged += m_groupName_TextChanged;

            m_showAll = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("ShowAll");
            m_showAll.Selected = m_showAllTerminalBlocks;
            m_showAll.ButtonClicked += showAll_Clicked;
            m_showAll.SetToolTip(MySpaceTexts.Terminal_ShowAllInTerminal);
            m_showAll.IconRotation = 0f;
            m_showAll.Icon = new MyGuiHighlightTexture
                {
                    Normal = @"Textures\GUI\Controls\button_hide.dds",
                    Highlight = @"Textures\GUI\Controls\button_unhide.dds",
                    SizePx = new Vector2(40f, 40f),
                };
            m_showAll.Size = new Vector2(0, 0);

            m_showAll.HighlightType = MyGuiControlHighlightType.FORCED;
            m_showAll.IconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            m_groupSave = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("GroupSave");
            m_groupSave.TextEnum = MySpaceTexts.TerminalButton_GroupSave;
            m_groupSave.TextAlignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_groupSave.ButtonClicked += groupSave_ButtonClicked;
            m_groupDelete = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("GroupDelete");
            m_groupDelete.ButtonClicked += groupDelete_ButtonClicked;
            m_groupDelete.Enabled = false;

            m_blockListbox.ItemsSelected += blockListbox_ItemSelected;

            RefreshBlockList();

            m_terminalSystem.BlockAdded += TerminalSystem_BlockAdded;
            m_terminalSystem.BlockRemoved += TerminalSystem_BlockRemoved;
            m_terminalSystem.GroupAdded += TerminalSystem_GroupAdded;
            m_terminalSystem.GroupRemoved += TerminalSystem_GroupRemoved;
            if (currentBlock != null)
                SelectBlocks(new MyTerminalBlock[] { currentBlock });
        }

        void m_groupName_TextChanged(MyGuiControlTextbox obj)
        {
            if (string.IsNullOrEmpty(obj.Text) || CurrentBlocks.Count == 0)
                m_groupSave.Enabled = false;
            else
                m_groupSave.Enabled = true;
        }

        void TerminalSystem_GroupRemoved(MyBlockGroup group)
        {
            Debug.Assert(m_blockListbox != null);
            if (m_blockListbox != null)
                foreach (var item in m_blockListbox.Items)
                    if (item.UserData == group)
                    {
                        m_blockListbox.Items.Remove(item);
                        break;
                    }
        }

        void TerminalSystem_GroupAdded(MyBlockGroup group)
        {
            Debug.Assert(m_blockListbox != null);
            if (m_blockListbox != null)
                AddGroupToList(group, 0);
        }

        void blockSearchClear_ButtonClicked(MyGuiControlButton obj)
        {
            m_blockSearch.Text = "";
        }

        void groupDelete_ButtonClicked(MyGuiControlButton obj)
        {
            bool containsEnemyBlock = false;
            foreach (var group in m_currentGroups)
            {
                foreach (var block in group.Blocks)
                {
                    if (!block.HasLocalPlayerAccess())
                    {
                        containsEnemyBlock = true;
                        break;
                    }
                }
            }

            if (containsEnemyBlock)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.OK,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextCannotDeleteGroup)
                ));
            }
            else
            {
                while (m_currentGroups.Count > 0)
                    m_terminalSystem.RemoveGroup(m_currentGroups[0]);
            }
        }

        void showAll_Clicked(MyGuiControlButton obj)
        {
            m_showAllTerminalBlocks = !m_showAllTerminalBlocks;
            m_showAll.Selected = m_showAllTerminalBlocks;
            ClearBlockList();
            PopulateBlockList();
            //GR: Scroll toolbar to top manually when needed from individual controls
            m_blockListbox.ScrollToolbarToTop();
        }

        void groupSave_ButtonClicked(MyGuiControlButton obj)
        {
            bool containsEnemyBlock = false;
            foreach (var block in m_tmpGroup.Blocks)
            {
                if (!block.HasLocalPlayerAccess())
                {
                    containsEnemyBlock = true;
                    break;
                }
            }

            if (containsEnemyBlock)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextCannotCreateGroup)
                    ));
            }
            else
            {
                Debug.Assert(!string.IsNullOrEmpty(m_groupName.Text));
                if (m_groupName.Text != "")
                {
                    m_currentGroups.Clear();
                    m_tmpGroup.Name.Clear().Append(m_groupName.Text);
                    m_tmpGroup = m_terminalSystem.AddUpdateGroup(m_tmpGroup);
                    m_currentGroups.Add(m_tmpGroup);
                    m_tmpGroup = new MyBlockGroup(null);
                    CurrentBlocks.AddList(m_currentGroups[0].Blocks);
                    SelectBlocks();
                }
            }
        }

        void blockSearch_TextChanged(MyGuiControlTextbox obj)
        {
            if (obj.Text != "")
            {
                String[] tmpSearch = obj.Text.Split(' ');
                foreach (var item in m_blockListbox.Items)
                {
                    String tmpName = item.Text.ToString().ToLower();
                    bool add = true;
                    foreach (var search in tmpSearch)
                        if (!tmpName.Contains(search.ToLower()))
                        {
                            add = false;
                            break;
                        }
                    if (add)
                        item.Visible = true;
                    else
                        item.Visible = false;
                }
            }
            else
            {
                foreach (var item in m_blockListbox.Items)
                    item.Visible = true;
            }
            //GR: Scroll toolbar to top manually when needed from individual controls
            m_blockListbox.ScrollToolbarToTop();
            //SelectBlocks();
        }

        void TerminalSystem_BlockAdded(MyTerminalBlock obj)
        {
            AddBlockToList(obj);
        }

        void TerminalSystem_BlockRemoved(MyTerminalBlock obj)
        {
            obj.CustomNameChanged -= block_CustomNameChanged;
            obj.PropertiesChanged -= block_CustomNameChanged;
            Debug.Assert(m_blockListbox != null);
            if (m_blockListbox != null && (obj.ShowInTerminal || m_showAllTerminalBlocks))
            { 
                m_blockListbox.Remove((item) => (item.UserData == obj));
            }
        }

        public void Close()
        {
            if (m_terminalSystem != null)
            {
                if (m_blockListbox != null)
                {
                    ClearBlockList();
                    m_blockListbox.ItemsSelected -= blockListbox_ItemSelected;
                }

                m_terminalSystem.BlockAdded -= TerminalSystem_BlockAdded;
                m_terminalSystem.BlockRemoved -= TerminalSystem_BlockRemoved;
                m_terminalSystem.GroupAdded -= TerminalSystem_GroupAdded;
                m_terminalSystem.GroupRemoved -= TerminalSystem_GroupRemoved;
            }

            if (m_tmpGroup != null)
            {
                m_tmpGroup.Blocks.Clear();
            }

            m_controlsParent = null;
            m_blockListbox = null;
            m_blockNameLabel = null;
            m_terminalSystem = null;
            m_currentGroups.Clear();
        }

        public void RefreshBlockList()
        {
            ProfilerShort.Begin("ControllerControlPanel.RefreshBlockList");
            if (m_blockListbox != null)
            {
                ClearBlockList();
                PopulateBlockList();
            }
            ProfilerShort.End();
        }

        public void ClearBlockList()
        {
            if (m_blockListbox != null)
            {
                foreach (var item in m_blockListbox.Items)
                {
                    if (item.UserData is MyTerminalBlock)
                    {
                        var block = (MyTerminalBlock)item.UserData;
                        block.CustomNameChanged -= block_CustomNameChanged;
                        block.PropertiesChanged -= block_CustomNameChanged;
                        block.ShowInTerminalChanged -= block_ShowInTerminalChanged;
                    }
                }
                m_blockListbox.Items.Clear();
            }
        }

        public void PopulateBlockList()
        {
            // null ref somewhere here
            if (m_terminalSystem == null) return; //this happens when populate is called after grid is removed from group and before its added to another
            if (m_terminalSystem.BlockGroups == null) MySandboxGame.Log.WriteLine("m_terminalSystem.BlockGroups is null");
            if (!m_terminalSystem.Blocks.IsValid) MySandboxGame.Log.WriteLine("m_terminalSystem.Blocks.IsValid is false");
            if (CurrentBlocks == null) MySandboxGame.Log.WriteLine("CurrentBlocks is null");
            if (m_blockListbox == null) MySandboxGame.Log.WriteLine("m_blockListbox is null");

            var sortArray1 = m_terminalSystem.BlockGroups.ToArray();
            Array.Sort(sortArray1, MyTerminalComparer.Static);
            foreach (var group in sortArray1)
                AddGroupToList(group);
            var sortArray = m_terminalSystem.Blocks.ToArray();
            Array.Sort(sortArray, MyTerminalComparer.Static);
            m_blockListbox.SelectedItems.Clear();
            foreach (var block in sortArray)
            {
                if (block.ShowInTerminal || m_showAllTerminalBlocks)
                {
                    AddBlockToList(block);
                }
            }

            if (CurrentBlocks.Count > 0)
                SelectBlocks();
            else
                foreach (var item in m_blockListbox.Items)
                {
                    if (item.UserData is MyTerminalBlock)
                    {
                        SelectBlocks(new MyTerminalBlock[] { (MyTerminalBlock)item.UserData });
                        break;
                    }
                }

        }

        private void AddGroupToList(MyBlockGroup group, int? position = null)
        {
            foreach (var it in m_blockListbox.Items)
                if (it.UserData == group)
                    return;
            var item = new MyGuiControlListbox.Item(userData: group);
            item.Text.Clear().Append("*").AppendStringBuilder(group.Name).Append("*");
            m_blockListbox.Add(item, position);
        }

        private MyGuiControlListbox.Item AddBlockToList(MyTerminalBlock block)
        {
            var item = new MyGuiControlListbox.Item(userData: block);
            UpdateItemAppearance(block, item);
            block.CustomNameChanged += block_CustomNameChanged;
            block.PropertiesChanged += block_CustomNameChanged;
            block.ShowInTerminalChanged += block_ShowInTerminalChanged;

            m_blockListbox.Add(item);
            return item;
        }

        private void UpdateItemAppearance(MyTerminalBlock block, MyGuiControlListbox.Item item)
        {
            item.Text.Clear().Append(block.CustomName);
            if (!block.IsFunctional)
            {
                item.ColorMask = Vector4.One;
                item.Text.AppendStringBuilder(MyTexts.Get(MySpaceTexts.Terminal_BlockIncomplete));
                item.FontOverride = MyFontEnum.Red;
            }
            else if (!block.HasPlayerAccess(m_controller.Identity.IdentityId))
            {
                item.ColorMask = Vector4.One;
                item.Text.AppendStringBuilder(MyTexts.Get(MySpaceTexts.Terminal_BlockAccessDenied));
                item.FontOverride = MyFontEnum.Red;
            }
            else if (block.ShowInTerminal == false)
            {
                item.ColorMask = 0.6f * m_colorHelper.GetGridColor(block.CubeGrid).ToVector4();
                item.FontOverride = null;
            }
            else
            {
                item.ColorMask = m_colorHelper.GetGridColor(block.CubeGrid).ToVector4();
                item.FontOverride = null;
            }
        }

        void block_CustomNameChanged(MyTerminalBlock obj)
        {
            System.Diagnostics.Debug.Assert(m_blockListbox != null, "block_CustomNameChanged was not unregistered!");

            if (m_blockListbox == null)
                return;

            ProfilerShort.Begin("MyTerminalControlPanel.block_CustomNameChanged");
            foreach (var item in m_blockListbox.Items)
            {
                if (item.UserData == obj)
                {
                    UpdateItemAppearance(obj, item);
                    break;
                }
            }

            if (CurrentBlocks.Count > 0 && CurrentBlocks[0] == obj)
                m_blockNameLabel.Text = obj.CustomName.ToString();

            ProfilerShort.End();
        }

        public void SelectBlocks(MyTerminalBlock[] blocks)
        {
            m_tmpGroup.Blocks.Clear();
            m_tmpGroup.Blocks.AddArray(blocks);
            m_currentGroups.Clear();

            CurrentBlocks.Clear();
            CurrentBlocks.AddArray(blocks);
            SelectBlocks();
        }

        private void SelectBlocks()
        {
            if (m_blockControl != null)
            {
                m_controlsParent.Controls.Remove(m_blockControl);
                m_blockControl = null;
            }
            m_blockNameLabel.Text = "";
            m_groupName.Text = "";

            if (m_currentGroups.Count == 1)
            {
                m_blockNameLabel.Text = m_currentGroups[0].Name.ToString();
                m_groupName.Text = m_blockNameLabel.Text;
            }

            if (CurrentBlocks.Count > 0)
            {
                CurrentBlocks.Sort(MyTerminalComparer.Static);

                if (CurrentBlocks.Count == 1)
                    m_blockNameLabel.Text = CurrentBlocks[0].CustomName.ToString();

                m_blockControl = new MyGuiControlGenericFunctionalBlock(CurrentBlocks.ToArray());
                m_controlsParent.Controls.Add(m_blockControl);

                m_blockControl.Size = new Vector2(0.595f, 0.64f);
                m_blockControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                m_blockControl.Position = new Vector2(-0.1415f, -0.3f); // m_blockNameLabel.Position + new Vector2(0f, 0.1f);
            }

            UpdateGroupControl();

            m_blockListbox.SelectedItems.Clear();
            foreach (var b in CurrentBlocks)
                foreach (var item in m_blockListbox.Items)
                    if (item.UserData == b)
                    {
                        m_blockListbox.SelectedItems.Add(item);
                        break;
                    }
            foreach (var g in m_currentGroups)
            {
                foreach (var item in m_blockListbox.Items)
                {
                    if (item.UserData == g)
                    {
                        m_blockListbox.SelectedItems.Add(item);
                        break;
                    }
                }
            }
        }

        public void SelectAllBlocks()
        {
            if (m_blockListbox != null)
            {
                m_blockListbox.SelectAllVisible();
            }
        }

        private void UpdateGroupControl()
        {
            if (m_currentGroups.Count > 0)
                m_groupDelete.Enabled = true;
            else
                m_groupDelete.Enabled = false;
        }

        public void UpdateCubeBlock(MyTerminalBlock block)
        {

            if (block == null)
                return;

            if (m_terminalSystem != null)
            {
                m_terminalSystem.BlockAdded -= TerminalSystem_BlockAdded;
                m_terminalSystem.BlockRemoved -= TerminalSystem_BlockRemoved;
                m_terminalSystem.GroupAdded -= TerminalSystem_GroupAdded;
                m_terminalSystem.GroupRemoved -= TerminalSystem_GroupRemoved;
            }

            var grid = block.CubeGrid;
            m_terminalSystem = grid.GridSystems.TerminalSystem;
            m_tmpGroup = new MyBlockGroup(grid);

            m_terminalSystem.BlockAdded += TerminalSystem_BlockAdded;
            m_terminalSystem.BlockRemoved += TerminalSystem_BlockRemoved;
            m_terminalSystem.GroupAdded += TerminalSystem_GroupAdded;
            m_terminalSystem.GroupRemoved += TerminalSystem_GroupRemoved;

            SelectBlocks(new MyTerminalBlock[] { block });


        }

        private void blockListbox_ItemSelected(MyGuiControlListbox sender)
        {
            Debug.Assert(sender == m_blockListbox);
            m_oldGroups.Clear();
            m_oldGroups.AddList(m_currentGroups);
            m_currentGroups.Clear();
            m_tmpGroup.Blocks.Clear();

            foreach (var item in sender.SelectedItems)
            {
                if (item.UserData is MyBlockGroup)
                    m_currentGroups.Add((MyBlockGroup)item.UserData);
                else if (item.UserData is MyTerminalBlock)
                {
                    CurrentBlocks.Add(item.UserData as MyTerminalBlock);
                }
                else
                    Debug.Fail("Terminal list must contain only Functional blocks and groups.");
            }
            for (int i = 0; i < m_currentGroups.Count; i++)
            {
                if (m_oldGroups.Contains(m_currentGroups[i]) && m_currentGroups[i].Blocks.Intersect(CurrentBlocks).Count() != 0)
                    continue;
                foreach (var b in m_currentGroups[i].Blocks)
                {
                    if (!CurrentBlocks.Contains(b))
                        CurrentBlocks.Add(b);
                }
            }
            SelectBlocks();
        }

        void block_ShowInTerminalChanged(MyTerminalBlock obj)
        {
            ClearBlockList();
            PopulateBlockList();
            
            //JC: This should never be null at this point but is happening...
            if (m_blockListbox != null)
                //GR: Scroll toolbar to top manually when needed from individual controls
                m_blockListbox.ScrollToolbarToTop();
        }
    }
}
