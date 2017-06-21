using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Library;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlCountdownWheel : MyGuiControlRotatingWheel
    {
        private StringBuilder m_sb;
        private int m_time;
        private int m_shownTime;

        public MyGuiControlCountdownWheel(
            Vector2? position = null,
            string texture = MyGuiConstants.LOADING_TEXTURE,
            Vector2? textureResolution = null,
            int seconds = 10,
            float radiansPerSecond = (float)(Math.PI * 2),
            float scale = 0.36f) :
            base(position: position, texture: texture, multipleSpinningWheels: false, textureResolution: textureResolution, radiansPerSecond: radiansPerSecond, scale: scale)
        {
            m_sb = new StringBuilder();
            m_sb.Append(seconds);
            m_time = MyEnvironment.TickCount + seconds * 1000 + 999;
            m_shownTime = seconds;
        }

        public override void Update() 
        {
            base.Update();

            int newShownTime = (m_time - MyEnvironment.TickCount) / 1000;
            if (newShownTime < 0) newShownTime = 0;
            if (newShownTime != m_shownTime)
            {
                m_shownTime = newShownTime;
                m_sb.Clear();
                if (m_shownTime > 0)
                {
                    m_sb.Append(m_shownTime);
                }
            }
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            Sandbox.Graphics.MyGuiManager.DrawString(
                VRage.Game.MyFontEnum.White,
                m_sb,
                base.GetPositionAbsoluteCenter(),
                1.0f,
                null,
                VRage.Utils.MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER
            );

            return;
        }
    }
}
