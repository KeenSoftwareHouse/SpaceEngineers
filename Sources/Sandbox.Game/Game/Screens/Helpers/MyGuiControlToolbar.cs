using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlToolbar : MyGuiControlBase
    {
        private static StringBuilder m_textCache = new StringBuilder();

        private MyGuiControlGrid m_toolbarItemsGrid;
        private MyGuiControlLabel m_selectedItemLabel;
        private MyGuiControlPanel m_colorVariantPanel;
        private MyGuiControlContextMenu m_contextMenu;
        private List<MyGuiControlLabel> m_pageLabelList = new List<MyGuiControlLabel>();
        private MyToolbar m_shownToolbar;
        public MyToolbar ShownToolbar
        {
            get 
            { 
                return m_shownToolbar; 
            }
        }

        private int m_contextMenuItemIndex = -1;

        public bool DrawNumbers { get { return MyToolbarComponent.CurrentToolbar.DrawNumbers; } }
        public Func<int, Sandbox.Graphics.GUI.MyGuiControlGrid.ColoredIcon> GetSymbol { get { return MyToolbarComponent.CurrentToolbar.GetSymbol; } }

        public MyGuiControlToolbar() :
            base(allowFocusingElements: true)
        {
            MyToolbarComponent.CurrentToolbarChanged += ToolbarComponent_CurrentToolbarChanged;

            RecreateControls(true);
            ShowToolbar(MyToolbarComponent.CurrentToolbar);
        }

        #region Overrides

        public override void OnRemoving()
        {
            MyToolbarComponent.CurrentToolbarChanged -= ToolbarComponent_CurrentToolbarChanged;
            if (m_shownToolbar != null)
            {
                m_shownToolbar.ItemChanged -= Toolbar_ItemChanged;
                m_shownToolbar.ItemUpdated -= Toolbar_ItemUpdated;
                m_shownToolbar.SelectedSlotChanged -= Toolbar_SelectedSlotChanged;
                m_shownToolbar.SlotActivated -= Toolbar_SlotActivated;
                m_shownToolbar.ItemEnabledChanged -= Toolbar_ItemEnabledChanged;
                m_shownToolbar.CurrentPageChanged -= Toolbar_CurrentPageChanged;
                m_shownToolbar = null;
            }

            base.OnRemoving();
        }

        public override MyGuiControlBase HandleInput()
        {

            var captureControl = base.HandleInput();
            if (captureControl == null)
                captureControl = base.HandleInputElements();

            if (MyInput.Static.IsMouseReleased(MyMouseButtonsEnum.Right) && m_contextMenu.Enabled)
            {
                m_contextMenu.Enabled = false;
                m_contextMenu.Activate();
            }
            return captureControl;
        }


        public override void Update()
        {
            base.Update();
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            Color c = Color.White;
            if (MyToolbar.ColorMaskHSV != null)
                c = (new Vector3(MyToolbar.ColorMaskHSV.X, MathHelper.Clamp(MyToolbar.ColorMaskHSV.Y + 0.8f, 0f, 1f), MathHelper.Clamp(MyToolbar.ColorMaskHSV.Z + 0.55f, 0f, 1f))).HSVtoColor();
            m_colorVariantPanel.ColorMask = c.ToVector4();//MyCubeBuilder.VariantColor;
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }

        protected override void OnSizeChanged()
        {
            RefreshInternals();
            base.OnSizeChanged();
        }

        private void RefreshInternals()
        {
            RepositionControls();
        }

        private void RepositionControls()
        {
            var position = Size * 0.5f;
            m_toolbarItemsGrid.Position = position;

            position.Y -= m_toolbarItemsGrid.Size.Y;
            m_selectedItemLabel.Position = position;

            position.Y -= m_selectedItemLabel.Size.Y;
            m_colorVariantPanel.Position = position;

            position = Size * 0.5f;
            position.X -= m_toolbarItemsGrid.Size.X;
            position.Y -= m_toolbarItemsGrid.Size.Y;
            foreach (var pageLabel in m_pageLabelList)
            {
                pageLabel.Position = position + new Vector2(pageLabel.Size.X * 0.5f, -pageLabel.Size.Y * 0.5f);
                position.X += pageLabel.Size.X + 0.001f;
            }

            // Move the context menu to the top
            Elements.Remove(m_contextMenu);
            Elements.Add(m_contextMenu);
        }

        private void RecreateControls(bool contructor)
        {
            m_toolbarItemsGrid = new MyGuiControlGrid()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                VisualStyle = MyGuiControlGridStyleEnum.Toolbar,
                ColumnsCount = MyToolbarComponent.CurrentToolbar.SlotCount + 1,
                RowsCount = 1
            };
            m_toolbarItemsGrid.ItemDoubleClicked += grid_ItemDoubleClicked;
            m_toolbarItemsGrid.ItemClickedWithoutDoubleClick += grid_ItemClicked;

            m_selectedItemLabel = new MyGuiControlLabel();
            m_colorVariantPanel = new MyGuiControlPanel(size: new Vector2(0.1f, 0.025f));
            m_colorVariantPanel.BackgroundTexture = MyGuiConstants.TEXTURE_GUI_BLANK;

            m_contextMenu = new MyGuiControlContextMenu();
            m_contextMenu.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            m_contextMenu.Deactivate();
            m_contextMenu.ItemClicked += contextMenu_ItemClicked;

            Elements.Add(m_colorVariantPanel);
            Elements.Add(m_selectedItemLabel);
            Elements.Add(m_toolbarItemsGrid);
            Elements.Add(m_contextMenu);

            m_colorVariantPanel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
            m_selectedItemLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
            m_toolbarItemsGrid.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
            m_contextMenu.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;

            RefreshInternals();
        }

        #endregion

        public bool IsToolbarGrid(MyGuiControlGrid grid)
        {
            return m_toolbarItemsGrid == grid;
        }

        private void ShowToolbar(MyToolbar toolbar)
        {
            if (m_shownToolbar != null)
            {
                m_shownToolbar.ItemChanged -= Toolbar_ItemChanged;
                m_shownToolbar.ItemUpdated -= Toolbar_ItemUpdated;
                m_shownToolbar.SelectedSlotChanged -= Toolbar_SelectedSlotChanged;
                m_shownToolbar.SlotActivated -= Toolbar_SlotActivated;
                m_shownToolbar.ItemEnabledChanged -= Toolbar_ItemEnabledChanged;
                m_shownToolbar.CurrentPageChanged -= Toolbar_CurrentPageChanged;

                foreach (var label in m_pageLabelList)
                {
                    Elements.Remove(label);
                }
                m_pageLabelList.Clear();
            }

            m_shownToolbar = toolbar;

            if (m_shownToolbar == null) // Toolbar can be null in the passenger seat
            {
                m_toolbarItemsGrid.Enabled = false;
                m_toolbarItemsGrid.Visible = false;
            }
            else
            {
                var slotCount = toolbar.SlotCount;
                m_toolbarItemsGrid.ColumnsCount = slotCount + (toolbar.ShowHolsterSlot ? 1 : 0);
                for (int i = 0; i < slotCount; ++i)
                    SetGridItemAt(i, toolbar.GetSlotItem(i));
                m_selectedItemLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
                m_colorVariantPanel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
                m_colorVariantPanel.Visible = MyFakes.ENABLE_BLOCK_COLORING; // character != null;
                
                if (toolbar.ShowHolsterSlot)
                    SetGridItemAt(slotCount, new MyToolbarItemEmpty(), @"Textures\GUI\Icons\HideWeapon.dds", null, MyTexts.GetString(MySpaceTexts.HideWeapon));

                if(toolbar.PageCount > 1)
                    for (int i = 0; i < toolbar.PageCount; ++i)
                    {
                        m_textCache.Clear();
                        m_textCache.AppendInt32(i + 1);

                        MyGuiControlLabel pageLabel = new MyGuiControlLabel(text: MyToolbarComponent.GetSlotControlText(i).ToString() ?? m_textCache.ToString());
                        pageLabel.BackgroundTexture = MyGuiConstants.TEXTURE_TOOLBAR_TAB;
                        pageLabel.TextScale = 0.7f;
                        pageLabel.Size = m_toolbarItemsGrid.ItemSize * new Vector2(0.5f, 0.35f);
                        pageLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

                        m_pageLabelList.Add(pageLabel);
                        Elements.Add(pageLabel);
                    }

                RepositionControls();

                HighlightCurrentPageLabel();
                RefreshSelectedItem(toolbar);

                m_shownToolbar.ItemChanged += Toolbar_ItemChanged;
                m_shownToolbar.ItemUpdated += Toolbar_ItemUpdated;
                m_shownToolbar.SelectedSlotChanged += Toolbar_SelectedSlotChanged;
                m_shownToolbar.SlotActivated += Toolbar_SlotActivated;
                m_shownToolbar.ItemEnabledChanged += Toolbar_ItemEnabledChanged;
                m_shownToolbar.CurrentPageChanged += Toolbar_CurrentPageChanged;

                MaxSize = MinSize = new Vector2(m_toolbarItemsGrid.Size.X, m_toolbarItemsGrid.Size.Y + m_selectedItemLabel.Size.Y + m_colorVariantPanel.Size.Y);

                m_toolbarItemsGrid.Enabled = true;
                m_toolbarItemsGrid.Visible = true;
            }
        }

        private void RefreshSelectedItem(MyToolbar toolbar)
        {
            m_toolbarItemsGrid.SelectedIndex = toolbar.SelectedSlot;
            var item = toolbar.SelectedItem;
            if (item != null)
            {
                m_selectedItemLabel.Text = item.DisplayName.ToString();
                Debug.Assert(MyCubeBuilder.Static != null, "Cube builder should be loaded here");

                m_colorVariantPanel.Visible = (item is MyToolbarItemCubeBlock) && MyFakes.ENABLE_BLOCK_COLORING;
            }
            else
            {
                m_colorVariantPanel.Visible = false;
                m_selectedItemLabel.Text = String.Empty;
            }
        }

        private void HighlightCurrentPageLabel()
        {
            int page = m_shownToolbar.CurrentPage;

            for (int i = 0; i < m_pageLabelList.Count(); ++i)
            {
                if (i != page && m_pageLabelList[i].BackgroundTexture == MyGuiConstants.TEXTURE_TOOLBAR_TAB_HIGHLIGHT)
                {
                    m_pageLabelList[i].BackgroundTexture = MyGuiConstants.TEXTURE_TOOLBAR_TAB;
                }
                else if (i == page && m_pageLabelList[i].BackgroundTexture == MyGuiConstants.TEXTURE_TOOLBAR_TAB)
                {
                    m_pageLabelList[i].BackgroundTexture = MyGuiConstants.TEXTURE_TOOLBAR_TAB_HIGHLIGHT;
                }
            }
        }

        private void SetGridItemAt(int slot, MyToolbarItem item)
        {
            if (item != null)
                SetGridItemAt(slot, item, item.Icon, item.SubIcon, item.DisplayName.ToString(), GetSymbol(slot));
            else
                SetGridItemAt(slot, null, null, null, null, GetSymbol(slot));
        }

        private void SetGridItemAt(int slot, MyToolbarItem item, string icon, string subicon, String tooltip, Sandbox.Graphics.GUI.MyGuiControlGrid.ColoredIcon? symbol = null)
        {
            var gridItem = m_toolbarItemsGrid.GetItemAt(slot);
            if (gridItem == null)
            {
                gridItem = new MyGuiControlGrid.Item(
                    icon: icon,
                    subicon: subicon,
                    toolTip: tooltip,
                    userData: item);
                if(DrawNumbers)
                    gridItem.AddText(MyToolbarComponent.GetSlotControlText(slot));
                //Ammo amount in toolbar
                if (item != null)
                    gridItem.AddText(item.IconText, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                else
                    gridItem.ClearText(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                gridItem.Enabled = (item != null) ? item.Enabled : true;
                if(symbol.HasValue)
                    gridItem.AddIcon(symbol.Value, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                m_toolbarItemsGrid.SetItemAt(slot, gridItem);
            }
            else
            {
                gridItem.UserData = item;
                gridItem.Icon = icon;
                gridItem.SubIcon = subicon;
                if (gridItem.ToolTip == null)
                    gridItem.ToolTip = new MyToolTips();
                gridItem.ToolTip.ToolTips.Clear();
                gridItem.ToolTip.AddToolTip(tooltip);
                //Ammo amount in toolbar
                if (item != null)
                    gridItem.AddText(item.IconText, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                else
                    gridItem.ClearText(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
                gridItem.Enabled = (item != null) ? item.Enabled : true;
                if (symbol.HasValue)
                    gridItem.AddIcon(symbol.Value, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }
        }

        private void RemoveToolbarItem(int slot)
        {
            if (slot < MyToolbarComponent.CurrentToolbar.SlotCount)
            {
                var toolbar = MyToolbarComponent.CurrentToolbar;
                toolbar.SetItemAtSlot(slot, null);
            }
        }

        #region Event handlers

        private void ToolbarComponent_CurrentToolbarChanged()
        {
            ShowToolbar(MyToolbarComponent.CurrentToolbar);
        }

        private void Toolbar_SelectedSlotChanged(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            Debug.Assert(toolbar == m_shownToolbar);
            RefreshSelectedItem(toolbar);
        }

        private void Toolbar_SlotActivated(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            Debug.Assert(toolbar == m_shownToolbar);
            m_toolbarItemsGrid.blinkSlot(args.SlotNumber);
        }

        private void Toolbar_ItemChanged(MyToolbar toolbar, MyToolbar.IndexArgs args)
        {
            UpdateItemAtIndex(toolbar, args.ItemIndex);
        }

        private void Toolbar_ItemUpdated(MyToolbar toolbar, MyToolbar.IndexArgs args, MyToolbarItem.ChangeInfo changes)
        {
            // Quicker method if only icon changed
            if (changes == MyToolbarItem.ChangeInfo.Icon)
            {
                UpdateItemIcon(toolbar, args);
            }
            else
            {
                UpdateItemAtIndex(toolbar, args.ItemIndex);
            }
        }

        private void UpdateItemAtIndex(MyToolbar toolbar, int index)
        {
            Debug.Assert(toolbar == m_shownToolbar);
            int slot = toolbar.IndexToSlot(index);
            if (!toolbar.IsValidIndex(index) || !toolbar.IsValidSlot(slot)) return;

            SetGridItemAt(slot, toolbar[index]);
            if (toolbar.SelectedSlot == slot)
                RefreshSelectedItem(toolbar);
        }

        private void Toolbar_ItemEnabledChanged(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (args.SlotNumber.HasValue)
            {
                var idx = args.SlotNumber.Value;
                m_toolbarItemsGrid.GetItemAt(idx).Enabled = toolbar.IsEnabled(idx);
            }
            else
            {
                for (int i = 0; i < m_toolbarItemsGrid.ColumnsCount; ++i)
                {
                    m_toolbarItemsGrid.GetItemAt(i).Enabled = toolbar.IsEnabled(i);
                }
            }
        }

        private void UpdateItemIcon(MyToolbar toolbar, MyToolbar.IndexArgs args)
        {
            if (toolbar.IsValidIndex(args.ItemIndex))
            {
                var slot = toolbar.IndexToSlot(args.ItemIndex);
                if (slot != -1)
                    m_toolbarItemsGrid.GetItemAt(slot).Icon = toolbar.GetItemIcon(args.ItemIndex);
            }
            else
            {
                for (int i = 0; i < m_toolbarItemsGrid.ColumnsCount; ++i)
                {
                    m_toolbarItemsGrid.GetItemAt(i).Icon = toolbar.GetItemIcon(toolbar.SlotToIndex(i));
                }
            }
        }

        private void Toolbar_CurrentPageChanged(MyToolbar toolbar, MyToolbar.PageChangeArgs args)
        {
            m_contextMenu.Deactivate();

            HighlightCurrentPageLabel();

            for (int i = 0; i < MyToolbarComponent.CurrentToolbar.SlotCount; ++i)
            {
                SetGridItemAt(i, toolbar.GetSlotItem(i));
            }
        }

        private void grid_ItemClicked(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            if (eventArgs.Button == MySharedButtonsEnum.Secondary)
            {
                int slot = eventArgs.ColumnIndex;
                var toolbar = MyToolbarComponent.CurrentToolbar;
                MyToolbarItem item = toolbar.GetSlotItem(slot);
                if (item == null) return;

                //right clicks in multifunctional items should trigger their menus (if they have more than 0 options)
                if (item is MyToolbarItemActions)
                {
                    var actionList = (item as MyToolbarItemActions).PossibleActions(ShownToolbar.ToolbarType);
                    if (actionList.Count > 0)
                    {
                        m_contextMenu.CreateNewContextMenu();
                        foreach (var action in actionList)
                            m_contextMenu.AddItem(action.Name, icon: action.Icon, userData: action.Id);

                        m_contextMenu.AddItem(MyTexts.Get(MySpaceTexts.BlockAction_RemoveFromToolbar));
                        m_contextMenu.Enabled = true;
                        m_contextMenuItemIndex = toolbar.SlotToIndex(slot);
                    }
                    else
                        RemoveToolbarItem(eventArgs.ColumnIndex);
                }
                else
                    RemoveToolbarItem(eventArgs.ColumnIndex);
            }

            if (m_shownToolbar.IsValidIndex(eventArgs.ColumnIndex))
                m_shownToolbar.ActivateItemAtSlot(eventArgs.ColumnIndex, true);
        }

        private void grid_ItemDoubleClicked(MyGuiControlGrid sender, MyGuiControlGrid.EventArgs eventArgs)
        {
            RemoveToolbarItem(eventArgs.ColumnIndex);

            if (m_shownToolbar.IsValidIndex(eventArgs.ColumnIndex))
                m_shownToolbar.ActivateItemAtSlot(eventArgs.ColumnIndex);
        }

        private void contextMenu_ItemClicked(MyGuiControlContextMenu sender, MyGuiControlContextMenu.EventArgs args)
        {
            int idx = args.ItemIndex;
            var toolbar = MyToolbarComponent.CurrentToolbar;
            Debug.Assert(toolbar != null);
            if (toolbar == null) return;

            int slot = toolbar.IndexToSlot(m_contextMenuItemIndex);

            if (toolbar.IsValidSlot(slot))
            {
                var item = toolbar.GetSlotItem(slot);
                Debug.Assert(item is MyToolbarItemActions);
                var actionsItem = item as MyToolbarItemActions;
                if (item != null)
                {
                    //"Remove from toolbar" index (usually the last one)
                    if (idx < 0 || idx >= actionsItem.PossibleActions(ShownToolbar.ToolbarType).Count)
                        RemoveToolbarItem(slot);
                    else
                    {
                        //First updates the action
                        actionsItem.ActionId = (string)args.UserData;

                        //Removes all items equal to the one you're adding
                        for (int i = 0; i < MyToolbarComponent.CurrentToolbar.SlotCount; ++i)
                        {
                            var cur = toolbar.GetSlotItem(i);
                            if (cur != null && cur.Equals(actionsItem))
                                MyToolbarComponent.CurrentToolbar.SetItemAtSlot(i, null);
                        }

                        //And then put it to the slot
                        MyToolbarComponent.CurrentToolbar.SetItemAtSlot(slot, actionsItem);
                    }
                }
            }

            m_contextMenuItemIndex = -1;
        }

        #endregion
    }
}
