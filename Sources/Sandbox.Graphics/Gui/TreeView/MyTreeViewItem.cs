using System;
using System.Text;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    class MyTreeViewItem : MyTreeViewBase
    {
        public EventHandler _Action;
        public EventHandler RightClick;
        public MyTreeViewItemDragAndDrop DragDrop { get; set; }

        public object Tag;
        public bool Visible;
        public bool Enabled;

        public bool IsExpanded;
        public StringBuilder Text;
        public MyToolTips ToolTip;
        public MyIconTexts IconTexts;

        public MyTreeViewBase Parent;

        private readonly float padding = 0.002f;
        private readonly float spacing = 0.01f;
        private readonly float rightBorder = 0.01f;

        private string m_icon;
        private string m_expandIcon;
        private string m_collapseIcon;

        private Vector2 m_iconSize;
        private Vector2 m_expandIconSize;

        private Vector2 m_currentOrigin;
        private Vector2 m_currentSize;
        private Vector2 m_currentTextSize;
        private float m_loadingIconRotation;

        public MyTreeViewItem(StringBuilder text, string icon, Vector2 iconSize, string expandIcon, string collapseIcon, Vector2 expandIconSize)
        {
            Visible = true;
            Enabled = true;
            Text = text;
            m_icon = icon;
            m_expandIcon = expandIcon;
            m_collapseIcon = collapseIcon;
            m_iconSize = iconSize;
            m_expandIconSize = expandIconSize;
        }

        private float GetHeight()
        {
            return Math.Max(m_currentTextSize.Y, Math.Max(m_iconSize.Y, m_expandIconSize.Y));
        }

        public Vector2 GetIconSize()
        {
            return m_iconSize;
        }

        private Vector2 GetExpandIconPosition()
        {
            return new Vector2(padding, padding + (m_currentSize.Y - m_expandIconSize.Y) / 2);
        }
        private Vector2 GetIconPosition()
        {
            return new Vector2(padding + m_expandIconSize.X + spacing, padding);
        }
        private Vector2 GetTextPosition()
        {
            float iconOffset = m_icon != null ? m_iconSize.X + spacing : 0;
            return new Vector2(padding + m_expandIconSize.X + spacing + iconOffset, (m_currentSize.Y - m_currentTextSize.Y) / 2);
        }

        public Vector2 GetOffset()
        {
            return new Vector2(padding + m_expandIconSize.X / 2, 2.0f * padding + GetHeight());
        }

        public Vector2 LayoutItem(Vector2 origin)
        {
            m_currentOrigin = origin;

            if (!Visible)
            {
                m_currentSize = Vector2.Zero;
                return Vector2.Zero;
            }

            m_currentTextSize = MyGuiManager.MeasureString(MyFontEnum.Blue, Text, 0.8f);

            float iconOffset = m_icon != null ? m_iconSize.X + spacing : 0;
            float width = padding + m_expandIconSize.X + spacing + iconOffset + m_currentTextSize.X + rightBorder + padding;
            float height = padding + GetHeight() + padding;

            m_currentSize = new Vector2(width, height);

            if (IsExpanded)
            {
                Vector2 offset = GetOffset();
                Vector2 itemsSize = LayoutItems(origin + GetOffset());

                width = Math.Max(width, offset.X + itemsSize.X);
                height += itemsSize.Y;
            }

            return new Vector2(width, height);
        }

        public void Draw(float transitionAlpha)
        {
            if (!Visible || !TreeView.Contains(m_currentOrigin, m_currentSize))
            {
                return;
            }

            bool isHighlighted = TreeView.HooveredItem == this;
            Vector2 expandIconPosition = GetExpandIconPosition();
            Vector2 iconPosition = GetIconPosition();
            Vector2 textPosition = GetTextPosition();

            Vector4 baseColor = Enabled ? Vector4.One : MyGuiConstants.TREEVIEW_DISABLED_ITEM_COLOR;

            if (TreeView.FocusedItem == this)
            {
                Color selectedColor = TreeView.GetColor(MyGuiConstants.TREEVIEW_SELECTED_ITEM_COLOR * baseColor, transitionAlpha);
                if (TreeView.WholeRowHighlight())
                {
                    MyGUIHelper.FillRectangle(new Vector2(TreeView.GetPosition().X, m_currentOrigin.Y), new Vector2(TreeView.GetBodySize().X, m_currentSize.Y), selectedColor);
                }
                else
                {
                    MyGUIHelper.FillRectangle(m_currentOrigin, m_currentSize, selectedColor);
                }
            }

            if (GetItemCount() > 0)
            {
                Vector4 expandColor = (isHighlighted) ? baseColor * MyGuiConstants.CONTROL_MOUSE_OVER_BACKGROUND_COLOR_MULTIPLIER : baseColor;
                MyGuiManager.DrawSpriteBatch(IsExpanded ? m_collapseIcon : m_expandIcon,
                                             m_currentOrigin + expandIconPosition, m_expandIconSize,
                                             TreeView.GetColor(expandColor, transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }

            if (m_icon == null)
            { // texture is still being loaded on other thread
                DrawLoadingIcon(baseColor, iconPosition, transitionAlpha);
            }
            else
            {
                MyGuiManager.DrawSpriteBatch(m_icon, m_currentOrigin + iconPosition, m_iconSize,
                                                TreeView.GetColor(baseColor, transitionAlpha),
                                                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }

            Vector4 textColor = (isHighlighted) ? MyGuiConstants.CONTROL_MOUSE_OVER_BACKGROUND_COLOR_MULTIPLIER * baseColor : MyGuiConstants.TREEVIEW_TEXT_COLOR * baseColor;

            MyGuiManager.DrawString(MyFontEnum.Blue, Text,
                                    m_currentOrigin + textPosition,
                                    0.8f, TreeView.GetColor(textColor, transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            if (IconTexts != null)
            {
                IconTexts.Draw(m_currentOrigin + iconPosition, m_iconSize, transitionAlpha, isHighlighted);
            }
        }

        private void DrawLoadingIcon(Vector4 baseColor, Vector2 iconPosition, float transitionAlpha)
        {
            string texture = MyGuiConstants.LOADING_TEXTURE;
            Vector2 normalizedCoord = m_currentOrigin + iconPosition + m_iconSize / 2;

            Vector2 iconSize = 0.5f * m_iconSize;
            MyGuiManager.DrawSpriteBatch(texture, normalizedCoord, iconSize, TreeView.GetColor(baseColor, transitionAlpha),
                                         MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, m_loadingIconRotation);
            m_loadingIconRotation += 0.02f;
            m_loadingIconRotation = m_loadingIconRotation % (MathHelper.Pi * 2);
        }

        public void DrawDraged(Vector2 position, float transitionAlpha)
        {
            if ((m_icon != null) || Text != null)
            {
                if (m_icon != null)
                {
                    string texture = m_icon;
                    if (texture == null)
                    { // texture is still being loaded on other thread
                        DrawLoadingIcon(Vector4.One, GetIconPosition(), transitionAlpha);
                    }
                    else
                    {
                        MyGUIHelper.OutsideBorder(position + GetIconPosition(), m_iconSize, 2, MyGuiConstants.THEMED_GUI_LINE_COLOR);
                        MyGuiManager.DrawSpriteBatch(m_icon,
                                                     position + GetIconPosition(), m_iconSize,
                                                     Color.White, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                    }
                }
                else if (Text != null)
                {
                    var leftTop = position + GetTextPosition();
                    var size = MyGuiManager.MeasureString(MyFontEnum.Blue, Text, 0.8f);
                    MyGUIHelper.OutsideBorder(leftTop, size, 2, MyGuiConstants.THEMED_GUI_LINE_COLOR);
                    MyGUIHelper.FillRectangle(leftTop, size, TreeView.GetColor(MyGuiConstants.TREEVIEW_SELECTED_ITEM_COLOR, transitionAlpha));

                    Color textColor = TreeView.GetColor(MyGuiConstants.CONTROL_MOUSE_OVER_BACKGROUND_COLOR_MULTIPLIER, transitionAlpha);
                    MyGuiManager.DrawString(MyFontEnum.Blue, Text,
                                            position + GetTextPosition(),
                                            0.8f, textColor, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                }
            }
        }

        public bool HandleInputEx(bool hasKeyboardActiveControl)
        {
            if (!Visible)
            {
                return false;
            }

            bool captured = false;

            // Hoover item if mouse cursor is inside its area
            if (TreeView.Contains(MyGuiManager.MouseCursorPosition.X, MyGuiManager.MouseCursorPosition.Y) &&
                MyGUIHelper.Contains(m_currentOrigin, m_currentSize, MyGuiManager.MouseCursorPosition.X, MyGuiManager.MouseCursorPosition.Y))
            {
                TreeView.HooveredItem = this;
            }

            if (Enabled && DragDrop != null)
            {
                captured = DragDrop.HandleInput(this);
            }

            // Single click - expand or focus item
            if (MyInput.Static.IsNewLeftMouseReleased())
            {
                if (GetItemCount() > 0 && MyGUIHelper.Contains(m_currentOrigin + GetExpandIconPosition(), m_expandIconSize, MyGuiManager.MouseCursorPosition.X, MyGuiManager.MouseCursorPosition.Y))
                {
                    IsExpanded = !IsExpanded;
                    captured = true;
                    MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                }
                else if (TreeView.HooveredItem == this)
                {
                    TreeView.FocusItem(this);
                    captured = true;
                    MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
                }
            }

            // Double click - launch Action event
            if (Enabled && /*!captured && MyInput.Static.IsNewLeftMouseDoubleClick() && */TreeView.HooveredItem == this)
            {
                if (_Action != null)
                {
                    DoAction();
                }
                else if (GetItemCount() > 0)
                {
                    IsExpanded = !IsExpanded;
                }
                captured = true;
            }

            // Right click - launch RightClick event
            if (/*!captured && */MyInput.Static.IsNewRightMousePressed() && TreeView.HooveredItem == this)
            {
                if (RightClick != null)
                {
                    RightClick(this, EventArgs.Empty);
                }
                captured = true;
                MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
            }

            return captured;
        }

        public int GetIndex()
        {
            return Parent.GetIndex(this);
        }

        public Vector2 GetPosition()
        {
            return m_currentOrigin;
        }

        public Vector2 GetSize()
        {
            return m_currentSize;
        }

        public void DoAction()
        {
            if (_Action != null)
            {
                _Action(this, EventArgs.Empty);
            }
        }
    }
}
