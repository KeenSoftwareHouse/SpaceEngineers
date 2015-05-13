using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    class MyTreeViewBody : MyTreeViewBase
    {
        private Vector2 m_position;
        private Vector2 m_size;
        private Vector2 m_realSize;

        public MyTreeViewBody(MyTreeView treeView, Vector2 position, Vector2 size)
        {
            TreeView = treeView;
            m_position = position;
            m_size = size;
        }

        public void Layout(Vector2 scroll)
        {
            m_realSize = LayoutItems(m_position - scroll);
        }

        public void Draw(float transitionAlpha)
        {
            DrawItems(transitionAlpha);
        }

        public Vector2 GetPosition()
        {
            return m_position;
        }

        public Vector2 GetSize()
        {
            return m_size;
        }

        public void SetSize(Vector2 size)
        {
            m_size = size;
        }

        public Vector2 GetRealSize()
        {
            return m_realSize;
        }

        public void SetPosition(Vector2 position)
        {
            m_position = position;
        }
    }
}
