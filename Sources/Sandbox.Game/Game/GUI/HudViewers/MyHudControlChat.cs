﻿using Sandbox.Common;
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
    public class MyHudControlChat : MyGuiControlMultilineText
    {
        private MyHudChat m_chat;
        private int m_lastTimestamp;
        private bool m_forceUpdate;

        public MyHudControlChat(
            MyHudChat chat,
            Vector2? position = null,
            Vector2? size = null,
            Vector4? backgroundColor = null,
            MyFontEnum font = MyFontEnum.White,
            float textScale = 0.7f,
            MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
            StringBuilder contents = null,
            bool drawScrollbar = false,
            MyGuiDrawAlignEnum textBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
            bool selectable = false)
            : base (position, size, backgroundColor, font, textScale, textAlign, contents, drawScrollbar, textBoxAlign, selectable)
        {
            m_forceUpdate = true;
            m_chat = chat;
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            base.VisibleChanged += MyHudControlChat_VisibleChanged;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            UpdateText();
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }

        private void MyHudControlChat_VisibleChanged(object sender, bool isVisible)
        {
            if (isVisible == false)
            {
                m_forceUpdate = true;
            }
        }

        private void UpdateText()
        {
            if (m_forceUpdate || m_lastTimestamp != m_chat.Timestamp)
            {
                Clear();

                foreach (var message in m_chat.MessagesQueue)
                {
                    bool isMe = Sandbox.Engine.Networking.MySteam.UserName == message.Item1;

                    AppendText(new StringBuilder(message.Item1), isMe ? MyFontEnum.Blue : MyFontEnum.White, TextScale, Vector4.One);
                    AppendText(new StringBuilder(": "));
                    AppendText(new StringBuilder(message.Item2));
                    AppendLine();
                }

                m_forceUpdate = false;
                m_lastTimestamp = m_chat.Timestamp;
            }
        }
    }
}
