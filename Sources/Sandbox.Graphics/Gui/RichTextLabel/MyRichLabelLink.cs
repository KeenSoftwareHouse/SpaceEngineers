using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;

namespace Sandbox.Gui.RichTextLabel
{
    class MyRichLabelLink : MyRichLabelText
    {
        private Action<string> m_onClick;
        private bool m_highlight;
        private int m_lastTimeClicked;
        private const string m_linkImgTex = "Textures\\GUI\\link.dds";
        private MyRichLabelImage m_linkImg;
        private const float m_linkImgSpace = 0.008f;

        public string Url { get; set; }

        public MyRichLabelLink(string url, string text, float scale, Action<string> onClick)
        {
            Init(text, MyFontEnum.Blue, scale, Vector4.Zero);
            this.Url = url;
            this.m_onClick = onClick;
            var size = MyGuiManager.GetNormalizedSizeFromScreenSize(
                new Vector2(MyGuiManager.GetScreenSizeFromNormalizedSize(new Vector2(0.015f * scale)).X));
            m_linkImg = new MyRichLabelImage(m_linkImgTex, size/*MyGuiManager.GetNormalizedSizeFromScreenSize(new Vector2(16))*/, Vector4.One);
        }

        public override bool Draw(VRageMath.Vector2 position)
        {
            if (m_highlight)
                MyGuiManager.DrawString(MyFontEnum.White, Text, position, Scale, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            else
                MyGuiManager.DrawString(MyFontEnum.Blue, Text, position, Scale, VRageMath.Color.PowderBlue, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            m_linkImg.Draw(position + new Vector2(base.Size.X + m_linkImgSpace, 0));
            m_highlight = false;
            return true;
        }

        public override bool HandleInput(VRageMath.Vector2 position)
        {
            var mouse = MyGuiManager.MouseCursorPosition;
            if (mouse.X > position.X + 0.001f && mouse.Y > position.Y && mouse.X < position.X + Size.X && mouse.Y < position.Y + Size.Y)
            {
                m_highlight = true;
                if (MyInput.Static.IsLeftMousePressed() && MyGuiManager.TotalTimeInMilliseconds - m_lastTimeClicked > MyGuiConstants.REPEAT_PRESS_DELAY)
                {
                    m_onClick(Url);
                    m_lastTimeClicked = MyGuiManager.TotalTimeInMilliseconds;
                    return true;
                }
            }
            else
                m_highlight = false;
            return false;
        }

        public override Vector2 Size
        {
            get
            {
                var b = base.Size;
                var img = m_linkImg.Size;
                return new Vector2(b.X + m_linkImgSpace + img.X, Math.Max(b.Y, img.Y));
            }
        }
    }
}
