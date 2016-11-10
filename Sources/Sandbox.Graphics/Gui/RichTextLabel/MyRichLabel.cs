using Sandbox.Gui.RichTextLabel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public delegate void ScissorRectangleHandler(ref RectangleF rectangle);

    public class MyRichLabel
    {
        private static readonly string[] LINE_SEPARATORS = new[] {"\n", "\r\n" };
        private const char m_wordSeparator = ' ';

        public event ScissorRectangleHandler AdjustingScissorRectangle;

        private MyGuiControlBase m_parent;

        private bool m_sizeDirty;
        private Vector2 m_size;

        private float m_maxLineWidth;
        private float m_minLineHeight;

        // NOTE: line separators, not lines themselves. Also even
        // at zero separators, there's always at least one line
        private List<MyRichLabelLine> m_lineSeparators;
        private int m_lineSeparatorsCount;
        private int m_lineSeparatorsCapacity;
        private int m_lineSeparatorFirst;

        private MyRichLabelLine m_currentLine;
        private float m_currentLineRestFreeSpace;
        private StringBuilder m_helperSb;

        private MyRichLabelLine m_emptyLine;
        private int m_visibleLinesCount;

        private List<MyRichLabelText> m_richTextsPool;
        private int m_richTextsOffset;
        private int m_richTexsCapacity;

        public MyGuiDrawAlignEnum TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

        public int NumberOfRows
        {
            get
            {
                return m_lineSeparatorsCount + 1;
            }
        }

        public MyRichLabel(MyGuiControlBase parent, float maxLineWidth, float minLineHeight, int? linesCountMax = null)
        {
            m_parent = parent;
            m_maxLineWidth = maxLineWidth;
            m_minLineHeight = minLineHeight;
            m_helperSb = new StringBuilder(256);
            m_visibleLinesCount = linesCountMax == null ? int.MaxValue : linesCountMax.Value;

            Init();
        }

        private void Init()
        {
            m_helperSb.Clear();

            m_sizeDirty = true;
            m_size = Vector2.Zero;
            m_lineSeparatorsCount = 0;
            m_lineSeparatorsCapacity = 32;
            m_richTextsOffset = 0;
            m_richTexsCapacity = 32;
            m_currentLineRestFreeSpace = m_maxLineWidth;

            m_lineSeparatorFirst = 0;
            m_lineSeparators = new List<MyRichLabelLine>(m_lineSeparatorsCapacity);
            for (int i = 0; i < m_lineSeparatorsCapacity; i++)
            {
                m_lineSeparators.Add(new MyRichLabelLine(m_minLineHeight));
            }
            m_currentLine = m_lineSeparators[0];

            m_richTextsPool = new List<MyRichLabelText>(m_richTexsCapacity);
            for (int i = 0; i < m_richTexsCapacity; i++)
            {
                m_richTextsPool.Add(new MyRichLabelText() { ShowTextShadow = this.ShowTextShadow, Tag = m_parent.Name });
            }
        }

        private void ReallocateLines()
        {
            // NOTE: Reallocate is done in advance of 1
            if (m_lineSeparatorsCount + 1 >= m_lineSeparatorsCapacity)
            {
                m_lineSeparatorsCapacity *= 2;
                m_lineSeparators.Capacity = m_lineSeparatorsCapacity;
                for (int i = m_lineSeparatorsCount + 1; i < m_lineSeparatorsCapacity; i++)
                {
                    m_lineSeparators.Add(new MyRichLabelLine(m_minLineHeight));
                }
            }
        }

        private void ReallocateRichTexts()
        {
            // NOTE: Reallocate is done in advance of 1
            if (m_richTextsOffset + 1 >= m_richTexsCapacity)
            {
                m_richTexsCapacity *= 2;
                m_richTextsPool.Capacity = m_richTexsCapacity;
                for (int i = m_richTextsOffset + 1; i < m_richTexsCapacity; i++)
                {
                    m_richTextsPool.Add(new MyRichLabelText() { ShowTextShadow = this.ShowTextShadow, Tag = m_parent.Name });
                }
            }
        }

        public void Append(StringBuilder text, string font, float scale, Vector4 color)
        {
            Append(text.ToString(), font, scale, color);
        }

        public void Append(string text, string font, float scale, Vector4 color)
        {
            string[] paragraphs = text.Split(LINE_SEPARATORS, StringSplitOptions.None);

            for (int i = 0; i < paragraphs.Length; i++)
            {
                AppendParagraph(paragraphs[i], font, scale, color);
                if (i < paragraphs.Length - 1)
                {
                    AppendLine();
                }
            }
        }

        public void Append(string texture, Vector2 size, Vector4 color)
        {
            MyRichLabelImage image = new MyRichLabelImage(texture, size, color);
            if (image.Size.X > m_currentLineRestFreeSpace)
            {
                AppendLine();
            }
            AppendPart(image);
        }

        public void AppendLink(string url, string text, float scale, Action<string> onClick)
        {
            MyRichLabelLink link = new MyRichLabelLink(url, text, scale, onClick);
            AppendPart(link);
        }

        public void AppendLine()
        {
            if (m_lineSeparatorsCount == m_visibleLinesCount)
            {
                m_lineSeparatorFirst = GetIndexSafe(1);
                m_currentLine = m_lineSeparators[GetIndexSafe(m_lineSeparatorsCount)];
                m_currentLine.ClearParts();
            }
            else
            {
                ReallocateLines();
                ++m_lineSeparatorsCount;
                m_currentLine = m_lineSeparators[GetIndexSafe(m_lineSeparatorsCount)];
            }

            m_currentLineRestFreeSpace = m_maxLineWidth;
            ReallocateRichTexts();
            m_sizeDirty = true;
        }

        public void AppendLine(StringBuilder text, string font, float scale, Vector4 color)
        {
            Append(text, font, scale, color);
            AppendLine();
        }

        public void AppendLine(string texture, Vector2 size, Vector4 color)
        {
            Append(texture, size, color);
            AppendLine();
        }

        private void AppendParagraph(string paragraph, string font, float scale, Vector4 color)
        {
            m_helperSb.Clear();
            m_helperSb.Append(paragraph);
            float textWidth = MyGuiManager.MeasureString(font, m_helperSb, scale).X;
            // first we try append all paragraph to current line
            if (textWidth < m_currentLineRestFreeSpace)
            {
                ReallocateRichTexts();
                m_richTextsPool[++m_richTextsOffset].Init(m_helperSb.ToString(), font, scale, color);
                AppendPart(m_richTextsPool[m_richTextsOffset]);
            }
            // if there is not enough free space in current line for whole paragraph
            else
            {
                ReallocateRichTexts();
                m_richTextsPool[++m_richTextsOffset].Init("", font, scale, color);
                string[] words = paragraph.Split(m_wordSeparator);
                int currentWordIndex = 0;
                while (currentWordIndex < words.Length)
                {
                    if (words[currentWordIndex].Trim().Length == 0)
                    {
                        currentWordIndex++;
                        continue;
                    }

                    m_helperSb.Clear();
                    if (m_richTextsPool[m_richTextsOffset].Text.Length > 0)
                    {
                        m_helperSb.Append(m_wordSeparator);
                    }
                    m_helperSb.Append(words[currentWordIndex]);

                    textWidth = MyGuiManager.MeasureString(font, m_helperSb, scale).X;

                    if (textWidth <= m_currentLineRestFreeSpace - m_richTextsPool[m_richTextsOffset].Size.X)
                    {
                        m_richTextsPool[m_richTextsOffset].Append(m_helperSb.ToString());
                        currentWordIndex++;
                    }
                    else
                    {
                        // if this word is wider than line and it will be only one word at line, we add what fits and leave the rest for other lines
                        if ((m_currentLine == null || m_currentLine.IsEmpty()) && m_richTextsPool[m_richTextsOffset].Text.Length == 0)
                        {
                            int numCharsThatFit = MyGuiManager.ComputeNumCharsThatFit(font, m_helperSb, scale, m_currentLineRestFreeSpace);
                            m_richTextsPool[m_richTextsOffset].Append(words[currentWordIndex].Substring(0, numCharsThatFit));
                            words[currentWordIndex] = words[currentWordIndex].Substring(numCharsThatFit);
                        }

                        AppendPart(m_richTextsPool[m_richTextsOffset]);
                        ReallocateRichTexts();
                        m_richTextsPool[++m_richTextsOffset].Init("", font, scale, color);
                        if (currentWordIndex < words.Length)
                        {
                            AppendLine();
                        }
                    }
                }

                if (m_richTextsPool[m_richTextsOffset].Text.Length > 0)
                {
                    AppendPart(m_richTextsPool[m_richTextsOffset]);
                }
            }
        }

        private void AppendPart(MyRichLabelPart part)
        {
            m_currentLine = m_lineSeparators[GetIndexSafe(m_lineSeparatorsCount)];
            m_currentLine.AddPart(part);
            m_currentLineRestFreeSpace = m_maxLineWidth - m_currentLine.Size.X;
            m_sizeDirty = true;
        }

        /// <summary>
        /// Draws label
        /// </summary>
        /// <param name="position">Top-left position</param>
        /// <param name="offset"></param>
        /// <param name="drawSizeMax"></param>
        /// <returns></returns>
        public bool Draw(Vector2 position, float offset, Vector2 drawSizeMax)
        {
            RectangleF scissorRect = new RectangleF(position, drawSizeMax);
            OnAdjustingScissorRectangle(ref scissorRect);
            Vector2 textSize = Size;
            using (MyGuiManager.UsingScissorRectangle(ref scissorRect))
            {
                // Compute data for text positioning.
                Vector2 drawPosition = Vector2.Zero;
                float horizontalAdjustment = 0.0f;
                switch (TextAlign)
                {
                    case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                        break;

                    case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                        drawPosition.X = 0.5f * drawSizeMax.X - 0.5f * textSize.X;
                        horizontalAdjustment = 0.5f;
                        break;

                    case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                        drawPosition.X = drawSizeMax.X - textSize.X;
                        horizontalAdjustment = 1.0f;
                        break;
                }

                switch (TextAlign)
                {
                    case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                        break;

                    case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                        drawPosition.Y = 0.5f * drawSizeMax.Y - 0.5f * textSize.Y;
                        break;

                    case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                        drawPosition.Y = drawSizeMax.Y - textSize.Y;
                        break;
                }
                drawPosition.Y -= offset;

                for (int i = 0; i <= m_lineSeparatorsCount; ++i)
                {
                    MyRichLabelLine currentLine = m_lineSeparators[GetIndexSafe(i)];
                    var lineSize = currentLine.Size;
                    var pos = position + drawPosition;
                    pos.X += horizontalAdjustment * (textSize.X - lineSize.X);
                    currentLine.Draw(pos);
                    drawPosition.Y += lineSize.Y;
                }
            }

            return true;
        }

        public Vector2 Size
        {
            get
            {
                if (!m_sizeDirty)
                    return m_size;

                m_size = Vector2.Zero;

                for (int i = 0; i <= m_lineSeparatorsCount; i++)
                {
                    var line = m_lineSeparators[GetIndexSafe(i)];
                    Vector2 lineSize = line.Size;
                    m_size.Y += lineSize.Y;
                    m_size.X = MathHelper.Max(m_size.X, lineSize.X);
                }

                m_sizeDirty = false;

                return m_size;
            }
        }

        private int GetIndexSafe(int index)
        {
            // NOTE: +1 is used to accomodates one clean line at the end
            // when overflowing visible lines count
            return (index + m_lineSeparatorFirst) % (m_visibleLinesCount + 1);
        }

        public void Clear()
        {
            m_lineSeparators.Clear();
            Init();
        }

        public float MaxLineWidth
        {
            get { return m_maxLineWidth; }
            set { m_maxLineWidth = value; }
        }

        public bool ShowTextShadow
        {
            get;
            set;
        }

        private void OnAdjustingScissorRectangle(ref RectangleF rectangle)
        {
            var handler = AdjustingScissorRectangle;
            if (handler != null)
                handler(ref rectangle);
        }

        internal bool HandleInput(Vector2 position, float offset)
        {
            position.Y -= offset;
            for (int i = 0; i <= m_lineSeparatorsCount; i++)
            {
                var line = m_lineSeparators[GetIndexSafe(i)];
                if (line.HandleInput(position))
                    return true;
                position.Y += line.Size.Y;
            }
            return false;
        }
    }
}
