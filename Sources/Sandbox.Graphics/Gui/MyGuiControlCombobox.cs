using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public enum MyGuiControlComboboxStyleEnum
    {
        Default,
        Debug,
        Terminal
    }

    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlCombobox))]
    public class MyGuiControlCombobox : MyGuiControlBase
    {
        const float ITEM_HEIGHT = 0.03f;

        #region Styles
        public class StyleDefinition
        {
            public string ItemFontHighlight;
            public string ItemFontNormal;
            public string ItemTextureHighlight;

            /// <summary>
            /// Offset of the text from left border.
            /// </summary>
            public Vector2 SelectedItemOffset;

            public MyGuiCompositeTexture DropDownTexture;
            public MyGuiCompositeTexture ComboboxTextureNormal;
            public MyGuiCompositeTexture ComboboxTextureHighlight;

            public float TextScale;
            public float DropDownHighlightExtraWidth;

            public MyGuiBorderThickness ScrollbarMargin;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlCombobox()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlComboboxStyleEnum>() + 1];
            m_styles[(int)MyGuiControlComboboxStyleEnum.Default] = new StyleDefinition()
            {
                DropDownTexture             = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ComboboxTextureNormal       = MyGuiConstants.TEXTURE_COMBOBOX_NORMAL,
                ComboboxTextureHighlight    = MyGuiConstants.TEXTURE_COMBOBOX_HIGHLIGHT,
                ItemTextureHighlight        = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal              = MyFontEnum.Blue,
                ItemFontHighlight           = MyFontEnum.White,
                SelectedItemOffset          = new Vector2(0.01f, 0.005f),
                TextScale                   = MyGuiConstants.DEFAULT_TEXT_SCALE * 0.9f,
                DropDownHighlightExtraWidth = 0.007f,
                ScrollbarMargin             = new MyGuiBorderThickness()
                {
                    Left   = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right  = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top    = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };

            m_styles[(int)MyGuiControlComboboxStyleEnum.Debug] = new StyleDefinition()
            {
                DropDownTexture             = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ComboboxTextureNormal       = MyGuiConstants.TEXTURE_COMBOBOX_NORMAL,
                ComboboxTextureHighlight    = MyGuiConstants.TEXTURE_COMBOBOX_HIGHLIGHT,
                ItemTextureHighlight        = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal              = MyFontEnum.Debug,
                ItemFontHighlight           = MyFontEnum.White,
                SelectedItemOffset          = new Vector2(0.01f, 0.005f),
                TextScale                   = MyGuiConstants.DEFAULT_TEXT_SCALE * 0.9f,
                DropDownHighlightExtraWidth = 0.007f,
                ScrollbarMargin             = new MyGuiBorderThickness()
                {
                    Left   = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right  = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top    = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };

            m_styles[(int)MyGuiControlComboboxStyleEnum.Terminal] = new StyleDefinition()
            {
                DropDownTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ComboboxTextureNormal = MyGuiConstants.TEXTURE_COMBOBOX_NORMAL,
                ComboboxTextureHighlight = MyGuiConstants.TEXTURE_COMBOBOX_HIGHLIGHT,
                ItemTextureHighlight = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal = MyFontEnum.Blue,       
                ItemFontHighlight = MyFontEnum.White,
                SelectedItemOffset = new Vector2(0.01f, 0.005f),
                TextScale = MyGuiConstants.DEFAULT_TEXT_SCALE * 0.9f,
                DropDownHighlightExtraWidth = 0.007f,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlComboboxStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion Styles

        public class Item : IComparable
        {
            public readonly long Key;
            public readonly int SortOrder;
            public readonly StringBuilder Value;
            public MyToolTips ToolTip;

            public Item(long key, StringBuilder value, int sortOrder, String toolTip = null)
            {
                Debug.Assert(value != null);

                Key       = key;
                SortOrder = sortOrder;

                if (value != null)
                    Value = new StringBuilder(value.Length).AppendStringBuilder(value);
                else
                    Value = new StringBuilder();

                if (toolTip != null)
                    ToolTip = new MyToolTips(toolTip);
            }

            public Item(long key, String value, int sortOrder, String toolTip = null)
            {
                Debug.Assert(value != null);

                Key = key;
                SortOrder = sortOrder;

                if (value != null)
                    Value = new StringBuilder(value.Length).Append(value);
                else
                    Value = new StringBuilder();

                if (toolTip != null)
                    ToolTip = new MyToolTips(toolTip);
            }

            //  Sorts from small to large, e.g. 0, 1, 2, 3, ...
            public int CompareTo(object compareToObject)
            {
                Item compareToItem = (Item)compareToObject;
                return this.SortOrder.CompareTo(compareToItem.SortOrder);
            }
        }

        public delegate void ItemSelectedDelegate();
        public event ItemSelectedDelegate ItemSelected;

        bool m_isOpen;
        bool m_scrollBarDragging = false;
        List<Item> m_items;
        Item m_selected;                            //  Item that is selected in the combobox, that is displayed in the main rectangle
        Item m_preselectedMouseOver;                //  Item that is under mouse and may be selected if user clicks on it
        Item m_preselectedMouseOverPrevious;        //  Same as m_preselectedMouseOver, but in previous update
        int? m_preselectedKeyboardIndex = null;                                 //  Same as m_preselectedMouseOver, but for keyboard. By default no item is selected.
        int? m_preselectedKeyboardIndexPrevious = null;                         //  Same as m_preselectedMouseOverPrevious, but for keyboard
        int? m_mouseWheelValueLast = null;

        //  Scroll Bar logic code
        int m_openAreaItemsCount;
        int m_middleIndex;
        bool m_showScrollBar;
        float m_scrollBarCurrentPosition;
        float m_scrollBarCurrentNonadjustedPosition;
        float m_mouseOldPosition;
        bool m_mousePositionReinit;
        float m_maxScrollBarPosition;
        float m_scrollBarEndPositionRelative;
        int m_displayItemsStartIndex;
        int m_displayItemsEndIndex;
        int m_scrollBarItemOffSet;
        float m_scrollBarHeight;
        float m_scrollBarWidth; // not the texture width, but the clickable area width
        float m_comboboxItemDeltaHeight;
        float m_scrollRatio;
        private Vector2 m_dropDownItemSize;

        private const float ITEM_DRAW_DELTA = 0.0001f;

        bool m_useScrollBarOffset = false;

        public MyGuiControlComboboxStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlComboboxStyleEnum m_visualStyle;
        private StyleDefinition m_styleDef;

        private RectangleF m_selectedItemArea;
        private RectangleF m_openedArea;
        private RectangleF m_openedItemArea;

        private string m_selectedItemFont;

        private MyGuiCompositeTexture m_scrollbarTexture;
        private Vector4 m_textColor;

        private float m_textScaleWithLanguage;

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            if (HasHighlight)
            {
                BackgroundTexture  = m_styleDef.ComboboxTextureHighlight;
                m_selectedItemFont = m_styleDef.ItemFontHighlight;
            }
            else
            {
                BackgroundTexture  = m_styleDef.ComboboxTextureNormal;
                m_selectedItemFont = m_styleDef.ItemFontNormal;
            }
            MinSize = BackgroundTexture.MinSizeGui;
            MaxSize = BackgroundTexture.MaxSizeGui;

            m_scrollbarTexture = (HasHighlight)
                ? MyGuiConstants.TEXTURE_SCROLLBAR_V_THUMB_HIGHLIGHT
                : MyGuiConstants.TEXTURE_SCROLLBAR_V_THUMB;

            m_selectedItemArea.Position = m_styleDef.SelectedItemOffset;
            m_selectedItemArea.Size = new Vector2(Size.X - (m_scrollbarTexture.MinSizeGui.X + m_styleDef.ScrollbarMargin.HorizontalSum + m_styleDef.SelectedItemOffset.X), ITEM_HEIGHT);

            var openedArea = GetOpenedArea();
            m_openedArea.Position = openedArea.LeftTop;
            m_openedArea.Size = openedArea.Size;

            m_openedItemArea.Position = m_openedArea.Position + new Vector2(m_styleDef.SelectedItemOffset.X, m_styleDef.DropDownTexture.LeftTop.SizeGui.Y);
            m_openedItemArea.Size = new Vector2(m_selectedItemArea.Size.X,
                (m_showScrollBar ? m_openAreaItemsCount : m_items.Count) * m_selectedItemArea.Size.Y);

            m_textScaleWithLanguage = m_styleDef.TextScale * MyGuiManager.LanguageTextScale;
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            RefreshInternals();
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();
            RefreshInternals();
        }

        protected override void OnOriginAlignChanged()
        {
            RefreshInternals();
            base.OnOriginAlignChanged();
        }

        public MyGuiControlCombobox() : this(position: null) { }

        public MyGuiControlCombobox(
            Vector2? position        = null,
            Vector2? size            = null,
            Vector4? backgroundColor = null,
            Vector2? textOffset      = null,
            int openAreaItemsCount   = 10,
            Vector2? iconSize        = null,
            bool useScrollBarOffset  = false,
            String toolTip           = null,
            MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            Vector4? textColor       = null)
            : base( position: position,
                    size: size ?? (new Vector2(455f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE),
                    colorMask: backgroundColor,
                    toolTip: toolTip,
                    canHaveFocus: true,
                    originAlign: originAlign)
        {
            Name = "Combobox";

            HighlightType                    = MyGuiControlHighlightType.WHEN_CURSOR_OVER;
            m_items                          = new List<Item>();
            m_isOpen                         = false;
            m_openAreaItemsCount             = openAreaItemsCount;
            m_middleIndex                    = Math.Max(m_openAreaItemsCount / 2 - 1, 0);

            m_textColor = textColor.HasValue ? textColor.Value : Vector4.One;
            m_dropDownItemSize        = GetItemSize();
            m_comboboxItemDeltaHeight = m_dropDownItemSize.Y;
            m_mousePositionReinit     = true;
            RefreshVisualStyle();

            InitializeScrollBarParameters();
            m_showToolTip        = true;
            m_useScrollBarOffset = useScrollBarOffset;
        }

        //  Clears/removes all items
        public void ClearItems()
        {
            m_items.Clear();
            m_selected = null;
            m_preselectedKeyboardIndex = null;
            m_preselectedKeyboardIndexPrevious = null;
            m_preselectedMouseOver = null;
            m_preselectedMouseOverPrevious = null;
            InitializeScrollBarParameters();
        }

        public void AddItem(long key, MyStringId value, int? sortOrder = null, MyStringId? toolTip = null)
        {
            AddItem(
                key,
                MyTexts.Get(value),
                sortOrder,
                (toolTip.HasValue) ? MyTexts.GetString(toolTip.Value) : null);
        }

        public void AddItem(long key, StringBuilder value, int? sortOrder = null, String toolTip = null)
        {
            System.Diagnostics.Debug.Assert(value != null);
            sortOrder = sortOrder ?? m_items.Count;

            m_items.Add(new Item(key, value, sortOrder.Value, toolTip));
            m_items.Sort();

            //  scroll bar parameters need to be recalculated when new item is added
            AdjustScrollBarParameters();

            RefreshInternals();
        }

        public void AddItem(long key, String value, int? sortOrder = null, String toolTip = null)
        {
            System.Diagnostics.Debug.Assert(value != null);
            sortOrder = sortOrder ?? m_items.Count;

            m_items.Add(new Item(key, value, sortOrder.Value, toolTip));
            m_items.Sort();

            //  scroll bar parameters need to be recalculated when new item is added
            AdjustScrollBarParameters();

            RefreshInternals();
        }

        public void RemoveItem(long key) 
        {
            Item removedItem = m_items.Find(x => x.Key == key);
            RemoveItem(removedItem);
        }

        public void RemoveItemByIndex(int index) 
        {
            if (index < 0 || index >= m_items.Count) 
            {
                throw new ArgumentOutOfRangeException("index");
            }

            RemoveItem(m_items[index]);
        }

        public Item GetItemByIndex(int index)
        {
            if (index < 0 || index >= m_items.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return m_items[index];
        }

        private void RemoveItem(Item item) 
        {
            Debug.Assert(item != null);
            if (item == null)
                return;

            m_items.Remove(item);

            // if we remove selected item (clear selection)
            if (m_selected == item)
                m_selected = null;
        }

        public Item TryGetItemByKey(long key)
        {
            foreach (var item in m_items)
                if (item.Key == key)
                    return item;
            return null;
        }

        public int GetItemsCount()
        {
            return m_items.Count;
        }

        public void SortItemsByValueText()
        {
            if (m_items != null)
            {
                m_items.Sort(delegate(Item item1, Item item2)
                {
                    return item1.Value.ToString().CompareTo(item2.Value.ToString());
                });
            }
        }

        public void CustomSortItems(Comparison<Item> comparison)
        {
            if (m_items != null)
            {
                m_items.Sort(comparison);
            }
        }

        public override MyGuiControlBase GetExclusiveInputHandler()
        {
            return m_isOpen ? this : null;
        }

        //  Selects item by index, so when you want to make first item as selected call SelectItemByIndex(0)
        public void SelectItemByIndex(int index)
        {
            if (!m_items.IsValidIndex(index))
            {
                m_selected = null;
                return;
            }

            m_selected = m_items[index];
            SetScrollBarPositionByIndex(index);
            if (ItemSelected != null)
                ItemSelected();
        }

        //  Selects item by key
        public void SelectItemByKey(long key, bool sendEvent = true)
        {
            for (int i = 0; i < m_items.Count; i++)
            {
                Item item = m_items[i];

                if (item.Key.Equals(key))
                {
                    m_selected = item;
                    m_preselectedKeyboardIndex = i;
                    SetScrollBarPositionByIndex(i);
                    if (sendEvent && ItemSelected != null)
                        ItemSelected();
                    return;
                }
            }
        }

        //  Return key of selected item
        public long GetSelectedKey()
        {
            if (m_selected == null)
                return -1;
            return m_selected.Key;
        }

        public int GetSelectedIndex() 
        {
            if (m_selected == null)
                return -1;
            return m_items.IndexOf(m_selected);
        }

        //  Return value of selected item
        public StringBuilder GetSelectedValue()
        {
            if (m_selected == null)
            {
                return null;
            }
            return m_selected.Value;
        }

        void Assert()
        {
            //  If you forget to set default or pre-selected item, you must do it! It won't be assigned automaticaly!
            MyDebug.AssertDebug(m_selected != null);

            //  Combobox can't be empty!
            MyDebug.AssertDebug(m_items.Count > 0);
        }

        private void SwitchComboboxMode()
        {
            if (m_scrollBarDragging == false)
            {
                m_isOpen = !m_isOpen;
            }
        }

        //  Method returns true if input was captured by control, so no other controls, nor screen can use input in this update
        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase captureInput = base.HandleInput();

            if (captureInput == null && Enabled)
            {
                if (IsMouseOver && MyInput.Static.IsNewPrimaryButtonPressed() && !m_isOpen && !m_scrollBarDragging)
                    return this;

                if (MyInput.Static.IsNewPrimaryButtonReleased() && !m_scrollBarDragging) 
                {
                    if (IsMouseOver && !m_isOpen || IsMouseOverSelectedItem() && m_isOpen) 
                    {
                        MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                        SwitchComboboxMode();
                        captureInput = this;
                    }
                }

                if (HasFocus && (MyInput.Static.IsNewKeyPressed(MyKeys.Enter) ||
                                 MyInput.Static.IsNewKeyPressed(MyKeys.Space)))
                {
                    MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                    if ((m_preselectedKeyboardIndex.HasValue) && (m_preselectedKeyboardIndex.Value < m_items.Count))
                    {
                        if (m_isOpen == false)
                        {
                            SetScrollBarPositionByIndex(m_selected.Key);
                        }
                        else
                        {
                            SelectItemByKey(m_items[m_preselectedKeyboardIndex.Value].Key);
                        }
                    }

                    //  Close but capture focus for this update so parent screen don't receive this ENTER
                    SwitchComboboxMode();
                    captureInput = this;
                }

                //  In listbox mode, the list is always in opened state
                if (m_isOpen == true)
                {
                    #region Handle mouse and scrollbar interaction
                    if (m_showScrollBar == true && MyInput.Static.IsPrimaryButtonPressed() == true)
                    {
                        //  Handles mouse input of dragging the scrollbar up or down
                        Vector2 position = GetPositionAbsoluteCenterLeft();
                        MyRectangle2D openedArea = GetOpenedArea();
                        openedArea.LeftTop += GetPositionAbsoluteTopLeft();
                        float minX = position.X + Size.X - m_scrollBarWidth;
                        float maxX = position.X + Size.X;
                        float minY = position.Y + Size.Y / 2.0f;
                        float maxY = minY + openedArea.Size.Y;

                        // if we are already scrolling, the area used for scrollbar moving will be extended to whole screen
                        if (m_scrollBarDragging)
                        {
                            minX = float.NegativeInfinity;
                            maxX = float.PositiveInfinity;
                            minY = float.NegativeInfinity;
                            maxY = float.PositiveInfinity;
                        }

                        // In case mouse cursor is intersecting scrollbar area, start scroll bar dragging mode
                        if ((MyGuiManager.MouseCursorPosition.X >= minX) && (MyGuiManager.MouseCursorPosition.X <= maxX)
                            && (MyGuiManager.MouseCursorPosition.Y >= minY) && (MyGuiManager.MouseCursorPosition.Y <= maxY))
                        {                           
                            // Are we over thee scroll bar handle?
                            float P0 = m_scrollBarCurrentPosition + (openedArea.LeftTop.Y);
                            if (MyGuiManager.MouseCursorPosition.Y > P0 && MyGuiManager.MouseCursorPosition.Y < P0 + m_scrollBarHeight)
                            {
                                if (m_mousePositionReinit)
                                {
                                    m_mouseOldPosition = MyGuiManager.MouseCursorPosition.Y;
                                    m_mousePositionReinit = false;
                                }

                                float mdeff = MyGuiManager.MouseCursorPosition.Y - m_mouseOldPosition;
                                if (mdeff > float.Epsilon || mdeff < float.Epsilon)
                                {
                                    SetScrollBarPosition(m_scrollBarCurrentNonadjustedPosition + mdeff);
                                }

                                m_mouseOldPosition = MyGuiManager.MouseCursorPosition.Y;
                            }
                            else
                            {
                                // If we are not over the scrollbar handle -> jump:
                                float scrollPositionY = MyGuiManager.MouseCursorPosition.Y - (openedArea.LeftTop.Y) - m_scrollBarHeight / 2.0f;
                                SetScrollBarPosition(scrollPositionY);
                            }

                            m_scrollBarDragging = true;
                        }
                    }
                    #endregion

                    // Reset mouse parameters after it was released now
                    if (MyInput.Static.IsNewPrimaryButtonReleased())
                    {
                        m_mouseOldPosition = MyGuiManager.MouseCursorPosition.Y;
                        m_mousePositionReinit = true;
                    }

                    //  If ESC was pressed while combobox has keyboard focus and combobox was opened, then close combobox but don't send this ESC to parent screen
                    //  Or if user clicked outside of the combobox's area
                    if ((HasFocus && (MyInput.Static.IsNewKeyPressed(MyKeys.Escape) || MyInput.Static.IsJoystickButtonNewPressed(MyJoystickButtonsEnum.J02))) ||
                        (!IsMouseOverOnOpenedArea() && !IsMouseOver && MyInput.Static.IsNewPrimaryButtonReleased())
                        )
                    {
                        MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                        m_isOpen = false;
                    }

                    //  Still capture focus, don't allow parent screen to receive this ESCAPE
                    captureInput = this;

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //  Mouse controling items in the combobox
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    if (m_scrollBarDragging == false)
                    {
                        #region Handle item that is under mouse cursor
                        //  Search for item that is under the mouse cursor
                        m_preselectedMouseOverPrevious = m_preselectedMouseOver;
                        m_preselectedMouseOver = null;

                        //  The following are used for controlling scroll window range
                        int startIndex = 0;
                        int endIndex = m_items.Count;
                        float widthOffSet = 0f;
                        if (m_showScrollBar == true)
                        {
                            startIndex = m_displayItemsStartIndex;
                            endIndex = m_displayItemsEndIndex;
                            widthOffSet = 0.025f;
                        }

                        for (int i = startIndex; i < endIndex; i++)
                        {
                            Vector2 position = GetOpenItemPosition(i - m_displayItemsStartIndex);
                            MyRectangle2D openedArea = GetOpenedArea();

                            Vector2 min = new Vector2(position.X, Math.Max(openedArea.LeftTop.Y, position.Y));
                            Vector2 max = min + new Vector2(Size.X - widthOffSet, m_comboboxItemDeltaHeight);
                            var mousePos = MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft();
                            if ((mousePos.X >= min.X) &&
                                (mousePos.X <= max.X) &&
                                (mousePos.Y >= min.Y) &&
                                (mousePos.Y <= max.Y))
                            {
                                m_preselectedMouseOver = m_items[i];
                            }
                        }

                        if (m_preselectedMouseOver != null && m_preselectedMouseOver != m_preselectedMouseOverPrevious)
                        {
                            MyGuiSoundManager.PlaySound(GuiSounds.MouseOver);
                        }

                        #endregion

                        #region Selecting item in opened combobox area
                        //  Select item when user clicks on it
                        if (MyInput.Static.IsNewPrimaryButtonReleased() == true && m_preselectedMouseOver != null)
                        {
                            SelectItemByKey(m_preselectedMouseOver.Key);

                            MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                            m_isOpen = false;

                            //  Still capture focus, don't allow parent screen to receive this CLICK
                            captureInput = this;
                        }
                        #endregion

                        #region Keyboard and scrollwheel controlling items in combobox

                        if (HasFocus || IsMouseOverOnOpenedArea())
                        {
                            if (m_mouseWheelValueLast == null) m_mouseWheelValueLast = MyInput.Static.MouseScrollWheelValue();

                            if (MyInput.Static.MouseScrollWheelValue() < m_mouseWheelValueLast)
                            {
                                HandleItemMovement(true);
                                captureInput = this;
                            }
                            else if (MyInput.Static.MouseScrollWheelValue() > m_mouseWheelValueLast)
                            {
                                HandleItemMovement(false);
                                captureInput = this;
                            }

                            //  Keyboard and mouse movement
                            if (MyInput.Static.IsNewKeyPressed(MyKeys.Down) || MyInput.Static.IsNewGamepadKeyDownPressed())
                            {
                                HandleItemMovement(true);
                                captureInput = this;
                                if (MyInput.Static.IsNewGamepadKeyDownPressed())
                                    SnapCursorToControl(m_preselectedKeyboardIndex.Value);
                            }
                            else if (MyInput.Static.IsNewKeyPressed(MyKeys.Up) || MyInput.Static.IsNewGamepadKeyUpPressed())
                            {
                                HandleItemMovement(false);
                                captureInput = this;
                                if (MyInput.Static.IsNewGamepadKeyUpPressed())
                                    SnapCursorToControl(m_preselectedKeyboardIndex.Value);
                            }
                            else if (MyInput.Static.IsNewKeyPressed(MyKeys.PageDown))
                            {
                                HandleItemMovement(true, true);
                            }
                            else if (MyInput.Static.IsNewKeyPressed(MyKeys.PageUp))
                            {
                                HandleItemMovement(false, true);
                            }
                            else if (MyInput.Static.IsNewKeyPressed(MyKeys.Home))
                            {
                                HandleItemMovement(true, false, true);
                            }
                            else if (MyInput.Static.IsNewKeyPressed(MyKeys.End))
                            {
                                HandleItemMovement(false, false, true);
                            }
                            else if (MyInput.Static.IsNewKeyPressed(MyKeys.Tab))
                            {
                                //  We want to close the combobox without selecting any item and forward TAB or SHIF+TAB to parent screen so it can navigate to next control
                                if (m_isOpen) SwitchComboboxMode();
                                captureInput = null;
                            }

                            m_mouseWheelValueLast = MyInput.Static.MouseScrollWheelValue();
                        }
                        #endregion
                    }
                    else
                    {
                        // When finished scrollbar dragging, set it to false and enable input capturing again
                        if (MyInput.Static.IsNewPrimaryButtonReleased()) 
                            m_scrollBarDragging = false;
                        captureInput = this;
                    }
                }
            }

            return captureInput;
        }

        //  Moves keyboard index to the next item, or previous item, or first item in the combobox.
        //  forwardMovement -> set to TRUE when you want forward movement, set to FALSE when you wasnt backward
        void HandleItemMovement(bool forwardMovement, bool page = false, bool list = false)
        {
            m_preselectedKeyboardIndexPrevious = m_preselectedKeyboardIndex;

            int step = 0;
            if (list && forwardMovement) // first item
            {
                m_preselectedKeyboardIndex = 0;
            }
            else if (list && !forwardMovement) // last item
            {
                m_preselectedKeyboardIndex = m_items.Count - 1;
            }
            else if (page && forwardMovement) // step + 1 page
            {
                if (m_openAreaItemsCount > m_items.Count)
                    step = m_items.Count - 1;
                else
                    step = m_openAreaItemsCount - 1;
            }
            else if (page && !forwardMovement) // step - 1 page
            {
                if (m_openAreaItemsCount > m_items.Count)
                    step = -(m_items.Count - 1);
                else
                    step = -m_openAreaItemsCount + 1;
            }
            else if (!page && !list && forwardMovement) // step 1 item
            {
                step = 1;
            }
            else // step -1 item
            {
                step = -1;
            }


            if (m_preselectedKeyboardIndex.HasValue == false)
            {
                //  If this is first keypress in this combobox, we will set keyboard index to begining or end of the list
                m_preselectedKeyboardIndex = (forwardMovement == true) ? 0 : m_items.Count - 1;
            }
            else
            {
                //  Increase or decrease and than check ranges and do sort of overflow
                m_preselectedKeyboardIndex += step;// sign;
                if (m_preselectedKeyboardIndex > (m_items.Count - 1)) m_preselectedKeyboardIndex = (m_items.Count - 1);
                if (m_preselectedKeyboardIndex < 0) m_preselectedKeyboardIndex = 0;
            }

            if (m_preselectedKeyboardIndex != m_preselectedKeyboardIndexPrevious)
            {
                MyGuiSoundManager.PlaySound(GuiSounds.MouseOver);
            }

            SetScrollBarPositionByIndex(m_preselectedKeyboardIndex.Value);
        }

        private void SetScrollBarPositionByIndex(long index)
        {
            //  Programmatically adjust the scroll bar position based on changes in m_preselectedKeyboardIndex
            //  So it handles the scrolling action when users press up and down keys
            if (m_showScrollBar == true)
            {
                m_scrollRatio = 0f; //  Reset to zero, since keyboard navigation always does full item movement
                //  These two conditions handle when either
                //  1. the index is at top of the display index range (so scrolls down)
                //  2. the index is at bottom of the display index range (so scrolls up)
                //  3. if neither, then the index is in between the display range, so no scrolling is needed yet
                if (m_preselectedKeyboardIndex >= m_displayItemsEndIndex)
                {
                    m_displayItemsEndIndex = Math.Max(m_openAreaItemsCount, m_preselectedKeyboardIndex.Value + 1);
                    m_displayItemsStartIndex = Math.Max(0, m_displayItemsEndIndex - m_openAreaItemsCount);
                    SetScrollBarPosition(m_preselectedKeyboardIndex.Value * m_maxScrollBarPosition  / (m_items.Count-1), false);
                }
                else if (m_preselectedKeyboardIndex < m_displayItemsStartIndex)
                {
                    m_displayItemsStartIndex = Math.Max(0, m_preselectedKeyboardIndex.Value);
                    m_displayItemsEndIndex = Math.Max(m_openAreaItemsCount, m_displayItemsStartIndex + m_openAreaItemsCount);
                    SetScrollBarPosition(m_preselectedKeyboardIndex.Value * m_maxScrollBarPosition / (m_items.Count - 1), false);
                }
                else if(m_preselectedKeyboardIndex.HasValue)
                {
                    SetScrollBarPosition(m_preselectedKeyboardIndex.Value * m_maxScrollBarPosition / (m_items.Count - 1), false);
                }
            }
        }

        //  Checks if mouse cursor is over opened combobox area
        bool IsMouseOverOnOpenedArea()
        {
            MyRectangle2D openedArea = GetOpenedArea();
            openedArea.Size.Y += m_dropDownItemSize.Y;

            Vector2 min = openedArea.LeftTop;
            Vector2 max = openedArea.LeftTop + openedArea.Size;

            var mousePos = MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft();
            return ((mousePos.X >= min.X) &&
                    (mousePos.X <= max.X) &&
                    (mousePos.Y >= min.Y) &&
                    (mousePos.Y <= max.Y));
        }

        MyRectangle2D GetOpenedArea()
        {
            MyRectangle2D ret;
            ret.LeftTop = new Vector2(0f, Size.Y);

            // Adjust the open area to be as big as the scroll bar MAX_VISIBLE_ITEMS_COUNT when scrollbar is on
            if (m_showScrollBar)
                ret.Size = new Vector2(m_dropDownItemSize.X, m_openAreaItemsCount * m_comboboxItemDeltaHeight);
            else
                ret.Size = new Vector2(m_dropDownItemSize.X, m_items.Count * m_comboboxItemDeltaHeight);

            return ret;
        }

        //  Returns position of item in open list
        Vector2 GetOpenItemPosition(int index)
        {
            float yOffSet = m_dropDownItemSize.Y / 2.0f;
            yOffSet += m_comboboxItemDeltaHeight * 0.5f;
            return new Vector2(0f, 0.5f * Size.Y) + new Vector2(0, yOffSet + index * m_comboboxItemDeltaHeight); 
        }

        /// <summary>
        /// two phase draw(two SpriteBatch phase):
        /// 1. combobox itself and selected item
        /// 2. opened area and display items draw(if opened area is displayed)
        ///     a. setup up and draw stencil area to stencil buffer for clipping
        ///     b. enable stencil
        ///     c. draw the display items
        ///     d. disable stencil
        /// </summary>
        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            // In case of listbox mode, before calling parent's draw, reset background color, because it will draw unwanted texture for first item in list(texture, that is used normally for closed combobox)
            base.Draw(transitionAlpha, transitionAlpha);

            if (m_selected != null)
                DrawSelectedItemText(transitionAlpha);

            Vector2 position = GetPositionAbsoluteCenterLeft();
            float scrollbarInnerTexturePositionX = position.X + Size.X - m_scrollBarWidth / 2;

            //  The following are used for controlling scroll window range
            int startIndex = 0;
            int endIndex = m_items.Count;

            if (m_showScrollBar)
            {
                startIndex = m_displayItemsStartIndex;
                endIndex = m_displayItemsEndIndex;
            }

            if (m_isOpen)
            {
                MyRectangle2D openedArea = GetOpenedArea();
                DrawOpenedAreaItems(startIndex, endIndex, transitionAlpha);
                if (m_showScrollBar)
                    DrawOpenedAreaScrollbar(scrollbarInnerTexturePositionX, openedArea, transitionAlpha);
            }

            //DebugDraw();
        }

        private void DebugDraw()
        {
            base.BorderEnabled = true;
            var topLeft = GetPositionAbsoluteTopLeft();
            MyGuiManager.DrawBorders(topLeft + m_selectedItemArea.Position, m_selectedItemArea.Size, Color.Cyan, 1);
            if (m_isOpen)
            {
                MyGuiManager.DrawBorders(topLeft + m_openedArea.Position, m_openedArea.Size, Color.GreenYellow, 1);
                MyGuiManager.DrawBorders(topLeft + m_openedItemArea.Position, m_openedItemArea.Size, Color.Red, 1);
            }
        }

        private void DrawOpenedAreaScrollbar(float scrollbarInnerTexturePositionX, MyRectangle2D openedArea, float transitionAlpha)
        {
            var margin = m_styleDef.ScrollbarMargin;

            var pos = GetPositionAbsoluteBottomRight() + new Vector2(-(margin.Right + m_scrollbarTexture.MinSizeGui.X),
                                                                     margin.Top + m_scrollBarCurrentPosition);
            m_scrollbarTexture.Draw(pos, m_scrollBarHeight - m_scrollbarTexture.MinSizeGui.Y, ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha));
        }

        private void DrawOpenedAreaItems(int startIndex, int endIndex, float transitionAlpha)
        {
            // Draw background
            float itemsHeight = (endIndex - startIndex) * (m_comboboxItemDeltaHeight + ITEM_DRAW_DELTA);
            var minSize = m_styleDef.DropDownTexture.MinSizeGui;
            var maxSize = m_styleDef.DropDownTexture.MaxSizeGui;
            Vector2 dropdownSize = Vector2.Clamp(new Vector2(Size.X, itemsHeight + minSize.Y), minSize, maxSize);
            var topLeft = GetPositionAbsoluteTopLeft();
            m_styleDef.DropDownTexture.Draw(topLeft + m_openedArea.Position, dropdownSize,
                ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha));

            // Scissor to cut off items that overflow dropdown area.
            var scissor         = m_openedItemArea;
            scissor.Position   += topLeft;
            scissor.Position.X -= m_styleDef.DropDownHighlightExtraWidth;
            scissor.Size.X     += m_styleDef.DropDownHighlightExtraWidth;
            using (MyGuiManager.UsingScissorRectangle(ref scissor))
            {
                Vector2 itemPosition = topLeft + m_openedItemArea.Position;
                for (int i = startIndex; i < endIndex; i++)
                {
                    Item item = m_items[i];

                    // Draw selected background texture
                    var font = m_styleDef.ItemFontNormal;
                    if ((item == m_preselectedMouseOver) || ((m_preselectedKeyboardIndex.HasValue) && (m_preselectedKeyboardIndex == i)))
                    {
                        MyGuiManager.DrawSpriteBatchRoundUp(m_styleDef.ItemTextureHighlight,
                            itemPosition - new Vector2(m_styleDef.DropDownHighlightExtraWidth, 0f),
                            m_selectedItemArea.Size + new Vector2(m_styleDef.DropDownHighlightExtraWidth, 0f),
                            ApplyColorMaskModifiers(Vector4.One, Enabled, transitionAlpha),
                            MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                        font = m_styleDef.ItemFontHighlight;
                    }

                    //  Draw combobox item's text
                    MyGuiManager.DrawString(font,
                        item.Value,
                        itemPosition,
                        m_textScaleWithLanguage,
                        ApplyColorMaskModifiers(m_textColor, Enabled, transitionAlpha),
                        MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                    itemPosition.Y += ITEM_HEIGHT;
                }
            }
        }

        private void DrawSelectedItemText(float transitionAlpha)
        {
            Debug.Assert(m_selected != null);

            var topLeft       = GetPositionAbsoluteTopLeft();
            var scissor       = m_selectedItemArea;
            scissor.Position += topLeft;
            using (MyGuiManager.UsingScissorRectangle(ref scissor))
            {
                var textPos = topLeft + m_selectedItemArea.Position + new Vector2(0f, m_selectedItemArea.Size.Y * 0.5f);
                MyGuiManager.DrawString(m_selectedItemFont,
                                        m_selected.Value,
                                        textPos,
                                        m_textScaleWithLanguage,
                                        ApplyColorMaskModifiers(m_textColor, Enabled, transitionAlpha),
                                        MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  Scroll Bar logic code
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void InitializeScrollBarParameters()
        {
            //  Misc
            m_showScrollBar = false;

            //  Scroll bar size related - this is exact pixel size, so that scrollbar always stays drawn same
            Vector2 scrollbarSize = MyGuiConstants.COMBOBOX_VSCROLLBAR_SIZE;
            m_scrollBarWidth = scrollbarSize.X;
            m_scrollBarHeight = scrollbarSize.Y;

            //  Scroll bar position and range related
            m_scrollBarCurrentPosition = 0;
            m_scrollBarEndPositionRelative = m_openAreaItemsCount * m_comboboxItemDeltaHeight + m_styleDef.DropDownTexture.LeftBottom.SizeGui.Y;
            //  Display items range index related
            m_displayItemsEndIndex = m_openAreaItemsCount;
        }

        void AdjustScrollBarParameters()
        {
            m_showScrollBar = m_items.Count > m_openAreaItemsCount;
            if (m_showScrollBar == true)
            {
                m_maxScrollBarPosition = m_scrollBarEndPositionRelative - m_scrollBarHeight;
                m_scrollBarItemOffSet = m_items.Count - m_openAreaItemsCount;
            }
        }

        void CalculateStartAndEndDisplayItemsIndex()
        {
            m_scrollRatio = m_scrollBarCurrentPosition == 0 ? 0.0f
                                                            : m_scrollBarCurrentPosition * m_scrollBarItemOffSet / m_maxScrollBarPosition;
            m_displayItemsStartIndex = Math.Max(0, (int) Math.Floor(m_scrollRatio + 0.5));
            m_displayItemsEndIndex = Math.Min(m_items.Count, m_displayItemsStartIndex + m_openAreaItemsCount);
        }

        public void ScrollToPreSelectedItem()
        {
            if (m_preselectedKeyboardIndex.HasValue == true)
            {
                m_displayItemsStartIndex = m_preselectedKeyboardIndex.Value <= m_middleIndex ?
                    0 : m_preselectedKeyboardIndex.Value - m_middleIndex;

                m_displayItemsEndIndex = m_displayItemsStartIndex + m_openAreaItemsCount;

                if (m_displayItemsEndIndex > m_items.Count)
                {
                    m_displayItemsEndIndex = m_items.Count;
                    m_displayItemsStartIndex = m_displayItemsEndIndex - m_openAreaItemsCount;
                }
                SetScrollBarPosition(m_displayItemsStartIndex * m_maxScrollBarPosition / m_scrollBarItemOffSet);
            }
        }

        void SetScrollBarPosition(float value, bool calculateItemIndexes = true)
        {
            value = MathHelper.Clamp(value, 0, m_maxScrollBarPosition);

            if (m_scrollBarCurrentPosition != value)
            {
                m_scrollBarCurrentNonadjustedPosition = value;
                m_scrollBarCurrentPosition = value;
                if (calculateItemIndexes)
                {
                    CalculateStartAndEndDisplayItemsIndex();
                }
            }
        }

        protected Vector2 GetItemSize()
        {
            return MyGuiConstants.COMBOBOX_MEDIUM_ELEMENT_SIZE;
        }

        public override bool CheckMouseOver()
        {
            if (m_isOpen)
            {
                int count = (m_showScrollBar) ? m_openAreaItemsCount : m_items.Count;
                for (int i = 0; i < count; i++)
                {
                    Vector2 position = GetOpenItemPosition(i);
                    MyRectangle2D openedArea = GetOpenedArea();
                    Vector2 min = new Vector2(position.X, Math.Max(openedArea.LeftTop.Y, position.Y));
                    Vector2 max = min + new Vector2(Size.X, m_comboboxItemDeltaHeight);

                    var mousePos = MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft();
                    if ((mousePos.X >= min.X) &&
                        (mousePos.X <= max.X) &&
                        (mousePos.Y >= min.Y) &&
                        (mousePos.Y <= max.Y))
                    {
                        return true;
                    }
                }
            }

            if (m_scrollBarDragging) return false;

            return CheckMouseOver(Size, GetPositionAbsolute(), OriginAlign);
        }

        private void SnapCursorToControl(int controlIndex)
        {          
            var position = GetOpenItemPosition(controlIndex);
            var startItemPosition = GetOpenItemPosition(m_displayItemsStartIndex);
            var offset = position - startItemPosition;
            var area = GetOpenedArea();
            var comboBoxCenter = GetPositionAbsoluteCenter();
            comboBoxCenter.Y += area.LeftTop.Y;
            comboBoxCenter.Y += offset.Y;
            var itemPosition = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(comboBoxCenter);

            m_preselectedMouseOver = m_items[controlIndex];
            MyInput.Static.SetMousePosition((int)itemPosition.X, (int)itemPosition.Y);
        }

        private bool IsMouseOverSelectedItem()
        {
            Vector2 position = GetPositionAbsoluteCenterLeft();
            Vector2 topLeft = position - new Vector2(0f, Size.Y / 2f);
            Vector2 bottomRight = topLeft + Size;

            return ((MyGuiManager.MouseCursorPosition.X >= topLeft.X) && (MyGuiManager.MouseCursorPosition.X <= bottomRight.X) && (MyGuiManager.MouseCursorPosition.Y >= topLeft.Y) && (MyGuiManager.MouseCursorPosition.Y <= bottomRight.Y));
        }

        public override void ShowToolTip()
        {
            MyToolTips tempTooltip = m_toolTip;
            if (m_isOpen && IsMouseOverOnOpenedArea() && m_preselectedMouseOver != null && m_preselectedMouseOver.ToolTip != null)
            {
                m_toolTip = m_preselectedMouseOver.ToolTip;
            }            
            base.ShowToolTip();
            m_toolTip = tempTooltip;
        }

        public void ApplyStyle(StyleDefinition style)
        {
            if (style != null)
            {
                m_styleDef = style;
                RefreshInternals();
            }
        }
    }
}
