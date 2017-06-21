﻿#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sandbox.Engine.Networking;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.Definitions.Animation;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using VRage.Profiler;
using Sandbox.Game.SessionComponents;
using VRage.Audio;

#endregion

namespace Sandbox.Game.Gui
{
    #region Search conditions

    public interface IMySearchCondition
    {
        bool MatchesCondition(string itemId);
        bool MatchesCondition(MyDefinitionBase itemId);
        void AddDefinitionGroup(MyCubeBlockDefinitionGroup definitionGruop);
        HashSet<MyCubeBlockDefinitionGroup> GetSortedBlocks();
        void CleanDefinitionGroups();
    }

    public class MySearchByCategoryCondition : IMySearchCondition
    {
        public List<MyGuiBlockCategoryDefinition> SelectedCategories = null;

        public bool MatchesCondition(string itemId)
        {
            return IsItemInAnySelectedCategory(itemId);
        }
        public bool MatchesCondition(MyDefinitionBase itemId)
        {
            return IsItemInAnySelectedCategory(itemId.Id.ToString());
        }
        public void AddDefinitionGroup(MyCubeBlockDefinitionGroup definitionGruop)
        {
            if (null == m_lastCategory)
            {
                return;
            }
            List<MyCubeBlockDefinitionGroup> definitionGroupsForCategory = null;
            if (false == m_blocksByCategories.TryGetValue(m_lastCategory.Name, out definitionGroupsForCategory))
            {
                definitionGroupsForCategory = new List<MyCubeBlockDefinitionGroup>();
                m_blocksByCategories.Add(m_lastCategory.Name, definitionGroupsForCategory);
            }
            definitionGroupsForCategory.Add(definitionGruop);
        }

        public HashSet<MyCubeBlockDefinitionGroup> GetSortedBlocks()
        {
            foreach (var category in m_blocksByCategories)
            {
                foreach (var definitionGroup in category.Value)
                {
                    m_sortedBlocks.Add(definitionGroup);
                }
            }
            return m_sortedBlocks;
        }

        public void CleanDefinitionGroups()
        {
            m_sortedBlocks.Clear();
            m_blocksByCategories.Clear();
        }

        private bool IsItemInAnySelectedCategory(string itemId)
        {
            m_lastCategory = null;
            if (null == SelectedCategories)
            {
                return true;
            }
            foreach (var category in SelectedCategories)
            {
                if (category.HasItem(itemId) || (category.ShowAnimations && itemId.Contains("AnimationDefinition")))
                {
                    m_lastCategory = category;
                    return true;
                }
            }
            return false;
        }

        MyGuiBlockCategoryDefinition m_lastCategory = null;
        private HashSet<MyCubeBlockDefinitionGroup> m_sortedBlocks = new HashSet<MyCubeBlockDefinitionGroup>();
        private Dictionary<string, List<MyCubeBlockDefinitionGroup>> m_blocksByCategories = new Dictionary<string, List<MyCubeBlockDefinitionGroup>>();

    }

    public class MySearchByStringCondition : IMySearchCondition
    {
        string[] m_searchItems;

        public string SearchName
        {
            set { m_searchItems = value.Split(' '); }
        }

        public bool IsValid
        {
            get { return m_searchItems != null; }
        }

        public void Clean()
        {
            m_searchItems = null;
            CleanDefinitionGroups();
        }

        public bool MatchesCondition(string itemId)
        {
            foreach (string item in m_searchItems)
            {
                if (!itemId.Contains(item, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
        public bool MatchesCondition(MyDefinitionBase itemId)
        {
            if (itemId != null)
                return MatchesCondition(itemId.DisplayNameText.ToString());
            return false;
        }
        public void AddDefinitionGroup(MyCubeBlockDefinitionGroup definitionGruop)
        {
            m_sortedBlocks.Add(definitionGruop);
        }
        public HashSet<MyCubeBlockDefinitionGroup> GetSortedBlocks()
        {
            return m_sortedBlocks;
        }
        public void CleanDefinitionGroups()
        {
            m_sortedBlocks.Clear();
        }
        private HashSet<MyCubeBlockDefinitionGroup> m_sortedBlocks = new HashSet<MyCubeBlockDefinitionGroup>();
    }

    #endregion

    // Base class for G-screen
    public class MyGuiScreenToolbarConfigBase : MyGuiScreenBase
    {
        #region GUI members
        public static MyGuiScreenToolbarConfigBase Static;

        protected MyGuiControlTextbox m_searchItemTextBox;
        protected MyGuiControlListbox m_categoriesListbox;
        protected MyGuiControlGrid m_gridBlocks;

        protected MyGuiControlScrollablePanel m_gridBlocksPanel;
        protected MyGuiControlLabel m_blocksLabel;

        protected MyGuiControlGridDragAndDrop m_dragAndDrop;

        protected MyGuiControlToolbar m_toolbarControl;

        protected MyGuiControlContextMenu m_contextMenu;
        protected MyGuiControlContextMenu m_onDropContextMenu;

        protected MyGuiControlVoxelHandSettings m_voxelHandConfig;
        #endregion

        #region Logic members

        MyShipController m_shipController = null;

        protected MyCharacter m_character = null;
        protected MyCubeGrid m_screenCubeGrid = null;

        protected const string SHIP_GROUPS_NAME = "Groups";
        protected const string CHARACTER_ANIMATIONS_GROUP_NAME = "CharacterAnimations";

        protected MyStringHash manipulationToolId = MyStringHash.GetOrCompute("ManipulationTool");

        protected string[] m_forcedCategoryOrder = new string[] { "ShipWeaponsTools", "WeaponsTools", "CharacterTools", CHARACTER_ANIMATIONS_GROUP_NAME, SHIP_GROUPS_NAME };

        protected MySearchByStringCondition m_nameSearchCondition = new MySearchByStringCondition();
        protected MySearchByCategoryCondition m_categorySearchCondition = new MySearchByCategoryCondition();

        protected SortedDictionary<String, MyGuiBlockCategoryDefinition> m_sortedCategories = new SortedDictionary<String, MyGuiBlockCategoryDefinition>();

        protected static List<MyGuiBlockCategoryDefinition> m_allSelectedCategories = new List<MyGuiBlockCategoryDefinition>();
        protected List<MyGuiBlockCategoryDefinition> m_searchInBlockCategories = new List<MyGuiBlockCategoryDefinition>();

        protected MyGuiBlockCategoryDefinition m_shipGroupsCategory = new MyGuiBlockCategoryDefinition();

        protected float m_scrollOffset;

        protected static float m_savedVPosition = 0.0f;
        protected int m_contextBlockX = -1, m_contextBlockY = -1;

        protected int m_onDropContextMenuToolbarIndex = -1;
        protected MyToolbarItem m_onDropContextMenuItem;
        public enum GroupModes
        {
            Default = 0,
            HideEmpty = 1,
            HideBlockGroups = 2,
            HideAll = 3
        }
        public static GroupModes GroupMode = GroupModes.Default;
        private const int BLOCK_GROUPS_MIN_COUNT = 20;

        public class GridItemUserData
        {
            public MyObjectBuilder_ToolbarItem ItemData;
        }

        protected MyCubeBlock m_screenOwner = null;
        protected static bool m_ownerChanged = false;
        protected static MyEntity m_previousOwner = null;

        int m_framesBeforeSearchEnabled = 5;
        private MyDragAndDropInfo m_dragAndDropInfo;

        #endregion

        #region constructros + overrides

        public MyGuiScreenToolbarConfigBase(int scrollOffset = 0, MyCubeBlock owner = null)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, backgroundTransition: MySandboxGame.Config.UIBkOpacity, guiTransition: MySandboxGame.Config.UIOpacity)
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenCubeBuilder.ctor START");

            Static = this;

            m_scrollOffset = scrollOffset / 6.5f;
            m_size = new Vector2(1, 1);
            m_canShareInput = true;
            m_drawEvenWithoutFocus = true;
            EnabledBackgroundFade = true;
            m_screenOwner = owner;
            m_defaultJoystickDpadUse = false;

            // To prevent "RecreateControls()" from getting called twice, let's check if
            //  the object being created is actually this class.  If not, we won't call
            //  recreate controls as the inherited class does that itself, hence the
            //  two calls to "RecreateControls()".
            Type type = this.GetType();
            if (typeof(MyGuiScreenToolbarConfigBase) == this.GetType())
            {
                RecreateControls(true);
            }

            m_framesBeforeSearchEnabled = 10;

            MySandboxGame.Log.WriteLine("MyGuiScreenCubeBuilder.ctor END");
        }

        protected override void OnClosed()
        {
            Static = null;
            base.OnClosed();
            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
        }

        //anything that needs to be reseted after the session is unloaded
        public static void Reset()
        {
            m_allSelectedCategories.Clear();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (FocusedControl == null && MyInput.Static.IsKeyPress(MyKeys.Tab))
                FocusedControl = m_searchItemTextBox;

            if (MyInput.Static.IsMouseReleased(MyMouseButtonsEnum.Right))
            {
                if (m_onDropContextMenu.Enabled)
                {
                    m_onDropContextMenu.Enabled = false;
                    m_contextMenu.Enabled = false;
                    m_onDropContextMenu.Activate();
                }
                else if (m_contextMenu.Enabled && !m_onDropContextMenu.Visible)
                {
                    m_contextMenu.Enabled = false;
                    m_contextMenu.Activate();
                }
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.BUILD_SCREEN))
            {
                if (false == m_searchItemTextBox.HasFocus)
                {
                    if (m_closingCueEnum.HasValue)
                        MyGuiSoundManager.PlaySound(m_closingCueEnum.Value);
                    else
                        MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                    CloseScreen();
                }
                else if (MyInput.Static.IsNewGameControlJoystickOnlyPressed(MyControlsSpace.BUILD_SCREEN))
                {
                    if (m_closingCueEnum.HasValue)
                        MyGuiSoundManager.PlaySound(m_closingCueEnum.Value);
                    else
                        MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                    CloseScreen();
                }
            }
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.PAUSE_GAME))
            {
                MySandboxGame.PauseToggle();
            }
        }

        public override bool CloseScreen()
        {
            m_savedVPosition = m_gridBlocksPanel.ScrollbarVPosition;
            Static = null;
            MyAnalyticsHelper.ReportActivityEnd(null, "show_toolbar_config");
            return base.CloseScreen();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenToolbarConfigBase";
        }

        #endregion

        #region init on pause
        public override void RecreateControls(bool contructor)
        {
            ProfilerShort.Begin("MyGuiScreenToolbarConfigBase.RecreateControls");
            base.RecreateControls(contructor);

            MyAnalyticsHelper.ReportActivityStart(null, "show_toolbar_config", string.Empty, "gui", string.Empty);

            m_character = null;
            m_shipController = null;

            m_ownerChanged = (m_previousOwner != MyToolbarComponent.CurrentToolbar.Owner);
            m_previousOwner = MyToolbarComponent.CurrentToolbar.Owner;

            // Few variables that modify the look of the screen
            if (MyToolbarComponent.CurrentToolbar.Owner == null)
            {
                m_character = MySession.Static.LocalCharacter;
            }
            else
            {
                m_shipController = MyToolbarComponent.CurrentToolbar.Owner as MyShipController;
            }
            m_screenCubeGrid = m_screenOwner == null ? null : m_screenOwner.CubeGrid;

            bool isShip = m_screenCubeGrid != null;

            var fileName = Path.Combine("Data", "Screens", "CubeBuilder.gsc");
            var fsPath = Path.Combine(MyFileSystem.ContentPath, fileName);

            MyObjectBuilder_GuiScreen objectBuilder;
            MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_GuiScreen>(fsPath, out objectBuilder);

            Init(objectBuilder);

            m_gridBlocks = (MyGuiControlGrid)Controls.GetControlByName("Grid");

            m_categoriesListbox = (MyGuiControlListbox)Controls.GetControlByName("CategorySelector");
            m_categoriesListbox.VisualStyle = MyGuiControlListboxStyleEnum.ToolsBlocks;
            m_categoriesListbox.ItemClicked += categories_ItemClicked;

            m_searchItemTextBox = (MyGuiControlTextbox)Controls.GetControlByName("SearchItemTextBox");
            m_searchItemTextBox.TextChanged += searchItemTexbox_TextChanged;
            m_searchItemTextBox.Enabled = false;
            m_searchItemTextBox.SkipCombinations = new MyGuiControlTextbox.MySkipCombination[] 
            {
                new MyGuiControlTextbox.MySkipCombination()
                {
                    Shift = true,
                    Keys = null //all
                },
                new MyGuiControlTextbox.MySkipCombination()
                {
                    Ctrl = true,
                    Keys = null //all
                }
                ,
                new MyGuiControlTextbox.MySkipCombination()
                {
                    Keys = new MyKeys[] { (MyKeys)0x2C, (MyKeys)0x2E} //<, >
                }
            };


            // Remove the block grid (it will be inside scrollable panel)
            Controls.Remove(m_gridBlocks);

            m_gridBlocks.VisualStyle = MyGuiControlGridStyleEnum.Toolbar;

            // Create the panel and insert block grid into it
            m_gridBlocksPanel = new MyGuiControlScrollablePanel(m_gridBlocks);
            var styleDef = MyGuiControlGrid.GetVisualStyle(MyGuiControlGridStyleEnum.ToolsBlocks);

            m_gridBlocksPanel.BackgroundTexture = styleDef.BackgroundTexture;
            m_gridBlocksPanel.ScrollbarVEnabled = true;
            m_gridBlocksPanel.ScrolledAreaPadding = new MyGuiBorderThickness(12f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 10f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
            m_gridBlocksPanel.FitSizeToScrolledControl();
            m_gridBlocksPanel.Size = m_gridBlocksPanel.Size + new Vector2(0.0f, -0.02f + 45f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
            m_gridBlocksPanel.PanelScrolled += grid_PanelScrolled;
            m_gridBlocksPanel.Position = new Vector2(-0.01f, 0.01f);

            Controls.Add(m_gridBlocksPanel);

            if (m_scrollOffset != 0.0f)
                m_gridBlocksPanel.SetPageVertical(m_scrollOffset);
            else
                m_gridBlocksPanel.ScrollbarVPosition = m_savedVPosition;

            m_blocksLabel = (MyGuiControlLabel)Controls.GetControlByName("BlocksLabel");

            m_toolbarControl = (MyGuiControlToolbar)Activator.CreateInstance(MyPerGameSettings.GUI.ToolbarControl);
            m_toolbarControl.Position = new Vector2(0f, 0.49f);
            m_toolbarControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM;
            if (MyPerGameSettings.Game == GameEnum.ME_GAME)
            {
                m_toolbarControl.ToolbarGrid.ItemDragged += OnItemDragged;
            }
            Controls.Add(m_toolbarControl);

            m_onDropContextMenu = new MyGuiControlContextMenu();
            m_onDropContextMenu.Deactivate();
            m_onDropContextMenu.ItemClicked += onDropContextMenu_ItemClicked;
            Controls.Add(m_onDropContextMenu);

            m_gridBlocks.SetItemsToDefault();
            m_gridBlocks.ItemDoubleClicked += grid_ItemDoubleClicked;
            m_gridBlocks.ItemClicked += grid_ItemClicked;
            m_gridBlocks.ItemDragged += grid_OnDrag;

            AddCategoryToDisplayList(MyTexts.GetString(MySpaceTexts.DisplayName_Category_AllBlocks), null);

            Dictionary<string, MyGuiBlockCategoryDefinition> categoriesDefinitions = MyDefinitionManager.Static.GetCategories();

            if (m_screenCubeGrid != null && (m_shipController == null || (m_shipController != null && !m_shipController.BuildingMode)))
            {
                bool isSeat = isShip && m_shipController != null && !m_shipController.EnableShipControl;
                if (false == isSeat)
                {
                    RecreateShipCategories(categoriesDefinitions, m_sortedCategories, m_screenCubeGrid);
                    AddShipGroupsIntoCategoryList(m_screenCubeGrid);
                    AddShipBlocksDefinitions(m_screenCubeGrid, isShip, null);
                    AddShipGunsToCategories(categoriesDefinitions, m_sortedCategories);
                }
                else
                {
                    //seat doesn't have any cube blocks so we need to remove All blocks category
                    m_categoriesListbox.Items.Clear();
                }

                if (m_shipController != null && m_shipController.ToolbarType != MyToolbarType.None)
                {

                    //Ship doesn't have animations between blocks, but player needs to be able to use animations in ships
                    MyGuiBlockCategoryDefinition characterAnimationsDefinition = null;
                    if (false == m_sortedCategories.TryGetValue(CHARACTER_ANIMATIONS_GROUP_NAME, out characterAnimationsDefinition) &&
                        true == categoriesDefinitions.TryGetValue(CHARACTER_ANIMATIONS_GROUP_NAME, out characterAnimationsDefinition))
                    {
                        m_sortedCategories.Add(CHARACTER_ANIMATIONS_GROUP_NAME, characterAnimationsDefinition);
                    }
                }
            }
            else if (m_character != null || (m_shipController != null && m_shipController.BuildingMode))
            {
                if (GroupMode != GroupModes.HideAll)
                    RecreateBlockCategories(categoriesDefinitions, m_sortedCategories);
                AddCubeDefinitionsToBlocks(m_categorySearchCondition);
            }

            //Setting gridBlocks always to visible to show terminal system block parts when in ship
            if (MyFakes.ENABLE_SHIP_BLOCKS_TOOLBAR)
            {
                m_gridBlocks.Visible = true;
                m_gridBlocksPanel.ScrollbarVEnabled = true;
                m_blocksLabel.Visible = true;
            }
            else
            {
                m_gridBlocksPanel.ScrollbarVEnabled = !isShip;
                m_gridBlocks.Visible = !isShip;
                m_blocksLabel.Visible = !isShip;
            }


            SortCategoriesToDisplayList();

            // initialize drag and drop
            m_dragAndDrop = new MyGuiControlGridDragAndDrop(MyGuiConstants.DRAG_AND_DROP_BACKGROUND_COLOR,
                                                               MyGuiConstants.DRAG_AND_DROP_TEXT_COLOR,
                                                               0.7f,
                                                               MyGuiConstants.DRAG_AND_DROP_TEXT_OFFSET, true);
            m_dragAndDrop.ItemDropped += dragAndDrop_OnDrop;
            m_dragAndDrop.DrawBackgroundTexture = false;
            Controls.Add(m_dragAndDrop);

            //When opening the G screen, context menu is always invisible by default
            m_contextMenu = new MyGuiControlContextMenu();
            m_contextMenu.ItemClicked += contextMenu_ItemClicked;
            Controls.Add(m_contextMenu);
            m_contextMenu.Deactivate();

            m_voxelHandConfig = new MyGuiControlVoxelHandSettings();
            m_voxelHandConfig.Visible = false;
            m_voxelHandConfig.Position = new Vector2(0.28f, -0.28f);
            Controls.Add(m_voxelHandConfig);

            if (m_categoriesListbox.Items.Count > 0)
            {
                SelectCategories();
            }

            ProfilerShort.End();
        }

        private void OnItemDragged(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            StartDragging(MyDropHandleType.MouseRelease, sender, ref eventArgs);
        }

        protected void SelectCategories()
        {
            List<MyGuiControlListbox.Item> selectedItems = new List<MyGuiControlListbox.Item>();

            if (m_allSelectedCategories.Count == 0 || m_ownerChanged)
            {
                selectedItems.Add(m_categoriesListbox.Items[0]);
            }
            else
            {
                foreach (var item in m_categoriesListbox.Items)
                {
                    if (m_allSelectedCategories.Exists(x => (x == item.UserData)))
                    {
                        selectedItems.Add(item);
                    }
                }
            }
            m_allSelectedCategories.Clear();
            m_categoriesListbox.SelectedItems = selectedItems;
            categories_ItemClicked(m_categoriesListbox);
        }

        protected void SortCategoriesToDisplayList()
        {
            foreach (var groupName in m_forcedCategoryOrder)
            {
                MyGuiBlockCategoryDefinition value = null;
                if (true == m_sortedCategories.TryGetValue(groupName, out value))
                {
                    AddCategoryToDisplayList(value.DisplayNameText, value);
                }
            }
            foreach (var category in m_sortedCategories)
            {
                if (false == m_forcedCategoryOrder.Contains(category.Key))
                {
                    AddCategoryToDisplayList(category.Value.DisplayNameText, category.Value);
                }
            }
        }

        public void RecreateBlockCategories(Dictionary<string, MyGuiBlockCategoryDefinition> loadedCategories, SortedDictionary<String, MyGuiBlockCategoryDefinition> categories)
        {
            categories.Clear();
            foreach (var category in loadedCategories)
                category.Value.ValidItems = 0;
            if (MyPerGameSettings.EnableResearch && (MySessionComponentResearch.Static.m_requiredResearch.Count > 0 || MySessionComponentResearch.Static.WhitelistMode))
            {
                var keys = MyDefinitionManager.Static.GetDefinitionPairNames();
                foreach (var key in keys)
                {
                    var group = MyDefinitionManager.Static.GetDefinitionGroup(key);
                    if (IsValidItem(Vector2I.Zero, group) == false)
                        continue;
                    if (group.AnyPublic != null)
                    {
                        if (MySessionComponentResearch.Static.CanUse(m_character, group.AnyPublic.Id))
                        {
                            foreach (var category in loadedCategories.Values)
                            {
                                if (category.HasItem(group.AnyPublic.Id.ToString()))
                                    category.ValidItems++;
                            }
                        }
                    }
                }
            }

            var player = MyPlayer.GetPlayerFromCharacter(m_character);
            if (player == null)
                return;


            foreach (var category in loadedCategories)
            {             
                if (MySession.Static.SurvivalMode && !category.Value.AvailableInSurvival && !MySession.Static.IsUserAdmin(player.Client.SteamUserId))
                    continue;

                if (MySession.Static.CreativeMode && !category.Value.ShowInCreative)
                    continue;

                if (!MyPerGameSettings.EnableResearch && category.Key.CompareTo("Schematics") == 0)
                    continue;

                if (GroupMode == GroupModes.HideBlockGroups && !category.Value.IsAnimationCategory && !category.Value.IsToolCategory)
                    continue;

                if (GroupMode == GroupModes.HideEmpty && !category.Value.IsAnimationCategory && !category.Value.IsToolCategory)
                {
                    if (category.Value.ItemIds.Count == 0)
                        continue;
                    if (MyPerGameSettings.EnableResearch && (MySessionComponentResearch.Static.m_requiredResearch.Count > 0 || MySessionComponentResearch.Static.WhitelistMode) && category.Value.ValidItems == 0)
                        continue;
                }

                if (true == category.Value.IsBlockCategory)
                {
                    categories.Add(category.Value.Name, category.Value);
                }
            }
        }

        private void AddCategoryToDisplayList(String displayName, MyGuiBlockCategoryDefinition categoryID)
        {
            MyGuiControlListbox.Item newItem = new MyGuiControlListbox.Item(text: new StringBuilder(displayName), toolTip: displayName, userData: categoryID);
            m_categoriesListbox.Add(newItem);
        }

        void AddShipGunsToCategories(Dictionary<string, MyGuiBlockCategoryDefinition> loadedCategories, SortedDictionary<String, MyGuiBlockCategoryDefinition> categories)
        {
            if (null == m_shipController)
            {
                return;
            }

            var gunsSets = m_shipController.CubeGrid.GridSystems.WeaponSystem.GetGunSets();
            foreach (KeyValuePair<MyDefinitionId, HashSet<IMyGunObject<MyDeviceBase>>> pair in gunsSets)
            {
                var definition = MyDefinitionManager.Static.GetDefinition(pair.Key);
                foreach (var category in loadedCategories)
                {
                    if (category.Value.IsShipCategory && category.Value.HasItem(definition.Id.ToString()))
                    {
                        MyGuiBlockCategoryDefinition categoryDefinition = null;
                        if (false == categories.TryGetValue(category.Value.Name, out categoryDefinition))
                        {
                            categories.Add(category.Value.Name, category.Value);
                        }
                    }
                }
            }
        }

        private void RecreateShipCategories(Dictionary<string, MyGuiBlockCategoryDefinition> loadedCategories, SortedDictionary<String, MyGuiBlockCategoryDefinition> categories, MyCubeGrid grid)
        {
            if (grid == null || grid.GridSystems.TerminalSystem == null || grid.GridSystems.TerminalSystem.BlockGroups == null)
            {
                return;
            }

            categories.Clear();

            var blockArray = grid.GridSystems.TerminalSystem.Blocks.ToArray();
            Array.Sort(blockArray, MyTerminalComparer.Static);

            // Create list with all single non-grouped blocks to search for used categories
            List<string> blocksToSearch = new List<string>();
            foreach (var block in blockArray)
            {
                if (block == null)
                {
                    continue;
                }

                string id = block.BlockDefinition.Id.ToString();
                if (false == blocksToSearch.Contains(id))
                {
                    blocksToSearch.Add(id);
                }
            }

            // Iterate the blocks and add used categories
            foreach (string blockID in blocksToSearch)
            {
                foreach (var category in loadedCategories)
                {
                    if (true == category.Value.IsShipCategory && true == category.Value.HasItem(blockID) && true == category.Value.SearchBlocks)
                    {
                        MyGuiBlockCategoryDefinition categoryDefinition = null;
                        if (false == categories.TryGetValue(category.Value.Name, out categoryDefinition))
                        {
                            categories.Add(category.Value.Name, category.Value);
                        }
                    }
                }
            }
        }

        private void AddShipGroupsIntoCategoryList(MyCubeGrid grid)
        {
            if (grid == null || grid.GridSystems.TerminalSystem == null || grid.GridSystems.TerminalSystem.BlockGroups == null)
            {
                return;
            }

            var groupArray = grid.GridSystems.TerminalSystem.BlockGroups.ToArray();
            Array.Sort(groupArray, MyTerminalComparer.Static);
            List<string> groups = new List<string>();
            foreach (MyBlockGroup group in groupArray)
            {
                if (group == null)
                {
                    continue;
                }
                groups.Add(group.Name.ToString());
            }
            if (groups.Count > 0)
            {
                m_shipGroupsCategory.DisplayNameString = MyTexts.GetString(MySpaceTexts.DisplayName_Category_ShipGroups);
                m_shipGroupsCategory.ItemIds = new HashSet<string>(groups);
                m_shipGroupsCategory.SearchBlocks = false;
                m_shipGroupsCategory.Name = SHIP_GROUPS_NAME;
                m_sortedCategories.Add(m_shipGroupsCategory.Name, m_shipGroupsCategory);
            }
        }

        public virtual bool AllowToolbarKeys()
        {
            return !m_searchItemTextBox.HasFocus;
        }
        #endregion

        #region grid block population - common
        protected virtual void UpdateGridBlocksBySearchCondition(IMySearchCondition searchCondition)
        {
            if (null != searchCondition)
            {
                searchCondition.CleanDefinitionGroups();
            }
            bool isSeat = m_shipController != null && !m_shipController.EnableShipControl;
            if (!isSeat)
            {
                if (null != m_character || (m_shipController != null && (m_shipController as MyShipController).BuildingMode))
                {
                    AddCubeDefinitionsToBlocks(searchCondition);
                }
                else if (null != m_screenCubeGrid)
                {
                    AddShipBlocksDefinitions(m_screenCubeGrid, true, searchCondition);
                }
            }
            m_gridBlocks.SelectedIndex = 0;
            m_gridBlocksPanel.ScrollbarVPosition = 0.0f;
        }

        protected virtual void AddToolsAndAnimations(IMySearchCondition searchCondition)
        {
            if (m_character != null)
            {
                var character = m_character;
                ListReader<MyPhysicalItemDefinition> definitions = MyDefinitionManager.Static.GetWeaponDefinitions();
                foreach (MyDefinitionBase definition in definitions)
                {
                    if (searchCondition != null && !searchCondition.MatchesCondition(definition))
                        continue;

                    var inventory = character.GetInventory() as MyInventory;
                    System.Diagnostics.Debug.Assert(inventory != null, "Null or unexpected inventory type returned");

                    bool enabled = definition.Id.SubtypeId == manipulationToolId || ((inventory != null && inventory.ContainItems(1, definition.Id)));

                    if (MyPerGameSettings.EnableResearch && MySession.Static.CreativeMode && !enabled && !MySessionComponentResearch.Static.CanUse(m_character, definition.Id))//remove unresearched items that you do not have (creative only)
                        continue;

                    enabled |= MySession.Static.CreativeMode;
                    if (enabled || MyPerGameSettings.Game == GameEnum.SE_GAME)
                        AddWeaponDefinition(m_gridBlocks, definition, enabled);
                }

                if (MyPerGameSettings.EnableAi && MyFakes.ENABLE_BARBARIANS)
                {
                    AddAiCommandDefinitions(searchCondition);
                    AddBotDefinitions(searchCondition);
                    AddAreaMarkerDefinitions(searchCondition);
                }

                if (!MySession.Static.SurvivalMode || MySession.Static.IsUserAdmin(MyPlayer.GetPlayerFromCharacter(character).Client.SteamUserId))
                    AddVoxelHands(searchCondition);

                if (MyFakes.ENABLE_PREFAB_THROWER)
                    AddPrefabThrowers(searchCondition);

                AddAnimations(false, searchCondition);
                AddGridCreators(searchCondition);
            }
            else
            {
                if (m_screenOwner != null)
                {
                    long OwnerEntityId = m_screenOwner.EntityId;
                    AddTerminalGroupsToGridBlocks(m_screenCubeGrid, OwnerEntityId, searchCondition);
                    if (m_shipController != null)
                    {
                        if (m_shipController.EnableShipControl)
                        {
                            AddTools(m_shipController, searchCondition);
                        }
                        AddAnimations(true, searchCondition);
                    }
                }
            }
        }

        bool IsValidItem(Vector2I pos, MyCubeBlockDefinitionGroup group)
        {
            bool canBeAdded = false;

            for (int i = 0; i < group.SizeCount; ++i)
            {
                var def = group[(MyCubeSize)i];
                if (MyFakes.ENABLE_NON_PUBLIC_BLOCKS || (def != null && def.Public && def.Enabled))
                {
                    canBeAdded = true;
                    break;
                }
            }
            return canBeAdded;
        }

        protected static void AddDefinition(MyGuiControlGrid grid, MyObjectBuilder_ToolbarItem data, MyDefinitionBase definition)
        {
            if (!definition.Public && !MyFakes.ENABLE_NON_PUBLIC_BLOCKS)
                return;
            if (!definition.AvailableInSurvival && MySession.Static.SurvivalMode)
                return;

            var gridItem = new MyGuiControlGrid.Item(
                icons: definition.Icons,
                toolTip: definition.DisplayNameText,
                userData: new GridItemUserData() { ItemData = data });

            grid.Add(gridItem);
        }

        protected virtual void AddDefinitionAtPosition(MyGuiControlGrid grid, MyDefinitionBase definition, Vector2I position, bool enabled = true, string subicon = null)
        {
            if (definition == null)
                return;
            if (MyPerGameSettings.EnableResearch && !MySessionComponentResearch.Static.CanUse(m_character, definition.Id))
                return;
            if (!definition.Public && !MyFakes.ENABLE_NON_PUBLIC_BLOCKS)
                return;
            if (!definition.AvailableInSurvival && MySession.Static.SurvivalMode)
                return;

            var gridItem = new MyGuiControlGrid.Item(
                icons: definition.Icons,
                toolTip: definition.DisplayNameText,
                subicon: subicon,
                userData: new GridItemUserData() { ItemData = MyToolbarItemFactory.ObjectBuilderFromDefinition(definition) },
                enabled: enabled);

            // Position.Y == -1  Any place on first page (G)
            // Position.Y == -2  Any place on second page (SHIFT+G)
            // Position.Y == -3  Any place on third page (CTRL+G)
            // Position.Y == -4  Any place on forth page (SHIFT+CTRL+G)

            int itemGenericPage = -position.Y - 1;

            if (position.Y < 0)
            {
                grid.Add(gridItem, itemGenericPage * 6);
                return;
            }

            if (grid.IsValidIndex(position.Y, position.X))
            {
                SetOrReplaceItemOnPosition(grid, gridItem, position);
            }
            else
            {
                if (grid.IsValidIndex(0, position.X))
                {
                    // add additional rows if there is lack of rows
                    grid.RecalculateRowsCount();
                    grid.AddRows(position.Y - grid.RowsCount + 1);

                    MyDebug.AssertDebug(grid.IsValidIndex(position.Y, position.X));

                    SetOrReplaceItemOnPosition(grid, gridItem, position);
                }
            }
        }

        void SetOrReplaceItemOnPosition(MyGuiControlGrid grid, MyGuiControlGrid.Item gridItem, Vector2I position)
        {
            var itemAtPosition = grid.TryGetItemAt(position.Y, position.X);
            grid.SetItemAt(position.Y, position.X, gridItem);
            if (itemAtPosition != null)
            {
                grid.Add(itemAtPosition);
            }
        }

        void AddCubeDefinitionsToBlocks(IMySearchCondition searchCondition)
        {
            var keys = MyDefinitionManager.Static.GetDefinitionPairNames();
            foreach (var key in keys)
            {
                //NOTE(AF): This is temporary for SE
                if (!MyFakes.ENABLE_MULTIBLOCKS_IN_SURVIVAL && MySession.Static.SurvivalMode && key.EndsWith("MultiBlock"))
                {
                    continue;
                }

                var group = MyDefinitionManager.Static.GetDefinitionGroup(key);
                var pos = MyDefinitionManager.Static.GetCubeBlockScreenPosition(key);

                if (MyPerGameSettings.EnableResearch && MySessionComponentResearch.Static.WhitelistMode && MySessionComponentResearch.Static.m_requiredResearch.Count < BLOCK_GROUPS_MIN_COUNT)
                    pos = new Vector2I(-1, -1);

                if (false == IsValidItem(pos, group))
                {
                    continue;
                }

                //search condition is null when we want to search all blocks,
                //if there is any search condition we need to reposition blocks, becaose not all blocks will be selected
                if (null != searchCondition)
                {
                    bool matchesCondition = false;
                    for (int i = 0; i < group.SizeCount && !matchesCondition; ++i)
                    {
                        var def = group[(MyCubeSize)i];
                        if (MyFakes.ENABLE_NON_PUBLIC_BLOCKS || (def != null && def.Public && def.Enabled))
                        {
                            if (null != def)//&& (!MyFakes.ENABLE_GUI_HIDDEN_CUBEBLOCKS || def.GuiVisible))
                            {
                                if (true == searchCondition.MatchesCondition(def) && (!MyFakes.ENABLE_GUI_HIDDEN_CUBEBLOCKS || def.GuiVisible || searchCondition is MySearchByStringCondition))
                                {
                                    matchesCondition = true;
                                    break;
                                }
                                if (def.BlockStages != null && def.BlockStages.Length > 0)
                                {
                                    for (int j = 0; j < def.BlockStages.Count() && !matchesCondition; j++)
                                    {
                                        var defblock = MyDefinitionManager.Static.GetCubeBlockDefinition(def.BlockStages[j]);
                                        if (defblock != null && true == searchCondition.MatchesCondition(defblock))
                                        {
                                            matchesCondition = true;
                                            break;
                                        }
                                    }
                                }

                            }
                        }
                    }

                    if (false == matchesCondition)
                    {
                        continue;
                    }

                    searchCondition.AddDefinitionGroup(group);
                }
                else
                {
                    if ((!MyFakes.ENABLE_GUI_HIDDEN_CUBEBLOCKS || group.Any.GuiVisible) && group.AnyPublic != null)
                        AddCubeDefinition(m_gridBlocks, group, pos);
                }
            }

            if (null != searchCondition)
            {
                HashSet<MyCubeBlockDefinitionGroup> sortedSelectedBlocks = searchCondition.GetSortedBlocks();
                int newItemPosition = 0;
                bool noPosition = (MyPerGameSettings.EnableResearch && MySessionComponentResearch.Static.WhitelistMode && MySessionComponentResearch.Static.m_requiredResearch.Count < BLOCK_GROUPS_MIN_COUNT);
                Vector2I defualtPos = new Vector2I(-1, -1);

                foreach (var blockGroup in sortedSelectedBlocks)
                {
                    Vector2I pos;
                    pos.X = newItemPosition % m_gridBlocks.ColumnsCount;
                    pos.Y = (int)(newItemPosition / (float)m_gridBlocks.ColumnsCount);
                    newItemPosition++;
                    AddCubeDefinition(m_gridBlocks, blockGroup, noPosition ? defualtPos : pos);
                    var anyDef = MyFakes.ENABLE_NON_PUBLIC_BLOCKS ? blockGroup.Any : blockGroup.AnyPublic;
                    if (anyDef.BlockStages != null && anyDef.BlockStages.Length > 0 && searchCondition is MySearchByCategoryCondition)
                    {
                        for (int i = 0; i < anyDef.BlockStages.Count(); i++)
                        {
                            pos.X = newItemPosition % m_gridBlocks.ColumnsCount;
                            pos.Y = (int)(newItemPosition / (float)m_gridBlocks.ColumnsCount);
                            newItemPosition++;
                            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(anyDef.BlockStages[i]);
                            if (def != null)
                                AddDefinitionAtPosition(m_gridBlocks, def, pos, MyCubeBuilder.Static.IsCubeSizeAvailable(def));
                        }
                    }
                }
            }
        }

        #endregion

        #region grid blocks population - character

        void AddCubeDefinition(MyGuiControlGrid grid, MyCubeBlockDefinitionGroup group, Vector2I position)
        {
            var anyDef = MyFakes.ENABLE_NON_PUBLIC_BLOCKS ? group.Any : group.AnyPublic;

            if (!MyFakes.ENABLE_MULTIBLOCK_CONSTRUCTION && MySession.Static.SurvivalMode && anyDef.MultiBlock != null)
                return;

            string subicon = null;
            if (anyDef.BlockStages != null && anyDef.BlockStages.Length > 0)
                subicon = MyToolbarItemCubeBlock.VariantsAvailableSubicon;

            bool enabled = true;

            //if (MyCubeBuilder.Static != null)
            //{
            //    enabled &= MyCubeBuilder.Static.IsCubeSizeAvailable(anyDef);
            //}

            enabled &= MyToolbarComponent.GlobalBuilding || MySession.Static.ControlledEntity is MyCharacter ||
                       (MySession.Static.ControlledEntity is MyCockpit && (MySession.Static.ControlledEntity as MyCockpit).BuildingMode);

            AddDefinitionAtPosition(grid, anyDef, position, enabled, subicon);
        }

        #endregion

        #region grid blocks population - ship

        void AddGridGun(MyShipController shipController, MyDefinitionId gunId, IMySearchCondition searchCondition)
        {
            var definition = MyDefinitionManager.Static.GetDefinition(gunId);
            if (null == searchCondition || true == searchCondition.MatchesCondition(definition))
            {
                AddWeaponDefinition(m_gridBlocks, definition);
            }
        }

        void AddTools(MyShipController shipController, IMySearchCondition searchCondition)
        {
            var gunsSets = shipController.CubeGrid.GridSystems.WeaponSystem.GetGunSets();
            foreach (KeyValuePair<MyDefinitionId, HashSet<IMyGunObject<MyDeviceBase>>> pair in gunsSets)
            {
                AddGridGun(shipController, pair.Key, searchCondition);
            }

            //var gunId = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));
            //AddGridGun(shipController, gunId, searchCondition);

        }

        private void AddAnimations(bool shipController, IMySearchCondition searchCondition)
        {
            ListReader<MyAnimationDefinition> definitions = MyDefinitionManager.Static.GetAnimationDefinitions();
            foreach (MyAnimationDefinition definition in definitions)
            {
                if (definition.Public && (!shipController || (shipController && definition.AllowInCockpit)))
                {
                    if (null != searchCondition && false == searchCondition.MatchesCondition(definition))
                    {
                        continue;
                    }
                    AddAnimationDefinition(m_gridBlocks, definition);
                }
            }
        }

        private void AddVoxelHands(IMySearchCondition searchCondition)
        {
            ListReader<MyVoxelHandDefinition> definitions = MyDefinitionManager.Static.GetVoxelHandDefinitions();
            foreach (MyVoxelHandDefinition definition in definitions)
            {
                if (definition.Public)
                {
                    if (searchCondition != null && !searchCondition.MatchesCondition(definition))
                        continue;

                    AddVoxelHandDefinition(m_gridBlocks, definition);
                }
            }
        }

        private void AddGridCreators(IMySearchCondition searchCondition)
        {
            var definitions = MyDefinitionManager.Static.GetGridCreatorDefinitions();
            foreach (MyGridCreateToolDefinition definition in definitions)
            {
                if (definition.Public)
                {
                    if (searchCondition != null && !searchCondition.MatchesCondition(definition))
                        continue;

                    AddGridCreatorDefinition(m_gridBlocks, definition);
                }
            }
        }

        private void AddPrefabThrowers(IMySearchCondition searchCondition)
        {
            ListReader<MyPrefabThrowerDefinition> definitions = MyDefinitionManager.Static.GetPrefabThrowerDefinitions();
            foreach (MyPrefabThrowerDefinition definition in definitions)
            {
                if ((definition.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS))
                {
                    if (searchCondition != null && !searchCondition.MatchesCondition(definition))
                        continue;
                    AddPrefabThrowerDefinition(m_gridBlocks, definition);
                }
            }
        }

        private void AddBotDefinitions(IMySearchCondition searchCondition)
        {
            ListReader<MyBotDefinition> definitions = MyDefinitionManager.Static.GetDefinitionsOfType<MyBotDefinition>();
            foreach (MyBotDefinition definition in definitions)
            {
                if ((definition.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS) && (definition.AvailableInSurvival || MySession.Static.CreativeMode))
                {
                    if (searchCondition != null && !searchCondition.MatchesCondition(definition))
                        continue;

                    AddBotDefinition(m_gridBlocks, definition);
                }
            }
        }

        private void AddAiCommandDefinitions(IMySearchCondition searchCondition)
        {
            ListReader<MyAiCommandDefinition> definitions = MyDefinitionManager.Static.GetDefinitionsOfType<MyAiCommandDefinition>();
            foreach (MyAiCommandDefinition definition in definitions)
            {
                if ((definition.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS) && (definition.AvailableInSurvival || MySession.Static.CreativeMode))
                {
                    if (searchCondition != null && !searchCondition.MatchesCondition(definition))
                        continue;

                    AddToolbarItemDefinition<MyObjectBuilder_ToolbarItemAiCommand>(m_gridBlocks, definition);
                }
            }
        }

        private void AddAreaMarkerDefinitions(IMySearchCondition searchCondition)
        {
            ListReader<MyAreaMarkerDefinition> definitions = MyDefinitionManager.Static.GetDefinitionsOfType<MyAreaMarkerDefinition>();
            foreach (MyAreaMarkerDefinition definition in definitions)
            {
                if ((definition.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS) && (definition.AvailableInSurvival || MySession.Static.CreativeMode))
                {
                    if (searchCondition != null && !searchCondition.MatchesCondition(definition))
                        continue;

                    AddToolbarItemDefinition<MyObjectBuilder_ToolbarItemAreaMarker>(m_gridBlocks, definition);
                }
            }
        }

        void AddWeaponDefinition(MyGuiControlGrid grid, MyDefinitionBase definition, bool enabled = true)
        {
            if ((!definition.Public && !MyFakes.ENABLE_NON_PUBLIC_BLOCKS) || (!definition.AvailableInSurvival && MySession.Static.SurvivalMode))
                return;

            {
                MyObjectBuilder_ToolbarItemWeapon weaponData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemWeapon>();
                weaponData.DefinitionId = definition.Id;

                var gridItem = new MyGuiControlGrid.Item(
                    icons: definition.Icons,
                    toolTip: definition.DisplayNameText,
                    userData: new GridItemUserData() { ItemData = weaponData },
                    enabled: enabled);

                grid.Add(gridItem);
            }
        }

        void AddAnimationDefinition(MyGuiControlGrid grid, MyDefinitionBase definition)
        {
            MyObjectBuilder_ToolbarItemAnimation animationData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemAnimation>();
            animationData.DefinitionId = definition.Id;
            AddDefinition(grid, animationData, definition);
        }

        void AddVoxelHandDefinition(MyGuiControlGrid grid, MyDefinitionBase definition)
        {
            MyObjectBuilder_ToolbarItemVoxelHand handData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemVoxelHand>();
            handData.DefinitionId = definition.Id;
            AddDefinition(grid, handData, definition);
        }

        void AddGridCreatorDefinition(MyGuiControlGrid grid, MyDefinitionBase definition)
        {
            MyObjectBuilder_ToolbarItemCreateGrid creator = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemCreateGrid>();
            creator.DefinitionId = definition.Id;
            AddDefinition(grid, creator, definition);
        }

        private void AddPrefabThrowerDefinition(MyGuiControlGrid grid, MyPrefabThrowerDefinition definition)
        {
            MyObjectBuilder_ToolbarItemPrefabThrower throwerData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemPrefabThrower>();
            throwerData.DefinitionId = definition.Id;
            AddDefinition(grid, throwerData, definition);
        }

        private void AddBotDefinition(MyGuiControlGrid grid, MyBotDefinition definition)
        {
            MyObjectBuilder_ToolbarItemBot agentData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemBot>();
            agentData.DefinitionId = definition.Id;
            AddDefinition(grid, agentData, definition);
        }

        private void AddToolbarItemDefinition<T>(MyGuiControlGrid grid, MyDefinitionBase definition) where T : MyObjectBuilder_ToolbarItemDefinition, new()
        {
            T objectBuilder = MyObjectBuilderSerializer.CreateNewObject<T>();
            objectBuilder.DefinitionId = definition.Id;
            AddDefinition(grid, objectBuilder, definition);
        }

        private void AddShipBlocksDefinitions(MyCubeGrid grid, bool isShip, IMySearchCondition searchCondition)
        {
            bool isSeat = isShip && m_shipController != null && !m_shipController.EnableShipControl;
            if (!isSeat)
            {
                if (MyFakes.ENABLE_SHIP_BLOCKS_TOOLBAR)
                {
                    AddTerminalSingleBlocksToGridBlocks(grid, searchCondition);
                }
            }
        }

        void AddTerminalGroupsToGridBlocks(MyCubeGrid grid, long Owner, IMySearchCondition searchCondition)
        {
            if (grid == null || grid.GridSystems.TerminalSystem == null || grid.GridSystems.TerminalSystem.BlockGroups == null)
                return;

            int v = 0;
            int nCols = m_gridBlocks.ColumnsCount;

            //Adds all block groups
            var groupArray = grid.GridSystems.TerminalSystem.BlockGroups.ToArray();
            Array.Sort(groupArray, MyTerminalComparer.Static);
            foreach (MyBlockGroup group in groupArray)
            {
                if (null != searchCondition && false == searchCondition.MatchesCondition(group.Name.ToString()))
                {
                    continue;
                }

                MyObjectBuilder_ToolbarItemTerminalGroup groupData = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(group);

                //group is functional iff at least one block is functional
                bool isGroupFunctional = false;
                foreach (var block in group.Blocks)
                {
                    if (block.IsFunctional)
                    {
                        isGroupFunctional = true;
                        break;
                    }
                }

                groupData.BlockEntityId = Owner;
                m_gridBlocks.Add(new MyGuiControlGrid.Item(
                    icons: MyToolbarItemFactory.GetIconForTerminalGroup(group),
                    toolTip: group.Name.ToString(),
                    userData: new GridItemUserData() { ItemData = groupData },
                    enabled: isGroupFunctional)
                );
                v++;
            }

            //Add blank spaces to separate groups from items (if there is any group): All blanks to fill a line + 1 empty line
            if (v > 0)
            {
                int w = v;
                int remainder = w % nCols;
                if (remainder == 0) remainder = nCols;
                for (int i = 0; i < 2 * nCols - remainder; i++)
                    if (v < m_gridBlocks.GetItemsCount())
                        m_gridBlocks.SetItemAt(v++, new MyGuiControlGrid.Item(icon: "", toolTip: String.Empty, userData: new GridItemUserData() { ItemData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemEmpty>() }, enabled: false));
                    else
                        m_gridBlocks.Add(new MyGuiControlGrid.Item(icon: "", toolTip: String.Empty, userData: new GridItemUserData() { ItemData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemEmpty>() }, enabled: false));
            }
        }

        void AddTerminalSingleBlocksToGridBlocks(MyCubeGrid grid, IMySearchCondition searchCondition)
        {
            if (grid == null || grid.GridSystems.TerminalSystem == null)
                return;

            var blockArray = grid.GridSystems.TerminalSystem.Blocks.ToArray();
            Array.Sort(blockArray, MyTerminalComparer.Static);

            //Adds all single non-grouped blocks
            foreach (var block in blockArray)
            {
                if (block == null)
                    continue;

                //only add it to GUI if it has actions
                if (MyTerminalControlFactory.GetActions(block.GetType()).Count <= 0)
                {
                    continue;
                }
                if (searchCondition != null && searchCondition.MatchesCondition(block.BlockDefinition) == false && searchCondition.MatchesCondition(block.CustomName.ToString()) == false)
                {
                    continue;
                }
                if (block.ShowInToolbarConfig == false || (!block.BlockDefinition.AvailableInSurvival && MySession.Static.SurvivalMode))
                {
                    continue;
                }

                MyObjectBuilder_ToolbarItemTerminalBlock blockData = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                m_gridBlocks.Add(new MyGuiControlGrid.Item(
                    icons: block.BlockDefinition.Icons,
                    subicon: MyTerminalActionIcons.NONE,
                    toolTip: block.CustomName.ToString(),
                    userData: new GridItemUserData() { ItemData = blockData },
                    enabled: block.IsFunctional)
                );
            }
        }

        #endregion

        #region event handlers

        void categories_ItemClicked(MyGuiControlListbox sender)
        {
            m_voxelHandConfig.Visible = false;

            m_gridBlocks.SetItemsToDefault();
            if (0 == sender.SelectedItems.Count)
            {
                return;
            }

            m_allSelectedCategories.Clear();
            m_searchInBlockCategories.Clear();

            bool isAllSelected = false;
            foreach (var selectedCategory in sender.SelectedItems)
            {
                MyGuiBlockCategoryDefinition category = (MyGuiBlockCategoryDefinition)selectedCategory.UserData;

                if (null == category)
                {
                    //ALL is special Category without any condition
                    isAllSelected = true;
                    continue;
                }

                if (category.SearchBlocks)
                {
                    m_searchInBlockCategories.Add(category);
                }
                m_allSelectedCategories.Add(category);
            }

            //GROUPS needs to be added first into grid 
            m_categorySearchCondition.SelectedCategories = m_allSelectedCategories;
            AddToolsAndAnimations(m_categorySearchCondition);

            m_categorySearchCondition.SelectedCategories = m_searchInBlockCategories;
            UpdateGridBlocksBySearchCondition(isAllSelected ? m_nameSearchCondition.IsValid ? (IMySearchCondition)m_nameSearchCondition : null : (IMySearchCondition)m_categorySearchCondition);
        }

        void grid_ItemClicked(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            m_voxelHandConfig.Visible = false;

            if (eventArgs.Button == MySharedButtonsEnum.Primary)
            {
                var gridItem = sender.TryGetItemAt(eventArgs.RowIndex, eventArgs.ColumnIndex);
                if (gridItem == null)
                    return;
                if (!gridItem.Enabled)
                    return;

                var data = (GridItemUserData)gridItem.UserData;
                var item = MyToolbarItemFactory.CreateToolbarItem(data.ItemData);

                var vhitem = item as MyToolbarItemVoxelHand;
                if (vhitem != null)
                {
                    m_voxelHandConfig.Visible = true;
                    m_voxelHandConfig.Item = vhitem;
                    m_voxelHandConfig.UpdateControls();
                }
            }
            else if (eventArgs.Button == MySharedButtonsEnum.Secondary)
            {
                var gridItem = sender.TryGetItemAt(eventArgs.RowIndex, eventArgs.ColumnIndex);
                if (gridItem == null)
                {
                    return;
                }
                if (!gridItem.Enabled)
                    return;

                var data = (GridItemUserData)gridItem.UserData;
                var item = MyToolbarItemFactory.CreateToolbarItem(data.ItemData);

                if (item is MyToolbarItemActions)
                {
                    m_contextBlockX = eventArgs.RowIndex;
                    m_contextBlockY = eventArgs.ColumnIndex;


                    //if the item has no actions available, just send it to toolbar right away
                    if (!UpdateContextMenu(ref m_contextMenu, item as MyToolbarItemActions, data))
                    {
                        grid_ItemDoubleClicked(sender, eventArgs);
                    }
                }
                else
                {
                    grid_ItemDoubleClicked(sender, eventArgs);
                }
            }
            else if (MyInput.Static.IsAnyShiftKeyPressed())
                grid_ItemShiftClicked(sender, eventArgs);

        }
        void grid_ItemShiftClicked(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            if (eventArgs.Button == MySharedButtonsEnum.Primary)
            {
                var gridItem = sender.TryGetItemAt(eventArgs.RowIndex, eventArgs.ColumnIndex);
                if (gridItem == null)
                    return;
                if (!gridItem.Enabled)
                    return;

                var data = (GridItemUserData)gridItem.UserData;
                var item = MyToolbarItemFactory.CreateToolbarItem(data.ItemData);

                //if it is an item that does not activate when dragged to toolbar(WantsToBeActivated = false), allow shift+click
                if (!item.WantsToBeActivated)
                    item.Activate();
            }
        }

        void grid_ItemDoubleClicked(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            // CH:TODO: Unify this code with drag-n-drop

            ProfilerShort.Begin("MyGuiScreenCubeBuilder.grid_ItemDoubleClicked");
            try
            {
                var gridItem = sender.TryGetItemAt(eventArgs.RowIndex, eventArgs.ColumnIndex);
                if (gridItem == null)
                    return;
                if (!gridItem.Enabled)
                    return;

                var data = (GridItemUserData)gridItem.UserData;
                if (data.ItemData is MyObjectBuilder_ToolbarItemEmpty)
                    return;

                AddGridItemToToolbar(data.ItemData);

            }
            finally
            {
                ProfilerShort.End();
            }

        }

        void grid_PanelScrolled(MyGuiControlScrollablePanel panel)
        {
            if (m_contextMenu != null)
                m_contextMenu.Deactivate();
        }

        private void grid_OnDrag(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            //   if (!m_isTransferingInProgress)
            {
                StartDragging(MyDropHandleType.MouseRelease, sender, ref eventArgs);
            }
        }

        private void dragAndDrop_OnDrop(object sender, MyDragAndDropEventArgs eventArgs)
        {
            if (eventArgs.DropTo != null && !m_toolbarControl.IsToolbarGrid(eventArgs.DragFrom.Grid) && m_toolbarControl.IsToolbarGrid(eventArgs.DropTo.Grid))
            {
                GridItemUserData data = (GridItemUserData)eventArgs.Item.UserData;
                if (data.ItemData is MyObjectBuilder_ToolbarItemEmpty)
                {
                    return;
                }

                if (eventArgs.DropTo.ItemIndex >= 0 && eventArgs.DropTo.ItemIndex < 9)
                {
                    var item = MyToolbarItemFactory.CreateToolbarItem(data.ItemData);

                    //If item is multifunctional, user will have to decide which action will it make
                    if (item is MyToolbarItemActions)
                    {
                        if (UpdateContextMenu(ref m_onDropContextMenu, item as MyToolbarItemActions, data))
                        {
                            m_onDropContextMenuToolbarIndex = eventArgs.DropTo.ItemIndex;
                            m_onDropContextMenu.Enabled = true;
                            m_onDropContextMenuItem = item;
                        }
                        else
                        {
                            DropGridItemToToolbar(item, eventArgs.DropTo.ItemIndex);
                        }
                    }
                    else
                    {
                        DropGridItemToToolbar(item, eventArgs.DropTo.ItemIndex);
                        if (item.WantsToBeActivated)
                        {
                            MyToolbarComponent.CurrentToolbar.ActivateItemAtSlot(eventArgs.DropTo.ItemIndex, playActivationSound: false);
                        }
                    }
                }
            }

            m_toolbarControl.HandleDragAndDrop(sender, eventArgs);
        }

        void searchItemTexbox_TextChanged(MyGuiControlTextbox sender)
        {
            if (m_framesBeforeSearchEnabled > 0)
                return;

            m_gridBlocks.SetItemsToDefault();
            string searchName = sender.Text;
            if (string.IsNullOrWhiteSpace(searchName) || string.IsNullOrEmpty(searchName))
            {
                if (null != m_character || (m_shipController != null && m_shipController.BuildingMode))
                {
                    AddCubeDefinitionsToBlocks(null);
                }
                else
                {
                    AddShipBlocksDefinitions(m_screenCubeGrid, true, null);
                }
                m_nameSearchCondition.Clean();
                return;
            }

            m_nameSearchCondition.SearchName = searchName;
            bool isSeat = m_shipController != null && !m_shipController.EnableShipControl;
            if (!isSeat)
            {
                AddToolsAndAnimations(m_nameSearchCondition);
                UpdateGridBlocksBySearchCondition(m_nameSearchCondition);
            }
            else
                AddAnimations(true, m_nameSearchCondition);
        }

        /// <summary>
        /// Updates Grid control with current category settings. 
        /// </summary>
        protected void UpdateGridControl()
        {
            categories_ItemClicked(m_categoriesListbox);
        }

        void contextMenu_ItemClicked(MyGuiControlContextMenu sender, MyGuiControlContextMenu.EventArgs args)
        {
            if (m_contextBlockX < 0 || m_contextBlockX >= m_gridBlocks.RowsCount || m_contextBlockY < 0 || m_contextBlockY >= m_gridBlocks.ColumnsCount)
                return;

            //Adds the clicked item in the context menu to the toolbar
            MyGuiControlGrid.Item item = m_gridBlocks.TryGetItemAt(m_contextBlockX, m_contextBlockY);
            if (item == null)
                return;

            GridItemUserData data = item.UserData as GridItemUserData;

            //Hard coded, will do it in a prettier way!
            MyObjectBuilder_ToolbarItemTerminal newData = (MyObjectBuilder_ToolbarItemTerminal)data.ItemData;
            newData._Action = (string)args.UserData;
            AddGridItemToToolbar(newData);
            newData._Action = null;
        }

        void onDropContextMenu_ItemClicked(MyGuiControlContextMenu sender, MyGuiControlContextMenu.EventArgs args)
        {
            int idx = m_onDropContextMenuToolbarIndex;
            if (idx >= 0 && idx < MyToolbarComponent.CurrentToolbar.SlotCount)
            {
                var item = m_onDropContextMenuItem;
                if (item is MyToolbarItemActions)
                {
                    (item as MyToolbarItemActions).ActionId = (string)args.UserData;
                    DropGridItemToToolbar(item, idx);
                }
            }
        }


        void AddGridItemToToolbar(MyObjectBuilder_ToolbarItem data)
        {
            var currentToolbar = MyToolbarComponent.CurrentToolbar;
            var slotCount = currentToolbar.SlotCount;
            var newItem = MyToolbarItemFactory.CreateToolbarItem(data);
            if (newItem == null)
                return;

            // Let the action retrieve its parameters if necessary. This may possibly display dialogs.
            RequestItemParameters(newItem, success =>
            {
                bool itemSet = false;
                int itemSlot = 0;

                // First, search whether the item is not already present in the toolbar and if it is, select it
                for (int i = 0; i < slotCount; i++)
                {
                    MyToolbarItem item = currentToolbar.GetSlotItem(i);
                    if (item != null && item.Equals(newItem))
                    {
                        if (item.WantsToBeActivated)
                            currentToolbar.ActivateItemAtSlot(i, playActivationSound: false);
                        itemSlot = i;
                        itemSet = true;
                        break;
                    }
                }

                // Then, if it was not found, put it in the first empty slot
                for (int i = 0; i < slotCount; i++)
                {
                    bool putItemHere = !itemSet && (currentToolbar.GetSlotItem(i) == null);

                    if (putItemHere)
                    {
                        currentToolbar.SetItemAtSlot(i, newItem);
                        if (newItem.WantsToBeActivated)
                            currentToolbar.ActivateItemAtSlot(i, playActivationSound: false);

                        itemSlot = i;
                        itemSet = true;
                    }
                    else if (i != itemSlot && currentToolbar.GetSlotItem(i) != null && currentToolbar.GetSlotItem(i).Equals(newItem))
                    {
                        currentToolbar.SetItemAtSlot(i, null);
                    }
                }

                // Then, if still not found, put it in the selected slot or the first slot
                if (!itemSet)
                {
                    int i = currentToolbar.SelectedSlot.HasValue ? currentToolbar.SelectedSlot.Value : 0;
                    currentToolbar.SetItemAtSlot(i, newItem);

                    if (newItem.WantsToBeActivated)
                        currentToolbar.ActivateItemAtSlot(i, playActivationSound: false);
                    itemSet = true;
                }
            });
        }

        public static void RequestItemParameters(MyToolbarItem item, Action<bool> callback)
        {
            var itemTerminalBlock = item as MyToolbarItemTerminalBlock;
            if (itemTerminalBlock != null)
            {
                var action = itemTerminalBlock.GetActionOrNull(itemTerminalBlock.ActionId);
                if (action != null && action.GetParameterDefinitions().Count > 0)
                {
                    action.RequestParameterCollection(itemTerminalBlock.Parameters, callback);
                    return;
                }
            }
            callback(true);
        }

        public static void DropGridItemToToolbar(MyToolbarItem item, int slot)
        {
            RequestItemParameters(item, success =>
            {
                if (!success)
                    return;
                var currentToolbar = MyToolbarComponent.CurrentToolbar;
                for (int i = 0; i < currentToolbar.SlotCount; i++)
                {
                    if (currentToolbar.GetSlotItem(i) != null && currentToolbar.GetSlotItem(i).Equals(item))
                    {
                        currentToolbar.SetItemAtSlot(i, null);
                    }
                }
                MyGuiAudio.PlaySound(MyGuiSounds.HudItem);
                MyToolbarComponent.CurrentToolbar.SetItemAtSlot(slot, item);
            });
        }

        public static void ReinitializeBlockScrollbarPosition()
        {
            m_savedVPosition = 0.0f;
        }

        private bool CanDropItem(MyPhysicalInventoryItem item, MyGuiControlGrid dropFrom, MyGuiControlGrid dropTo)
        {
            return (dropTo != dropFrom);
        }

        private void StartDragging(MyDropHandleType dropHandlingType, MyGuiControlGrid grid, ref MyGuiControlGrid.EventArgs args)
        {
            MyDragAndDropInfo dragAndDropInfo = new MyDragAndDropInfo();

            dragAndDropInfo.Grid = grid;
            dragAndDropInfo.ItemIndex = args.ItemIndex;

            var draggingItem = grid.GetItemAt(args.ItemIndex);

            if (!draggingItem.Enabled)
                return;

            m_dragAndDrop.StartDragging(dropHandlingType, args.Button, draggingItem, dragAndDropInfo);
            grid.HideToolTip();
        }

        private void StopDragging()
        {
            m_dragAndDrop.Stop();
        }
        bool UpdateContextMenu(ref MyGuiControlContextMenu currentContextMenu, MyToolbarItemActions item, GridItemUserData data)
        {
            var actionList = item.PossibleActions(m_toolbarControl.ShownToolbar.ToolbarType);
            if (actionList.Count > 0)
            {
                //will open context menu once right click is released
                currentContextMenu.Enabled = true;

                currentContextMenu.CreateNewContextMenu();
                foreach (ITerminalAction action in actionList)
                {
                    currentContextMenu.AddItem(action.Name, icon: action.Icon, userData: action.Id);
                }
                return true;
            }
            return false;
        }

        public override bool Update(bool hasFocus)
        {
            if (m_framesBeforeSearchEnabled > 0)
                m_framesBeforeSearchEnabled--;
            if (m_framesBeforeSearchEnabled == 0)
            {
                m_searchItemTextBox.Enabled = true;
                m_searchItemTextBox.CanHaveFocus = true;
                //FocusedControl = m_searchItemTextBox;     For the sake of consistency
                FocusedControl = null;
                m_framesBeforeSearchEnabled--;
            }
            return base.Update(hasFocus);
        }

        #endregion
    }
}