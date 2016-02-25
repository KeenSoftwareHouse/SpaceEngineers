using Sandbox.Common;
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
        private MyFontEnum m_font;
        private Vector4 m_color;
        private float m_scale;
        private bool mShowTextShadow;

        private Vector2 m_actualSize;
        private Vector2 m_size;

        private float SMALL_STRING_WIDTH = 0.018f;
        private float NO_OFFSET_THRESHOLD = 0.014f;

        public MyRichLabelText(StringBuilder text, MyFontEnum font, float scale, Vector4 color)
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

        public void Init(string text, MyFontEnum font, float scale, Vector4 color)
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
            set
            {
                m_text = value;
                RecalculateSize();
            }
        }

        public bool ShowTextShadow
        {
            get { return mShowTextShadow; }
            set
            {
                mShowTextShadow = value;
                RecalculateSize();
            }
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

        public MyFontEnum Font
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

        public override Vector2 GetSize()
        {
            return m_actualSize;
        }

        /// <summary>
        /// Draws text
        /// </summary>
        /// <param name="position">Top-left position</param>
        /// <returns></returns>
        public override bool Draw(Vector2 position)
        {
            if (ShowTextShadow)
                DrawShadow(position);

            MyGuiManager.DrawString(m_font, m_text, position, m_scale, new Color(m_color), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            return true;
        }

        public override bool HandleInput(Vector2 position)
        {
            return false;
        }

        private void RecalculateSize()
        {
            m_size = MyGuiManager.MeasureString(m_font, m_text, m_scale);
            m_actualSize = m_size;
            if (ShowTextShadow)
            {
                // String is enlarged because of cropping
                if (m_size.X < SMALL_STRING_WIDTH)
                    ResizeXSmallText(ref m_actualSize);
                else
                    ResizeXLargeText(ref m_actualSize);
            }
        }

        // WARNING: Lot of hard coded stuff. Logic was mostly copied from MyGuiScreenHudBase
        private void DrawShadow(Vector2 position)
        {
            Color color = new Color(0, 0, 0, (byte)(255 * 0.85f));

            Vector2 shadowSize = m_size;
            if (m_size.X < SMALL_STRING_WIDTH)
            {
                ResizeXSmallText(ref shadowSize);
                shadowSize.Y *= 1.2f;
                position.Y += (shadowSize.Y - m_size.Y) * 1.5f;

                MyGuiManager.DrawSpriteBatch(MyGuiConstants.FOG_SMALL2, position, shadowSize, color,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, false);
            }
            else
            {
                ResizeXLargeText(ref shadowSize);
                shadowSize.Y = shadowSize.Y * 0.7f * 3.0f;

                float actualSizeDiff = shadowSize.X - m_size.X;
                if (actualSizeDiff > NO_OFFSET_THRESHOLD)
                    position.X -= actualSizeDiff - NO_OFFSET_THRESHOLD;

                position.Y -= (shadowSize.Y - m_size.Y) / 7.5f;

                MyGuiManager.DrawSpriteBatch(MyGuiConstants.FOG_SMALL3, position, shadowSize, color,
                    MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, false);
            }
        }

        // Enlarges the string size and/or the shadow for better effect
        private void ResizeXSmallText(ref Vector2 size)
        {
            size.X *= 1.2f;
        }

        // Enlarges the string size and/or the shadow for better effect
        private void ResizeXLargeText(ref Vector2 size)
        {
            size.X *= 1.1f;
        }
    }
}