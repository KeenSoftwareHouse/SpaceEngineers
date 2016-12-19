using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlListbox))]
    public class MyGuiControlListbox : MyGuiControlBase
    {
        #region Styles
        public class StyleDefinition
        {
            public string ItemFontHighlight;
            public string ItemFontNormal;
            public Vector2 ItemSize;
            public string ItemTextureHighlight;

            public Vector2 ItemsOffset;
            /// <summary>
            /// Offset of the text from left border.
            /// </summary>
            public float TextOffset;
            public bool DrawScroll;
            public bool PriorityCaptureInput;
            public bool XSizeVariable;
            public float TextScale;
            public MyGuiCompositeTexture Texture;
            public MyGuiBorderThickness ScrollbarMargin;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlListbox()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlListboxStyleEnum>() + 1];
            SetupStyles();
        }

        private static void SetupStyles()
        {
            m_styles[(int)MyGuiControlListboxStyleEnum.Default] = new StyleDefinition()
            {
                Texture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ItemTextureHighlight = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemSize = new Vector2(0.25f, 0.04f),
                TextScale = MyGuiConstants.DEFAULT_TEXT_SCALE,
                TextOffset = 0.006f,
                ItemsOffset = new Vector2(6f, 2f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                DrawScroll = true,
                PriorityCaptureInput = false,
                XSizeVariable = false,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };

            m_styles[(int)MyGuiControlListboxStyleEnum.ContextMenu] = new StyleDefinition()
            {
                Texture = MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL,
                ItemTextureHighlight = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemSize = new Vector2(0.25f, 0.035f),
                TextScale = MyGuiConstants.DEFAULT_TEXT_SCALE,
                TextOffset = 0.004f,
                ItemsOffset = new Vector2(6f, 2f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                DrawScroll = true,
                PriorityCaptureInput = true,
                XSizeVariable = true,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left = 0f,
                    Right = 0f,
                    Top = 0f,
                    Bottom = 0f,
                },
            };

            m_styles[(int)MyGuiControlListboxStyleEnum.Blueprints] = new StyleDefinition()
            {
                Texture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ItemTextureHighlight = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemSize = new Vector2(0.25f, 0.035f),
                TextScale = 0.8f,
                TextOffset = 0.006f,
                ItemsOffset = new Vector2(6f, 2f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                DrawScroll = true,
                PriorityCaptureInput = false,
                XSizeVariable = false,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };

            m_styles[(int)MyGuiControlListboxStyleEnum.ToolsBlocks] = new StyleDefinition()
            {
                Texture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST_TOOLS_BLOCKS,
                ItemTextureHighlight = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemSize = new Vector2(0.15f, 0.0272f),
                TextScale = 0.78f,
                TextOffset = 0.006f,
                ItemsOffset = new Vector2(6f, 6f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                DrawScroll = true,
                PriorityCaptureInput = false,
                XSizeVariable = false,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };

            m_styles[(int)MyGuiControlListboxStyleEnum.Terminal] = new StyleDefinition()
            {
                Texture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ItemTextureHighlight = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemSize = new Vector2(0.21f, 0.025f),
                TextScale = 0.8f,
                TextOffset = 0.006f,
                ItemsOffset = new Vector2(6f, 2f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                DrawScroll = true,
                PriorityCaptureInput = false,
                XSizeVariable = false,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };
            m_styles[(int)MyGuiControlListboxStyleEnum.IngameScipts] = new StyleDefinition()
            {
                Texture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ItemTextureHighlight = @"Textures\GUI\Controls\item_highlight_dark.dds",
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemSize = new Vector2(0.24f, 0.035f),
                TextScale = 0.8f,
                TextOffset = 0.006f,
                ItemsOffset = new Vector2(6f, 2f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                DrawScroll = true,
                PriorityCaptureInput = false,
                XSizeVariable = false,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlListboxStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion Styles

        public class Item
        {
            public event Action OnVisibleChanged;
            public readonly StringBuilder Text;
            public readonly string Icon;
            public readonly MyToolTips ToolTip;
            public readonly object UserData;
            public string FontOverride;
            public Vector4 ColorMask = Vector4.One;
            public bool Visible {
                get 
                {
                    return m_visible;
                }
                set
                {
                    if (m_visible != value)
                    {
                        m_visible = value;
                        if (OnVisibleChanged != null)
                            OnVisibleChanged();
                    }
                }
            }
            private bool m_visible;

            /// <summary>
            /// Do not construct directly. Use AddItem on listbox for that.
            /// </summary>
            public Item(StringBuilder text = null, String toolTip = null, string icon = null, object userData = null, string fontOverride = null)
            {
                Text         = new StringBuilder((text != null) ? text.ToString() : "");
                ToolTip      = (toolTip != null) ? new MyToolTips(toolTip) : null;
                Icon         = icon;
                UserData     = userData;
                FontOverride = fontOverride;
                Visible      = true;
            }
            //same as above, but designed to have realtime updates of text inside. So no copy of text :-)
            public Item(ref StringBuilder text, String toolTip = null, string icon = null, object userData = null, string fontOverride = null)
            {
                Text = text;
                ToolTip = (toolTip != null) ? new MyToolTips(toolTip) : null;
                Icon = icon;
                UserData = userData;
                FontOverride = fontOverride;
                Visible = true;
            }
        }

        #region Private fields
        private Vector2 m_doubleClickFirstPosition;
        private int? m_doubleClickStarted;
        private RectangleF m_itemsRectangle;
        private Item m_mouseOverItem;
        private StyleDefinition m_styleDef;
        private StyleDefinition m_customStyle;
        private bool m_useCustomStyle;
        private MyVScrollbar m_scrollBar;
        private int m_visibleRowIndexOffset;
        #endregion

        #region Properties
        public readonly ObservableCollection<Item> Items;

        public List<Item> SelectedItems = new List<Item>();

        public Item MouseOverItem
        {
            get 
            {
                return m_mouseOverItem;    
            }
        }

        public Vector2 ItemSize
        {
            get;
            set;
        }

        public float TextScale
        {
            get;
            private set;
        }

        public int VisibleRowsCount
        {
            get { return m_visibleRows; }
            set
            {
                m_visibleRows = value;
                RefreshInternals();
            }
        }
        private int m_visibleRows;

        public int FirstVisibleRow
        {
            get
            {
                return m_visibleRowIndexOffset;
            }
            set
            {
                Debug.Assert(value >= 0, "Index should be positive!");
                Debug.Assert(value < Items.Count, "Index should be in range!");

                m_scrollBar.Value = value;
            }
        }

        public MyGuiControlListboxStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlListboxStyleEnum m_visualStyle;

        public bool MultiSelect;
        #endregion

        #region Events
        public event Action<MyGuiControlListbox> ItemClicked;
        public event Action<MyGuiControlListbox> ItemDoubleClicked;
        public event Action<MyGuiControlListbox> ItemsSelected;
        public event Action<MyGuiControlListbox> ItemMouseOver;
        #endregion

        #region Construction & serialization
        public MyGuiControlListbox() : this(position: null) { }

        public MyGuiControlListbox(
            Vector2? position = null,
            MyGuiControlListboxStyleEnum visualStyle = MyGuiControlListboxStyleEnum.Default)
            : base( position: position,
                    isActiveControl: true,
                    canHaveFocus: true)
        {
            SetupStyles();
            m_scrollBar = new MyVScrollbar(this);
            m_scrollBar.ValueChanged += verticalScrollBar_ValueChanged;

            Items = new ObservableCollection<Item>();
            Items.CollectionChanged += Items_CollectionChanged;

            VisualStyle = visualStyle;

            Name = "Listbox";

            MultiSelect = true;
        }

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            MyObjectBuilder_GuiControlListbox listboxOb = (MyObjectBuilder_GuiControlListbox)objectBuilder;

            VisibleRowsCount = listboxOb.VisibleRows;
            VisualStyle = listboxOb.VisualStyle;
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            MyObjectBuilder_GuiControlListbox listboxOb = (MyObjectBuilder_GuiControlListbox)base.GetObjectBuilder();

            listboxOb.VisibleRows = VisibleRowsCount;
            listboxOb.VisualStyle = VisualStyle;

            return listboxOb;
        }

        #endregion

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase captureInput = base.HandleInput();
            if (captureInput != null)
                return captureInput;

            if (!Enabled || !IsMouseOver)
                return null;

            if (m_scrollBar != null &&
                m_scrollBar.HandleInput())

            if(m_styleDef.PriorityCaptureInput)
                captureInput = this;

            // Handle mouse
            HandleNewMousePress( ref captureInput);
            var mousePos = MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft();
            if (m_itemsRectangle.Contains(mousePos))
            {
                var idx = ComputeIndexFromPosition(mousePos);
                m_mouseOverItem = IsValidIndex(idx) ? Items[idx] : null;

                if (ItemMouseOver != null)
                {
                    ItemMouseOver(this);
                }

                if (m_styleDef.PriorityCaptureInput)
                    captureInput = this;
            }
            else
                m_mouseOverItem = null;

            if (m_doubleClickStarted.HasValue && (MyGuiManager.TotalTimeInMilliseconds - m_doubleClickStarted.Value) >= MyGuiConstants.DOUBLE_CLICK_DELAY)
                m_doubleClickStarted = null;

            return captureInput;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            Debug.Assert(m_visibleRowIndexOffset >= 0);
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
            var positionTopLeft = GetPositionAbsoluteTopLeft();

            m_styleDef.Texture.Draw(positionTopLeft, Size, ApplyColorMaskModifiers(ColorMask, Enabled, backgroundTransitionAlpha));

            var position = positionTopLeft + new Vector2(m_itemsRectangle.X, m_itemsRectangle.Y);
            int index = m_visibleRowIndexOffset;

            //if at least one item has an icon, draw it with spacing to all of them (alighment)
            Vector2 iconSize = Vector2.Zero;
            Vector2 iconOffset = Vector2.Zero;
            if (ShouldDrawIconSpacing())
            {
                iconSize = MyGuiConstants.LISTBOX_ICON_SIZE;
                iconOffset = MyGuiConstants.LISTBOX_ICON_OFFSET;
            }

            for (int i = 0; i < VisibleRowsCount; ++i)
            {
                
                var idx = i + m_visibleRowIndexOffset;
                if (idx >= Items.Count)
                    break;
                
                Debug.Assert(idx >= 0);
                if (idx < 0)
                    continue;
                while (index < Items.Count && !Items[index].Visible)
                    index++;
                if (index >= Items.Count)
                    break;
                var item = Items[index];
                index++;
                if (item != null)
                {
                    Color color     = ApplyColorMaskModifiers(item.ColorMask * ColorMask, Enabled, transitionAlpha);
                    bool isHighlit  = SelectedItems.Contains(item) || item == m_mouseOverItem;
                    string font = item.FontOverride ?? (isHighlit ? m_styleDef.ItemFontHighlight : m_styleDef.ItemFontNormal);

                    
                    if (isHighlit)
                    {
                        MyGuiManager.DrawSpriteBatch(m_styleDef.ItemTextureHighlight, position, ItemSize, color, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                    }

                    if (!String.IsNullOrEmpty(item.Icon))
                    {
                        MyGuiManager.DrawSpriteBatch(
                           texture: item.Icon,
                           normalizedCoord: position + iconOffset,
                           normalizedSize: iconSize,
                           color: color,
                           drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
                       );
                    }

                    MyGuiManager.DrawString(font, item.Text,
                        position + new Vector2(iconSize.X + 2 * iconOffset.X, 0) + new Vector2(m_styleDef.TextOffset, 0f),
                        TextScale, color, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, maxTextWidth: ItemSize.X - iconSize.X - 2 * iconOffset.X);
                }
                position.Y += ItemSize.Y;
            }

            //DebugDraw();

            if (m_styleDef.DrawScroll)
                m_scrollBar.Draw(ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha));
        }

        private Boolean ShouldDrawIconSpacing()
        {
            int index = m_visibleRowIndexOffset;
            for (int i = 0; i < VisibleRowsCount; ++i)
            {
                
                var idx = i + m_visibleRowIndexOffset;
                if (idx >= Items.Count)
                    break;
                
                Debug.Assert(idx >= 0);
                if (idx < 0)
                    continue;
                
                while (index < Items.Count && !Items[index].Visible)
                    index++;
                if (index >= Items.Count)
                    break;
                var item = Items[index];
                index++;
                if (item != null && !String.IsNullOrEmpty(item.Icon))
                {
                    return true;
                }
           }
            return false;
        }

        public override void ShowToolTip()
        {
            // if listbox supports icons and mouse is over any item, then show item's value in tooltip
            if (m_mouseOverItem != null && m_mouseOverItem.ToolTip != null && m_mouseOverItem.ToolTip.ToolTips.Count > 0)
                m_toolTip = m_mouseOverItem.ToolTip;
            else
                m_toolTip = null;

            base.ShowToolTip();
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();
            RefreshInternals();
        }

        protected override void OnOriginAlignChanged()
        {
            base.OnOriginAlignChanged();
            RefreshInternals();
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            m_scrollBar.HasHighlight = this.HasHighlight;
            m_mouseOverItem = null;
        }

        public override void OnRemoving()
        {
            Items.CollectionChanged -= Items_CollectionChanged;
            Items.Clear();
            ItemClicked = null;
            ItemDoubleClicked = null;
            ItemsSelected = null;
            base.OnRemoving();
        }

        public void Remove(Predicate<Item> match)
        {
            int foundIndex = Items.FindIndex(match);
            Debug.Assert(foundIndex != -1);
            if (foundIndex == -1)
                return;
            Items.RemoveAt(foundIndex);
        }

        public void Add(Item item, int? position = null)
        {
            item.OnVisibleChanged += item_OnVisibleChanged;
            if (position.HasValue)
                Items.Insert(position.Value, item);
            else
                Items.Add(item);
        }

        void item_OnVisibleChanged()
        {
            RefreshScrollBar();
        }

        #region Private helpers

        private int ComputeIndexFromPosition(Vector2 position)
        {
            int row  = (int)((position.Y - m_itemsRectangle.Position.Y) / ItemSize.Y);
            row++;
            int visibleRows = 0;
            for (int i = m_visibleRowIndexOffset; i < Items.Count; i++)
            {
                if (Items[i].Visible)
                    visibleRows++;
                if (visibleRows == row)
                    return i;
            }
            return -1;
        }

        private void DebugDraw()
        {
            MyGuiManager.DrawBorders(GetPositionAbsoluteTopLeft() + m_itemsRectangle.Position, m_itemsRectangle.Size, Color.White, 1);
            m_scrollBar.DebugDraw();
        }

        private void HandleNewMousePress(ref MyGuiControlBase captureInput)
        {
            var mousePos = MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft();
            bool cursorInItems = m_itemsRectangle.Contains(mousePos);

            if (MyInput.Static.IsAnyNewMouseOrJoystickPressed() && cursorInItems)
            {
                int row = ComputeIndexFromPosition(mousePos);
                if (IsValidIndex(row) && Items[row].Visible)
                {
                    if (MultiSelect && MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        if (SelectedItems.Contains(Items[row]))
                            SelectedItems.Remove(Items[row]);
                        else
                            SelectedItems.Add(Items[row]);
                    }
                    else if (MultiSelect && MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        int index = 0;
                        if (SelectedItems.Count > 0)
                            index = Items.IndexOf(SelectedItems[SelectedItems.Count - 1]);

                        do
                        {
                            index += index > row ? -1 : 1;
                            if (!IsValidIndex(index))
                                break;
                            if (!Items[index].Visible)
                                continue;
                            if (SelectedItems.Contains(Items[index]))
                                SelectedItems.Remove(Items[index]);
                            else
                                SelectedItems.Add(Items[index]);
                        } while (index != row);
                    }
                    else
                    {
                        SelectedItems.Clear();
                        SelectedItems.Add(Items[row]);
                    }
                    if (ItemsSelected != null)
                        ItemsSelected(this);

                    captureInput = this;
                    if (ItemClicked != null)
                    {
                        ItemClicked(this);
                        MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                    }
                }
            }

            if (MyInput.Static.IsNewPrimaryButtonPressed() && cursorInItems)
            {
                if (!m_doubleClickStarted.HasValue)
                {
                    int row = ComputeIndexFromPosition(mousePos);
                    if (IsValidIndex(row) && Items[row].Visible)
                    {
                        m_doubleClickStarted = MyGuiManager.TotalTimeInMilliseconds;
                        m_doubleClickFirstPosition = MyGuiManager.MouseCursorPosition;
                    }
                }
                else if ((MyGuiManager.TotalTimeInMilliseconds - m_doubleClickStarted.Value) <= MyGuiConstants.DOUBLE_CLICK_DELAY &&
                         (m_doubleClickFirstPosition - MyGuiManager.MouseCursorPosition).Length() <= 0.005f)
                {
                    if (ItemDoubleClicked != null)
                        ItemDoubleClicked(this);

                    m_doubleClickStarted = null;
                    captureInput = this;
                }
            }
        }

        private void RefreshVisualStyle()
        {
            if (m_useCustomStyle)
            {
                m_styleDef = m_customStyle;
            }
            else
            {
                m_styleDef = GetVisualStyle(VisualStyle);
            }
            ItemSize = m_styleDef.ItemSize;
            TextScale = m_styleDef.TextScale;
            RefreshInternals();
        }

        private float ComputeVariableItemWidth()
        {
            //this works on average but definitely has to be improved
            float itemStep = 0.0125f;
            int maxlen = 0;
            foreach (var item in Items)
                if (item.Text.Length > maxlen)
                    maxlen = item.Text.Length;

            return maxlen*itemStep;
        }

        public bool IsOverScrollBar()
        {
            return m_scrollBar.IsOverCaret;
        }

        private void RefreshInternals()
        {
            var minSize = m_styleDef.Texture.MinSizeGui;
            var maxSize = m_styleDef.Texture.MaxSizeGui;
            if (m_styleDef.XSizeVariable)
                ItemSize = new Vector2(ComputeVariableItemWidth(), ItemSize.Y);

            if (m_styleDef.DrawScroll && m_styleDef.XSizeVariable == false)
            {
                Size = Vector2.Clamp(new Vector2(m_styleDef.TextOffset + m_styleDef.ScrollbarMargin.HorizontalSum + m_styleDef.ItemSize.X + m_scrollBar.Size.X,
                                             minSize.Y + m_styleDef.ItemSize.Y * VisibleRowsCount),
                                  minSize, maxSize);
            }
            else
            {
                Size = Vector2.Clamp(new Vector2(m_styleDef.TextOffset + ItemSize.X,
                                             minSize.Y + ItemSize.Y * VisibleRowsCount),
                                  minSize, maxSize);
            }

            RefreshScrollBar();

            // Recompute rectangle where items are placed.
            m_itemsRectangle.X      = m_styleDef.ItemsOffset.X;
            m_itemsRectangle.Y      = m_styleDef.ItemsOffset.Y + m_styleDef.Texture.LeftTop.SizeGui.Y;
            m_itemsRectangle.Width  = ItemSize.X;
            m_itemsRectangle.Height = ItemSize.Y * VisibleRowsCount;
        }

        private void RefreshScrollBar()
        {
            int visibleItems = 0;
            foreach (var item in Items)
                if (item.Visible)
                    visibleItems++;

            m_scrollBar.Visible = visibleItems > VisibleRowsCount;
            m_scrollBar.Init((float)visibleItems, (float)VisibleRowsCount);

            var posTopRight = Size * new Vector2(0.5f, -0.5f);
            var margin = m_styleDef.ScrollbarMargin;
            var position = new Vector2(posTopRight.X - (margin.Right + m_scrollBar.Size.X),
                                       posTopRight.Y + margin.Top);
            m_scrollBar.Layout(position, Size.Y - margin.VerticalSum);
        }

        /// <summary>
        /// GR: Individual controls should reset toolbar postion if needed.
        /// Do no do in refresh of toolbar becaues it may happen often and cause bugs (autoscrolling to top every few frames when not intended)
        /// </summary>
        public void ScrollToolbarToTop()
        {
            m_scrollBar.SetPage(0);
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                // Deselect item if it was involved in the change.
                foreach (var item in e.OldItems)
                {
                    if (SelectedItems.Contains((Item)item))
                        SelectedItems.Remove((Item)item);
                }
                if (ItemsSelected != null)
                    ItemsSelected(this);
            }

            RefreshScrollBar();
        }

        private void verticalScrollBar_ValueChanged(MyScrollbar scrollbar)
        {
            int offset = (int)scrollbar.Value;
            int visibleCounter = -1;
            for(int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Visible)
                    visibleCounter++;
                if (visibleCounter == offset)
                {
                    offset = i;
                    break;
                }
            }
            m_visibleRowIndexOffset = offset;
        }

        private bool IsValidIndex(int idx)
        {
            return 0 <= idx && idx < Items.Count;
        }
        #endregion

        public void SelectAllVisible()
        {
            SelectedItems.Clear();
            foreach (var item in Items)
            {
                if (item.Visible)
                    SelectedItems.Add(item);
            }
            if (ItemsSelected != null)
                ItemsSelected(this);
        }

        public void ChangeSelection(List<bool> states)
        {
            SelectedItems.Clear();

            int i = 0;
            foreach (var item in Items)
            {
                if (i >= states.Count) break;

                if (states[i]) SelectedItems.Add(item);

                i++;
            }

            if (ItemsSelected != null)
                ItemsSelected(this);
        }

        public void ClearItems()
        {
            Items.Clear();
        }


        #region store/restore
        //this will save scrollbar position, selected items etc.
        //you can re-populate list with new data and then restore over new content
        //restore is only best effort as difference of content allows, no guarantees
        private List<Item> m_StoredSelectedItems = new List<Item>();

        private int m_StoredTopmostSelectedPosition;
        private Item m_StoredTopmostSelectedItem;

        private Item m_StoredMouseOverItem;
        private int m_StoredMouseOverPosition;

        private Item m_StoredItemOnTop;

        private float m_StoredScrollbarValue;

        public void StoreSituation()
        {
            m_StoredSelectedItems.Clear();
            m_StoredTopmostSelectedItem = null;
            m_StoredMouseOverItem = null;
            m_StoredItemOnTop = null;
            m_StoredTopmostSelectedPosition = m_visibleRows;
            foreach (Item item in SelectedItems)
            {
                m_StoredSelectedItems.Add(item);
                var pos = Items.IndexOf(SelectedItems[0]);
                if (pos<m_StoredTopmostSelectedPosition && pos>=m_visibleRowIndexOffset)
                {
                    m_StoredTopmostSelectedPosition = pos;
                    m_StoredTopmostSelectedItem = item;
                }

            }
            m_StoredMouseOverItem = m_mouseOverItem;
            int count = 0;
            if (m_mouseOverItem!=null)
                foreach (var item in Items)
                {
                    if (m_mouseOverItem == item)
                    {
                        m_StoredMouseOverPosition = count;
                        break;
                    }
                    count++;
                }

            if (FirstVisibleRow<Items.Count)
                m_StoredItemOnTop = Items[FirstVisibleRow];

            m_StoredScrollbarValue = m_scrollBar.Value;
        }

        private bool CompareItems(Item item1, Item item2, bool compareUserData, bool compareText)
        {
            if (compareUserData && compareText)
                if (item1.UserData == item2.UserData && 0 == item1.Text.CompareTo(item2.Text))
                    return true;
                else
                    return false;
            if (compareUserData)
                if (item1.UserData == item2.UserData)
                    return true;
            if (compareText)
                if (0 == item1.Text.CompareTo(item2.Text))
                    return true;
            return false;
        }

        public void RestoreSituation(bool compareUserData, bool compareText)
        {
            Debug.Assert(compareUserData||compareText,"Bad compare combination in listbox restore");
            //RESTORE SELECTION:
            SelectedItems.Clear();
            foreach (var stored in m_StoredSelectedItems)
                foreach (var item in Items)
                    if (CompareItems(stored, item, compareUserData, compareText))
                    {
                        SelectedItems.Add(item);
                        break;
                    }

            //RESTORE POSITION:
            int topmostSelectedPosition = -1;
            int topmostVisiblePosition = -1;
            //highest priority: keep position of item under mouse
            int count = 0;
            foreach (var item in Items)
            {
                if (m_StoredMouseOverItem!=null)
                    if (CompareItems(item, m_StoredMouseOverItem, compareUserData, compareText))//still exists, great!
                    {
                        m_scrollBar.Value = m_StoredScrollbarValue + count - m_StoredMouseOverPosition;
                        return;
                    }
                if (m_StoredTopmostSelectedItem != null)
                    if (CompareItems(item, m_StoredTopmostSelectedItem, compareUserData, compareText))
                        topmostSelectedPosition = count;

                if (m_StoredItemOnTop != null)
                    if (CompareItems(item, m_StoredItemOnTop, compareUserData, compareText))
                        topmostVisiblePosition = count;
                count++;
            }
            //if a selected item was in view, keep its position
            if (m_StoredTopmostSelectedPosition != m_visibleRows)
            {
                m_scrollBar.Value = m_StoredScrollbarValue + topmostSelectedPosition - m_StoredTopmostSelectedPosition;
                return;
            }
            //keep item on top of visible area 
            if (topmostVisiblePosition!=-1)
            {
                m_scrollBar.Value = topmostVisiblePosition;
                return;
            }

            //no bright idea anymore, just set the scrollbar and hope for the best
            m_scrollBar.Value = m_StoredScrollbarValue;
            return;
        }
        #endregion

        public void ApplyStyle(StyleDefinition style)
        {
            m_useCustomStyle = true;
            m_customStyle = style;
            RefreshVisualStyle();
        }
    }
}
