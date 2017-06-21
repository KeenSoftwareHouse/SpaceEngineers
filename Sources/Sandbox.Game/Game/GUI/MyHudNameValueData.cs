using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using VRage;
using VRageMath;
using Sandbox.Graphics;
using VRage.Game;
using VRage.Utils;

namespace Sandbox.Game.Gui
{
    public class MyHudNameValueData
    {
        public class Data
        {
            public StringBuilder Name;
            public StringBuilder Value;
            public string NameFont;
            public string ValueFont;
            public bool Visible;
            public Data()
            {
                Name = new StringBuilder();
                Value = new StringBuilder();
                Visible = true;
            }
        }

        private readonly List<Data> m_items;
        private int m_count;

        public string DefaultNameFont;
        public string DefaultValueFont;
        public float LineSpacing;
        public bool ShowBackgroundFog;

        public int Count
        {
            get { return m_count; }
            set
            {
                m_count = value;
                EnsureItemsExist();
            }
        }

        public int GetVisibleCount()
        {
            int count = 0;
            for (int i = 0; i < m_count; i++)
            {
                if (m_items[i].Visible)
                {
                    count++;
                }
            }

            return count;
        }

        public float GetGuiHeight()
        {
            return (GetVisibleCount() + 1) * LineSpacing;
        }

        public Data this[int i]
        {
            get { return m_items[i]; }
        }

        public MyHudNameValueData(int itemCount,
            string defaultNameFont = MyFontEnum.Blue,
            string defaultValueFont = MyFontEnum.White,
            float lineSpacing      = MyGuiConstants.HUD_LINE_SPACING,
            bool showBackgroundFog = false)
        {
            DefaultNameFont   = defaultNameFont;
            DefaultValueFont  = defaultValueFont;
            LineSpacing       = lineSpacing;
            m_count           = itemCount;
            m_items           = new List<Data>(itemCount);
            ShowBackgroundFog = showBackgroundFog;
            EnsureItemsExist();
        }

        public void DrawTopDown(Vector2 namesTopLeft, Vector2 valuesTopRight, float textScale)
        {
            const MyGuiDrawAlignEnum alignNames  = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            const MyGuiDrawAlignEnum alignValues = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            Color color = Color.White;
            if (ShowBackgroundFog)
                DrawBackgroundFog(namesTopLeft, valuesTopRight, topDown: true);

            for (int i = 0; i < Count; ++i)
            {
                var item = m_items[i];
                if (!item.Visible)
                    continue;

                MyGuiManager.DrawString(item.NameFont ?? DefaultNameFont, item.Name, namesTopLeft, textScale, color, alignNames);
                MyGuiManager.DrawString(item.ValueFont ?? DefaultValueFont, item.Value, valuesTopRight, textScale, color, alignValues);

                namesTopLeft.Y   += LineSpacing;
                valuesTopRight.Y += LineSpacing;
            }
        }

        public void DrawBottomUp(Vector2 namesBottomLeft, Vector2 valuesBottomRight, float textScale)
        {
            const MyGuiDrawAlignEnum alignNames  = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            const MyGuiDrawAlignEnum alignValues = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
            Color color = Color.White;

            if (ShowBackgroundFog)
                DrawBackgroundFog(namesBottomLeft, valuesBottomRight, topDown: false);

            for (int i = Count - 1; i >= 0; --i)
            {
                var item = m_items[i];
                if (!item.Visible)
                    continue;

                MyGuiManager.DrawString(item.NameFont ?? DefaultNameFont, item.Name, namesBottomLeft, textScale, color, alignNames);
                MyGuiManager.DrawString(item.ValueFont ?? DefaultValueFont, item.Value, valuesBottomRight, textScale, color, alignValues);

                namesBottomLeft.Y   -= LineSpacing;
                valuesBottomRight.Y -= LineSpacing;
            }
        }

        internal float ComputeMaxLineWidth(float textScale)
        {
            float maxWidth = 0f;
            for (int i = 0; i < Count; ++i)
            {
                var item      = m_items[i];
                var nameFont  = item.NameFont ?? DefaultNameFont;
                var valueFont = item.ValueFont ?? DefaultValueFont;
                var nameSize  = MyGuiManager.MeasureString(nameFont, item.Name, textScale);
                var valueSize = MyGuiManager.MeasureString(valueFont, item.Value, textScale);
                maxWidth      = Math.Max(maxWidth, nameSize.X + valueSize.X);
            }

            return maxWidth;
        }

        private void DrawBackgroundFog(Vector2 namesTopLeft, Vector2 valuesTopRight, bool topDown)
        {
            float lineOffset;
            int startIdx, endIdx, step;
            if (topDown)
            {
                lineOffset = LineSpacing;
                startIdx   = 0;
                endIdx     = Count;
                step       = 1;
            }
            else
            {
                lineOffset = -LineSpacing;
                startIdx   = Count-1;
                endIdx     = -1;
                step       = -1;
            }

            for (int i = startIdx; i != endIdx; i += step)
            {
                var item = m_items[i];
                if (!item.Visible)
                    continue;

                var center = new Vector2((namesTopLeft.X + valuesTopRight.X) * 0.5f,
                                         namesTopLeft.Y + 0.5f * lineOffset);
                var size = new Vector2(Math.Abs(namesTopLeft.X - valuesTopRight.X),
                                       LineSpacing);

                MyGuiTextShadows.DrawShadow(ref center, ref size);

                namesTopLeft.Y   += lineOffset;
                valuesTopRight.Y += lineOffset;
            }
        }

        private void EnsureItemsExist()
        {
            m_items.Capacity = Math.Max(this.Count, m_items.Capacity);
            while (m_items.Count < this.Count)
            {
                m_items.Add(new Data());
            }
        }

    }
}
