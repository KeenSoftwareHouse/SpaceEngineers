using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlGrid))]
    public class MyGuiControlGrid : MyGuiControlBase
    {
        #region Styles
        public class StyleDefinition
        {
            public MyGuiCompositeTexture BackgroundTexture;
            public Vector2 BackgroundPaddingSize;
            public string ItemFontHighlight;
            public string ItemFontNormal;
            public MyGuiBorderThickness ItemMargin; // outer margin of each item used when spacing items
            public MyGuiBorderThickness ItemPadding; // internal padding of each item used when drawing text
            public MyGuiBorderThickness ContentPadding; // internal padding surrounding items
            public MyGuiHighlightTexture ItemTexture;
            public float ItemTextScale = MyGuiConstants.DEFAULT_TEXT_SCALE;
            public Vector2? SizeOverride;
            public bool FitSizeToItems;
            public bool BorderEnabled;
            public Vector4 BorderColor = Vector4.One;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlGrid()
        {
            var itemPadding = new MyGuiBorderThickness(horizontal: 4f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                                                       vertical: 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
            var itemMargin = new MyGuiBorderThickness(horizontal: 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                                                      vertical: 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);

            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlGridStyleEnum>() + 1];
            m_styles[(int)MyGuiControlGridStyleEnum.Default] = new StyleDefinition()
            {
                BackgroundTexture = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture(MyGuiConstants.TEXTURE_SCREEN_BACKGROUND) },
                BackgroundPaddingSize = MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.PaddingSizeGui,
                ItemTexture = MyGuiConstants.TEXTURE_GRID_ITEM,
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemPadding = itemPadding,
            };
            m_styles[(int)MyGuiControlGridStyleEnum.Toolbar] = new StyleDefinition()
            {
                ItemTexture = MyGuiConstants.TEXTURE_GRID_ITEM,
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                SizeOverride = MyGuiConstants.TEXTURE_GRID_ITEM.SizeGui * new Vector2(10, 1),
                ItemMargin = itemMargin,
                ItemPadding = itemPadding,
                ItemTextScale = MyGuiConstants.DEFAULT_TEXT_SCALE * 0.75f,
                FitSizeToItems = true,
            };
            m_styles[(int)MyGuiControlGridStyleEnum.ToolsBlocks] = new StyleDefinition()
            {
                BackgroundTexture = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture(MyGuiConstants.TEXTURE_SCREEN_TOOLS_BACKGROUND_BLOCKS) },
                BackgroundPaddingSize = MyGuiConstants.TEXTURE_SCREEN_TOOLS_BACKGROUND_BLOCKS.PaddingSizeGui,
                ItemTexture = MyGuiConstants.TEXTURE_GRID_ITEM,
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemMargin = itemMargin,
                ItemPadding = itemPadding,
            };
            m_styles[(int)MyGuiControlGridStyleEnum.ToolsWeapons] = new StyleDefinition()
            {
                BackgroundTexture = new MyGuiCompositeTexture() { LeftTop = new MyGuiSizedTexture(MyGuiConstants.TEXTURE_SCREEN_TOOLS_BACKGROUND_WEAPONS) },
                BackgroundPaddingSize = MyGuiConstants.TEXTURE_SCREEN_TOOLS_BACKGROUND_WEAPONS.PaddingSizeGui,
                ItemTexture = MyGuiConstants.TEXTURE_GRID_ITEM,
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemMargin = itemMargin,
                ItemPadding = itemPadding,
                FitSizeToItems = true,
            };
            m_styles[(int)MyGuiControlGridStyleEnum.Inventory] = new StyleDefinition()
            {
                ItemTexture = MyGuiConstants.TEXTURE_GRID_ITEM,
                ItemFontNormal = MyFontEnum.Blue,
                ItemFontHighlight = MyFontEnum.White,
                ItemMargin = itemMargin,
                ItemPadding = itemPadding,
                SizeOverride = new Vector2(593f, 91f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                ItemTextScale = MyGuiConstants.DEFAULT_TEXT_SCALE * 0.8f,
                BorderEnabled = true,
                BorderColor = new Vector4(0.37f, 0.58f, 0.68f, 1f),
                FitSizeToItems = true,
                ContentPadding = new MyGuiBorderThickness(horizontal: 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                                                             vertical: 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlGridStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion Styles

        public struct EventArgs
        {
            public int RowIndex;
            public int ColumnIndex;
            public int ItemIndex;
            public MySharedButtonsEnum Button;
        }

        public struct ColoredIcon
        {
            public string Icon;
            public Vector4 Color;

            public ColoredIcon(string icon, Vector4 color)
            {
                Icon = icon;
                Color = color;
            }
        }

        public class Item
        {
            public readonly Dictionary<MyGuiDrawAlignEnum, StringBuilder> TextsByAlign;
            public readonly Dictionary<MyGuiDrawAlignEnum, ColoredIcon> IconsByAlign;
            public string[] Icons;
            public string SubIcon;
            public MyToolTips ToolTip;
            public object UserData;
            public bool Enabled;
            public float OverlayPercent; // little hack to allow progress-like filling
            public Vector4 IconColorMask;
            public Vector4 OverlayColorMask;
            public long blinkCount = 0;
            public const int MILISSECONDS_TO_BLINK = 400;


            public Item(string icon = null,
                        string subicon = null,
                        String toolTip = null,
                        object userData = null,
                        bool enabled = true) :
                this(new string[] { icon }, subicon, (toolTip != null) ? new MyToolTips(toolTip) : null, userData, enabled)
            {
            }

            public Item(string[] icons = null,
                        string subicon = null,
                        String toolTip = null,
                        object userData = null,
                        bool enabled = true) :
                this(icons, subicon, (toolTip != null) ? new MyToolTips(toolTip) : null, userData, enabled)
            {
            }

            public Item(
                string icon = null,
                string subicon = null,
                MyToolTips toolTips = null,
                object userData = null,
                bool enabled = true)
            {
                TextsByAlign = new Dictionary<MyGuiDrawAlignEnum, StringBuilder>();
                IconsByAlign = new Dictionary<MyGuiDrawAlignEnum, ColoredIcon>();
                Icons = new string[] { icon };
                SubIcon = subicon;
                ToolTip = toolTips;
                UserData = userData;
                Enabled = enabled;
                IconColorMask = Vector4.One;
                OverlayColorMask = Vector4.One;
                blinkCount = 0;
            }

            public Item(
                string[] icons = null,
                string subicon = null,
                MyToolTips toolTips = null,
                object userData = null,
                bool enabled = true)
            {
                TextsByAlign = new Dictionary<MyGuiDrawAlignEnum, StringBuilder>();
                IconsByAlign = new Dictionary<MyGuiDrawAlignEnum, ColoredIcon>();
                Icons = icons;
                SubIcon = subicon;
                ToolTip = toolTips;
                UserData = userData;
                Enabled = enabled;
                IconColorMask = Vector4.One;
                OverlayColorMask = Vector4.One;
                blinkCount = 0;
            }

            public void AddText(StringBuilder text, MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                if (!TextsByAlign.ContainsKey(textAlign))
                    TextsByAlign[textAlign] = new StringBuilder();
                if (TextsByAlign[textAlign].CompareTo(text) == 0)
                    return;
                TextsByAlign[textAlign].Clear().AppendStringBuilder(text);
            }

            public void AddText(string text, MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                if (!TextsByAlign.ContainsKey(textAlign))
                    TextsByAlign[textAlign] = new StringBuilder();
                if (TextsByAlign[textAlign].CompareTo(text) == 0)
                    return;
                TextsByAlign[textAlign].Clear().Append(text);
            }

            public void AddIcon(ColoredIcon icon, MyGuiDrawAlignEnum iconAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP)
            {
                if (!IconsByAlign.ContainsKey(iconAlign))
                    IconsByAlign.Add(iconAlign, icon);
                else
                    IconsByAlign[iconAlign] = icon;
            }

            public void ClearText(MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                TextsByAlign.Remove(textAlign);
            }

            public void ClearAllText()
            {
                TextsByAlign.Clear();
            }

            public float blinkingTransparency()
            {
                if (MyGuiManager.TotalTimeInMilliseconds - blinkCount > Item.MILISSECONDS_TO_BLINK)
                    return 1.0f;

                long index = MyGuiManager.TotalTimeInMilliseconds - blinkCount;
                //(3+cos(x))/4 converts the (-1,1) cos range to (0.5, 1). Multiplying by 4 makes it blink twice (2 times 2pi)
                return (3 + (float)Math.Cos(index * 4 * Math.PI / MILISSECONDS_TO_BLINK)) / 4;
            }

            public void startBlinking()
            {
                blinkCount = MyGuiManager.TotalTimeInMilliseconds;
            }
        }

        public const int INVALID_INDEX = -1;

        public bool EnableSelectEmptyCell { get; set; }

        #region Private fields
        private Vector2 m_doubleClickFirstPosition;
        private int? m_doubleClickStarted;

        private bool m_isItemDraggingLeft;
        private bool m_isItemDraggingRight;
        private Vector2 m_mouseDragStartPosition;

        protected RectangleF m_itemsRectangle;
        protected Vector2 m_itemStep;
        private readonly List<Item> m_items;

        private MyToolTips m_emptyItemToolTip;
        //Do not raise click events immediately. Wait for double click
        private EventArgs? m_singleClickEvents;
        // Previously clicked item. Used for check, whether item released can be called
        private EventArgs? m_itemClicked;
        private int? m_lastClick = null;

        public Dictionary<int, Color> ModalItems;

        #endregion

        #region Construction & serialization
        public MyGuiControlGrid()
            : base(position: Vector2.Zero,
                    size: new Vector2(0.05f, 0.05f),
                    colorMask: MyGuiConstants.LISTBOX_BACKGROUND_COLOR,
                    toolTip: null,
                    isActiveControl: true,
                    canHaveFocus: true)
        {
            m_items = new List<Item>();

            RefreshVisualStyle();
            RowsCount = 1;
            ColumnsCount = 1;

            base.Name = "Grid";
            base.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            EnableSelectEmptyCell = true;
        }

        public override void Init(MyObjectBuilder_GuiControlBase objectBuilder)
        {
            base.Init(objectBuilder);

            MyObjectBuilder_GuiControlGrid gridOb = (MyObjectBuilder_GuiControlGrid)objectBuilder;

            VisualStyle = gridOb.VisualStyle;
            RowsCount = gridOb.DisplayRowsCount;
            ColumnsCount = gridOb.DisplayColumnsCount;
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            MyObjectBuilder_GuiControlGrid gridOb = (MyObjectBuilder_GuiControlGrid)base.GetObjectBuilder();

            gridOb.VisualStyle = VisualStyle;
            gridOb.DisplayRowsCount = RowsCount;
            gridOb.DisplayColumnsCount = ColumnsCount;

            return gridOb;
        }
        #endregion

        #region Events
        public event Action<MyGuiControlGrid, EventArgs> ItemChanged;
        public event Action<MyGuiControlGrid, EventArgs> ItemClicked;
        public event Action<MyGuiControlGrid, EventArgs> ItemReleased;
        public event Action<MyGuiControlGrid, EventArgs> ItemClickedWithoutDoubleClick;
        public event Action<MyGuiControlGrid, EventArgs> ItemDoubleClicked;
        public event Action<MyGuiControlGrid, EventArgs> ItemDragged;
        public event Action<MyGuiControlGrid, EventArgs> ItemSelected;
        public event Action<MyGuiControlGrid, EventArgs> MouseOverIndexChanged;
        #endregion

        #region Properties

        public Vector2 ItemStep
        {
            get { return m_itemStep; }
        }

        public int ColumnsCount
        {
            get { return m_columnsCount; }
            set
            {
                if (m_columnsCount != value)
                {
                    m_columnsCount = value;
                    RefreshInternals();
                }
            }
        }
        private int m_columnsCount;

        public int RowsCount
        {
            get { return m_rowsCount; }
            set
            {
                if (m_rowsCount != value)
                {
                    m_rowsCount = value;
                    RefreshInternals();
                }
            }
        }
        private int m_rowsCount;

        public int MaxItemCount
        {
            get { return m_maxItemCount; }
            set
            {
                if (m_maxItemCount != value)
                {
                    m_maxItemCount = value;
                    RefreshInternals();
                }
            }
        }
        private int m_maxItemCount = int.MaxValue;

        public Vector2 ItemSize
        {
            get;
            private set;
        }

        private int m_mouseOverIndex;
        public int MouseOverIndex
        {
            get { return m_mouseOverIndex; }
            private set
            {
                if (value != m_mouseOverIndex)
                {
                    m_mouseOverIndex = value;
                    if (MouseOverIndexChanged != null)
                    {
                        var args = new EventArgs();
                        PrepareEventArgs(ref args, value);
                        MouseOverIndexChanged(this, args);
                    }

                }
            }
        }

        public Item MouseOverItem
        {
            get { return TryGetItemAt(MouseOverIndex); }
        }

        public int? SelectedIndex
        {
            get { return m_selectedIndex; }
            set
            {
                ProfilerShort.Begin("MyGuiControlGrid.SelectedIndex");
                try
                {
                    if (m_selectedIndex != value)
                    {
                        m_selectedIndex = value;
                        if (value.HasValue && ItemSelected != null)
                        {
                            EventArgs eventArgs;
                            MakeEventArgs(out eventArgs, value.Value, MySharedButtonsEnum.None);
                            ItemSelected(this, eventArgs);
                        }
                    }
                }
                finally
                {
                    ProfilerShort.End();
                }
            }
        }

        private int? m_selectedIndex;

        public Item SelectedItem
        {
            get { return (SelectedIndex.HasValue) ? TryGetItemAt(SelectedIndex.Value) : null; }
        }

        public MyGuiControlGridStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlGridStyleEnum m_visualStyle;
        protected StyleDefinition m_styleDef;

        private float m_itemTextScale;
        public float ItemTextScale
        {
            get { return m_itemTextScale; }
            private set
            {
                m_itemTextScale = value;
                ItemTextScaleWithLanguage = value * MyGuiManager.LanguageTextScale;
            }
        }

        private float m_itemTextScaleWithLanguage;
        public float ItemTextScaleWithLanguage
        {
            get { return m_itemTextScaleWithLanguage; }
            private set { m_itemTextScaleWithLanguage = value; }
        }

        public string EmptyItemIcon;

        public bool SelectionEnabled = true;
        public bool ShowEmptySlots = true;

        #endregion

        #region Item access
        /// <summary>
        /// Adds item to the first empty (null) position.
        /// </summary>
        public void Add(Item item, int startingRow = 0)
        {
            int emptyIdx;
            if (!TryFindEmptyIndex(out emptyIdx, startingRow))
            {
                emptyIdx = m_items.Count;
                m_items.Add(null);
            }

            m_items[emptyIdx] = item;

            if (ItemChanged != null)
            {
                var args = new EventArgs();
                PrepareEventArgs(ref args, emptyIdx);
                ItemChanged(this, args);
            }

            // Recalculate row count
            float count = (emptyIdx / this.m_columnsCount) + 1f;
            this.RowsCount = Math.Max(this.RowsCount, (int)count);
        }

        public Item GetItemAt(int index)
        {
            var valid = IsValidIndex(index);
            Debug.Assert(valid);
            if (!valid)
                index = MathHelper.Clamp(index, 0, m_items.Count - 1);
            return m_items[index];
        }

        public bool IsValidIndex(int row, int col)
        {
            return IsValidIndex(ComputeIndex(row, col));
        }

        public bool IsValidIndex(int index)
        {
            if (ModalItems != null && ModalItems.Count > 0)
            {
                if (!ModalItems.ContainsKey(index))
                {
                    return false;
                }
            }
            return 0 <= index && index < m_items.Count && index < m_maxItemCount;
        }

        public Item GetItemAt(int rowIdx, int colIdx)
        {
            Debug.Assert(IsValidIndex(ComputeIndex(rowIdx, colIdx)));
            return m_items[ComputeIndex(rowIdx, colIdx)];
        }

        public void SetItemAt(int index, Item item)
        {
            m_items[index] = item;
            if (ItemChanged != null)
            {
                var args = new EventArgs();
                PrepareEventArgs(ref args, index);
                ItemChanged(this, args);
            }

            // Recalculate row count
            float count = (index / this.m_columnsCount) + 1;
            this.RowsCount = Math.Max(this.RowsCount, (int)count);
        }

        public void SetItemAt(int rowIdx, int colIdx, Item item)
        {
            int index = ComputeIndex(rowIdx, colIdx);
            if (index < 0 || index >= m_items.Count)
                // index is out of range
                return;
            m_items[index] = item;
            if (ItemChanged != null)
            {
                var args = new EventArgs();
                PrepareEventArgs(ref args, index, rowIdx, colIdx);
                ItemChanged(this, args);
            }

            // Recalculate row count
            this.RowsCount = Math.Max(this.RowsCount, (rowIdx + 1));
        }

        public void blinkSlot(int? slot)
        {
            if (slot.HasValue)
                m_items[slot.Value].startBlinking();
        }

        /// <summary>
        /// Sets all items to default value (null). Note that this does not affect
        /// the number of items.
        /// </summary>
        public void SetItemsToDefault()
        {
            for (int i = 0; i < m_items.Count; ++i)
                m_items[i] = null;

            this.RowsCount = 0;
        }

        /// <summary>
        /// Removes all items. This affects the size of the collection.
        /// </summary>
        public virtual void Clear()
        {
            m_items.Clear();
            m_selectedIndex = null;

            this.RowsCount = 0;
        }

        /// <summary>
        /// Removes items which are null (empty) from the end. Stops as soon as first non-empty item is found.
        /// </summary>
        public void TrimEmptyItems()
        {
            int index = m_items.Count - 1;
            while (m_items.Count > 0 && m_items[index] == null)
            {
                m_items.RemoveAt(index);
                --index;
            }

            if (SelectedIndex.HasValue && !IsValidIndex(SelectedIndex.Value))
                SelectedIndex = null;

            float count = (index / this.m_columnsCount) + 1;
            this.RowsCount = Math.Max(this.RowsCount, (int)count);
        }

        public Item TryGetItemAt(int rowIdx, int colIdx)
        {
            return TryGetItemAt(ComputeIndex(rowIdx, colIdx));
        }

        public Item TryGetItemAt(int itemIdx)
        {
            return (m_items.IsValidIndex(itemIdx)) ? m_items[itemIdx] : null;
        }

        public void SelectLastItem()
        {
            SelectedIndex = (m_items.Count > 0) ? (int?)(m_items.Count - 1) : null;
        }

        public void AddRows(int numberOfRows)
        {
            if (numberOfRows <= 0 || ColumnsCount <= 0)
                return;

            // fill up row to the end
            while ((m_items.Count % ColumnsCount) != 0)
            {
                m_items.Add(null);
            }

            for (int i = 0; i < numberOfRows; i++)
            {
                // adding of one row
                for (int j = 0; j < ColumnsCount; j++)
                    m_items.Add(null);
            }
            RecalculateRowsCount();
        }

        public void RecalculateRowsCount()
        {
            float count = m_items.Count / this.m_columnsCount;
            this.RowsCount = Math.Max(this.RowsCount, (int)count);
        }

        #endregion

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
            RefreshItemsRectangle();
            DrawItemBackgrounds(backgroundTransitionAlpha);
            DrawItems(transitionAlpha);
            DrawItemTexts(transitionAlpha);
            //DebugDraw();
        }

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase captureInput = base.HandleInput();
            if (captureInput != null)
            {
                MouseOverIndex = INVALID_INDEX;
                return captureInput;
            }

            if (!Enabled)
                return captureInput;

            if (!IsMouseOver)
            {
                //User moved mouse from toolbar area. Try to trigger any pending clicks registered
                TryTriggerSingleClickEvent();
                return captureInput;
            }

            var oldMouseOverIndex = MouseOverIndex;
            MouseOverIndex = IsMouseOver ? ComputeIndex(MyGuiManager.MouseCursorPosition) : INVALID_INDEX;
            if (oldMouseOverIndex != MouseOverIndex && Enabled && MouseOverIndex != INVALID_INDEX)
            {
                MyGuiSoundManager.PlaySound(GuiSounds.MouseOver);
            }

            HandleNewMousePress(ref captureInput);
            HandleMouseDrag(ref captureInput, MySharedButtonsEnum.Primary, ref m_isItemDraggingLeft);
            HandleMouseDrag(ref captureInput, MySharedButtonsEnum.Secondary, ref m_isItemDraggingRight);

            //Handle right mouse button instantly. It cannot be double clicked
            if (m_singleClickEvents != null && m_singleClickEvents.Value.Button == MySharedButtonsEnum.Secondary)
            {
                TryTriggerSingleClickEvent();
            }

            if (m_doubleClickStarted.HasValue && (MyGuiManager.TotalTimeInMilliseconds - m_doubleClickStarted.Value) >= MyGuiConstants.DOUBLE_CLICK_DELAY)
            {
                m_doubleClickStarted = null;
                //No double click, but a click was registered
                TryTriggerSingleClickEvent();
            }

            return captureInput;
        }

        private void TryTriggerSingleClickEvent()
        {
            if (m_singleClickEvents != null)
            {
                if (ItemClickedWithoutDoubleClick != null)
                    ItemClickedWithoutDoubleClick(this, m_singleClickEvents.Value);
                m_singleClickEvents = null;
            }
        }

        public override void Update()
        {
            base.Update();
            if (!IsMouseOver)
                MouseOverIndex = INVALID_INDEX;
        }

        public override void ShowToolTip()
        {
            var oldTooltip = m_toolTip;

            var idx = ComputeIndex(MyGuiManager.MouseCursorPosition);
            if (idx != INVALID_INDEX)
            {
                var item = TryGetItemAt(idx);
                if (item != null)
                    m_toolTip = item.ToolTip;
                else
                    m_toolTip = m_emptyItemToolTip;
            }

            base.ShowToolTip();

            m_toolTip = oldTooltip;
        }

        public int ComputeIndex(int row, int col)
        {
            return row * ColumnsCount + col;
        }

        public void SetEmptyItemToolTip(String toolTip)
        {
            if (toolTip == null)
                m_emptyItemToolTip = null;
            else
                m_emptyItemToolTip = new MyToolTips(toolTip);
        }

        #region Private helpers
        private int ComputeColumn(int itemIndex)
        {
            return itemIndex % ColumnsCount;
        }

        private int ComputeIndex(Vector2 normalizedPosition)
        {
            if (!m_itemsRectangle.Contains(normalizedPosition))
                return INVALID_INDEX;

            Vector2I coords;
            coords.X = (int)((normalizedPosition.X - m_itemsRectangle.Position.X) / m_itemStep.X);
            coords.Y = (int)((normalizedPosition.Y - m_itemsRectangle.Position.Y) / m_itemStep.Y);
            int index = coords.Y * ColumnsCount + coords.X;

            if (!IsValidCellIndex(index))
                return INVALID_INDEX;

            return index;
        }

        private int ComputeRow(int itemIndex)
        {
            return itemIndex / ColumnsCount;
        }

        /// <summary>
        /// Says, whether the given index points at a cell that can possibly contain an item.
        /// The thing is, the item does not necessarily have to be there. m_items can be even smaller than a valid index (but not larger)
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        private bool IsValidCellIndex(int itemIndex)
        {
            return 0 <= itemIndex && itemIndex < m_maxItemCount;
        }

        private void DebugDraw()
        {
            MyGuiManager.DrawBorders(new Vector2(m_itemsRectangle.X, m_itemsRectangle.Y),
                                     new Vector2(m_itemsRectangle.Width, m_itemsRectangle.Height),
                                     Color.White, 1);

            if (IsValidIndex(MouseOverIndex))
            {
                var padding = m_styleDef.ItemPadding;

                var col = ComputeColumn(MouseOverIndex);
                var row = ComputeRow(MouseOverIndex);
                var positionTopLeft = m_itemsRectangle.Position + m_itemStep * new Vector2((float)col, (float)row);
                positionTopLeft += padding.TopLeftOffset;
                var size = ItemSize - padding.SizeChange;
                MyGuiManager.DrawBorders(positionTopLeft, size, Color.White, 1);
            }
        }

        private void DrawItemBackgrounds(float transitionAlpha)
        {
            var normalTexture = m_styleDef.ItemTexture.Normal;
            var highlightTexture = m_styleDef.ItemTexture.Highlight;
            int itemNumber = Math.Min(m_maxItemCount, RowsCount * ColumnsCount);
            for (int idx = 0; idx < itemNumber; ++idx)
            {
                int row = idx / ColumnsCount;
                int col = idx % ColumnsCount;

                var drawPositionTopLeft = m_itemsRectangle.Position + m_itemStep * new Vector2((float)col, (float)row);
                var item = TryGetItemAt(idx);
                bool enabled = this.Enabled && ((item != null) ? item.Enabled : true);

                bool shouldBlink = false;
                float blinkingTransparency = 1.0f;
                if (item != null)
                {
                    shouldBlink = MyGuiManager.TotalTimeInMilliseconds - item.blinkCount <= Item.MILISSECONDS_TO_BLINK;
                    if (shouldBlink)
                    {
                        blinkingTransparency = item.blinkingTransparency();
                    }
                }
                bool highlight = enabled && IsValidIndex(MouseOverIndex) && (idx == MouseOverIndex || idx == SelectedIndex || shouldBlink);

                var colorMask = ColorMask;
                if (ModalItems != null && ModalItems.Count > 0)
                {
                    if (ModalItems.ContainsKey(idx))
                    {
                        colorMask = ModalItems[idx];
                        MyGuiConstants.TEXTURE_RECTANGLE_NEUTRAL.Draw(drawPositionTopLeft, ItemSize, Color.Yellow);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (ShowEmptySlots)
                {
                    MyGuiManager.DrawSpriteBatch(
                        texture: highlight ? highlightTexture : normalTexture,
                        normalizedCoord: drawPositionTopLeft,
                        normalizedSize: ItemSize,
                        color: ApplyColorMaskModifiers(colorMask, enabled, transitionAlpha * blinkingTransparency),
                        drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                }
                else if (item != null)
                {
                    MyGuiManager.DrawSpriteBatch(
                        texture: highlight ? highlightTexture : normalTexture,
                        normalizedCoord: drawPositionTopLeft,
                        normalizedSize: ItemSize,
                        color: ApplyColorMaskModifiers(colorMask, enabled, transitionAlpha * blinkingTransparency),
                        drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                }
            }
        }


        private void DrawItems(float transitionAlpha)
        {
            int itemNumber = Math.Min(m_maxItemCount, RowsCount * ColumnsCount);
            for (int idx = 0; idx < itemNumber; ++idx)
            {
                int row = idx / ColumnsCount;
                int col = idx % ColumnsCount;

                var item = TryGetItemAt(idx);
                var drawPositionTopLeft = m_itemsRectangle.Position + m_itemStep * new Vector2((float)col, (float)row);

                var colorMask = ColorMask;

                bool itemEnabled = true;

                if (ModalItems != null && ModalItems.Count > 0)
                {
                    if (!ModalItems.ContainsKey(idx))
                        continue;
                }

                //Sets the subicon at 8/9 of the cube
                Vector2 subIconAdjust = new Vector2(8 / 9.0f, 4 / 9.0f);
                var drawPositionTopRight = m_itemsRectangle.Position + m_itemStep * (new Vector2((float)col, (float)row) + subIconAdjust);

                if (item != null && item.Icons != null)
                {
                    bool enabled = this.Enabled && item.Enabled && itemEnabled;
                    for (int i = 0; i < item.Icons.Length; i++)
                        MyGuiManager.DrawSpriteBatch(
                            texture: item.Icons[i],
                            normalizedCoord: drawPositionTopLeft,
                            normalizedSize: ItemSize,
                            color: ApplyColorMaskModifiers(colorMask * item.IconColorMask, enabled, transitionAlpha),
                            drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                            waitTillLoaded: false);
                    if (item.SubIcon != null && item.SubIcon != "")
                    {
                        MyGuiManager.DrawSpriteBatch(
                        texture: item.SubIcon,
                        normalizedCoord: drawPositionTopRight,
                        normalizedSize: ItemSize / 3,
                        color: ApplyColorMaskModifiers(colorMask * item.IconColorMask, enabled, transitionAlpha),
                        drawAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                        waitTillLoaded: false);

                    }
                    if (item.OverlayPercent != 0f)
                    {
                        MyGuiManager.DrawSpriteBatch(
                            texture: MyGuiConstants.BLANK_TEXTURE,
                            normalizedCoord: drawPositionTopLeft,
                            normalizedSize: ItemSize * new Vector2(item.OverlayPercent, 1f),
                            color: ApplyColorMaskModifiers(colorMask * item.OverlayColorMask, enabled, transitionAlpha * 0.5f),
                            drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                            waitTillLoaded: false);
                    }

                }
                else if (EmptyItemIcon != null)
                {
                    bool enabled = this.Enabled && itemEnabled;
                    MyGuiManager.DrawSpriteBatch(
                        texture: EmptyItemIcon,
                        normalizedCoord: drawPositionTopLeft,
                        normalizedSize: ItemSize,
                        color: ApplyColorMaskModifiers(ColorMask, enabled, transitionAlpha),
                        drawAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                        waitTillLoaded: true);
                }

                if (item != null)
                {
                    foreach (var alignIcon in item.IconsByAlign)
                    {
                        if (string.IsNullOrEmpty(alignIcon.Value.Icon))
                            continue;
                        MyGuiManager.DrawSpriteBatch(
                        texture: alignIcon.Value.Icon,
                        normalizedCoord: drawPositionTopLeft + m_itemStep * new Vector2(0.5f / 9.0f, 1 / 9.0f),
                        normalizedSize: ItemSize / 3,
                        color: ApplyColorMaskModifiers(alignIcon.Value.Color, true, transitionAlpha),
                        drawAlign: alignIcon.Key,
                        waitTillLoaded: true);
                    }
                }
            }
        }

        private void DrawItemTexts(float transitionAlpha)
        {
            var padding = m_styleDef.ItemPadding;
            var normalFont = m_styleDef.ItemFontNormal;
            var highlightFont = m_styleDef.ItemFontHighlight;

            int itemNumber = Math.Min(m_maxItemCount, RowsCount * ColumnsCount);
            for (int idx = 0; idx < itemNumber; ++idx)
            {
                int row = idx / ColumnsCount;
                int col = idx % ColumnsCount;

                var item = TryGetItemAt(idx);
                if (item != null)
                {
                    foreach (var entry in item.TextsByAlign)
                    {
                        var drawPositionTopLeft = m_itemsRectangle.Position + m_itemStep * new Vector2((float)col, (float)row);
                        var paddedRect = new RectangleF(drawPositionTopLeft + padding.TopLeftOffset,
                                                        ItemSize - padding.SizeChange);
                        var drawPos = MyUtils.GetCoordAlignedFromRectangle(ref paddedRect, entry.Key);
                        bool enabled = this.Enabled && item.Enabled;
                        MyGuiManager.DrawString(
                            font: (idx == MouseOverIndex || idx == SelectedIndex) ? highlightFont : normalFont,
                            text: entry.Value,
                            normalizedCoord: drawPos,
                            scale: ItemTextScaleWithLanguage,
                            colorMask: ApplyColorMaskModifiers(ColorMask, enabled, transitionAlpha),
                            drawAlign: entry.Key,
                            maxTextWidth: paddedRect.Size.X);
                    }
                }
            }
        }

        private void HandleMouseDrag(ref MyGuiControlBase captureInput, MySharedButtonsEnum button, ref bool isDragging)
        {
            if (MyInput.Static.IsNewButtonPressed(button))
            {
                isDragging = true;
                m_mouseDragStartPosition = MyGuiManager.MouseCursorPosition;
            }
            else if (MyInput.Static.IsButtonPressed(button))
            {
                if (isDragging && SelectedItem != null)
                {
                    Vector2 mouseDistanceFromLastUpdate = MyGuiManager.MouseCursorPosition - m_mouseDragStartPosition;
                    if (mouseDistanceFromLastUpdate.Length() != 0.0f)
                    {
                        if (ItemDragged != null)
                        {
                            var dragIdx = ComputeIndex(MyGuiManager.MouseCursorPosition);
                            if (IsValidIndex(dragIdx) && GetItemAt(dragIdx) != null)
                            {
                                EventArgs args;
                                MakeEventArgs(out args, dragIdx, button);
                                ItemDragged(this, args);
                            }
                        }
                        isDragging = false;
                    }
                    captureInput = this;
                }
            }
            else
            {
                isDragging = false;
            }
        }

        private void HandleNewMousePress(ref MyGuiControlBase captureInput)
        {
            bool cursorInItems = m_itemsRectangle.Contains(MyGuiManager.MouseCursorPosition);

            if (MyInput.Static.IsNewPrimaryButtonReleased() || MyInput.Static.IsNewSecondaryButtonReleased())
            {
                if (cursorInItems)
                {
                    int? mouseOverIndex = ComputeIndex(MyGuiManager.MouseCursorPosition);
                    if (!IsValidIndex(mouseOverIndex.Value))
                        mouseOverIndex = null;

                    SelectMouseOverItem(mouseOverIndex);

                    if (SelectedIndex.HasValue && m_itemClicked.HasValue && m_lastClick.HasValue && mouseOverIndex.HasValue)
                    {
                        if (MyGuiManager.TotalTimeInMilliseconds - m_lastClick.Value < MyGuiConstants.CLICK_RELEASE_DELAY && m_itemClicked.Value.ItemIndex == mouseOverIndex.Value)
                        {
                            captureInput = this;
                            MySharedButtonsEnum button = MySharedButtonsEnum.None;
                            if (MyInput.Static.IsNewPrimaryButtonReleased())
                                button = MySharedButtonsEnum.Primary;
                            else if (MyInput.Static.IsNewSecondaryButtonReleased())
                                button = MySharedButtonsEnum.Secondary;

                            EventArgs args;
                            MakeEventArgs(out args, SelectedIndex.Value, button);

                            var handler = ItemReleased;
                            if (handler != null)
                                handler(this, args);
                        }
                    }
                }
                m_itemClicked = null;
                m_lastClick = null;
            }

            if (MyInput.Static.IsAnyNewMouseOrJoystickPressed() && cursorInItems)
            {
                m_lastClick = MyGuiManager.TotalTimeInMilliseconds;

                int? mouseOverIndex = ComputeIndex(MyGuiManager.MouseCursorPosition);
                if (!IsValidIndex(mouseOverIndex.Value))
                    mouseOverIndex = null;
                SelectMouseOverItem(mouseOverIndex);

                captureInput = this;
                if (SelectedIndex.HasValue && (ItemClicked != null || ItemClickedWithoutDoubleClick != null))
                {
                    MySharedButtonsEnum button = MySharedButtonsEnum.None;
                    if (MyInput.Static.IsNewPrimaryButtonPressed())
                        button = MySharedButtonsEnum.Primary;
                    else if (MyInput.Static.IsNewSecondaryButtonPressed())
                        button = MySharedButtonsEnum.Secondary;

                    EventArgs args;
                    MakeEventArgs(out args, SelectedIndex.Value, button);
                    var handler = ItemClicked;
                    if (handler != null)
                        handler(this, args);

                    m_singleClickEvents = args;
                    m_itemClicked = args;

                    if (MyInput.Static.IsAnyCtrlKeyPressed() || MyInput.Static.IsAnyShiftKeyPressed())
                        MyGuiSoundManager.PlaySound(GuiSounds.Item);
                }
            }

            if (MyInput.Static.IsNewPrimaryButtonPressed() && cursorInItems)
            {
                if (!m_doubleClickStarted.HasValue)
                {
                    m_doubleClickStarted = MyGuiManager.TotalTimeInMilliseconds;
                    m_doubleClickFirstPosition = MyGuiManager.MouseCursorPosition;
                }
                else if ((MyGuiManager.TotalTimeInMilliseconds - m_doubleClickStarted.Value) <= MyGuiConstants.DOUBLE_CLICK_DELAY &&
                         (m_doubleClickFirstPosition - MyGuiManager.MouseCursorPosition).Length() <= 0.005f)
                {
                    if (SelectedIndex.HasValue && TryGetItemAt(SelectedIndex.Value) != null && ItemDoubleClicked != null)
                    {
                        //Cancel click event when we double click
                        m_singleClickEvents = null;

                        EventArgs args;
                        MakeEventArgs(out args, SelectedIndex.Value, MySharedButtonsEnum.Primary);
                        Debug.Assert(GetItemAt(args.ItemIndex) != null, "Double click should not be reported when clicking on empty position.");
                        ItemDoubleClicked(this, args);
                        MyGuiSoundManager.PlaySound(GuiSounds.Item);
                    }

                    m_doubleClickStarted = null;
                    captureInput = this;
                }
            }
        }

        private void SelectMouseOverItem(int? mouseOverIndex)
        {
            if (SelectionEnabled && mouseOverIndex.HasValue)
            {
                if (EnableSelectEmptyCell)
                {
                    SelectedIndex = mouseOverIndex.Value;
                }
                else if (TryGetItemAt(mouseOverIndex.Value) != null)
                {
                    SelectedIndex = mouseOverIndex.Value;
                }
            }
        }

        private void MakeEventArgs(out EventArgs args, int itemIndex, MySharedButtonsEnum button)
        {
            args.ItemIndex = itemIndex;
            args.RowIndex = ComputeRow(itemIndex);
            args.ColumnIndex = ComputeColumn(itemIndex);
            args.Button = button;
        }

        private void RefreshInternals()
        {
            if (m_styleDef.FitSizeToItems)
            {
                Size = m_styleDef.ContentPadding.SizeChange +
                       m_styleDef.ItemMargin.TopLeftOffset +
                       m_itemStep * new Vector2((float)ColumnsCount, (float)RowsCount);
            }

            var requiredItemCount = Math.Min(m_maxItemCount, RowsCount * ColumnsCount);
            Debug.Assert(requiredItemCount >= 0);
            // Make sure we have minimum number of Items.
            while (m_items.Count < requiredItemCount)
                m_items.Add(null);
            RefreshItemsRectangle();
        }

        private void RefreshItemsRectangle()
        {
            m_itemsRectangle.Position = GetPositionAbsoluteTopLeft() + m_styleDef.BackgroundPaddingSize + m_styleDef.ContentPadding.TopLeftOffset + m_styleDef.ItemMargin.TopLeftOffset;
            m_itemsRectangle.Size = m_itemStep * new Vector2((float)ColumnsCount, (float)RowsCount);
        }

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            BackgroundTexture = m_styleDef.BackgroundTexture;
            ItemSize = m_styleDef.ItemTexture.SizeGui;
            m_itemStep = ItemSize + m_styleDef.ItemMargin.MarginStep;
            ItemTextScale = m_styleDef.ItemTextScale;
            BorderEnabled = m_styleDef.BorderEnabled;
            BorderColor = m_styleDef.BorderColor;
            if (!m_styleDef.FitSizeToItems)
                Size = m_styleDef.SizeOverride ?? BackgroundTexture.MinSizeGui;
            RefreshInternals();
        }

        private void PrepareEventArgs(ref EventArgs args, int itemIndex, int? rowIdx = null, int? columnIdx = null)
        {
            args.ItemIndex = itemIndex;
            args.ColumnIndex = columnIdx ?? ComputeColumn(itemIndex);
            args.RowIndex = rowIdx ?? ComputeRow(itemIndex);
        }

        private bool TryFindEmptyIndex(out int emptyIdx, int startingRow)
        {
            for (int i = startingRow * m_columnsCount; i < m_items.Count; ++i)
                if (m_items[i] == null)
                {
                    emptyIdx = i;
                    return true;
                }
            emptyIdx = 0;
            return false;
        }

        public int GetItemsCount()
        {
            return m_items.Count;
        }

        #endregion
    }
}
