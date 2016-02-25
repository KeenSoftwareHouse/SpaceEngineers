using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox
{
    public enum GuiSounds
    {
        MouseClick,
        MouseOver,
        None,
        Item
    }

    public interface IMyGuiAudio
    {
        void PlaySound(GuiSounds sound);
    }

    public static class MyGuiSoundManager
    {
        public static IMyGuiAudio Audio { set { m_audio = value; } }
        private static IMyGuiAudio m_audio;

        public static void PlaySound(GuiSounds sound)
        {
            if (m_audio != null)
                m_audio.PlaySound(sound);
        }
    }
}
