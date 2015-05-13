using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public struct MyLayoutTable
    {
        private IMyGuiControlsParent m_parent;
        private Vector2 m_parentTopLeft;
        private float[] m_prefixScanX;
        private float[] m_prefixScanY;

        public int LastRow
        {
            get { return (m_prefixScanY != null) ? m_prefixScanY.Length - 2 : 0; }
        }

        public int LastColumn
        {
            get { return (m_prefixScanX != null) ? m_prefixScanX.Length - 2 : 0; }
        }

        public MyLayoutTable(IMyGuiControlsParent parent)
        {
            m_parent = parent;
            m_parentTopLeft = -0.5f * (m_parent.GetSize() ?? Vector2.One);
            m_prefixScanX = null;
            m_prefixScanY = null;
        }

        public void SetColumnWidths(params float[] widthsPx)
        {
            m_prefixScanX = new float[widthsPx.Length + 1];
            var size = m_parent.GetSize();
            m_prefixScanX[0] = -0.5f * (size.HasValue ? size.Value.X : 1f);

            float optimalGuiWidth = MyGuiConstants.GUI_OPTIMAL_SIZE.X;
            for (int i = 0; i < widthsPx.Length; i++)
            {
                float widthNormalized = widthsPx[i] / optimalGuiWidth;
                m_prefixScanX[i + 1] = m_prefixScanX[i] + widthNormalized;
            }
        }

        public void SetRowHeights(params float[] heightsPx)
        {
            m_prefixScanY = new float[heightsPx.Length + 1];
            var size = m_parent.GetSize();
            m_prefixScanY[0] = -0.5f * (size.HasValue ? size.Value.Y : 1f);

            float optimalGuiHeight = MyGuiConstants.GUI_OPTIMAL_SIZE.Y;
            for (int i = 0; i < heightsPx.Length; i++)
            {
                float heightNormalized = heightsPx[i] / optimalGuiHeight;
                m_prefixScanY[i + 1] = m_prefixScanY[i] + heightNormalized;
            }
        }

        public void Add(MyGuiControlBase control, MyAlignH alignH, MyAlignV alignV, int row, int col, int rowSpan = 1, int colSpan = 1)
        {
            var min = new Vector2(m_prefixScanX[col], m_prefixScanY[row]);
            var max = new Vector2(m_prefixScanX[col + colSpan], m_prefixScanY[row + rowSpan]);
            var size = max - min;
            control.Position = new Vector2(
                min.X + size.X * 0.5f * (int)alignH,
                min.Y + size.Y * 0.5f * (int)alignV);
            control.OriginAlign = (MyGuiDrawAlignEnum)(3 * (int)alignH + (int)alignV);
            m_parent.Controls.Add(control);
        }

    }
}
