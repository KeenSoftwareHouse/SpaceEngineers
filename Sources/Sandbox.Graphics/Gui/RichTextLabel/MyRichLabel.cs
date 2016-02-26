using Sandbox.Common;
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
    public class MyRichLabel
    {
        private static readonly string[] m_lineSeparators = new[] {"\n", "\r\n" };
        private const char m_wordSeparator = ' ';

        private float m_maxLineWidth;
        private float m_minLineHeight;

        private List<MyRichLabelLine> m_lines;

        private MyRichLabelLine m_currentLine;
        private float m_currentLineRestFreeSpace;
        private StringBuilder m_helperSb;

        private MyRichLabelLine m_emptyLine;
        private int m_linesCount; // number of line separators, not lines themselves (1 line means 0 separators)
        private int m_linesCountMax;

        private List<MyRichLabelText> m_richTextsPool;
        private int m_richTextsOffset;
        private int m_richTextsMax;

        public MyGuiDrawAlignEnum TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

        public MyRichLabel(float maxLineWidth, float minLineHeight)
        {
            m_maxLineWidth = maxLineWidth;
            m_minLineHeight = minLineHeight;
            m_helperSb = new StringBuilder(256);

            Init();
        }

        private void Init()
        {
            m_helperSb.Clear();

            m_linesCount = 0;
            m_linesCountMax = 64;
            m_richTextsOffset = 0;
            m_richTextsMax = 64;
            m_currentLineRestFreeSpace = m_maxLineWidth;

            m_lines = new List<MyRichLabelLine>(m_linesCountMax);
            for (int i = 0; i < m_linesCountMax; i++)
            {
                m_lines.Add(new MyRichLabelLine(m_minLineHeight));
            }

            m_richTextsPool = new List<MyRichLabelText>(m_richTextsMax);
            for (int i = 0; i < m_richTextsMax; i++)
            {
                m_richTextsPool.Add(new MyRichLabelText() { ShowTextShadow = this.ShowTextShadow });
            }
        }

        private void RealocateLines()
        {
            if ((m_linesCount + 1) >= m_linesCountMax)
            {
                m_linesCountMax *= 2;
                m_lines.Capacity = m_linesCountMax;
                for (int i = m_linesCount + 1; i < m_linesCountMax; i++)
                {
                    m_lines.Add(new MyRichLabelLine(m_minLineHeight));
                }
            }
        }

        private void RealocateRichTexts()
        {
            if ((m_richTextsOffset + 1) >= m_richTextsMax)
            {
                m_richTextsMax *= 2;
                m_richTextsPool.Capacity = m_richTextsMax;
                for (int i = m_richTextsOffset + 1; i < m_richTextsMax; i++)
                {
                    m_richTextsPool.Add(new MyRichLabelText() { ShowTextShadow = this.ShowTextShadow });
                }
            }
        }

        public void Append(StringBuilder text, MyFontEnum font, float scale, Vector4 color)
        {
            Append(text.ToString(), font, scale, color);
        }

        public void Append(string text, MyFontEnum font, float scale, Vector4 color)
        {
            string[] paragraphs = text.Split(m_lineSeparators, StringSplitOptions.None);

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
            if (image.GetSize().X > m_currentLineRestFreeSpace)
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
            RealocateLines();
            m_currentLine = m_lines[++m_linesCount];
            m_currentLineRestFreeSpace = m_maxLineWidth;
            RealocateRichTexts();

        }

        public void AppendLine(StringBuilder text, MyFontEnum font, float scale, Vector4 color)
        {
            Append(text, font, scale, color);
            AppendLine();
        }

        public void AppendLine(string texture, Vector2 size, Vector4 color)
        {
            Append(texture, size, color);
            AppendLine();
        }

        private void AppendParagraph(string paragraph, MyFontEnum font, float scale, Vector4 color)
        {
            m_helperSb.Clear();
            m_helperSb.Append(paragraph);
            float textWidth = MyGuiManager.MeasureString(font, m_helperSb, scale).X;
            // first we try append all paragraph to current line
            if (textWidth < m_currentLineRestFreeSpace)
            {
                RealocateRichTexts();
                m_richTextsPool[++m_richTextsOffset].Init(m_helperSb.ToString(), font, scale, color);
                AppendPart(m_richTextsPool[m_richTextsOffset]);
            }
            // if there is not enough free space in current line for whole paragraph
            else
            {
                RealocateRichTexts();
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

                    if (textWidth <= m_currentLineRestFreeSpace - m_richTextsPool[m_richTextsOffset].GetSize().X)
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
                        RealocateRichTexts();
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
            m_currentLine = m_lines[m_linesCount];
            m_currentLine.AddPart(part);
            m_currentLineRestFreeSpace = m_maxLineWidth - m_currentLine.GetSize().X;
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
            using (MyGuiManager.UsingScissorRectangle(ref scissorRect))
            {
                // Compute size of visible text for alignment.
                Vector2 textSize = Vector2.Zero;
                for (int i = 0; i <= m_linesCount; ++i)
                {
                    var lineSize = m_lines[i].GetSize();
                    textSize.X = MathHelper.Max(textSize.X, lineSize.X);
                    textSize.Y += lineSize.Y;
                }

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

                for (int i = 0; i <= m_linesCount; ++i)
                {
                    MyRichLabelLine currentLine = m_lines[i];
                    var lineSize = currentLine.GetSize();
                    var pos = position + drawPosition;
                    pos.X += horizontalAdjustment * (textSize.X - lineSize.X);
                    currentLine.Draw(pos);
                    drawPosition.Y += lineSize.Y;
                }
            }

            return true;
        }

        public Vector2 GetSize()
        {
            Vector2 size = Vector2.Zero;

            for (int i = 0; i <= m_linesCount; i++)
            {
                Vector2 lineSize = m_lines[i].GetSize();
                size.Y += lineSize.Y;
                size.X = MathHelper.Max(size.X, lineSize.X);
            }

            return size;
        }

        public void Clear()
        {
            m_lines.Clear();
            m_currentLine = null;
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

        internal bool HandleInput(Vector2 position, float offset)
        {
            position.Y -= offset;
            foreach (var line in m_lines)
            {
                if (line.HandleInput(position))
                    return true;
                position.Y += line.GetSize().Y;
            }
            return false;
        }
    }
}
