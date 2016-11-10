using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Graphics.GUI
{
    class MyRichLabelText : MyRichLabelPart
    {
        private StringBuilder m_text;
        private string m_font;
        private Vector4 m_color;
        private float m_scale;
        private bool mShowTextShadow;

        public MyRichLabelText(StringBuilder text, string font, float scale, Vector4 color)
        {
            m_text = text;
            m_font = font;
            m_scale = scale;
            m_color = color;
            RecalculateSize();
        }

        public MyRichLabelText()
        {
            m_text = new StringBuilder(512);
            m_font = MyFontEnum.Blue;
            m_scale = 0;
            m_color = Vector4.Zero;
        }

        public void Init(string text, string font, float scale, Vector4 color)
        {
            m_text.Append(text);
            m_font = font;
            m_scale = scale;
            m_color = color;
            RecalculateSize();
        }

        public StringBuilder Text
        {
            get
            {
                return m_text;
            }
        }

        public bool ShowTextShadow
        {
            get { return mShowTextShadow; }
            set { mShowTextShadow = value; }
        }

        public void Append(string text)
        {
            m_text.Append(text);
            RecalculateSize();
        }

        public float Scale
        {
            get
            {
                return m_scale;
            }
            set
            {
                m_scale = value;
                RecalculateSize();
            }
        }

        public string Font
        {
            get
            {
                return m_font;
            }
            set
            {
                m_font = value;
                RecalculateSize();
            }
        }

        public Vector4 Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        public string Tag
        {
            get;
            set;
        }

        public override void AppendTextTo(StringBuilder builder)
        {
            builder.Append(m_text);
        }

        /// <summary>
        /// Draws text
        /// </summary>
        /// <param name="position">Top-left position</param>
        /// <returns></returns>
        public override bool Draw(Vector2 position)
        {
            if (ShowTextShadow && !String.IsNullOrWhiteSpace(m_text.ToString()))
            {
                Vector2 size = Size;
                MyGuiTextShadows.DrawShadow(ref position, ref size,
                    alignment: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }

            MyGuiManager.DrawString(m_font, m_text, position, m_scale, new Color(m_color),
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            return true;
        }

        public override bool HandleInput(Vector2 position)
        {
            return false;
        }

        private void RecalculateSize()
        {
            Size = MyGuiManager.MeasureString(m_font, m_text, m_scale);
        }
    }
}