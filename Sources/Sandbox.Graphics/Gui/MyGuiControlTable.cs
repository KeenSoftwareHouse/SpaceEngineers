using Sandbox.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{

    public enum MyGuiControlTableStyleEnum
    {
        Default
    }

    public class MyGuiControlTable : MyGuiControlBase
    {
        #region Styles
        private static StyleDefinition[] m_styles;

        static MyGuiControlTable()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlTableStyleEnum>() + 1];
            m_styles[(int)MyGuiControlTableStyleEnum.Default] = new StyleDefinition()
            {
                Texture                = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,//  MyGuiConstants.TEXTURE_TABLE_BACKGROUND,
                RowTextureHighlight    = @"Textures\GUI\Controls\item_highlight_dark.dds",
                HeaderTextureHighlight = @"Textures\GUI\Controls\item_highlight_light.dds",
                RowFontNormal = MyFontEnum.Blue,
                RowFontHighlight = MyFontEnum.White,
                HeaderFontNormal = MyFontEnum.White,
                HeaderFontHighlight = MyFontEnum.White,
                TextScale              = MyGuiConstants.DEFAULT_TEXT_SCALE,
                RowHeight              = 40f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                Padding                = new MyGuiBorderThickness()
                {
                    Left = 5f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top = 5f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
                ScrollbarMargin        = new MyGuiBorderThickness()
                {
                    Left   = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right  = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top    = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 5f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlTableStyleEnum style)
        {
            return m_styles[(int)style];
        }

        public class StyleDefinition
        {
            public MyFontEnum HeaderFontHighlight;
            public MyFontEnum HeaderFontNormal;
            public string HeaderTextureHighlight;
            public MyGuiBorderThickness Padding;
            public MyFontEnum RowFontHighlight;
            public MyFontEnum RowFontNormal;
            public float RowHeight;
            public string RowTextureHighlight;
            public float TextScale;
            public MyGuiBorderThickness ScrollbarMargin;
            public MyGuiCompositeTexture Texture;
        }
        #endregion Styles

        private List<ColumnMetaData> m_columnsMetaData;
        private List<Row> m_rows;

        private Vector2 m_doubleClickFirstPosition;
        private int? m_doubleClickStarted;

        private bool m_mouseOverHeader;
        private int? m_mouseOverColumnIndex;
        private int? m_mouseOverRowIndex;

        private RectangleF m_headerArea;
        private RectangleF m_rowsArea;

        private StyleDefinition m_styleDef;
        private MyVScrollbar m_scrollBar;
        public MyVScrollbar ScrollBar { get { return m_scrollBar; } }

        /// <summary>
        /// Index computed from scrollbar.
        /// </summary>
        private int m_visibleRowIndexOffset;

        private int m_lastSortedColumnIdx;

        private float m_textScale;
        private float m_textScaleWithLanguage;

        int m_sortColumn = -1;
        SortStateEnum? m_sortColumnState = null;

        #region Properties
        private bool m_headerVisible = true;
        public bool HeaderVisible
        {
            get { return m_headerVisible; }
            set
            {
                m_headerVisible = value;
                RefreshInternals();
            }
        }

        public int ColumnsCount
        {
            get { return m_columnsCount; }
            set
            {
                m_columnsCount = value;
                RefreshInternals();
            }
        }
        private int m_columnsCount = 1;

        public int? SelectedRowIndex
        {
            get { return m_selectedRowIndex; }
            set
            {
                m_selectedRowIndex = value;
            }
        }
        private int? m_selectedRowIndex;

        public Row SelectedRow
        {
            get
            {
                if (IsValidRowIndex(SelectedRowIndex))
                    return m_rows[SelectedRowIndex.Value];

                return null;
            }

            set
            {
                int idx = m_rows.IndexOf(value);
                if (idx >= 0)
                    m_selectedRowIndex = idx;
                else
                    Debug.Fail("Row is not in table!");
            }
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
        private int m_visibleRows = 1;

        public float RowHeight
        {
            get;
            private set;
        }

        public MyGuiControlTableStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlTableStyleEnum m_visualStyle;

        public float TextScale
        {
            get { return m_textScale; }
            private set
            {
                m_textScale = value;
                TextScaleWithLanguage = value * MyGuiManager.LanguageTextScale;
            }
        }

        public float TextScaleWithLanguage
        {
            get { return m_textScaleWithLanguage; }
            private set { m_textScaleWithLanguage = value; }
        }
        #endregion

        #region Events
        public event Action<MyGuiControlTable, EventArgs> ItemDoubleClicked;

        public event Action<MyGuiControlTable, EventArgs> ItemRightClicked;

        public event Action<MyGuiControlTable, EventArgs> ItemSelected;

        public event Action<MyGuiControlTable, EventArgs> ItemConfirmed;

        public event Action<MyGuiControlTable, int> ColumnClicked;
        #endregion

        #region Construction
        public MyGuiControlTable():
            base(canHaveFocus: true)
        {
            m_scrollBar = new MyVScrollbar(this);
            m_scrollBar.ValueChanged += verticalScrollBar_ValueChanged;
            m_rows = new List<Row>();
            m_columnsMetaData = new List<ColumnMetaData>();
            VisualStyle = MyGuiControlTableStyleEnum.Default;

            base.Name = "Table";
        }
        #endregion

        public void Add(Row row)
        {
            m_rows.Add(row);
            RefreshScrollbar();
        }

        public void Insert(int index, Row row)
        {
            m_rows.Insert(index, row);
            RefreshScrollbar();
        }

        public void Clear()
        {
            m_rows.Clear();
            SelectedRowIndex = null;
            RefreshScrollbar();
        }

        public int RowsCount
        {
            get { return m_rows.Count; }
        }

        public Row GetRow(int index)
        {
            return m_rows[index];
        }

        public Row Find(Predicate<Row> match)
        {
            return m_rows.Find(match);
        }

        public int FindIndex(Predicate<Row> match)
        {
            return m_rows.FindIndex(match);
        }

        public void Remove(Predicate<Row> match)
        {
            int foundIndex = m_rows.FindIndex(match);
            //Debug.Assert(foundIndex != -1); //Assert when no match?
            if (foundIndex == -1)
                return;
            m_rows.RemoveAt(foundIndex);

            if (SelectedRowIndex.HasValue)
            {
                if (SelectedRowIndex.Value == foundIndex)
                {

                }
                else if (SelectedRowIndex.Value > foundIndex)
                {
                    SelectedRowIndex = SelectedRowIndex.Value - 1;
                }
            }

        }

        public void RemoveSelectedRow()
        {
            if (SelectedRowIndex.HasValue)
            {
                m_rows.RemoveAt(SelectedRowIndex.Value);

                if (!IsValidRowIndex(SelectedRowIndex.Value))
                    SelectedRowIndex = null;
                RefreshScrollbar();
            }
        }

        public void MoveSelectedRowUp()
        {
            if (SelectedRow != null)
            {
                if (IsValidRowIndex(SelectedRowIndex - 1))
                {
                    var tmp = m_rows[SelectedRowIndex.Value - 1];
                    m_rows[SelectedRowIndex.Value - 1] = m_rows[SelectedRowIndex.Value];
                    m_rows[SelectedRowIndex.Value] = tmp;
                    SelectedRowIndex = SelectedRowIndex - 1;
                }
            }
        }

        public void MoveSelectedRowDown()
        {
            if (SelectedRow != null)
            {
                if (IsValidRowIndex(SelectedRowIndex + 1))
                {
                    var tmp = m_rows[SelectedRowIndex.Value + 1];
                    m_rows[SelectedRowIndex.Value + 1] = m_rows[SelectedRowIndex.Value];
                    m_rows[SelectedRowIndex.Value] = tmp;
                    SelectedRowIndex = SelectedRowIndex + 1;
                }
            }
        }

        public void MoveSelectedRowTop()
        {
            if (SelectedRow != null)
            {
                var tmp = SelectedRow;
                RemoveSelectedRow();
                m_rows.Insert(0, tmp);
                SelectedRowIndex = 0;
            }
        }

        public void MoveSelectedRowBottom()
        {
            if (SelectedRow != null)
            {
                var tmp = SelectedRow;
                RemoveSelectedRow();
                m_rows.Add(tmp);
                SelectedRowIndex = RowsCount - 1;
            }
        }

        public void MoveToNextRow()
        {
            if (m_rows.Count == 0)
                return;

            if (!SelectedRowIndex.HasValue)
            {
                SelectedRowIndex = 0;
            }
            else
            {
                int nextRow = SelectedRowIndex.Value + 1;
                nextRow = Math.Min(nextRow, m_rows.Count - 1);
                if (nextRow != SelectedRowIndex.Value)
                {
                    ItemSelected(this, new EventArgs() { RowIndex = SelectedRowIndex.Value, MouseButton = MyMouseButtonsEnum.Left });
                    SelectedRowIndex = nextRow;
                }
            }
        }

        public void MoveToPreviousRow()
        {
            if (m_rows.Count == 0)
                return;

            if (!SelectedRowIndex.HasValue)
            {
                SelectedRowIndex = 0;
            }
            else
            {
                int prevRow = SelectedRowIndex.Value - 1;
                prevRow = Math.Max(prevRow, 0);
                if (prevRow != SelectedRowIndex.Value)
                {
                    ItemSelected(this, new EventArgs() { RowIndex = SelectedRowIndex.Value, MouseButton = MyMouseButtonsEnum.Left });
                    SelectedRowIndex = prevRow;
                }     
            }
        }

        public void SetColumnName(int colIdx, StringBuilder name)
        {
            Debug.Assert(colIdx < ColumnsCount);
            Debug.Assert(m_columnsMetaData.Count == ColumnsCount);
            m_columnsMetaData[colIdx].Name.Clear().AppendStringBuilder(name);
        }

        public void SetColumnComparison(int colIdx, Comparison<Cell> ascendingComparison)
        {
            Debug.Assert(colIdx < m_columnsMetaData.Count);
            m_columnsMetaData[colIdx].AscendingComparison = ascendingComparison;
        }

        /// <summary>
        /// Modifies width of each column. Note that widths are relative to the width of table (excluding slider),
        /// so they should sum up to 1. Setting widths to 0.75 and 0.25 for 2 column table will give 3/4 of size to
        /// one column and 1/4 to the second one.
        /// </summary>
        public void SetCustomColumnWidths(float[] p)
        {
            Debug.Assert(p.Length == ColumnsCount);
            for (int i = 0; i < ColumnsCount; ++i)
                m_columnsMetaData[i].Width = p[i];
        }

        public void SetColumnAlign(int colIdx, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
        {
            Debug.Assert(m_columnsMetaData.IsValidIndex(colIdx));
            Debug.Assert(m_columnsMetaData.Count == ColumnsCount);
            m_columnsMetaData[colIdx].TextAlign = align;
        }

        public void SetHeaderColumnAlign(int colIdx, MyGuiDrawAlignEnum align = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
        {
            Debug.Assert(m_columnsMetaData.IsValidIndex(colIdx));
            Debug.Assert(m_columnsMetaData.Count == ColumnsCount);
            m_columnsMetaData[colIdx].HeaderTextAlign = align;
        }

        public void ScrollToSelection()
        {
            if (SelectedRow == null)
                return;

            int selectedIdx = SelectedRowIndex.Value;

            if (selectedIdx > (m_visibleRowIndexOffset + VisibleRowsCount))
                m_scrollBar.Value = (selectedIdx - VisibleRowsCount + 1);

            if(selectedIdx < m_visibleRowIndexOffset)
                m_scrollBar.Value = selectedIdx;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            var position = GetPositionAbsoluteTopLeft();
            float height = RowHeight * (VisibleRowsCount + 1); // One row is taken up by the header of the table.
            m_styleDef.Texture.Draw(position, Size,
                ApplyColorMaskModifiers(ColorMask, Enabled, backgroundTransitionAlpha));

            if (HeaderVisible)
                DrawHeader(transitionAlpha);
            DrawRows(transitionAlpha);

            m_scrollBar.Draw(ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha));

            //DebugDraw();
        }

        public override MyGuiControlBase HandleInput()
        {
            var captureControl = base.HandleInput();

            if (captureControl != null)
                return captureControl;

            if (!Enabled)
                return null;

            if (m_scrollBar != null &&
                m_scrollBar.HandleInput())
                captureControl = this;

            HandleMouseOver();

            HandleNewMousePress(ref captureControl);

            if (m_doubleClickStarted.HasValue &&
                (MyGuiManager.TotalTimeInMilliseconds - m_doubleClickStarted.Value) >= MyGuiConstants.DOUBLE_CLICK_DELAY)
                m_doubleClickStarted = null;

            if (!HasFocus)
                return captureControl;

            if (SelectedRowIndex.HasValue && MyInput.Static.IsNewKeyPressed(MyKeys.Enter) && ItemConfirmed != null)
            {
                captureControl = this;
                ItemConfirmed(this, new EventArgs() { RowIndex = SelectedRowIndex.Value });
            }

            return captureControl;
        }

        public override void Update()
        {
            base.Update();
            if (!IsMouseOver)
            {
                m_mouseOverColumnIndex = null;
                m_mouseOverRowIndex = null;
                m_mouseOverHeader = false;
            }
        }

        protected override void OnOriginAlignChanged()
        {
            base.OnOriginAlignChanged();
            RefreshInternals();
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();
            RefreshInternals();
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            m_scrollBar.HasHighlight = this.HasHighlight;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshInternals();
        }

        public override void ShowToolTip()
        {
            var oldTooltip = m_toolTip;

            if (m_mouseOverRowIndex.HasValue && m_rows.IsValidIndex(m_mouseOverRowIndex.Value))
            {
                var row = m_rows[m_mouseOverRowIndex.Value];
                if (row.Cells.IsValidIndex(m_mouseOverColumnIndex.Value))
                {
                    var cell = row.Cells[m_mouseOverColumnIndex.Value];
                    if (cell.ToolTip != null)
                        m_toolTip = cell.ToolTip;
                }
            }

            base.ShowToolTip();

            m_toolTip = oldTooltip;
        }

        private int ComputeColumnIndexFromPosition(Vector2 normalizedPosition)
        {
            normalizedPosition -= GetPositionAbsoluteTopLeft();
            float relativePos = (normalizedPosition.X - m_rowsArea.Position.X) / m_rowsArea.Size.X;
            int idx = 0;
            for (; idx < m_columnsMetaData.Count; ++idx)
            {
                if (relativePos < m_columnsMetaData[idx].Width)
                    break;
                relativePos -= m_columnsMetaData[idx].Width;
            }

            return idx;
        }

        private int ComputeRowIndexFromPosition(Vector2 normalizedPosition)
        {
            normalizedPosition -= GetPositionAbsoluteTopLeft();
            return (int)((normalizedPosition.Y - m_rowsArea.Position.Y) / RowHeight) + m_visibleRowIndexOffset;
        }

        private void DebugDraw()
        {
            var topLeft = GetPositionAbsoluteTopLeft();
            MyGuiManager.DrawBorders(topLeft + m_headerArea.Position, m_headerArea.Size, Color.Cyan, 1);
            MyGuiManager.DrawBorders(topLeft + m_rowsArea.Position, m_rowsArea.Size, Color.White, 1);

            var position = topLeft + m_headerArea.Position;
            for (int i = 0; i < m_columnsMetaData.Count; ++i)
            {
                var meta = m_columnsMetaData[i];
                var cellSize = new Vector2(meta.Width * m_rowsArea.Size.X, m_headerArea.Height);
                MyGuiManager.DrawBorders(position, cellSize, Color.Yellow, 1);
                position.X += meta.Width * m_headerArea.Width;
            }
            m_scrollBar.DebugDraw();
        }

        private void DrawHeader(float transitionAlpha)
        {
            var topLeft = GetPositionAbsoluteTopLeft();
            var position = topLeft + m_headerArea.Position;
            MyGuiManager.DrawSpriteBatch(m_styleDef.HeaderTextureHighlight, position, m_headerArea.Size, Color.White, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            for (int i = 0; i < m_columnsMetaData.Count; ++i)
            {
                var meta = m_columnsMetaData[i];
                var font = m_styleDef.HeaderFontNormal;
                if (m_mouseOverColumnIndex.HasValue && m_mouseOverColumnIndex.Value == i)
                    font = m_styleDef.HeaderFontHighlight;

                var cellSize = new Vector2(meta.Width * m_rowsArea.Size.X, m_headerArea.Height);

                var textPos = MyUtils.GetCoordAlignedFromCenter(position + 0.5f * cellSize, cellSize, meta.HeaderTextAlign);

                MyGuiManager.DrawString(font, meta.Name, textPos,
                    TextScaleWithLanguage,
                    ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                    meta.HeaderTextAlign,
                    maxTextWidth: cellSize.X);
                position.X += meta.Width * m_headerArea.Width;
            }
        }

        private void DrawRows(float transitionAlpha)
        {
            var positionTopLeft = GetPositionAbsoluteTopLeft() + m_rowsArea.Position;
            for (int i = 0; i < VisibleRowsCount; ++i)
            {
                Debug.Assert(m_visibleRowIndexOffset >= 0);
                var idx = i + m_visibleRowIndexOffset;
                if (idx >= m_rows.Count)
                    break;

                Debug.Assert(idx >= 0);
                if (idx < 0)
                    continue;

                bool highlightRow = (m_mouseOverRowIndex.HasValue && m_mouseOverRowIndex.Value == idx) ||
                                    (SelectedRowIndex.HasValue && SelectedRowIndex.Value == idx);
                var rowFont = m_styleDef.RowFontNormal;
                if (highlightRow)
                {
                    MyGuiManager.DrawSpriteBatch(m_styleDef.RowTextureHighlight,
                        positionTopLeft,
                        new Vector2(m_rowsArea.Size.X, RowHeight),
                        ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                        MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                    rowFont = m_styleDef.RowFontHighlight;
                }

                var row = m_rows[idx];
                Debug.Assert(row != null);
                if (row != null)
                {
                    var cellPos = positionTopLeft;
                    for (int j = 0; j < ColumnsCount; ++j)
                    {
                        if (j >= row.Cells.Count)
                            break;
                        var cell = row.Cells[j];
                        var meta = m_columnsMetaData[j];
                        var cellSize = new Vector2(meta.Width * m_rowsArea.Size.X, RowHeight);
                        if (cell != null && cell.Text != null)
                        {
                            if (cell.Icon.HasValue)
                            {
                                var iconPosition = MyUtils.GetCoordAlignedFromTopLeft(cellPos, cellSize, cell.IconOriginAlign);
                                var icon = cell.Icon.Value;
                                var ratios = Vector2.Min(icon.SizeGui, cellSize) / icon.SizeGui;
                                float scale = Math.Min(ratios.X, ratios.Y);
                                MyGuiManager.DrawSpriteBatch(
                                    texture: (HasHighlight) ? icon.Highlight : icon.Normal,
                                    normalizedCoord: iconPosition,
                                    normalizedSize: icon.SizeGui * scale,
                                    color: ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                                    drawAlign: cell.IconOriginAlign);
                            }

                            var textPos = MyUtils.GetCoordAlignedFromCenter(cellPos + 0.5f * cellSize, cellSize, meta.TextAlign);
                            MyGuiManager.DrawString(rowFont, cell.Text, textPos,
                                TextScaleWithLanguage,
                                (cell.TextColor != null) ? cell.TextColor : ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha),
                                meta.TextAlign,
                                maxTextWidth: cellSize.X);
                        }
                        cellPos.X += cellSize.X;
                    }
                }

                positionTopLeft.Y += RowHeight;
            }
        }

        private void HandleMouseOver()
        {
            if (m_rowsArea.Contains(MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft()))
            {
                m_mouseOverRowIndex = ComputeRowIndexFromPosition(MyGuiManager.MouseCursorPosition);
                m_mouseOverColumnIndex = ComputeColumnIndexFromPosition(MyGuiManager.MouseCursorPosition);
                m_mouseOverHeader = false;
            }
            else if (m_headerArea.Contains(MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft()))
            {
                m_mouseOverRowIndex = null;
                m_mouseOverColumnIndex = ComputeColumnIndexFromPosition(MyGuiManager.MouseCursorPosition);
                m_mouseOverHeader = true;
            }
            else
            {
                m_mouseOverRowIndex = null;
                m_mouseOverColumnIndex = null;
                m_mouseOverHeader = false;
            }
        }

        private void HandleNewMousePress(ref MyGuiControlBase captureInput)
        {
            bool cursorInItems = m_rowsArea.Contains(MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft());

            MyMouseButtonsEnum mouseButton = MyMouseButtonsEnum.None;

            if (MyInput.Static.IsNewPrimaryButtonPressed())
            {
                mouseButton = MyMouseButtonsEnum.Left;
            }
            else if (MyInput.Static.IsNewSecondaryButtonPressed())
            {
                mouseButton = MyMouseButtonsEnum.Right;
            }
            else if (MyInput.Static.IsNewMiddleMousePressed())
            {
                mouseButton = MyMouseButtonsEnum.Middle;
            }
            else if (MyInput.Static.IsNewXButton1MousePressed())
            {
                mouseButton = MyMouseButtonsEnum.XButton1;
            }
            else if (MyInput.Static.IsNewXButton2MousePressed())
            {
                mouseButton = MyMouseButtonsEnum.XButton2;
            }

            if (MyInput.Static.IsAnyNewMouseOrJoystickPressed() && cursorInItems)
            {
                SelectedRowIndex = ComputeRowIndexFromPosition(MyGuiManager.MouseCursorPosition);
                captureInput = this;
                if (ItemSelected != null)
                {
                    ItemSelected(this, new EventArgs() { RowIndex = SelectedRowIndex.Value, MouseButton = mouseButton });
                    MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                }
            }

            if (MyInput.Static.IsNewPrimaryButtonPressed())
            {
                if (m_mouseOverHeader)
                {
                    Debug.Assert(m_mouseOverColumnIndex.HasValue);
                    SortByColumn(m_mouseOverColumnIndex.Value);
                    if (ColumnClicked != null)
                        ColumnClicked(this, m_mouseOverColumnIndex.Value);
                }
                else if (cursorInItems)
                {
                    if (!m_doubleClickStarted.HasValue)
                    {
                        m_doubleClickStarted = MyGuiManager.TotalTimeInMilliseconds;
                        m_doubleClickFirstPosition = MyGuiManager.MouseCursorPosition;
                    }
                    else if ((MyGuiManager.TotalTimeInMilliseconds - m_doubleClickStarted.Value) <= MyGuiConstants.DOUBLE_CLICK_DELAY &&
                             (m_doubleClickFirstPosition - MyGuiManager.MouseCursorPosition).Length() <= 0.005f)
                    {
                        if (ItemDoubleClicked != null && SelectedRowIndex.HasValue)
                            ItemDoubleClicked(this, new EventArgs() { RowIndex = SelectedRowIndex.Value, MouseButton = mouseButton});

                        m_doubleClickStarted = null;
                        captureInput = this;
                    }
                }
            }
        }

        public void Sort(bool switchSort = true)
        {
            if (m_sortColumn != -1)
                SortByColumn(m_sortColumn, null, switchSort);
        }

        public void SortByColumn(int columnIdx, SortStateEnum? sortState = null, bool switchSort = true)
        {
            columnIdx = MathHelper.Clamp(columnIdx, 0, m_columnsMetaData.Count - 1);
            m_sortColumn = columnIdx;
            m_sortColumnState = sortState.HasValue ? sortState.Value : m_sortColumnState;            
            var colMeta = m_columnsMetaData[columnIdx];
            var originalSortState = colMeta.SortState;

            m_columnsMetaData[m_lastSortedColumnIdx].SortState = SortStateEnum.Unsorted;

            var comparison = colMeta.AscendingComparison;
            if (comparison == null)
                return;

            SortStateEnum targetSortState = originalSortState;

            if (switchSort)
            {
                targetSortState = ((originalSortState == SortStateEnum.Ascending)
                    ? SortStateEnum.Descending
                    : SortStateEnum.Ascending);
            }

            if (sortState.HasValue)
                targetSortState = sortState.Value;
            else
                if (m_sortColumnState.HasValue)
                    targetSortState = m_sortColumnState.Value;


            if (targetSortState == SortStateEnum.Ascending)
                m_rows.Sort((a, b) => comparison(a.Cells[columnIdx], b.Cells[columnIdx]));
            else
                m_rows.Sort((a, b) => comparison(b.Cells[columnIdx], a.Cells[columnIdx]));

            m_lastSortedColumnIdx = columnIdx;
            colMeta.SortState = targetSortState;
            SelectedRowIndex = null;
        }

        public int FindRow(Row row)
        {
            return m_rows.IndexOf(row);
        }

        private bool IsValidRowIndex(int? index)
        {
            return index.HasValue && 0 <= index.Value && index.Value < m_rows.Count;
        }

        private void RefreshInternals()
        {
            // Expand column descriptions to have at least as much as is needed.
            while (m_columnsMetaData.Count < ColumnsCount)
                m_columnsMetaData.Add(new ColumnMetaData());

            var minSize = m_styleDef.Texture.MinSizeGui;
            var maxSize = m_styleDef.Texture.MaxSizeGui;
            Size = Vector2.Clamp(new Vector2(Size.X, RowHeight * (VisibleRowsCount + 1) + minSize.Y), minSize, maxSize);

            m_headerArea.Position = new Vector2(m_styleDef.Padding.Left, m_styleDef.Padding.Top);
            m_headerArea.Size = new Vector2(Size.X - (m_styleDef.Padding.Left + m_styleDef.ScrollbarMargin.HorizontalSum + m_scrollBar.Size.X),
                                            RowHeight);

            m_rowsArea.Position = m_headerArea.Position + ((HeaderVisible) ? new Vector2(0f, RowHeight) : Vector2.Zero);
            m_rowsArea.Size = new Vector2(m_headerArea.Size.X, RowHeight * VisibleRowsCount);

            RefreshScrollbar();
        }

        private void RefreshScrollbar()
        {
            m_scrollBar.Visible = m_rows.Count > VisibleRowsCount;
            m_scrollBar.Init((float)m_rows.Count, (float)VisibleRowsCount);

            var posTopRight = Size * new Vector2(0.5f, -0.5f);
            var margin = m_styleDef.ScrollbarMargin;
            var position = new Vector2(posTopRight.X - (margin.Right + m_scrollBar.Size.X),
                                       posTopRight.Y + margin.Top);
            m_scrollBar.Layout(position, Size.Y - (margin.Top + margin.Bottom));
        }

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            RowHeight = m_styleDef.RowHeight;
            TextScale = m_styleDef.TextScale;
            RefreshInternals();
        }

        private void verticalScrollBar_ValueChanged(MyScrollbar scrollbar)
        {
            m_visibleRowIndexOffset = (int)scrollbar.Value;
        }

        public struct EventArgs
        {
            public int RowIndex;
            public MyMouseButtonsEnum MouseButton;
        }

        public class Cell
        {
            public readonly StringBuilder Text;
            public readonly object UserData;
            public readonly MyToolTips ToolTip;
            public readonly MyGuiHighlightTexture? Icon;
            public readonly MyGuiDrawAlignEnum IconOriginAlign;
            public  Color? TextColor;

            public Row Row;
            private StringBuilder text;
            private StringBuilder toolTip;

            public Cell(String text = null, object userData = null, String toolTip = null, Color? textColor = null,
                        MyGuiHighlightTexture? icon = null, MyGuiDrawAlignEnum iconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP)
            {
                if (text != null)
                    Text = new StringBuilder().Append(text);
                if (toolTip != null)
                    ToolTip = new MyToolTips(toolTip);
                UserData = userData;
                Icon = icon;
                IconOriginAlign = iconOriginAlign;
                TextColor = textColor;
            }

            public Cell(StringBuilder text, object userData = null, String toolTip = null, Color? textColor = null,
                        MyGuiHighlightTexture? icon = null, MyGuiDrawAlignEnum iconOriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP)
            {
                if (text != null)
                    Text = new StringBuilder().AppendStringBuilder(text);
                if (toolTip != null)
                    ToolTip = new MyToolTips(toolTip);
                UserData = userData;
                Icon = icon;
                IconOriginAlign = iconOriginAlign;
                TextColor = textColor;
            }
        }

        public class Row
        {
            internal readonly List<Cell> Cells;
            public readonly object UserData;

            public Row(object userData = null)
            {
                UserData = userData;

                Cells = new List<Cell>();
            }

            public void AddCell(Cell cell)
            {
                Cells.Add(cell);
                cell.Row = this;
            }

            public Cell GetCell(int cell)
            {
                return Cells[cell];
            }
        }

        public enum SortStateEnum
        {
            Unsorted,
            Ascending,
            Descending,
        }

        private class ColumnMetaData
        {
            public StringBuilder Name;
            public float Width;
            public Comparison<Cell> AscendingComparison;
            public SortStateEnum SortState;
            public MyGuiDrawAlignEnum TextAlign;
            public MyGuiDrawAlignEnum HeaderTextAlign;

            public ColumnMetaData()
            {
                Name = new StringBuilder();
                SortState = SortStateEnum.Unsorted;
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                HeaderTextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
            }
        }
    }
}
