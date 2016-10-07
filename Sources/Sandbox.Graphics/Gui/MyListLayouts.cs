using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public enum MyAlignH
    { // don't change values, they are used in computations
        Left = 0,
        Center = 1,
        Right = 2,
    }

    public enum MyAlignV
    { // don't change values, they are used in computations
        Top = 0,
        Center = 1,
        Bottom = 2,
    }

    public struct MyLayoutVertical
    {
        private IMyGuiControlsParent m_parent;
        private Vector2 m_parentSize;
        private float m_currentPosY;
        private float m_horizontalPadding;

        public float CurrentY
        {
            get { return m_currentPosY; }
        }

        public float HorrizontalPadding
        {
            get { return m_horizontalPadding; }
        }

        public MyLayoutVertical(IMyGuiControlsParent parent, float horizontalPaddingPx)
        {
            m_parent = parent;

            // not sure if Vector2.One is correct, screens without size might be positioning things differently
            m_parentSize = parent.GetSize() ?? Vector2.One;
            m_currentPosY = m_parentSize.Y * -0.5f;
            m_horizontalPadding = horizontalPaddingPx / MyGuiConstants.GUI_OPTIMAL_SIZE.X;
        }

        public void Add(MyGuiControlBase control, MyAlignH align, bool advance = true)
        {
            AddInternal(control, align, MyAlignV.Top, advance, control.Size.Y);
        }

        public void Add(MyGuiControlBase control, float preferredWidthPx, float preferredHeightPx, MyAlignH align)
        {
            control.Size = new Vector2(preferredWidthPx, preferredHeightPx) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Add(control, align);
        }

        public void Add(MyGuiControlBase leftControl, MyGuiControlBase rightControl)
        {
            var verticalSize = Math.Max(leftControl.Size.Y, rightControl.Size.Y);
            AddInternal(leftControl, MyAlignH.Left, MyAlignV.Center, false, verticalSize);
            AddInternal(rightControl, MyAlignH.Right, MyAlignV.Center, false, verticalSize);
            m_currentPosY += verticalSize;
        }

        public void Add(MyGuiControlBase leftControl, MyGuiControlBase centerControl, MyGuiControlBase rightControl)
        {
            var verticalSize = MathHelper.Max(leftControl.Size.Y, centerControl.Size.Y, rightControl.Size.Y);
            AddInternal(leftControl, MyAlignH.Left, MyAlignV.Center, false, verticalSize);
            AddInternal(centerControl, MyAlignH.Center, MyAlignV.Center, false, verticalSize);
            AddInternal(rightControl, MyAlignH.Right, MyAlignV.Center, false, verticalSize);
            m_currentPosY += verticalSize;
        }

        public void Advance(float advanceAmountPx)
        {
            m_currentPosY += advanceAmountPx / MyGuiConstants.GUI_OPTIMAL_SIZE.Y;
        }

        private void AddInternal(MyGuiControlBase control, MyAlignH alignH, MyAlignV alignV, bool advance, float verticalSize)
        {
            control.OriginAlign = (MyGuiDrawAlignEnum)(3 * (int)alignH + (int)alignV);
            int alignHSign = (-1 + (int)alignH);
            var offsetV = verticalSize * 0.5f * (int)alignV;
            control.Position = new Vector2(
                alignHSign * (0.5f * m_parentSize.X - m_horizontalPadding),
                m_currentPosY + offsetV);
            m_currentPosY += (advance) ? (verticalSize - offsetV) : 0f;
            m_parent.Controls.Add(control);
        }
    }

    public struct MyLayoutHorizontal
    {
        private IMyGuiControlsParent m_parent;
        private Vector2 m_parentSize;
        private float m_currentPosX;
        private float m_verticalPadding;

        public float CurrentX
        {
            get { return m_currentPosX; }
        }

        public float VerticalPadding
        {
            get { return m_verticalPadding; }
        }

        public MyLayoutHorizontal(IMyGuiControlsParent parent, float verticalPaddingPx)
        {
            m_parent = parent;

            // not sure if Vector2.One is correct, screens without size might be positioning things differently
            m_parentSize = parent.GetSize() ?? Vector2.One;
            m_currentPosX = m_parentSize.X * -0.5f;
            m_verticalPadding = verticalPaddingPx / MyGuiConstants.GUI_OPTIMAL_SIZE.X;
        }

        public void Add(MyGuiControlBase control, MyAlignV align, bool advance = true)
        {
            AddInternal(control, MyAlignH.Left, align, advance, control.Size.X);
        }

        public void Add(MyGuiControlBase control, float preferredHeightPx, float preferredWidthPx, MyAlignV align)
        {
            control.Size = new Vector2(preferredWidthPx, preferredHeightPx) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Add(control, align);
        }

        public void Advance(float advanceAmountPx)
        {
            m_currentPosX += advanceAmountPx / MyGuiConstants.GUI_OPTIMAL_SIZE.X;
        }

        private void AddInternal(MyGuiControlBase control, MyAlignH alignH, MyAlignV alignV, bool advance, float horizontalSize)
        {
            control.OriginAlign = (MyGuiDrawAlignEnum)(3 * (int)alignH + (int)alignV);
            int alignVSign = (-1 + (int)alignV);
            var offsetH = horizontalSize * 0.5f * (int)alignH;
            control.Position = new Vector2(m_currentPosX + offsetH, alignVSign * (0.5f * m_parentSize.Y - m_verticalPadding));
            m_currentPosX += (advance) ? (horizontalSize - offsetH) : 0f;
            m_parent.Controls.Add(control);
        }
    }
}
