using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenControlMenu : MyGuiScreenBase
    {
        private enum ItemUpdateType
        {
            Activate,
            Next,
            Previous,
        }

        private class MyGuiControlItem : MyGuiControlParent
        {
            private MyAbstractControlMenuItem m_item;
            private MyGuiControlLabel m_label;
            private MyGuiControlLabel m_value;

            public bool IsItemEnabled
            {
                get { return m_item.Enabled; }
            }

            public MyGuiControlItem(MyAbstractControlMenuItem item, Vector2? size = null)
                : base(size: size)
            {
                m_item = item;
                m_item.UpdateValue();
                m_label = new MyGuiControlLabel(text: item.ControlLabel, textScale: 0.8f);
                m_value = new MyGuiControlLabel(text: item.CurrentValue, textScale: 0.8f);

                MyLayoutVertical layout = new MyLayoutVertical(this, 28);
                layout.Add(m_label, m_value);
            }

            public override MyGuiControlBase GetNextFocusControl(MyGuiControlBase currentFocusControl, bool forwardMovement)
            {
                if (HasFocus)
                    return Owner.GetNextFocusControl(this, forwardMovement);
                else
                    return this;
            }

            public override void Update()
            {
                base.Update();
                RefreshValueLabel();
                if (IsItemEnabled)
                {
                    m_label.Enabled = true;
                    m_value.Enabled = true;
                }
                else
                {
                    m_label.Enabled = false;
                    m_value.Enabled = false;
                }
            }

            private void RefreshValueLabel()
            {
                m_item.UpdateValue();
                m_value.Text = m_item.CurrentValue;
            }

            internal void UpdateItem(ItemUpdateType updateType)
            {
                switch (updateType)
                {
                    case ItemUpdateType.Next:
                        m_item.Next();
                        break;
                    case ItemUpdateType.Previous:
                        m_item.Previous();
                        break;
                    case ItemUpdateType.Activate:
                        m_item.Activate();
                        break;
                }
                RefreshValueLabel();
            }
        }

        private const float ITEM_SIZE = 0.03f;

        private MyGuiControlScrollablePanel m_scrollPanel;
        private List<MyGuiControlItem> m_items;
        private int m_selectedItem;
        private RectangleF m_itemsRect;

        public MyGuiScreenControlMenu()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.4f, 0.57f))
        {
            DrawMouseCursor = false;
            CanHideOthers = false;
            m_items = new List<MyGuiControlItem>();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MyCommonTexts.ScreenControlMenu_Title, captionScale: 1.3f);

            MyGuiControlParent parent = new MyGuiControlParent(size: new Vector2(Size.Value.X - 0.05f, m_items.Count * ITEM_SIZE));
            m_scrollPanel = new MyGuiControlScrollablePanel(parent);
            m_scrollPanel.ScrollbarVEnabled = true;
            m_scrollPanel.ScrollBarVScale = 1f;
            m_scrollPanel.Size = new Vector2(Size.Value.X - 0.05f, Size.Value.Y - 0.2f);

            MyLayoutVertical layout = new MyLayoutVertical(parent, 20);
            foreach (var item in m_items)
            {
                layout.Add(item, MyAlignH.Left, true);
            }
            m_itemsRect.Position = m_scrollPanel.GetPositionAbsoluteTopLeft();
            m_itemsRect.Size = new Vector2(Size.Value.X - 0.05f, Size.Value.Y - 0.2f);

            FocusedControl = parent;

            m_selectedItem = m_items.Count != 0 ? 0 : -1;

            Controls.Add(m_scrollPanel);
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Up)
                || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_UP, MyControlStateType.NEW_PRESSED))
            {
                UpdateSelectedItem(true);
                UpdateScroll();
            }
            else if (MyInput.Static.IsNewKeyPressed(MyKeys.Down)
                || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_DOWN, MyControlStateType.NEW_PRESSED))
            {
                UpdateSelectedItem(false);
                UpdateScroll();
            }
            else if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape)
                || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.CANCEL, MyControlStateType.NEW_PRESSED)
                || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsSpace.CONTROL_MENU, MyControlStateType.NEW_PRESSED))
            {
                Canceling();
            }

            if (m_selectedItem != -1)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Right)
                    || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_RIGHT, MyControlStateType.NEW_PRESSED))
                {
                    m_items[m_selectedItem].UpdateItem(ItemUpdateType.Next);
                }
                else if (MyInput.Static.IsNewKeyPressed(MyKeys.Left)
                    || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_LEFT, MyControlStateType.NEW_PRESSED))
                {
                    m_items[m_selectedItem].UpdateItem(ItemUpdateType.Previous);
                }
                else if (MyInput.Static.IsNewKeyPressed(MyKeys.Enter)
                    || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.ACCEPT, MyControlStateType.NEW_PRESSED))
                {
                    m_items[m_selectedItem].UpdateItem(ItemUpdateType.Activate);
                }
            }
        }

        public override bool Draw()
        {
            base.Draw();

            if (m_selectedItem == -1)
                return true;

            var selectedItem = m_items[m_selectedItem];
            if (selectedItem is MyGuiControlItem)
            {
                m_itemsRect.Position = m_scrollPanel.GetPositionAbsoluteTopLeft();
                using (MyGuiManager.UsingScissorRectangle(ref m_itemsRect))
                {
                    Vector2 position = selectedItem.GetPositionAbsoluteTopLeft();
                    string texture = MyGuiConstants.TEXTURE_HIGHLIGHT_DARK.Center.Texture;
                    MyGuiManager.DrawSpriteBatch(texture, position, selectedItem.Size, Color.White, VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                }

               // MyGuiManager.DrawBorders(selectedItem.GetPositionAbsoluteTopLeft(), selectedItem.Size, Color.Red, 2);
            }

            return true;
        }

        private void UpdateSelectedItem(bool up)
        {
            bool found = false;
            if (up)
            {
                for (int i = 0; i < m_items.Count; i++)
                {
                    m_selectedItem--;
                    if (m_selectedItem < 0)
                        m_selectedItem = m_items.Count - 1;
                    if (m_items[m_selectedItem].IsItemEnabled)
                    {
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_items.Count; i++)
                {
                    m_selectedItem = (m_selectedItem + 1) % m_items.Count;
                    if (m_items[m_selectedItem].IsItemEnabled)
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                m_selectedItem = -1;
        }

        private void UpdateScroll()
        {
            if (m_selectedItem == -1)
                return;

            var selectedItem = m_items[m_selectedItem];
            var lastItem = m_items[m_items.Count - 1];
            var selectedItemTopPos = selectedItem.GetPositionAbsoluteTopLeft();
            var lastItemBotPos = lastItem.GetPositionAbsoluteTopLeft() + lastItem.Size;

            var scrollPanelY = m_scrollPanel.GetPositionAbsoluteTopLeft().Y;
            selectedItemTopPos.Y -= scrollPanelY;
            lastItemBotPos.Y -= scrollPanelY;

            float topItemRect = selectedItemTopPos.Y;
            float bottomItemRect = selectedItemTopPos.Y + selectedItem.Size.Y;
            float topItemScroll = (topItemRect / lastItemBotPos.Y) * m_scrollPanel.ScrolledAreaSize.Y;
            float botItemScroll = (bottomItemRect / lastItemBotPos.Y) * m_scrollPanel.ScrolledAreaSize.Y;
            if (topItemScroll < m_scrollPanel.ScrollbarVPosition)
            {
                m_scrollPanel.ScrollbarVPosition = topItemScroll;
            }
            if (botItemScroll > m_scrollPanel.ScrollbarVPosition)
            {
                m_scrollPanel.ScrollbarVPosition = botItemScroll;          
            }           
        }

        public void AddItem(MyAbstractControlMenuItem item)
        {
            m_items.Add(new MyGuiControlItem(item, new Vector2(Size.Value.X - 0.1f, ITEM_SIZE)));
        }

        public void AddItems(params MyAbstractControlMenuItem[] items)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        public void ClearItems()
        {
            m_items.Clear();
        }

        protected override void OnClosed()
        {
            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
        }

        public override string GetFriendlyName()
        {
            return "Control menu screen";
        }
    }
}
