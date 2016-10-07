using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    class MyRichLabelImage : MyRichLabelPart
    {
        private string m_texture;
        private Vector4 m_color;
        private Vector2 m_size;

        public MyRichLabelImage(string texture, Vector2 size, Vector4 color)
        {
            m_texture = texture;
            m_size = size;
            m_color = color;
        }

        public string Texture
        {
            get { return m_texture; }
            set { m_texture = value; }
        }

        public Vector4 Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        public new Vector2 Size
        {
            get { return base.Size; }
            set { base.Size = value; }
        }

        /// <summary>
        /// Draws image
        /// </summary>
        /// <param name="position">Top-left position</param>
        /// <returns></returns>
        public override bool Draw(Vector2 position)
        {
            MyGuiManager.DrawSpriteBatch(m_texture, position, m_size, new Color(m_color), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            return true;
        }

        public override bool HandleInput(Vector2 position)
        {
            return false;
        }
    }
}
