using Sandbox.Common;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.GUI.HudViewers
{
    public class MyHudControlChat : MyGuiControlBase
    {
        private MyGuiControlMultilineText m_chatMultilineControl;

        public float TextScale
        {
            get { return m_chatMultilineControl.TextScale; }
            set { m_chatMultilineControl.TextScale = value; }
        }

        public MyHudControlChat(Vector2 position, Vector2 size)
        {
            m_chatMultilineControl = new MyGuiControlMultilineText(
                position: position,
                size: size,
                backgroundColor: null,
                font: MyFontEnum.White,
                textScale: 0.7f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                contents: null,
                drawScrollbar: false,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            //m_chatMultilineControl.BackgroundTexture = MyGuiConstants.TEXTURE_MESSAGEBOX_BACKGROUND_BLUE.Texture;
            m_chatMultilineControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;

            Elements.Add(m_chatMultilineControl);
        }

        public void UpdateChat(MyHudChat chat)
        {
            if (MyHud.Chat.Dirty)
            {
                m_chatMultilineControl.Clear();

                foreach (var message in MyHud.Chat.MessagesQueue)
                {
                    bool isMe = Sandbox.Engine.Networking.MySteam.UserName == message.Item1;

                    m_chatMultilineControl.AppendText(new StringBuilder(message.Item1), isMe ? MyFontEnum.Blue : MyFontEnum.White, m_chatMultilineControl.TextScale, Vector4.One);
                    m_chatMultilineControl.AppendText(new StringBuilder(": "));
                    m_chatMultilineControl.AppendText(new StringBuilder(message.Item2));
                    m_chatMultilineControl.AppendLine();
                }

                MyHud.Chat.Dirty = false;
            }
            //m_chatMultilineControl.BackgroundTexture
        }
    }
}
