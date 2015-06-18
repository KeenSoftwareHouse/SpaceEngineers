using System;
using System.Diagnostics;
using VRage.Input;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    class MyTreeViewItemDragAndDrop : MyGuiControlBase
    {
        /// <summary>
        /// Uggly hack, this control needs contain mouse just one more frame after actual drop
        /// because otherwise for example editor will consume mouse events and do things like
        /// unselecting added objects
        /// </summary>
        private bool m_frameBackDragging;

        public EventHandler Drop;

        public bool Dragging { get; set; }
        public Vector2 StartDragPosition { get; set; }
        public MyTreeViewItem DraggedItem { get; set; }

        public MyTreeViewItemDragAndDrop()
            : base( position: Vector2.Zero,
                    size: null,
                    colorMask: null,
                    toolTip: null)
        {

        }

        public void Init(MyTreeViewItem item, Vector2 startDragPosition)
        {
            Dragging = false;
            DraggedItem = item;
            StartDragPosition = startDragPosition;
        }

        public bool HandleInput(MyTreeViewItem treeViewItem)
        {
            bool captured = false;
            if (DraggedItem == null)
            {
                if (MyGUIHelper.Contains(treeViewItem.GetPosition(), treeViewItem.GetSize(), MyGuiManager.MouseCursorPosition.X, MyGuiManager.MouseCursorPosition.Y) &&
                    treeViewItem.TreeView.Contains(MyGuiManager.MouseCursorPosition.X, MyGuiManager.MouseCursorPosition.Y))
                {
                    if (MyInput.Static.IsNewLeftMousePressed())
                    {
                        Dragging = false;
                        DraggedItem = treeViewItem;
                        StartDragPosition = MyGuiManager.MouseCursorPosition;
                        captured = true;
                    }
                }
            }
            return captured;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
            if (Dragging)
            {
                Debug.Assert(DraggedItem != null);

                Vector2 offset = StartDragPosition - DraggedItem.GetPosition();
                DraggedItem.DrawDraged(MyGuiManager.MouseCursorPosition - offset, transitionAlpha);
            }

            m_frameBackDragging = Dragging;
        }

        public override MyGuiControlBase HandleInput()
        {
            if (DraggedItem != null)
            {
                if (MyInput.Static.IsLeftMousePressed())
                {
                    float dragDistanceSquared = MyGuiManager.GetScreenSizeFromNormalizedSize(StartDragPosition - MyGuiManager.MouseCursorPosition).LengthSquared();
                    if (dragDistanceSquared > 16)   // Drag Detection Sensitivity - 4 pixels
                    {
                        Dragging = m_frameBackDragging = true;
                    }
                }
                else
                {
                    if (Drop != null && Dragging)
                    {
                        Drop(this, EventArgs.Empty);
                    }

                    Dragging = false;
                    DraggedItem = null;
                }
            }

            return base.HandleInput();
        }

        public override bool CheckMouseOver()
        {
            return m_frameBackDragging;
        }
    }
}
