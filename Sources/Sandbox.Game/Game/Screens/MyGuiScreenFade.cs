using System;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenFade : MyGuiScreenBase
    {
        private uint m_fadeInTimeMs;
        private uint m_fadeOutTimeMs;

        public event Action<MyGuiScreenFade> Shown;

        public override string GetFriendlyName()
        {
            return "Fade Screen";
        }

        public override int GetTransitionOpeningTime()
        {
            return (int)m_fadeInTimeMs;
        }

        public override int GetTransitionClosingTime()
        {
            return (int)m_fadeOutTimeMs;
        }

        public MyGuiScreenFade(Color fadeColor, uint fadeInTimeMs = 5000, uint fadeOutTimeMs = 5000) 
            : base(Vector2.Zero, fadeColor, Vector2.One * 2.5f, true)
        {
            m_fadeInTimeMs = fadeInTimeMs;
            m_fadeOutTimeMs = fadeOutTimeMs;
            m_backgroundFadeColor = fadeColor;
            EnabledBackgroundFade = true;
        }

        protected override void OnShow()
        {
            if(Shown != null)
                Shown(this);
        }
    }
}
