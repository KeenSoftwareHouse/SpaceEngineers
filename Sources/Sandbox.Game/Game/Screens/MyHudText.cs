#region Using

using System.Collections.Generic;
using System.Text;
using Sandbox.Common;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudText
    {
        public class ComparerType : IComparer<MyHudText>
        {
            public int Compare(MyHudText x, MyHudText y)
            {
                return x.Font.CompareTo(y.Font);
            }
        }
        public static readonly ComparerType Comparer = new ComparerType();

        public string Font;
        public Vector2 Position;                                //  Normalized position in HUD fullscreen (height isn't 1.0)
        public Color Color;
        public float Scale;
        public MyGuiDrawAlignEnum Alignement;
        public bool Visible;
        readonly StringBuilder m_text;


        //  IMPORTANT: This class isn't initialized by constructor, but by Start() because it's supposed to be used in memory pool
        public MyHudText()
        {
            //  Must be preallocated because during game-play we will just use this string object for storing hud texts
            m_text = new StringBuilder(256);
        }

        //  IMPORTANT: This class isn't initialized by constructor, but by Start() because it's supposed to be used in memory pool
        public MyHudText Start(string font, Vector2 position, Color color, float scale, MyGuiDrawAlignEnum alignement)
        {
            Font = font;
            Position = position;
            Color = color;
            Scale = scale;
            Alignement = alignement;

            m_text.Clear();
            return this;
        }

        public void Append(StringBuilder sb)
        {
            m_text.AppendStringBuilder(sb);
        }

        public void Append(string text)
        {
            m_text.Append(text);
        }

        public void AppendInt32(int number)
        {
            m_text.AppendInt32(number);
        }

        public void AppendLine()
        {
            m_text.AppendLine();
        }

        public StringBuilder GetStringBuilder()
        {
            return m_text;
        }
    }
}
