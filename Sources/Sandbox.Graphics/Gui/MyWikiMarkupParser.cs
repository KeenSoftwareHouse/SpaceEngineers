using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VRageMath;

namespace Sandbox.Gui
{
    public class MyWikiMarkupParser
    {
        private static Regex m_splitRegex = new Regex("\\[.*?\\]{1,2}");
        private static Regex m_markupRegex = new Regex("(?<=\\[)(?!\\[).*?(?=\\])");
        private static Regex m_digitsRegex = new Regex("\\d+");
        private static StringBuilder m_stringCache = new StringBuilder();

        public static void ParseText(string text, ref MyGuiControlMultilineText label)
        {
            try
            {
                var texts = m_splitRegex.Split(text);
                var matches = m_splitRegex.Matches(text);
                for (int i = 0; i < matches.Count || i < texts.Length; i++)
                {
                    if (i < texts.Length)
                        label.AppendText(m_stringCache.Clear().Append(texts[i]));
                    if (i < matches.Count)
                        ParseMarkup(label, matches[i].Value);
                }
            }
            catch
            {
            }
        }

        private static void ParseMarkup(MyGuiControlMultilineText label, string markup)
        {
            var s = m_markupRegex.Match(markup);
            if (s.Value.Contains('|'))
            {
                var sub = s.Value.Substring(5);
                var split = sub.Split('|');
                var match = m_digitsRegex.Matches(split[1]);
                int width, height;
                if(int.TryParse(match[0].Value, out width) && int.TryParse(match[1].Value, out height))
                    label.AppendImage(split[0], MyGuiManager.GetNormalizedSizeFromScreenSize(new VRageMath.Vector2(width, height)), Vector4.One);
            }
            else
                label.AppendLink(s.Value.Substring(0, s.Value.IndexOf(' ')), s.Value.Substring(s.Value.IndexOf(' ') + 1));
        }
    }
}
