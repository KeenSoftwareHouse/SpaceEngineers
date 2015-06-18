using System.Collections.Generic;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyDragAndDropInfo
    {
        public MyGuiControlGrid Grid;
        //public int RowIndex;
        //public int ColumnIndex;
        public int ItemIndex;
    }

    public class MyDragAndDropRestriction
    {
        // main types enums can be dropped, if empty, all object builders can be dropped
        public List<ushort> ObjectBuilders { get; private set; }
        // sub types enums can be dropped, if empty, all object builder types can be dropped
        public List<ushort> ObjectBuilderTypes { get; private set; }

        public MyDragAndDropRestriction()
        {
            ObjectBuilders = new List<ushort>();
            ObjectBuilderTypes = new List<ushort>();
        }
    }

    public enum MyDropHandleType
    {
        /// <summary>
        /// Drop released on mouse button click
        /// </summary>
        MouseClick,
        /// <summary>
        /// Drop released on mouse button release
        /// </summary>
        MouseRelease,
    }

    public class MyDragAndDropEventArgs
    {
        public MyDragAndDropInfo DragFrom { get; set; }
        public MyDragAndDropInfo DropTo { get; set; }
        public MyGuiControlGrid.Item Item { get; set; }
        public MySharedButtonsEnum DragButton;
    }
    
    public delegate void OnItemDropped(object sender, MyDragAndDropEventArgs eventArgs);

    public class MyGuiControlGridDragAndDrop : MyGuiControlBase
    {
        #region fields
        private MyGuiControlGrid.Item m_draggingGridItem;
        private List<MyGuiControlListbox> m_listboxesToDrop;
        private MyDragAndDropInfo m_draggingFrom;
        private Vector4 m_textColor;
        private float m_textScale;
        private Vector2 m_textOffset;
        private bool m_supportIcon;
        private MyDropHandleType? m_currentDropHandleType;
        private MySharedButtonsEnum? m_dragButton;
        List<MyGuiControlBase> m_dropToControls = new List<MyGuiControlBase>();
        #endregion

        #region constructors
        public MyGuiControlGridDragAndDrop(Vector4 backgroundColor, Vector4 textColor, float textScale, Vector2 textOffset, bool supportIcon)
            : base(
            position: new Vector2(0.0f, 0.0f),
            size: MyGuiConstants.DRAG_AND_DROP_SMALL_SIZE,
            colorMask: backgroundColor,
            toolTip: null,
            isActiveControl: true)
        {
            m_textColor = textColor;
            m_textScale = textScale;
            m_textOffset = textOffset;
            m_supportIcon = supportIcon;
            DrawBackgroundTexture = true;
        }
        #endregion

        #region events and delegates
        public event OnItemDropped ItemDropped;
        #endregion

        #region overriden methods
        public override bool CheckMouseOver()
        {
            return IsActive();
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            //base.Draw();

            if (IsActive())
            {
                // draw item's background
                if (DrawBackgroundTexture)
                {
                    MyGuiManager.DrawSpriteBatch(MyGuiConstants.BLANK_TEXTURE,
                        MyGuiManager.MouseCursorPosition,
                        Size,
                        ApplyColorMaskModifiers(ColorMask * new Color(50, 66, 70, 255).ToVector4(), true, backgroundTransitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                }

                Vector2 itemPosition = MyGuiManager.MouseCursorPosition - Size / 2.0f;
                Vector2 textPosition = itemPosition + m_textOffset;
                textPosition.Y += (Size.Y / 2.0f);
                // draw item's icon
                if (m_supportIcon == true && m_draggingGridItem.Icon != null)
                {
                    MyGuiManager.DrawSpriteBatch(m_draggingGridItem.Icon, itemPosition, Size, ApplyColorMaskModifiers(ColorMask, true, transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                }
                ShowToolTip();
            }
        }

        public bool DrawBackgroundTexture { get; set; }

        public override void ShowToolTip()
        {
            if (IsActive() && m_toolTip != null && m_toolTip.ToolTips.Count > 0)
            {
                m_toolTipPosition = MyGuiManager.MouseCursorPosition;
                m_toolTip.Draw(m_toolTipPosition);
            }
        }

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase captureInput = base.HandleInput();
            if (captureInput == null && IsActive())
            {
                // handling left mouse pressed drag and drop
                switch (m_currentDropHandleType.Value)
                {
                    case MyDropHandleType.MouseRelease:
                        HandleButtonPressedDrop(m_dragButton.Value, ref captureInput);
                        break;

                    case MyDropHandleType.MouseClick:
                        HandleButtonClickDrop(m_dragButton.Value, ref captureInput);
                        break;
                }
            }
            return null;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Starts dragging item
        /// </summary>
        /// <param name="dropHandleType">On which action released drop event</param>
        /// <param name="draggingItem">Item which is dragging</param>
        /// <param name="draggingFrom">Information about item's origin</param>
        public void StartDragging(MyDropHandleType dropHandleType, MySharedButtonsEnum dragButton, MyGuiControlGrid.Item draggingItem, MyDragAndDropInfo draggingFrom, bool includeTooltip = true)
        {
            m_currentDropHandleType = dropHandleType;
            m_dragButton = dragButton;
            m_draggingGridItem = draggingItem;
            m_draggingFrom = draggingFrom;
            m_toolTip = (includeTooltip) ? draggingItem.ToolTip : null;
        }

        /// <summary>
        /// Stops dragging item
        /// </summary>
        public void Stop()
        {
            m_draggingFrom = null;
            m_draggingGridItem = null;
            m_currentDropHandleType = null;
            m_dragButton = null;
        }

        /// <summary>
        /// Returns if dragging is active
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return m_draggingGridItem != null && m_draggingFrom != null && m_currentDropHandleType != null && m_dragButton != null;
        }
        #endregion

        public void Drop()
        {
            if (!IsActive())
                return;

            MyDragAndDropInfo dropTo = null;

            m_dropToControls.Clear();

            MyScreenManager.GetControlsUnderMouseCursor(m_dropToControls, true);

            foreach (var control in m_dropToControls)
            {
                var grid = control as MyGuiControlGrid;
                if (grid != null)
                {
                    if (grid.Enabled && grid.MouseOverIndex != MyGuiControlGrid.INVALID_INDEX)
                    {
                        dropTo = new MyDragAndDropInfo();
                        dropTo.Grid = grid;
                        dropTo.ItemIndex = grid.MouseOverIndex;
                        break;
                    }
                }
            }

            ItemDropped(this, new MyDragAndDropEventArgs()
            {
                DragFrom    = m_draggingFrom,
                DropTo      = dropTo,
                Item = m_draggingGridItem,
                DragButton  = m_dragButton.Value,
            });

            Stop();
        }

        #region private methods
        private void HandleButtonPressedDrop(MySharedButtonsEnum button, ref MyGuiControlBase captureInput)
        {
            if (MyInput.Static.IsButtonPressed(button))
                captureInput = this;
            else
                HandleDropingItem();
        }

        private void HandleButtonClickDrop(MySharedButtonsEnum button, ref MyGuiControlBase captureInput)
        {
            if (MyInput.Static.IsNewButtonPressed(button))
            {
                HandleDropingItem();
                captureInput = this;
            }
        }

        private void HandleDropingItem()
        {
            if (IsActive())
            {
                Drop();
            }
        }
        #endregion
    }
}
