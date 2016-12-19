using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;

namespace System.Text
{
    public static class StringBuilderExtensions
    {
        [ThreadStatic]
        static StringBuilder m_tmp;

        /// <summary>
        /// Inserts newlines into text to make it fit size.
        /// </summary>
        public static void Autowrap(this StringBuilder sb, float width, string font, float textScale)
        {
            int inputPos = 0;
            int wordCount = 0;
            if (m_tmp == null)
                m_tmp = new StringBuilder(sb.Length);

            m_tmp.Clear();

            while (true)
            {
                int beforeWord = m_tmp.Length;
                int oldPos = inputPos;
                inputPos = AppendWord(sb, m_tmp, inputPos);
                if (inputPos == oldPos)
                    break;
                wordCount++;
                float measuredWidth = MyGuiManager.MeasureString(font, m_tmp, textScale).X;
                bool fits = measuredWidth <= width;
                if (!fits)
                {
                    if (wordCount == 1)
                    {
                        m_tmp.AppendLine();
                        inputPos = MoveSpaces(sb, inputPos);
                        wordCount = 0;
                        //width = measuredWidth;
                    }
                    else
                    {
                        m_tmp.Length = beforeWord;
                        inputPos = oldPos;
                        m_tmp.AppendLine();
                        inputPos = MoveSpaces(sb, inputPos);
                        wordCount = 0;
                        width = MyGuiManager.MeasureString(font, m_tmp, textScale).X;
                    }
                }
            }
            sb.Clear().AppendStringBuilder(m_tmp);
        }

        static int MoveSpaces(StringBuilder from, int pos)
        {
            while (pos < from.Length && from[pos] == ' ')
                pos++;
            return pos;
        }

        static int AppendWord(StringBuilder from, StringBuilder to, int wordPos)
        {
            int i = wordPos;
            bool hasValidChar = false;
            for (; i < from.Length; i++)
            {
                char c = from[i];
                bool isValid = c == ' ' || c == '\r' || c == '\n';
                if (!isValid && hasValidChar)
                    break;
                hasValidChar = hasValidChar || isValid;
                to.Append(c);
            }
            return i;
        }
    }
}
