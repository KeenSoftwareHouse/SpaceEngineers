using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using System.Diagnostics;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlContextMenu: MyGuiControlBase
    {
        const int NUM_VISIBLE_ITEMS = 20;

        int m_numItems = 0;

        public struct EventArgs
        {
            public int ItemIndex;
            public object UserData;
        }

        private enum MyContextMenuKeys
        {
            UP = 0,
            DOWN = 1,
            ENTER = 2
        }
        private class MyContextMenuKeyTimerController
        {
            public MyKeys Key;

            /// <summary>
            /// This is not for converting key to string, but for controling repeated key input with delay
            /// </summary>
            public int LastKeyPressTime;

            public MyContextMenuKeyTimerController(MyKeys key)
            {
                Key = key;
                LastKeyPressTime = MyGuiManager.FAREST_TIME_IN_PAST;
            }
        }

        //The context menu is basically a non-scrollable list box with an event that will return the index of the item clicked and some key control
        private MyGuiControlListbox m_itemsList;
        public event Action<MyGuiControlContextMenu, EventArgs> ItemClicked;
        private MyContextMenuKeyTimerController[] m_keys;

        public MyGuiControlContextMenu()
        {
            m_itemsList = new MyGuiControlListbox();
            m_itemsList.Name = "ContextMenuListbox";
            m_itemsList.VisibleRowsCount = NUM_VISIBLE_ITEMS;
            Enabled = false;

            m_keys = new MyContextMenuKeyTimerController[3];
            m_keys[(int)MyContextMenuKeys.UP] = new MyContextMenuKeyTimerController(MyKeys.Up);
            m_keys[(int)MyContextMenuKeys.DOWN] = new MyContextMenuKeyTimerController(MyKeys.Down);
            m_keys[(int)MyContextMenuKeys.ENTER] = new MyContextMenuKeyTimerController(MyKeys.Enter);

            Name = "ContextMenu";
            Elements.Add(m_itemsList);
        }

        public void CreateNewContextMenu()
        {
            Clear();
            Deactivate();
            CreateContextMenu();
        }

        public List<MyGuiControlListbox.Item> Items
        {
            get
            {
                return m_itemsList.Items.ToList();
            }
        }

        private bool m_allowKeyboardNavigation = false;
        public bool AllowKeyboardNavigation
        {
            get
            {
                return m_allowKeyboardNavigation;
            }
            set
            {
                if(m_allowKeyboardNavigation != value)
                    m_allowKeyboardNavigation = value;
            }
        }
        private void CreateContextMenu()
        {
            m_itemsList = new MyGuiControlListbox(visualStyle: MyGuiControlListboxStyleEnum.ContextMenu);

            //Todo: automatically decide how to draw it given the position
            m_itemsList.HighlightType = MyGuiControlHighlightType.WHEN_CURSOR_OVER;
            m_itemsList.Enabled = true;
            m_itemsList.ItemClicked += list_ItemClicked;
            m_itemsList.MultiSelect = false;

        }

        public void Clear()
        {
            m_itemsList.Items.Clear();
            m_numItems = 0;
        }

        public void AddItem(StringBuilder text, string tooltip = "", string icon = "", object userData = null)
        {
            var item = new MyGuiControlListbox.Item(text: text, icon: icon, userData: userData);
            m_itemsList.Add(item);
            m_itemsList.VisibleRowsCount = Math.Min(NUM_VISIBLE_ITEMS, m_numItems++)+1;
        }

        void list_ItemClicked(MyGuiControlListbox sender)
        {
            if (!Visible)
                return;

            int selectedIndex = -1;
            object userData = null;
            foreach (var item in sender.SelectedItems)
            {
                selectedIndex = sender.Items.IndexOf(item);
                userData = item.UserData;
                break;
            }

            if (ItemClicked != null)
                ItemClicked(this, new EventArgs { ItemIndex = selectedIndex, UserData = userData });

            //GK: If the item list have scrollbar and we are over the caret then let scrollbar hanlde input. In any other case disappear when clicked
            if(!m_itemsList.IsOverScrollBar())
                Deactivate();
        }

        public override MyGuiControlBase HandleInput()
        {
            if ((MyInput.Static.IsNewMousePressed(MyMouseButtonsEnum.Left) || MyInput.Static.IsNewMousePressed(MyMouseButtonsEnum.Right)) && Visible && !IsMouseOver)
                Deactivate();

            if (MyInput.Static.IsKeyPress(MyKeys.Escape) && Visible)
            {
                Deactivate();
                return this;
            }
            
            if (AllowKeyboardNavigation)
            {
                Vector2 mousepos = MyGuiManager.MouseCursorPosition;

                //listbox mouseover apparently not working correctly
                if (mousepos.X >= m_itemsList.Position.X && mousepos.X <= m_itemsList.Position.X + m_itemsList.Size.X && mousepos.Y >= m_itemsList.Position.Y && mousepos.Y <= m_itemsList.Position.Y + m_itemsList.Size.Y)
                    m_itemsList.SelectedItems.Clear();
                else
                {
                    if (MyInput.Static.IsKeyPress(MyKeys.Up) && IsEnoughDelay(MyContextMenuKeys.UP, MyGuiConstants.TEXTBOX_MOVEMENT_DELAY))
                    {
                        UpdateLastKeyPressTimes(MyContextMenuKeys.UP);
                        SelectPrevious();

                        //Prevent focus change by stating you've handled the event
                        return this;
                    }
                    if (MyInput.Static.IsKeyPress(MyKeys.Down) && IsEnoughDelay(MyContextMenuKeys.DOWN, MyGuiConstants.TEXTBOX_MOVEMENT_DELAY))
                    {
                        UpdateLastKeyPressTimes(MyContextMenuKeys.DOWN);
                        SelectNext();

                        //Prevent focus change by stating you've handled the event
                        return this;
                    }
                    if (MyInput.Static.IsKeyPress(MyKeys.Enter) && IsEnoughDelay(MyContextMenuKeys.ENTER, MyGuiConstants.TEXTBOX_MOVEMENT_DELAY))
                    {
                        UpdateLastKeyPressTimes(MyContextMenuKeys.ENTER);
                        if (m_itemsList.SelectedItems.Count > 0)
                        {
                            EventArgs args;
                            args.ItemIndex = m_itemsList.Items.IndexOf(m_itemsList.SelectedItems[0]);
                            args.UserData = m_itemsList.SelectedItems[0].UserData;
                            ItemClicked(this, args);
                            Deactivate();
                            return this;
                        }
                    }
                }
            }

            return m_itemsList.HandleInput();
        }

        private void SelectPrevious()
        {
            int idx = -1;
            int cnt = 0;
            int numitems = m_itemsList.Items.Count;
            foreach (var item in m_itemsList.Items)
            {
                if (m_itemsList.SelectedItems.Contains(item))
                    idx = cnt;
                cnt++;
            }

            m_itemsList.SelectedItems.Clear();
            if (idx >= 0)
                m_itemsList.SelectedItems.Add(m_itemsList.Items[((idx - 1)%numitems + numitems)%numitems]);
            else if(m_itemsList.Items.Count > 0)
                m_itemsList.SelectedItems.Add(m_itemsList.Items[0]);
        }

        private void SelectNext()
        {
            int cnt = 0, last = -1;
            int numitems = m_itemsList.Items.Count;
            foreach (var item in m_itemsList.Items)
            {
                if (m_itemsList.SelectedItems.Contains(item))
                    last = cnt;
                cnt++;
            }

            m_itemsList.SelectedItems.Clear();
            if (last >= 0)
                m_itemsList.SelectedItems.Add(m_itemsList.Items[((last + 1) % numitems + numitems) % numitems]);
            else if (m_itemsList.Items.Count > 0)
                m_itemsList.SelectedItems.Add(m_itemsList.Items[0]);
        }

        public void Deactivate()
        {
            m_itemsList.IsActiveControl = false;
            m_itemsList.Visible = false;
            this.IsActiveControl = false;
            this.Visible = false;
        }

        public void Activate(bool autoPositionOnMouseTip = true)
        {
            if (autoPositionOnMouseTip)
            {
                m_itemsList.Position = MyGuiManager.MouseCursorPosition;
                m_itemsList.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                FitContextMenuToScreen();
            }
            else
            {
                m_itemsList.Position = Position;
                m_itemsList.OriginAlign = OriginAlign;
            }
            m_itemsList.Visible = true;
            m_itemsList.IsActiveControl = true;
            this.Visible = true;
            this.IsActiveControl = true;
        }

        private void FitContextMenuToScreen()
        {
            if (m_itemsList.Position.X < 0) m_itemsList.Position = new Vector2(0.0f, m_itemsList.Position.Y);
            if (m_itemsList.Position.X + m_itemsList.Size.X >= 1) m_itemsList.Position = new Vector2(1.0f - m_itemsList.Size.X, m_itemsList.Position.Y);
            if (m_itemsList.Position.Y < 0) m_itemsList.Position = new Vector2(m_itemsList.Position.X, 0.0f);
            if (m_itemsList.Position.Y + m_itemsList.Size.Y >= 1) m_itemsList.Position = new Vector2(m_itemsList.Position.X, 1.0f - m_itemsList.Size.Y);
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
            m_itemsList.Draw(transitionAlpha * m_itemsList.Alpha, backgroundTransitionAlpha * m_itemsList.Alpha);
        }

        private bool IsEnoughDelay(MyContextMenuKeys key, int forcedDelay)
        {
            MyContextMenuKeyTimerController keyEx = m_keys[(int)key];
            if (keyEx == null) return true;

            return ((MyGuiManager.TotalTimeInMilliseconds - keyEx.LastKeyPressTime) > forcedDelay);
        }

        private void UpdateLastKeyPressTimes(MyContextMenuKeys key)
        {
            MyContextMenuKeyTimerController keyEx = m_keys[(int)key];
            if (keyEx != null)
            {
                keyEx.LastKeyPressTime = MyGuiManager.TotalTimeInMilliseconds;
            }

        }
    }
}
