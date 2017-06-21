using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // XB1
using VRageMath;

namespace Sandbox.Gui
{
    public class MyWikiMarkupParser
    {
#if XB1
        //implementation without RegularExpressions
        public static void ParseText(string text, ref MyGuiControlMultilineText label)
        {
            try
            {
                var substrings = text.Split(']');
                foreach (var substring in substrings)
                {
                    var textAndMarkup = substring.Split('[');
                    if (textAndMarkup.Length == 2)
                    {
                        label.AppendText(textAndMarkup[0]);
                        var indexOfSpace = textAndMarkup[1].IndexOf(' ');
                        if (indexOfSpace != -1) 
                        {
                            label.AppendLink(textAndMarkup[1].Substring(0, indexOfSpace), textAndMarkup[1].Substring(indexOfSpace + 1));
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false);
                            label.AppendText(textAndMarkup[1]);
                        }
                    } else {
                        label.AppendText(substring);
                    }
                }
            }
            catch
            {
            }
        }
#else // !XB1
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
#endif // !XB1
    }
}
